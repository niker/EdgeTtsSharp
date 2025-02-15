namespace EdgeTtsSharp;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// This stream allows writing to a buffer while independently and concurrently reading from it and seeking.
/// </summary>
public class PipedAudioStream : Stream
{
    private readonly byte[] Buffer;
    private readonly object BufferLock = new object();
    private readonly int bufferSize;
    private int availableBytes;
    private bool IsCompleted;
    private int writePos;

    /// <inheritdoc />
    public PipedAudioStream(int size = 1024 * 1024)
    {
        this.bufferSize = size;
        this.Buffer = new byte[size];
    }

    /// <summary>
    /// Gets or sets a value indicating whether debug messages are written to the console.
    /// </summary>
    public static bool Debug { get; set; }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            lock (this.BufferLock)
            {
                return this.availableBytes;
            }
        }
    }

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Use GetReader to obtain a reader stream.");

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <summary>
    /// Writes data to the circular buffer.
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        lock (this.BufferLock)
        {
            for (var i = 0; i < count; i++)
            {
                var nextWritePos = (this.writePos + 1) % this.bufferSize;
                this.Buffer[this.writePos] = buffer[offset + i];
                this.writePos = nextWritePos;
            }

            this.availableBytes = Math.Min(this.availableBytes + count, this.bufferSize);

            // Notify waiting readers **after** all writes are completed
            Monitor.PulseAll(this.BufferLock);
        }

        if (Debug)
        {
            Console.WriteLine($"[WRITE] {count} bytes written. Buffer Size: {this.availableBytes}");
        }
    }


    /// <summary>
    /// Writes data asynchronously to the circular buffer.
    /// </summary>
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        this.Write(buffer, offset, count);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Marks the stream as completed and notifies readers.
    /// </summary>
    public void Complete()
    {
        lock (this.BufferLock)
        {
            this.IsCompleted = true;
            Monitor.PulseAll(this.BufferLock);
        }

        if (Debug)
        {
            Console.WriteLine("[COMPLETE] Writing finished.");
        }
    }

    /// <summary>
    /// Creates a new reader stream for the buffer.
    /// </summary>
    public Reader GetReader() => new Reader(this);

    /// <summary>
    /// PipedAudioStream Reader
    /// </summary>
    public class Reader : Stream
    {
        private readonly PipedAudioStream parent;
        private bool isSeeking;
        private int readPos;


        /// <inheritdoc />
        public Reader(PipedAudioStream parent)
        {
            this.parent = parent;
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length
        {
            get
            {
                lock (this.parent.BufferLock)
                {
                    return this.parent.availableBytes;
                }
            }
        }

        /// <inheritdoc />
        public override long Position
        {
            get => this.readPos;
            set
            {
                lock (this.parent.BufferLock)
                {
                    if ((value < 0) || (value > this.parent.availableBytes))
                    {
                        throw new ArgumentOutOfRangeException(nameof(this.Position), value, "Seek position is out of range.");
                    }

                    this.readPos = (int)value;
                }
            }
        }

        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            var bytesRead = 0;

            lock (this.parent.BufferLock)
            {
                while ((bytesRead < count) && !ct.IsCancellationRequested)
                {
                    if ((this.parent.availableBytes == 0) && this.parent.IsCompleted)
                    {
                        return Task.FromResult(bytesRead); // End of stream
                    }

                    // Wait for data
                    while ((this.parent.availableBytes == 0) && !this.parent.IsCompleted)
                    {
                        Monitor.Wait(this.parent.BufferLock);
                    }

                    var readableBytes = Math.Min(count - bytesRead, this.parent.availableBytes);
                    for (var i = 0; i < readableBytes; i++)
                    {
                        buffer[offset + bytesRead + i] = this.parent.Buffer[this.readPos];
                        this.readPos = (this.readPos + 1) % this.parent.bufferSize;
                    }

                    this.parent.availableBytes -= readableBytes;
                    bytesRead += readableBytes;

                    Monitor.PulseAll(this.parent.BufferLock); // Notify writers that space is freed
                }
            }

            return Task.FromResult(bytesRead);
        }


        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (this.parent.BufferLock)
            {
                if (this.isSeeking)
                {
                    throw new InvalidOperationException("Seeking is already in progress.");
                }

                this.isSeeking = true;

                var newPosition = origin switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => this.readPos + offset,
                    SeekOrigin.End => this.parent.availableBytes + offset,
                    _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
                };

                if ((newPosition < 0) || (newPosition > this.parent.availableBytes))
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), offset, "Seek position is out of range.");
                }

                this.readPos = (int)(newPosition % this.parent.bufferSize);
                this.isSeeking = false;

                Monitor.PulseAll(this.parent.BufferLock);
                return this.readPos;
            }
        }

        /// <inheritdoc />
        public override void SetLength(long value) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
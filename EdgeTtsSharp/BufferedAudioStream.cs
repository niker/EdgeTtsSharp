namespace EdgeTtsSharp;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EliteCore.Utility;

/// <summary>
/// This is a simplified prototype that may replace PipedAudioStream if the benchmarks are positive.
/// </summary>
public class BufferedAudioStream : Stream
{
    private readonly MemoryStream Content = new();
    private readonly AsyncLock WriteLock = new();
    private long ReadPos;
    private long WritePos;

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            return this.WriteLock.LockSync(() =>
            {
                if (this.WriteCompleted)
                {
                    return this.Content.Length;
                }

                return this.WritePos;
            });
        }
    }

    /// <summary>
    /// Gets a value indicating whether the write operation is completed.
    /// </summary>
    public bool WriteCompleted { get; private set; }


    /// <inheritdoc />
    public override long Position
    {
        get => this.ReadPos;
        set => this.Seek(value, SeekOrigin.Begin);
    }

    /// <inheritdoc />
    public override void Close()
    {
        this.WriteLock.LockSync(() =>
        {
            this.Content.Flush();
            this.WriteCompleted = true;
        });
    }

    /// <inheritdoc />
    public override void Flush()
    {
        this.WriteLock.LockSync(() =>
        {
            this.Content.Position = this.WritePos;
            this.Content.Flush();
        });
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        while ((this.Length == this.ReadPos) && !this.WriteCompleted)
        {
            Task.Delay(50).GetAwaiter().GetResult();
        }

        return this.WriteLock.LockSync(() =>
        {
            this.Content.Position = this.ReadPos;
            var read = this.Content.Read(buffer, offset, count);
            this.ReadPos = this.Content.Position;
            return read;
        });
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        while ((this.Length == this.ReadPos) && !this.WriteCompleted)
        {
            await Task.Delay(100, cancellationToken);
        }

        return await this.WriteLock.Lock(async () =>
        {
            this.Content.Position = this.ReadPos;
#if NETSTANDARD2_0
            var read = await this.Content.ReadAsync(buffer, offset, count, cancellationToken);
#else
            var read = await this.Content.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
#endif
            this.ReadPos = this.Content.Position;
            return read;
        });
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        return this.WriteLock.LockSync(() =>
        {
            this.ReadPos = this.Content.Seek(offset, origin);
            return this.ReadPos;
        });
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        this.WriteLock.LockSync(() =>
        {
            if (this.WriteCompleted)
            {
                throw new InvalidOperationException("Write operation is completed.");
            }

            this.Content.Position = this.WritePos;
            this.Content.Write(buffer, offset, count);
            this.WritePos = this.Content.Position;
        });
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await this.WriteLock.Lock(async () =>
        {
            if (this.WriteCompleted)
            {
                throw new InvalidOperationException("Write operation is completed.");
            }

            this.Content.Position = this.WritePos;
#if NETSTANDARD2_0
            await this.Content.WriteAsync(buffer, offset, count, cancellationToken);
#else
            await this.Content.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
#endif
            this.WritePos = this.Content.Position;
        });
    }

#if NETSTANDARD2_0
    /// <summary>
    /// Dispose asynchronously.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        this.Content.Dispose();
        this.WriteCompleted = true;
        return new ValueTask(Task.CompletedTask);
    }
#else
    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await this.Content.DisposeAsync();
        this.WriteCompleted = true;
        await base.DisposeAsync();
    }
#endif

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        this.Content.Dispose();
        this.WriteCompleted = true;
        base.Dispose(disposing);
    }
}
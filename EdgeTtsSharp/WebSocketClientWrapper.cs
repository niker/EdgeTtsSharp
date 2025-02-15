namespace EdgeTtsSharp
{
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class WebSocketClientWrapper
    {
        private readonly CancellationToken CancelToken;
        private readonly Func<ValueTask> OnClose;
        private readonly Func<Exception, ValueTask>? OnError;
        private readonly Func<WebSocketClientWrapper, byte[], ValueTask> OnMessage;
        private readonly Uri Uri;
        private readonly ClientWebSocket WebSocket;

        public WebSocketClientWrapper(string url,
                                      Func<WebSocketClientWrapper, byte[], ValueTask> onMessage,
                                      Func<ValueTask> onClose,
                                      Func<Exception, ValueTask>? onError = null,
                                      CancellationToken ct = default)
        {
            this.Uri = new Uri(url);
            this.WebSocket = new ClientWebSocket
            {
                Options =
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                }
            };

            this.OnMessage = onMessage;
            this.OnClose = onClose;
            this.OnError = onError;
            this.CancelToken = ct;
        }


        public async ValueTask Connect()
        {
            try
            {
                await this.WebSocket.ConnectAsync(this.Uri, this.CancelToken);

                // Start receiving messages
                this.ReceiveMessages(this.CancelToken).GetAwaiter();

                if (this.WebSocket.State != WebSocketState.Open)
                {
                    throw new Exception("Failed to connect to the server.");
                }
            }
            catch (Exception e)
            {
                this.OnError?.Invoke(e);
                throw;
            }
        }

        public async ValueTask Close()
        {
            await this.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", this.CancelToken);
            await this.OnClose();
        }

        public async ValueTask Send(string msg)
        {
            var messageBuffer = Encoding.UTF8.GetBytes(msg);
            await this.WebSocket.SendAsync(messageBuffer.AsMemory(), WebSocketMessageType.Text, true, this.CancelToken);
        }

        private async ValueTask ReceiveMessages(CancellationToken ct)
        {
            try
            {
                var buffer = new byte[1024 * 4];
                while ((this.WebSocket.State == WebSocketState.Open) && !ct.IsCancellationRequested)
                {
                    var result = await this.WebSocket.ReceiveAsync(buffer.AsMemory(), this.CancelToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await this.Close();
                    }
                    else
                    {
                        var rawData = buffer.AsMemory()[..result.Count].ToArray();
                        await this.OnMessage(this, rawData);
                    }
                }
            }
            catch (Exception e)
            {
                if (this.OnError != null)
                {
                    await this.OnError(e);
                }
            }
        }
    }
}
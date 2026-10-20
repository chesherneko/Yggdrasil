using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueMarble.Server
{
    public class RoomConnection
    {
        private readonly ClientWebSocket socket = new();
        private readonly SemaphoreSlim sendLock = new(1, 1);

        public UniTask ConnectAsync(Uri uri, CancellationToken token) 
            => socket.ConnectAsync(uri, token).AsUniTask();

        public async UniTask SendAsync(string text, CancellationToken token)
        {
            await sendLock.WaitAsync(token);

            try
            {
                if (socket.State != WebSocketState.Open) return;
                
                byte[] bytes = Encoding.UTF8.GetBytes(text);

                await socket.SendAsync(
                    new ArraySegment<byte>(bytes), 
                    WebSocketMessageType.Text, 
                    true, 
                    token).AsUniTask();
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            finally
            {
                sendLock.Release();
            }
        }

        public async UniTask ReceiveLoopAsync(Action<string> onReceived, CancellationToken token)
        {
            StringBuilder stringBuilder = new();
            byte[] buffer = new byte[4096];

            while (true)
            {
                if (socket.State != WebSocketState.Open) break;
                
                WebSocketReceiveResult result;

                try
                {
                    result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        token).AsUniTask();
                }
                catch (OperationCanceledException) { return; }
                catch (WebSocketException)
                {
                    Debug.LogWarning("[RoomConnection] RoomConnection socket closed unexpectedly");
                    return;
                }
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (socket.State == WebSocketState.CloseReceived)
                    {
                        try
                        {
                            await socket.CloseOutputAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "bye",
                                token).AsUniTask();
                        }
                        catch (WebSocketException) { }
                    }

                    return;
                }
                
                stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (!result.EndOfMessage) continue;
                
                onReceived(stringBuilder.ToString());
                stringBuilder.Clear();
            }
        }

        public async UniTask CloseAsync(CancellationToken token)
        {
            await sendLock.WaitAsync(token);

            try
            {
                if (socket.State != WebSocketState.Open) return;

                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "bye",
                    token).AsUniTask();
            }
            catch (WebSocketException) { }
            finally
            {
                socket.Abort();
                sendLock.Release();
            }
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}

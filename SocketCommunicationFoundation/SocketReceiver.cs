using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;
namespace SocketCommunicationFoundation
{
    public sealed class SocketReceiver : SocketSenderReceiverBase
    {
        public SocketReceiver(Socket socket, PipeScheduler scheduler, Action<object> userCallback) : base(socket, scheduler, userCallback)
        {
        }

        public SocketAwaitableEventArgs ReceiveAsync(Memory<byte> buffer)
        {
            var segment = buffer.GetArray();
            
            _awaitableEventArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            //Console.WriteLine($"::{SocketSenderReceiverBase.RecvQty++}::{segment.Offset}::{segment.Count}");
            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            // 用户回调函数
            //_scheduler.UserCallback(_receiver.Args);

            return _awaitableEventArgs;
        }
    }
}

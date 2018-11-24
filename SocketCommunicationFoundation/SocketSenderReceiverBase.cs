using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;

namespace SocketCommunicationFoundation
{
    public abstract class SocketSenderReceiverBase : IDisposable
    {
        protected readonly Socket _socket;
        protected readonly SocketAwaitableEventArgs _awaitableEventArgs;
        protected Action<object> _userCallback;
        public Socket InnerSocket { get => _socket; }
        public SocketAwaitableEventArgs Args { get => _awaitableEventArgs; }
        public static int RecvQty = 0;
        protected SocketSenderReceiverBase(Socket socket, PipeScheduler scheduler, Action<object> userCallback)
        {
            _socket = socket;
            _userCallback = userCallback;
            _awaitableEventArgs = new SocketAwaitableEventArgs(scheduler, userCallback, _socket);
        }

        public void Dispose() => _awaitableEventArgs.Dispose();
    }
}

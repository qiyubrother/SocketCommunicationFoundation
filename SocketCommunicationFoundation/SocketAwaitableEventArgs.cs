using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SocketCommunicationFoundation
{
    public class SocketAwaitableEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        private static readonly Action _callbackCompleted = () => { };

        private readonly PipeScheduler _ioScheduler;

        private Action _callback;
        private Action<object> _userCallback;
        private Socket _socket;

        public SocketAwaitableEventArgs(PipeScheduler ioScheduler, Action<object> userCallback, Socket socket)
        {
            _ioScheduler = ioScheduler;
            _userCallback = userCallback;
            _socket = socket;
        }

        public Socket InnerSocket { get => _socket; }

        public SocketAwaitableEventArgs GetAwaiter() => this;
        public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

        public int GetResult()
        {
            Debug.Assert(ReferenceEquals(_callback, _callbackCompleted));

            _callback = null;

            if (SocketError != SocketError.Success)
            {
                ThrowSocketException(SocketError);
            }

            return BytesTransferred;

            void ThrowSocketException(SocketError e)
            {
                throw new SocketException((int)e);
            }
        }

        public void OnCompleted(Action continuation)
        {
            if (ReferenceEquals(_callback, _callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
            {
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Complete()
        {
            OnCompleted(this);
        }
        protected override void OnCompleted(SocketAsyncEventArgs _)
        {
            var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);

            if (continuation != null)
            {
                _ioScheduler.Schedule(state => ((Action)state)(), continuation);

                //Action<object> a = (state) =>
                //{
                //    var param = state as SocketAsyncEventArgs;
                //    Console.WriteLine($"BytesTransferred=>{_.BytesTransferred}, Buffer=>{System.Text.Encoding.Default.GetString(param.Buffer, 0, param.BytesTransferred)}");
                //};
                //if (_userCallback != null)
                //{
                //    _ioScheduler.Schedule(_userCallback, _);
                //}
            }
        }
    }
}

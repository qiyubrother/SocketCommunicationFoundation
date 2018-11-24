using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SocketCommunicationFoundation
{
    public sealed class SocketSender : SocketSenderReceiverBase
    {
        private List<ArraySegment<byte>> _bufferList;

        public SocketSender(Socket socket, PipeScheduler scheduler, Action<object> userCallback) : base(socket, scheduler, userCallback)
        {
        }

        public SocketAwaitableEventArgs SendAsync(ReadOnlySequence<byte> buffers)
        {
            if (buffers.IsSingleSegment)
            {
                return SendAsync(buffers.First);
            }

            if (_awaitableEventArgs.Buffer != null)
            {
                _awaitableEventArgs.SetBuffer(null, 0, 0);
            }

            _awaitableEventArgs.BufferList = GetBufferList(buffers);
            //
            // 摘要:
            //     Sends data asynchronously to a connected System.Net.Sockets.Socket object.
            //
            // 参数:
            //   e:
            //     The System.Net.Sockets.SocketAsyncEventArgs object to use for this asynchronous
            //     socket operation.
            //
            // 返回结果:
            //     Returns true if the I/O operation is pending. The System.Net.Sockets.SocketAsyncEventArgs.Completed
            //     event on the e parameter will be raised upon completion of the operation. Returns
            //     false if the I/O operation completed synchronously. In this case, The System.Net.Sockets.SocketAsyncEventArgs.Completed
            //     event on the e parameter will not be raised and the e object passed as a parameter
            //     may be examined immediately after the method call returns to retrieve the result
            //     of the operation.
            //
            // 异常:
            //   T:System.ArgumentException:
            //     The System.Net.Sockets.SocketAsyncEventArgs.Buffer or System.Net.Sockets.SocketAsyncEventArgs.BufferList
            //     properties on the e parameter must reference valid buffers. One or the other
            //     of these properties may be set, but not both at the same time.
            //
            //   T:System.InvalidOperationException:
            //     A socket operation was already in progress using the System.Net.Sockets.SocketAsyncEventArgs
            //     object specified in the e parameter.
            //
            //   T:System.NotSupportedException:
            //     Windows XP or later is required for this method.
            //
            //   T:System.ObjectDisposedException:
            //     The System.Net.Sockets.Socket has been closed.
            //
            //   T:System.Net.Sockets.SocketException:
            //     The System.Net.Sockets.Socket is not yet connected or was not obtained via an
            //     System.Net.Sockets.Socket.Accept, System.Net.Sockets.Socket.AcceptAsync(System.Net.Sockets.SocketAsyncEventArgs),or
            //     Overload:System.Net.Sockets.Socket.BeginAccept, method.
            if (!_socket.SendAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }

        private SocketAwaitableEventArgs SendAsync(ReadOnlyMemory<byte> memory)
        {
            // The BufferList getter is much less expensive then the setter.
            if (_awaitableEventArgs.BufferList != null)
            {
                _awaitableEventArgs.BufferList = null;
            }

            var segment = memory.GetArray();

            _awaitableEventArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            if (!_socket.SendAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }

        private List<ArraySegment<byte>> GetBufferList(ReadOnlySequence<byte> buffer)
        {
            Debug.Assert(!buffer.IsEmpty);
            Debug.Assert(!buffer.IsSingleSegment);

            if (_bufferList == null)
            {
                _bufferList = new List<ArraySegment<byte>>();
            }
            else
            {
                // Buffers are pooled, so it's OK to root them until the next multi-buffer write.
                _bufferList.Clear();
            }

            foreach (var b in buffer)
            {
                _bufferList.Add(b.GetArray());
            }

            return _bufferList;
        }
    }
}

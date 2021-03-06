﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SocketCommunicationFoundation
{
    
    internal sealed class SocketConnection : IDisposable
    {
        private static readonly int MinAllocBufferSize = 1024;
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private readonly Socket _socket;
        private readonly IOQueue _scheduler;
        private readonly ISocketsTrace _trace;
        private readonly SocketReceiver _receiver;
        private readonly SocketSender _sender;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();
        private readonly object _shutdownLock = new object();
        private volatile bool _aborted;
        private volatile ConnectionAbortedException _abortReason;
        private long _totalBytesWritten;
        private Pipe _pipe = new Pipe();
        private Dictionary<object, object> _session = new Dictionary<object, object>();

        public string ConnectionId = string.Empty;
        public int LocalPort { get; set; }
        public IPAddress LocalAddress { get; set; }
        public int RemotePort { get; set; }
        public IPAddress RemoteAddress { get; set; }
        public PipeWriter Input { get => _pipe.Writer; }
        public PipeReader Output { get => _pipe.Reader; }
        public Action<object> ReceivedDataCallBack { get; set; }
        public DateTime ConnectedTime { get; set; }
        public DateTime LastReceivedTime { get; set; }
        public Socket Socket { get => _socket; }
        internal SocketConnection(Socket socket, IOQueue scheduler, ISocketsTrace trace)
        {
            Debug.Assert(socket != null);
            Debug.Assert(trace != null);

            _socket = socket;
            _scheduler = scheduler;
            _trace = trace;
            
            var localEndPoint = (IPEndPoint)_socket.LocalEndPoint;
            var remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;

            LocalAddress = localEndPoint.Address;
            LocalPort = localEndPoint.Port;

            RemoteAddress = remoteEndPoint.Address;
            RemotePort = remoteEndPoint.Port;

            var awaiterScheduler = IsWindows ? _scheduler : PipeScheduler.Inline;

            _receiver = new SocketReceiver(_socket, awaiterScheduler, _scheduler.UserCallback);
            _sender = new SocketSender(_socket, awaiterScheduler, _scheduler.UserCallback);

            ConnectedTime = DateTime.Now;
        }

        public MemoryPool<byte> MemoryPool { get; }
        public long TotalBytesWritten => Interlocked.Read(ref _totalBytesWritten);

        public async Task StartAsync()
        {
            try
            {
                // Spawn send and receive logic
                var receiveTask = DoReceive();
                var sendTask = DoSend();

                // Now wait for both to complete
                await receiveTask;
                await sendTask;

                _receiver.Dispose();
                _sender.Dispose();
                ThreadPool.QueueUserWorkItem(state => ((SocketConnection)state).CancelConnectionClosedToken(), this);
            }
            catch (Exception ex)
            {
                _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(StartAsync)}.");
            }
        }

        public void Abort(ConnectionAbortedException abortReason)
        {
            _abortReason = abortReason;
            Output.CancelPendingRead();

            // Try to gracefully close the socket to match libuv behavior.
            Shutdown();
        }

        // Only called after connection middleware is complete which means the ConnectionClosed token has fired.
        public void Dispose()
        {
            _connectionClosedTokenSource.Dispose();
            _session.Clear();
        }

        private async Task DoReceive()
        {
            Exception error = null;

            try
            {
                await ProcessReceives();
            }
            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                // A connection reset can be reported as SocketError.ConnectionAborted on Windows
                if (!_aborted)
                {
                    error = new ConnectionResetException(ex.Message, ex);
                    _trace.ConnectionReset(ConnectionId);
                }
            }
            catch (SocketException ex) when (IsConnectionAbortError(ex.SocketErrorCode))
            {
                if (!_aborted)
                {
                    // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
                    _trace.ConnectionError(ConnectionId, error);
                }
            }
            catch (ObjectDisposedException)
            {
                if (!_aborted)
                {
                    _trace.ConnectionError(ConnectionId, error);
                }
            }
            catch (IOException ex)
            {
                error = ex;
                _trace.ConnectionError(ConnectionId, error);
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
                _trace.ConnectionError(ConnectionId, error);
            }
            finally
            {
                if (_aborted)
                {
                    error = error ?? _abortReason ?? new ConnectionAbortedException();
                }

                Input.Complete(error);
            }
        }

        private async Task ProcessReceives()
        {
            while (true)
            {
                // Ensure we have some reasonable amount of buffer space
                var buffer = Input.GetMemory(MinAllocBufferSize);

                var bytesReceived = await _receiver.ReceiveAsync(buffer);

                if (bytesReceived > 0)
                {
                    LastReceivedTime = DateTime.Now;
                    ReceivedDataCallBack(new SocketCallbackEventArgs(buffer.Slice(0, bytesReceived).ToArray(), _receiver.InnerSocket, _session));
                }

                if (bytesReceived == 0)
                {
                    // FIN
                    _trace.ConnectionReadFin(ConnectionId);
                    break;
                }

                Input.Advance(bytesReceived);

                var flushTask = Input.FlushAsync();

                if (!flushTask.IsCompleted)
                {
                    _trace.ConnectionPause(ConnectionId);

                    await flushTask;

                    _trace.ConnectionResume(ConnectionId);
                }

                var result = flushTask.GetAwaiter().GetResult();
                if (result.IsCompleted)
                {
                    // Pipe consumer is shut down, do we stop writing

                    break;
                }
            }
        }

        private async Task DoSend()
        {
            Exception error = null;

            try
            {
                await ProcessSends();
            }
            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                // A connection reset can be reported as SocketError.ConnectionAborted on Windows
                error = null;
                _trace.ConnectionReset(ConnectionId);
            }
            catch (SocketException ex) when (IsConnectionAbortError(ex.SocketErrorCode))
            {
                error = null;
            }
            catch (ObjectDisposedException)
            {
                error = null;
            }
            catch (IOException ex)
            {
                error = ex;
                _trace.ConnectionError(ConnectionId, error);
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
                _trace.ConnectionError(ConnectionId, error);
            }
            finally
            {
                Shutdown();

                // Complete the output after disposing the socket
                Output.Complete(error);
            }
        }

        private async Task ProcessSends()
        {
            while (true)
            {
                var result = await Output.ReadAsync();

                if (result.IsCanceled)
                {
                    break;
                }

                var buffer = result.Buffer;

                var end = buffer.End;
                var isCompleted = result.IsCompleted;
                if (!buffer.IsEmpty)
                {
                    // 挂起发送原文
                    //await _sender.SendAsync(buffer);
                }
                
                // This is not interlocked because there could be a concurrent writer.
                // Instead it's to prevent read tearing on 32-bit systems.
                Interlocked.Add(ref _totalBytesWritten, buffer.Length);

                Output.AdvanceTo(end);


                if (isCompleted)
                {
                    break;
                }
            }
        }

        private void Shutdown()
        {
            lock (_shutdownLock)
            {
                if (!_aborted)
                {
                    // Make sure to close the connection only after the _aborted flag is set.
                    // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
                    // a BadHttpRequestException is thrown instead of a TaskCanceledException.
                    _aborted = true;
                    _trace.ConnectionWriteFin(ConnectionId);

                    try
                    {
                        // Try to gracefully close the socket even for aborts to match libuv behavior.
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                        // Ignore any errors from Socket.Shutdown since we're tearing down the connection anyway.
                    }

                    _socket.Dispose();
                }
            }
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _connectionClosedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(CancelConnectionClosedToken)}.");
            }
        }

        private static bool IsConnectionResetError(SocketError errorCode)
        {
            return errorCode == SocketError.ConnectionReset ||
                   errorCode == SocketError.ConnectionAborted ||
                   errorCode == SocketError.Shutdown;
        }

        private static bool IsConnectionAbortError(SocketError errorCode)
        {
            return errorCode == SocketError.OperationAborted ||
                   errorCode == SocketError.Interrupted ||
                   errorCode == SocketError.InvalidArgument;
        }
    }
}

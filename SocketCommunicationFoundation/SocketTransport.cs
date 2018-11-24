using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading;

namespace SocketCommunicationFoundation
{
    public sealed class SocketTransport
    {
        private static readonly PipeScheduler[] ThreadPoolSchedulerArray = new PipeScheduler[] { PipeScheduler.ThreadPool };

        private IPEndPoint _endPointInformation;

        Action<object> _receivedDataCallBack;
        private readonly ISocketsTrace _trace;
        private Socket _listenSocket;
        private Task _listenTask;
        private Exception _listenException;
        private volatile bool _unbinding;
        private double _maxLinkIdleSecond;
        private double _maxDataIdleSecond;
        private ConcurrentBag<SocketConnection> _concurrentBag = new ConcurrentBag<SocketConnection>();

        public SocketTransport(
            IPEndPoint endPointInformation,
            int ioQueueCount,
            ISocketsTrace trace,
            Action<object> receivedDataCallBack,
            double maxLinkIdleSecond,
            double maxDataIdleSecond
            )
        {
            Debug.Assert(endPointInformation != null);
            Debug.Assert(trace != null);
            Debug.Assert(maxLinkIdleSecond >= 0);

            _endPointInformation = endPointInformation;
            _trace = trace;
            _maxLinkIdleSecond = maxLinkIdleSecond;
            _maxDataIdleSecond = maxDataIdleSecond;
            _receivedDataCallBack = receivedDataCallBack;
        }

        public Task BindAsync()
        {
            if (_listenSocket != null)
            {
                throw new InvalidOperationException("Transport is already bound.");
            }

            IPEndPoint endPoint = _endPointInformation;

            var listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            EnableRebinding(listenSocket);

            // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
            if (endPoint.Address == IPAddress.IPv6Any)
            {
                listenSocket.DualMode = true;
            }

            try
            {
                listenSocket.Bind(endPoint);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                throw new AddressInUseException(e.Message, e);
            }

            // If requested port was "0", replace with assigned dynamic port.
            if (_endPointInformation.Port == 0)
            {
                _endPointInformation = (IPEndPoint)listenSocket.LocalEndPoint;
            }

            listenSocket.Listen(512);

            _listenSocket = listenSocket;

            _listenTask = Task.Run(() => RunAcceptLoopAsync());

            Task.Run(() => SocketConnectionInspector()); // 启动SocketConnection检查程序

            return Task.CompletedTask;
        }

        public async Task UnbindAsync()
        {
            if (_listenSocket != null)
            {
                _unbinding = true;
                _listenSocket.Dispose();

                Debug.Assert(_listenTask != null);
                await _listenTask.ConfigureAwait(false);

                _unbinding = false;
                _listenSocket = null;
                _listenTask = null;

                if (_listenException != null)
                {
                    var exInfo = ExceptionDispatchInfo.Capture(_listenException);
                    _listenException = null;
                    exInfo.Throw();
                }
            }
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        private async Task RunAcceptLoopAsync()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        var acceptSocket = await _listenSocket.AcceptAsync();
                        acceptSocket.NoDelay = false;

                        var connection = new SocketConnection(acceptSocket, new IOQueue() { UserCallback = _receivedDataCallBack }, _trace) { ReceivedDataCallBack = _receivedDataCallBack };

                        if (_maxLinkIdleSecond > 0 || _maxDataIdleSecond > 0)
                        {
                            _concurrentBag.Add(connection); // 添加SocketConnection到监控队列
                        }
                        
                        // REVIEW: This task should be tracked by the server for graceful shutdown
                        // Today it's handled specifically for http but not for arbitrary middleware
                        _ = HandleConnectionAsync(connection);
                    }
                    catch (SocketException) when (!_unbinding)
                    {
                        _trace.ConnectionReset(connectionId: "(null)");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_unbinding)
                {
                    // Means we must be unbinding. Eat the exception.
                }
                else
                {
                    _trace.LogCritical(ex, $"Unexpected exception in {nameof(SocketTransport)}.{nameof(RunAcceptLoopAsync)}.");
                    _listenException = ex;

                    // Request shutdown so we can rethrow this exception
                    // in Stop which should be observable.
                    //_appLifetime.StopApplication();
                }
            }
        }
        public Task SocketConnectionInspector()
        {
            if (_maxLinkIdleSecond != 0 || _maxDataIdleSecond != 0)
            {
                while (true)
                {
                    SocketConnection item = null;
                    if (_concurrentBag.Count == 0)
                    {
                        Thread.Sleep(500); // 没有任何连接。
                        continue;
                    }
                    if (_concurrentBag.TryPeek(out item)) // 从队列头取出一个SocketConnection
                    {
                        using (item)
                        {
                            if (item != null)
                            {
                                if (item.LastReceivedTime.Ticks == 0)
                                {
                                    if (_maxLinkIdleSecond > 0)
                                    {
                                        var ts = DateTime.Now - item.ConnectedTime;
                                        if (ts.TotalSeconds >= _maxLinkIdleSecond)
                                        {
                                            // 如果长时间没有收到报文，服务端主动断开Socket连接。
                                            try
                                            {
                                                item.Socket.Shutdown(SocketShutdown.Both);
                                                item.Socket.Close();
                                                item.Dispose();
                                                _concurrentBag.TryTake(out _);
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e.Message);
                                            }
                                        }
                                        //else
                                        //{
                                        //     // 没有超时并且状态正常，重新添加到连接队列。
                                        //}
                                    }
                                }
                                else
                                {
                                    if (_maxDataIdleSecond > 0)
                                    {
                                        var ts = DateTime.Now - item.LastReceivedTime;
                                        if (ts.TotalSeconds >= _maxDataIdleSecond)
                                        {
                                            // 如果长时间没有收到报文，服务端主动断开Socket连接。
                                            try
                                            {
                                                item.Socket.Shutdown(SocketShutdown.Both);
                                                item.Socket.Close();
                                                item.Dispose();
                                                _concurrentBag.TryTake(out _);
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine(e.Message);
                                            }
                                        }
                                        //else
                                        //{
                                        //    _concurrentBag.Add(item); // 没有超时并且状态正常，重新添加到连接队列。
                                        //}
                                    }
                                }
                                
                                Thread.Sleep(50); // 释放CPU资源50毫秒
                            }
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
        private async Task HandleConnectionAsync(SocketConnection connection)
        {
            try
            {
                //var middlewareTask = _dispatcher.OnConnection(connection);

                var transportTask = connection.StartAsync();

                await transportTask;
                //await middlewareTask;
                connection.Dispose();
            }
            catch (Exception ex)
            {
                _trace.LogCritical(ex, $"Unexpected exception in {nameof(SocketTransport)}.{nameof(HandleConnectionAsync)}.");
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int setsockopt(int socket, int level, int option_name, IntPtr option_value, uint option_len);

        private const int SOL_SOCKET_OSX = 0xffff;
        private const int SO_REUSEADDR_OSX = 0x0004;
        private const int SOL_SOCKET_LINUX = 0x0001;
        private const int SO_REUSEADDR_LINUX = 0x0002;

        // Without setting SO_REUSEADDR on macOS and Linux, binding to a recently used endpoint can fail.
        // https://github.com/dotnet/corefx/issues/24562
        private unsafe void EnableRebinding(Socket listenSocket)
        {
            var optionValue = 1;
            var setsockoptStatus = 0;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                setsockoptStatus = setsockopt(listenSocket.Handle.ToInt32(), SOL_SOCKET_LINUX, SO_REUSEADDR_LINUX,
                                              (IntPtr)(&optionValue), sizeof(int));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                setsockoptStatus = setsockopt(listenSocket.Handle.ToInt32(), SOL_SOCKET_OSX, SO_REUSEADDR_OSX,
                                              (IntPtr)(&optionValue), sizeof(int));
            }

            if (setsockoptStatus != 0)
            {
                _trace.LogInformation("Setting SO_REUSEADDR failed with errno '{errno}'.", Marshal.GetLastWin32Error());
            }
        }
    }
}

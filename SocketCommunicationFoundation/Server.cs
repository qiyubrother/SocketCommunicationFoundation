using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketCommunicationFoundation;
using System.Net;
using Microsoft.Extensions.Logging;
using System.IO.Pipelines;
using Microsoft.Extensions.Options;

namespace SocketCommunicationFoundation
{
    public class Server
    {
        public static void Startup(
            LoggerFactory loggerFactory,
            IPEndPoint iPEndPoint,
            Action<object> receivedDataCallBack,
            int queueCount = 512
            )
        {
            var opt = Options.Create<SocketTransportOptions>(new SocketTransportOptions { IOQueueCount = queueCount });
            if (loggerFactory == null)
            {
                loggerFactory = new LoggerFactory();
            }
            new SocketTransportFactory(opt, loggerFactory)
                .Create(iPEndPoint, receivedDataCallBack)
                .BindAsync();
        }

        public static void Startup(
            IPEndPoint iPEndPoint,
            Action<object> receivedDataCallBack,
            int queueCount = 512, 
            int maxLinkIdleSecond = 0,
            int maxDataIdleSecond = 0
            )
        {
            var opt = Options.Create<SocketTransportOptions>(new SocketTransportOptions { IOQueueCount = queueCount, MaxLinkIdleSecond = maxLinkIdleSecond, MaxDataIdleSecond = maxDataIdleSecond });
            var loggerFactory = new LoggerFactory();
            new SocketTransportFactory(opt, loggerFactory)
                .Create(iPEndPoint, receivedDataCallBack)
                .BindAsync();
        }
    }
}

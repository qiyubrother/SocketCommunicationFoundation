using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.IO.Pipelines;

namespace SocketCommunicationFoundation
{
#pragma warning disable PUB0001 // Pubternal type in public API
    public sealed class SocketTransportFactory
#pragma warning restore PUB0001 // Pubternal type in public API
    {
        private readonly SocketTransportOptions _options;
        private readonly SocketsTrace _trace;

        public SocketTransportFactory(
            IOptions<SocketTransportOptions> options,
            ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = options.Value;
            var logger  = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
            _trace = new SocketsTrace(logger);
        }

#pragma warning disable PUB0001 // Pubternal type in public API
        public SocketTransport Create(IPEndPoint endPointInformation,
            Action<object> receivedDataCallBack
            )
#pragma warning restore PUB0001 // Pubternal type in public API
        {
            if (endPointInformation == null)
            {
                throw new ArgumentNullException(nameof(endPointInformation));
            }
            return new SocketTransport(endPointInformation, _options.IOQueueCount, _trace, receivedDataCallBack, _options.MaxLinkIdleSecond, _options.MaxDataIdleSecond);
        }
    }
}

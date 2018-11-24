using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
namespace SocketCommunicationFoundation
{
    public interface ISocketsTrace : ILogger
    {
        void ConnectionReadFin(string connectionId);

        void ConnectionWriteFin(string connectionId);

        void ConnectionError(string connectionId, Exception ex);

        void ConnectionReset(string connectionId);

        void ConnectionPause(string connectionId);

        void ConnectionResume(string connectionId);
    }
}

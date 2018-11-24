using System;
using System.Collections.Generic;
using System.Text;

namespace SocketCommunicationFoundation
{
    public class ConnectionAbortedException : OperationCanceledException
    {
        public ConnectionAbortedException() { }
        public ConnectionAbortedException(string message) { }
        public ConnectionAbortedException(string message, Exception inner) { }
    }
}

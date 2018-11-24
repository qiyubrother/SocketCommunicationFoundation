using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace SocketCommunicationFoundation
{
    public class ConnectionResetException : IOException
    {
        public ConnectionResetException(string message) { }
        public ConnectionResetException(string message, Exception inner) { }
    }
}

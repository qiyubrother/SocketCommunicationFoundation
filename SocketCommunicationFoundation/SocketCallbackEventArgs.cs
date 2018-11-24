using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace SocketCommunicationFoundation
{
    public class SocketCallbackEventArgs : EventArgs
    {
        protected Socket _socket;
        protected byte[] _buffer;
        protected Dictionary<object, object> _session = null;
        public SocketCallbackEventArgs(byte[] buffer, Socket socket, Dictionary<object, object> session)
        {
            _buffer = buffer;
            _socket = socket;
            _session = session;
        }

        public Socket Socket { get => _socket; }

        public byte[] Buffer { get => _buffer; }
        public Dictionary<object, object> Session { get => _session; }
    }
}

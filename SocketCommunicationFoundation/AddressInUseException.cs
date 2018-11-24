using System;
using System.Collections.Generic;
using System.Text;

namespace SocketCommunicationFoundation
{
    public class AddressInUseException : InvalidOperationException
    {
        public AddressInUseException(string message) { }
        public AddressInUseException(string message, Exception inner) { }
    }
}

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketCommunicationFoundation
{
    public interface ITransportDispatcher
    {
        //Task OnReceivedData(SocketReceiver receiver, Memory<byte> buffer, int bytesReceived);
        //Task OnSendData(SocketSender sender, ValueTask<ReadResult> result);


    }
}

using System;
using System.Net;
using System.Text;
using SocketCommunicationFoundation;

namespace TestCase02Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Action<object> receivedDataCallBack = (o) =>
            {
                var param = o as SocketCallbackEventArgs;
                {
                    var data = Encoding.Default.GetString(param.Buffer);
                    Console.WriteLine($"Handle:{param.Socket.Handle}, Buffer.Length=>{param.Buffer.Length}, Buffer=>{data}...");
                    param.Socket.Send(param.Buffer);
                }
            };

            Server.Startup(
                new IPEndPoint(new IPAddress(new byte[] { 192, 168, 0, 6 }), 5050),
                receivedDataCallBack);

            Console.WriteLine("服务已启动。");
            Console.ReadKey();
        }
    }
}

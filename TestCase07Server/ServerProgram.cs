using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketCommunicationFoundation;
using System.Net;

namespace TestCase07Server
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            Action<object> receivedDataCallBack = (o) =>
            {
                var param = o as SocketCallbackEventArgs;
                {
                    var data = Encoding.Default.GetString(param.Buffer);
                    Console.WriteLine($"Handle:{param.Socket.Handle}, Buffer.Length=>{param.Buffer.Length}, Buffer=>{data}...");
                    //param.Socket.Send(param.Buffer);
                }
            };

            Server.Startup(
                new IPEndPoint(new IPAddress(new byte[] { 192, 168, 0, 6 }), 6050),
                receivedDataCallBack, maxLinkIdleSecond:5 /* 5秒不收到任何数据就主动断开。如果填写0表示，服务端不主动断开连接。*/); 

            Console.WriteLine("服务已启动。");
            Console.ReadKey();
        }
    }
}

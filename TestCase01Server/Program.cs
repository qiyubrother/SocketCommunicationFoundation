using System;
using System.Net;
using System.Text;
using SocketCommunicationFoundation;

namespace TestCase01Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Action<object> receivedDataCallBack = (o) =>
            {
                var param = o as SocketCallbackEventArgs;
                {
                    //Console.WriteLine($"Handle:{param.InnerSocket.Handle}, BytesTransferred=>{param.BytesTransferred}, Offset=>{param.Offset}...");
                    var data = Encoding.Default.GetString(param.Buffer);
                    //Console.WriteLine($"Handle:{param.Socket.Handle}, Buffer.Length=>{param.Buffer.Length}, Buffer=>{data}...");
                    //var arr = data.Split('+');
                    //var result = Convert.ToInt32(arr[0]) + Convert.ToInt32(arr[1]);
                    var returnMsg = $"{data}";
                    GetValueAsync(param.Socket, data); 
                    //param.Socket.Send(Encoding.Default.GetBytes(returnMsg));
                }
            };

            Server.Startup(
                new IPEndPoint(new IPAddress(new byte[] { 192, 168, 0, 2 }), 5050),
                receivedDataCallBack,maxDataIdleSecond:5, maxLinkIdleSecond:5);

            Console.WriteLine("服务已启动。");
            Console.ReadKey();

        }

        static void GetValueAsync(System.Net.Sockets.Socket so, string s)
        {
            GetValueAsync1(so,s);
            //await System.Threading.Tasks.Task.Run(() =>
            //{
            //    System.Threading.Thread.Sleep(50000);
            //    for (int i = 0; i < 5; ++i)
            //    {
            //        Console.Out.WriteLine(String.Format("From task : {0}", i));
            //    }
            //});

            //await GetValueAsync1();
            Console.Out.WriteLine("Task End end");
        }
        static async System.Threading.Tasks.Task GetValueAsync1(System.Net.Sockets.Socket so, string s)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                System.Threading.Thread.Sleep(5000);
                for (int i = 0; i < 5; ++i)
                {
                    Console.Out.WriteLine(String.Format("From task : {0}", i));
                }
            });
            so.Send(Encoding.Default.GetBytes(s));
            Console.Out.WriteLine("Task End");
        }
    }
}

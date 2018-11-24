using System;
using System.Net;
using System.Text;
using SocketCommunicationFoundation;

namespace TestCase06Server
{
    class ServerProgram
    {
        static void Main(string[] args)
        {
            Action<object> receivedDataCallBack = (o) =>
            {
                #region 植入命令行插件
                var param = o as SocketCallbackEventArgs;
                CommandLineProcess.Do(o, "#", Encoding.Default, (obj, cmdList, lastCommand) =>
                {
                    cmdList.ForEach(x => Console.WriteLine(x));
                });
                #endregion
            };

            Server.Startup(
                new IPEndPoint(new IPAddress(new byte[] { 192, 168, 0, 6 }), 5050),
                receivedDataCallBack);

            Console.WriteLine("服务已启动。");
            Console.ReadKey();
        }
    }
}

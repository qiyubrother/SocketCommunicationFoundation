using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestCase06Client
{
    class ClientProgram
    {
        static void Main(string[] args)
        {
            //设定服务器IP地址 
            IPAddress ip = IPAddress.Parse("192.168.0.6");
            Socket clientSocket = null;
            byte[] recvBuffer = new byte[1024];
            //通过 clientSocket 发送数据 
            var commandList = new[]
            {
                //"Dir#",
                "Beijing#Tia",
                "njin#Shanghai",
                "#Guangdong#Fujian#",
                "Pinggu#",
                "#Qingyingtun#Exi",
                "t#Bye"
            };

            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    clientSocket.Connect(new IPEndPoint(ip, 5050)); //配置服务器IP与端口 
                    Console.WriteLine("连接服务器成功");
                }
                catch
                {
                    Console.WriteLine("连接服务器失败，请按回车键退出！");
                    return;
                }

                for (int i = 0; i < commandList.Length; i++)
                {
                    Thread.Sleep(2000);    //等待1秒钟 
                    var command = commandList[i];
                    string sendMessage = $"{command}";
                    clientSocket.Send(Encoding.Default.GetBytes(sendMessage));
                    Console.WriteLine($"向服务器发送消息：{sendMessage}");
                }
            }
            catch
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            finally
            {
                try
                {
                    clientSocket.Close();
                }
                catch { }
            }

            Console.WriteLine("发送完毕，按回车键退出");
            Console.ReadLine();


        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestCase07Client
{
    class ClientProgram
    {
        static void Main(string[] args)
        {
            //设定服务器IP地址 
            IPAddress ip = IPAddress.Parse("192.168.0.6");
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect(new IPEndPoint(ip, 6050)); //配置服务器IP与端口 
                Console.WriteLine("连接服务器成功");
            }
            catch
            {
                Console.WriteLine("连接服务器失败，请按回车键退出！");
                return;
            }

            try
            {
                string sendMessage = $"你好! {DateTime.Now}.{DateTime.Now.Millisecond}";
                clientSocket.Send(Encoding.Default.GetBytes(sendMessage));
                Console.WriteLine($"向服务器发送消息：{sendMessage}");
                Thread.Sleep(5000);    //等待5秒钟 
                var buffer = new byte[1024];
                while (true)
                {
                    if (clientSocket.Poll(-1, SelectMode.SelectWrite) 
                        && clientSocket.Receive(buffer, SocketFlags.None) == 0)
                    {
                        Console.Write("Server Closed!");
                        break;
                    }
                    Thread.Sleep(50);    //等待3秒钟 
                }
            }
            catch
            {
                Console.WriteLine($"异常抛出时间：{DateTime.Now}");
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            Console.ReadLine();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace TestCase03Client
{
    class Program
    {
        static void Main(string[] args)
        {
            //设定服务器IP地址 
            IPAddress ip = IPAddress.Parse("192.168.0.6");
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
            byte[] recvBuffer = new byte[102400];
            //通过 clientSocket 发送数据 
            for (int i = 0; i < 200000; i++)
            {
                try
                {
                    Thread.Sleep(50);    //等待1秒钟 
                    string sendMessage = $"{clientSocket.LocalEndPoint.ToString()}, {DateTime.Now}.{DateTime.Now.Millisecond}::\n" + GetBigBufferFileData();
                    clientSocket.Send(Encoding.Default.GetBytes(sendMessage));
                    Console.WriteLine($"向服务器发送消息：{sendMessage}");
                    var len = clientSocket.Receive(recvBuffer);
                    Console.WriteLine("从服务器接收的消息：");
                    Console.WriteLine(Encoding.Default.GetString(recvBuffer, 0, len));
                }
                catch
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    break;
                }
            }
            Console.WriteLine("发送完毕，按回车键退出");
            Console.ReadLine();


        }

        private static string GetBigBufferFileData()
        {
            using (var sr = new StreamReader("BigBufferFile.txt"))
            {
                return sr.ReadToEnd();
            }
        }
    }
}

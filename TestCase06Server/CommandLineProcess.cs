using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketCommunicationFoundation;

namespace TestCase06Server
{
    class CommandLineProcess
    {
        public static void Do(object o, string splitString, Encoding encoding, Action<object, List<string>, string> action)
        {
            var param = o as SocketCallbackEventArgs;
            var session = param.Session as Dictionary<object, object>;
            var splitChar = splitString == string.Empty ? "#" : splitString; // Default "#"
            var bufferString = encoding.GetString(param.Buffer);
            if (!session.ContainsKey("lastCommand"))
            {
                session["lastCommand"] = string.Empty;
            }
            if (!session.ContainsKey("commandList"))
            {
                session["commandList"] = new List<string>();
            }
            List<string> commandList = session["commandList"] as List<string>;

            var arr = bufferString.Split(splitChar[0]);
            var i = 0;
            do
            {
                arr[i] = (session["lastCommand"] as string) + arr[i];
                session["lastCommand"] = string.Empty;
                commandList.Add(arr[i]);
                i++;
            } while (i < arr.Length - 1);

            if (!bufferString.EndsWith(splitChar))
            {
                session["lastCommand"] = arr[arr.Length - 1];
            }

            action?.Invoke(o, commandList, session["lastCommand"] as string);

            commandList.Clear(); // 必须清楚命令行列表
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteKunServer
{
    internal static class Flag
    {
        static object lockObj = new object();
        static bool isSendingMonitor; // 画面送信中
        static public bool IsSendingMonitor
        {
            get { lock (lockObj) return isSendingMonitor; }
            set { lock (lockObj) isSendingMonitor = value; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteKun
{
    internal static class Flag
    {
        static object lockObj = new object();
        static bool isConnecting;
        static bool isConnected;
        static bool isReceivingMonitor;
        // モニター受信中
        static public bool IsReceivingMonitor
        {
            get { lock (lockObj) return isReceivingMonitor; }
            set { lock (lockObj) isReceivingMonitor = value; }
        }
        // 接続中
        static public bool IsConnecting
        {
            get { lock (lockObj) return isConnecting; }
            set { lock (lockObj) isConnecting = value; }
        }
        // 接続確立
        static public bool IsConnected
        {
            get { lock (lockObj) return isConnected; }
            set { lock (lockObj) isConnected = value; }
        }
    }
}

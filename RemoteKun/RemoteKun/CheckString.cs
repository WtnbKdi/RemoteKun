using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RemoteKun
{
    // テキストボックスの不正入力をチェック
    internal static class CheckString
    {
        // IPアドレスのフォーマットをチェック
        public static bool ipAddr(string ipAddrStr)
        {
            Regex chkIPaddr = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");
            return chkIPaddr.IsMatch(ipAddrStr);
        }

        // ポート番号をチェック
        public static bool port(string portStr)
        {
            int port = Convert.ToInt32(portStr);
            return 0 <= port && port <= 65535;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteKun
{
    // サーバー操作コマンド
    internal class CommandKind
    {
        public static readonly string MouseWheelUp = "MouseWheelUp:";                    // マウスホイール上
        public static readonly string MouseWheelDown = "MouseWheelDown:";                // マウスホイール下
        public static readonly string MonitorMouseMove = "MouseMove:";                   // マウス移動
        public static readonly string Message = "Message:";                              // メッセージ
        public static readonly string GetMonitor = "GetMonitor:";                        // 画面要求命令
        public static readonly string StopGetMonitor = "StopGetMonitor:";                // 画面送信停止命令
        public static readonly string MonitorClickLeftDown = "MonitorClickLeftDown:";    // 左クリック押したとき
        public static readonly string MonitorClickLeftUp = "MonitorClickLeftUp:";        // 左クリック離したとき
        public static readonly string MonitorClickRightDown = "MonitorClickRightDown:";  // 右クリック押したとき
        public static readonly string MonitorClickRightUp = "MonitorClickRightUp:";      // 右クリック離したとき
        public static readonly string MonitorDblClickLeft = "MonitorClickDblLeft:";      // 左ダブルクリック
        public static readonly string MonitorDblClickRight = "MonitorClickDblRight:";    // 右ダブルクリック
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Diagnostics;
namespace RemoteKunServer
{

    public partial class MainForm : Form
    {
        // マウスカーソルを移動させる為の関数
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        // マウスイベントを発生させる為の関数
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        NetworkStream ns = null;
        TcpClient tc = null;
        TcpListener tl = null;

        static class LockFlg 
        {
            static object lockObj = new object();
            static bool sendMonitorFlg;
            // 送信中
            static public bool SendMonitorFlg 
            {
                get { lock(lockObj) return sendMonitorFlg; }
                set { lock (lockObj) sendMonitorFlg = value; }
            }
        }


        static class Order
        {
            static string monitorClick = "MonitorClick";
            public static readonly string Message = "Message:";
            public static readonly string GetMonitor = "GetMonitor:";
            public static readonly string StopGetMonitor = "StopGetMonitor:";
            public static readonly string MonitorClickLeftDown = monitorClick + "LeftDown:";
            public static readonly string MonitorClickLeftUp = monitorClick + "LeftUp:";
            public static readonly string MonitorClickRightDown = monitorClick + "RightDown:";
            public static readonly string MonitorClickRightUp = monitorClick + "RightUp:";
            public static readonly string MonitorDblClickLeft = monitorClick + "DblLeft:";
            public static readonly string MonitorDblClickRight = monitorClick + "DblRight:";
        }

        // クリックイベント
        static class MouseEvent
        {
            public static readonly int MOUSEEVENTF_LEFTDOWN = 0x0002;
            public static readonly int MOUSEEVENTF_LEFTUP = 0x0004;
            public static readonly int MOUSEEVENTF_RIGHTDOWN = 0x0008;
            public static readonly int MOUSEEVENTF_RIGHTUP = 0x0010;
        }

        enum MousePoint
        {
            X = 1,
            Y,
        }

        public MainForm()
        {
            InitializeComponent();
        }

        // 命令を受信
        async Task<string> GetOrder(int getBuffSize = 128, int delay = 100)
        {
            byte[] getBuff = new byte[getBuffSize];
            int readByteSize; // 受け取ったメッセージのサイズ
            string getByteStr = null;　// 受け取ったメッセージ

            readByteSize = await ns.ReadAsync(getBuff, 0, getBuff.Length);
            if (readByteSize == 0) return null;
            getByteStr = Encoding.UTF8.GetString(getBuff, 0, readByteSize);
            return getByteStr;
        }

        // ビットマップからByteへ変換
        byte[] BMPtoByte(Bitmap bmp)
        {
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.GetBuffer();
        }

        // 画面送信
        async Task sendDesktopAsync()
        {
            byte[] sendBuff = null;
            Bitmap bm = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height); 
            using (Graphics grph = Graphics.FromImage(bm)) // スクリーンショット
                grph.CopyFromScreen(new Point(0, 0), new Point(0, 0), bm.Size);
            sendBuff = BMPtoByte(bm);
            await ns.WriteAsync(sendBuff, 0, sendBuff.Length);
        }

        // 受信命令の実行
        void GetOrderRun(string res)
        {
            if (res.StartsWith(Order.Message)) // テキストを受信
            {
                Invoke(new Action(() => msgListBox.Items.Add(res.Replace(Order.Message, ""))));
                return;
            }

            // デスクトップ画面送信終了命令受信
            if (res.StartsWith(Order.StopGetMonitor)) 
            {
                LockFlg.SendMonitorFlg = false;
                return;
            }

            // デスクトップ画面送信開始命令受信
            if (res.StartsWith(Order.GetMonitor)) 
            {
                LockFlg.SendMonitorFlg = true;
                Task.Run(async () => {
                    while (LockFlg.SendMonitorFlg)
                    {
                        await sendDesktopAsync();
                        await Task.Delay(340);
                    }
                });
                return;
            }

            // 左クリック(押す)命令受信
            if (res.StartsWith(Order.MonitorClickLeftDown))
            {
                string[] rsvStr = res.Split(':');
                int pointX = Convert.ToInt32(rsvStr[(int)MousePoint.X]);
                int pointY = Convert.ToInt32(rsvStr[(int)MousePoint.Y]);
                SetCursorPos(pointX, pointY);
                mouse_event(MouseEvent.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                return;
            }

            // 左クリック(離す)命令受信
            if (res.StartsWith(Order.MonitorClickLeftUp))
            {
                string[] rsvStr = res.Split(':');
                int pointX = Convert.ToInt32(rsvStr[(int)MousePoint.X]);
                int pointY = Convert.ToInt32(rsvStr[(int)MousePoint.Y]);
                SetCursorPos(pointX, pointY);
                mouse_event(MouseEvent.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                return;
            }

            // 右クリック(押す)命令受信
            if (res.StartsWith(Order.MonitorClickRightDown))
            {
                string[] rsvStr = res.Split(':');
                int pointX = Convert.ToInt32(rsvStr[(int)MousePoint.X]);
                int pointY = Convert.ToInt32(rsvStr[(int)MousePoint.Y]);
                SetCursorPos(pointX, pointY);
                mouse_event(MouseEvent.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                return;
            }

            // 左クリック(離す)命令受信
            if (res.StartsWith(Order.MonitorClickRightUp))
            {
                string[] rsvStr = res.Split(':');
                int pointX = Convert.ToInt32(rsvStr[(int)MousePoint.X]);
                int pointY = Convert.ToInt32(rsvStr[(int)MousePoint.Y]);
                SetCursorPos(pointX, pointY);
                mouse_event(MouseEvent.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                return;
            }
        }

        // 画面読み込み時
        private async void MainForm_Load(object sender, EventArgs e)
        {
            tl = new TcpListener(System.Net.IPAddress.Any, 12345); // 全てのアドレスから受け付ける
            tl.Start();
            tc = await tl.AcceptTcpClientAsync(); // 接続確立
            msgListBox.Items.Add("接続完了");
            await Task.Run(async () => {
                while (true)
                {
                    string res = await GetOrder(); // クライアントから命令を受け取る
                    if (res == null) continue; // 命令がなければ繰り返す
                    GetOrderRun(res); // 受信した命令を実行
                }
            });
        }
    }
}

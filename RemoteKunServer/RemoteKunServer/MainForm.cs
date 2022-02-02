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
        NetworkStream ns = null;
        TcpClient tc = null;
        TcpListener tl = null;

        static class Flag 
        {
            static object lockObj = new object();
            static bool sendMonitorFlg; // 画面送信中
            static public bool SendMonitorFlg 
            {
                get { lock(lockObj) return sendMonitorFlg; }
                set { lock (lockObj) sendMonitorFlg = value; }
            }
        }

        // 受信命令の種類
        // 座標なし命令の場合 命令の種類:
        // 座標あり命令の場合 命令の種類:X座標:Y座標
        static class OrderKind
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

        // マウス座標
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
        void GetOrderRun(string getOrder)
        {
            Regex orderParameter = new Regex(@"^[a-zA-Z]+\:\d+\:\d+$");

            // 座標付きの命令に使う
            string[] rsvStr;
            int pointX, pointY;
            string getOdrPoint = orderParameter.Match(getOrder).ToString();

            // テキストメッセージを受信した場合
            if (getOrder.StartsWith(OrderKind.Message)) 
            {
                Invoke(new Action(() => msgListBox.Items.Add(getOrder.Replace(OrderKind.Message, ""))));
                return;
            }

            // デスクトップ画面送信終了命令
            if (getOrder.StartsWith(OrderKind.StopGetMonitor)) 
            {
                Flag.SendMonitorFlg = false;
                return;
            }

            // デスクトップ画面送信開始命令
            if (getOrder.StartsWith(OrderKind.GetMonitor)) 
            {
                Flag.SendMonitorFlg = true;
                Task.Run(async () => {
                    while (Flag.SendMonitorFlg)
                    {
                        await sendDesktopAsync();
                        await Task.Delay(100);
                    }
                });
                return;
            }

            // マウスホイール上方向命令
            if (getOrder.StartsWith(OrderKind.MouseWheelUp))
            {
                WindowsAPI.INPUT input = new WindowsAPI.INPUT
                {
                    type = WindowsAPI.INPUT_MOUSE,
                    ui = new WindowsAPI.INPUT_UNION
                    {
                        mouse = new WindowsAPI.MOUSEINPUT
                        {
                            dwFlags = WindowsAPI.MOUSEEVENTF_WHEEL,
                            dx = 0,
                            dy = 0,
                            mouseData = 120, // ホイール上方向
                            dwExtraInfo = IntPtr.Zero,
                            time = 0
                        }
                    }
                };
                WindowsAPI.SendInput(1, ref input, Marshal.SizeOf(input));
            }

            // マウスホイール下方向命令
            if (getOrder.StartsWith(OrderKind.MouseWheelDown))
            {
                WindowsAPI.INPUT input = new WindowsAPI.INPUT
                {
                    type = WindowsAPI.INPUT_MOUSE,
                    ui = new WindowsAPI.INPUT_UNION
                    {
                        mouse = new WindowsAPI.MOUSEINPUT
                        {
                            dwFlags = WindowsAPI.MOUSEEVENTF_WHEEL,
                            dx = 0,
                            dy = 0,
                            mouseData = -120, // ホイール下方向
                            dwExtraInfo = IntPtr.Zero,
                            time = 0
                        }
                    }
                };
                WindowsAPI.SendInput(1, ref input, Marshal.SizeOf(input));
            }

            // 座標付き命令の書式が一致しない場合
            if (!orderParameter.IsMatch(getOrder)) 
                return;

            rsvStr = getOdrPoint.Split(':');
            pointX = Convert.ToInt32(rsvStr[(int)MousePoint.X]);
            pointY = Convert.ToInt32(rsvStr[(int)MousePoint.Y]);

            // マウス移動命令受信
            if (getOrder.StartsWith(OrderKind.MonitorMouseMove))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                return;
            }

            // 左クリック(押す)命令受信
            if (getOrder.StartsWith(OrderKind.MonitorClickLeftDown))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                WindowsAPI.mouse_event(WindowsAPI.MouseEvent.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                return;
            }

            // 左クリック(離す)命令受信
            if (getOrder.StartsWith(OrderKind.MonitorClickLeftUp))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                WindowsAPI.mouse_event(WindowsAPI.MouseEvent.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                return;
            }

            // 右クリック(押す)命令受信
            if (getOrder.StartsWith(OrderKind.MonitorClickRightDown))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                WindowsAPI.mouse_event(WindowsAPI.MouseEvent.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                return;
            }

            // 左クリック(離す)命令受信
            if (getOrder.StartsWith(OrderKind.MonitorClickRightUp))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                WindowsAPI.mouse_event(WindowsAPI.MouseEvent.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                return;
            }
        }

        // 画面読み込み時
        private async void MainForm_Load(object sender, EventArgs e)
        {
            while (true)
            {
                try
                {
                    tl = new TcpListener(System.Net.IPAddress.Any, 12345); // 全てのアドレスから受け付ける
                    tl.Start();
                    tc = await tl.AcceptTcpClientAsync(); // 接続確立
                    msgListBox.Items.Add("接続完了");
                    ns = tc.GetStream();
                    await Task.Run(async () => {
                        while (true)
                        {
                            string res = await GetOrder(); // クライアントから命令を受け取る
                            if (res == null) continue; // 命令がなければ繰り返す
                            GetOrderRun(res); // 受信した命令を実行
                        }
                    });
                }
                catch (Exception ex)
                {
                    msgListBox.Items.Add(ex.Message);
                }
            }
        }
    }
}

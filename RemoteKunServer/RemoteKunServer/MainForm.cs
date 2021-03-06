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
        async Task<string> getCommand(int getBuffSize = 128, int delay = 100)
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
        async Task sendMonitorAsync()
        {
            byte[] sendBuff = null;
            Bitmap bm = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height); 
            using (Graphics grph = Graphics.FromImage(bm)) // スクリーンショット
                grph.CopyFromScreen(new Point(0, 0), new Point(0, 0), bm.Size);
            sendBuff = BMPtoByte(bm);
            await ns.WriteAsync(sendBuff, 0, sendBuff.Length);
        }

        // ホイール入力情報
        WindowsAPI.INPUT makeWheelInput(int vector, int step = 1)
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
                        mouseData = 120 * vector * step, // 正数:ホイール上方向 負数:下方向 step:ホイール感度
                        dwExtraInfo = IntPtr.Zero,
                        time = 0
                    }
                }
            };

            return input;
        }


        // 受信命令の実行
        void GetCommandRun(string getCommand)
        {
            Regex commandParameter = new Regex(@"^[a-zA-Z]+\:\d+\:\d+$");

            // 座標付きの命令に使う
            string[] rsvStr;
            int pointX, pointY;
            string getOdrPoint = commandParameter.Match(getCommand).ToString();

            // テキストメッセージを受信した場合
            if (getCommand.StartsWith(CommandKind.Message)) 
            {
                Invoke(new Action(() => msgListBox.Items.Add(getCommand.Replace(CommandKind.Message, ""))));
                return;
            }

            // デスクトップ画面送信終了命令
            if (getCommand.StartsWith(CommandKind.StopGetMonitor)) 
            {
                Flag.IsSendingMonitor  = false;
                return;
            }

            // デスクトップ画面送信開始命令
            if (getCommand.StartsWith(CommandKind.GetMonitor)) 
            {
                Flag.IsSendingMonitor  = true;
                Task.Run(async () => {
                    while (Flag.IsSendingMonitor )
                    {
                        await sendMonitorAsync();
                        await Task.Delay(100);
                    }
                });
                return;
            }

            // マウスホイール上方向命令
            if (getCommand.StartsWith(CommandKind.MouseWheelUp))
            {
                WindowsAPI.INPUT input = makeWheelInput(1);
                WindowsAPI.SendInput(1, ref input, Marshal.SizeOf(input));
            }

            // マウスホイール下方向命令
            if (getCommand.StartsWith(CommandKind.MouseWheelDown))
            {
                WindowsAPI.INPUT input = makeWheelInput(-1);
                WindowsAPI.SendInput(1, ref input, Marshal.SizeOf(input));
            }

            // 座標月コマンドのフォーマットが一致しない場合
            if (!commandParameter.IsMatch(getCommand)) 
                return;

            rsvStr = getOdrPoint.Split(':');
            pointX = Convert.ToInt32(rsvStr[(int)MousePoint.X]);
            pointY = Convert.ToInt32(rsvStr[(int)MousePoint.Y]);

            // マウス移動命令受信
            if (getCommand.StartsWith(CommandKind.MonitorMouseMove))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                return;
            }

            // 左クリック(押す)命令受信
            if (getCommand.StartsWith(CommandKind.MonitorClickLeftDown))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                WindowsAPI.mouse_event(WindowsAPI.MouseEvent.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                return;
            }

            // 左クリック(離す)命令受信
            if (getCommand.StartsWith(CommandKind.MonitorClickLeftUp))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                WindowsAPI.mouse_event(WindowsAPI.MouseEvent.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                return;
            }

            // 右クリック(押す)命令受信
            if (getCommand.StartsWith(CommandKind.MonitorClickRightDown))
            {
                WindowsAPI.SetCursorPos(pointX, pointY);
                WindowsAPI.mouse_event(WindowsAPI.MouseEvent.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                return;
            }

            // 左クリック(離す)命令受信
            if (getCommand.StartsWith(CommandKind.MonitorClickRightUp))
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
                    tl.Stop();
                    msgListBox.Items.Add("接続完了");
                    ns = tc.GetStream();
                    await Task.Run(async () => {
                        while (true)
                        {
                            string res = await getCommand(); // クライアントから命令を受け取る
                            if (res == null) continue; // 命令がなければ繰り返す
                            GetCommandRun(res); // 受信した命令を実行
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

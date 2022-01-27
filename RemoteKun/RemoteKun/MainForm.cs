using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteKun
{
    public partial class MainForm : Form
    {
        TcpClient client = null;
        NetworkStream ns = null;
        MousePoint mPoint = null;
        class MousePoint
        {
            public int X { get; set; } = 0;
            public int Y { get; set; } = 0;
        }

        static class ErrorMessage
        {
            public static readonly string IPAddr = "正しいIPアドレスを入力してください。";
            public static readonly string Port = "正しいポート番号を入力してください。";
        }

        class Protocol
        {
            public Protocol() { }
            public Protocol(string type) { Type = type; }
            public string Type { get; private set; }
            
        }

        // サーバーへ対しての命令, 送信の種類
        class TypeProtocol
        {
            public static readonly string MonitorMouseMove = "MouseMove:"; // マウス移動(未)
            static string monitorClick = "MonitorClick"; // マウスクリック
            public static readonly string Message = "Message:"; // メッセージ
            public static readonly string GetMonitor = "GetMonitor:"; // 画面要求命令
            public static readonly string StopGetMonitor = "StopGetMonitor:"; // 画面送信停止命令
            public static readonly string MonitorClickLeftDown = monitorClick + "LeftDown:"; // 左クリック押したとき
            public static readonly string MonitorClickLeftUp = monitorClick + "LeftUp:"; // 左クリック離したとき
            public static readonly string MonitorClickRightDown = monitorClick + "RightDown:"; // 右クリック押したとき
            public static readonly string MonitorClickRightUp = monitorClick + "RightUp:";  // 右クリック離したとき
            public static readonly string MonitorDblClickLeft = monitorClick + "DblLeft:"; // 左ダブルクリック
            public static readonly string MonitorDblClickRight = monitorClick + "DblRight:";  // 右ダブルクリック
        }

        static class LockFlg
        {
            static object lockObj = new object();

            static bool conectFlg;
            static bool rsvMonitorFlg;
            // モニター受信中
            static public bool SendMonitorFlg
            {
                get { lock (lockObj) return rsvMonitorFlg; }
                set { lock (lockObj) rsvMonitorFlg = value; }
            }
            // 接続確立
            static public bool ConectFlg
            {
                get { lock (lockObj) return conectFlg; }
                set { lock (lockObj) conectFlg = value; }
            }
        }

        public MainForm()
        {
            LockFlg.ConectFlg = false;
            LockFlg.SendMonitorFlg = false;
            mPoint = new MousePoint();
            InitializeComponent();
        }

        // テキストボックスの不正入力をチェック
        static class CheckString
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
        
        // 初期化
        void ClientReset()
        {
            pictureBox.Image = null;
            LockFlg.ConectFlg = false;
            startButton.Text = "開始";
            client?.Close();
            client?.Dispose();
            client = null;
        }

        // 開始
        private async void startButton_Click(object sender, EventArgs e)
        {
            LockFlg.ConectFlg = LockFlg.ConectFlg ? false : true;
            if (LockFlg.ConectFlg) startButton.Text = "終了";
            else startButton.Text = "開始";

            string ipAddr = ipAddrTextBox.Text;
            string port = portTextBox.Text;

            if (!CheckString.ipAddr(ipAddr))
            {
                MessageBox.Show(ErrorMessage.IPAddr);
                return;
            }

            if (!CheckString.port(port))
            {
                MessageBox.Show(ErrorMessage.Port);
                return;
            }

            if (!LockFlg.ConectFlg)
            {
                ClientReset();
                return;
            }

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ipAddr, Convert.ToInt32(port));
                msgListBox.Items.Add("サーバーに接続しました。");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                msgListBox.Items.Add("サーバーに接続できませんでした。");
                ClientReset();
            }
            catch (Exception ex)
            {
                msgListBox.Items.Add(ex.Message);
                ClientReset();
            }
        }

        // メッセージ送信
        async Task sendMessageAsync(Protocol ptl, string sendMsg = null)
        {
            string msg = ptl.Type + sendMsg;
            byte[] sendBuff = Encoding.UTF8.GetBytes(msg);
            ns = client.GetStream();
            await ns.WriteAsync(sendBuff, 0, sendBuff.Length);
            await Task.Delay(300); // 遅延必須
        }

        // メッセージ送信ボタン
        private async void sendMsgButton_Click(object sender, EventArgs e)
        {
            if (!LockFlg.ConectFlg) return;
            await sendMessageAsync(new Protocol(TypeProtocol.Message), msgTextBox.Text);
        }

        // 画面リクエストボタン
        private async void desktopReqButton_Click(object sender, EventArgs e)
        {
            if (!LockFlg.ConectFlg) return;

            int sendSize = 8 * 32 * 20000; 
            byte[] image;
            
            LockFlg.SendMonitorFlg = LockFlg.SendMonitorFlg ? false : true;
            if(!LockFlg.SendMonitorFlg)
            {
                pictureBox.Enabled = false;
                await sendMessageAsync(new Protocol(TypeProtocol.StopGetMonitor));
                return;
            }

            pictureBox.Enabled = true;
            await Task.Run(async () => {
                await sendMessageAsync(new Protocol(TypeProtocol.GetMonitor)); // 画面をリクエスト
                try
                {
                    while (LockFlg.SendMonitorFlg)
                    {
                        image = new byte[sendSize];
                        await ns.ReadAsync(image, 0, image.Length);
                        Bitmap bm = new Bitmap(new MemoryStream(image));
                        bm.Save(new MemoryStream(image), System.Drawing.Imaging.ImageFormat.Jpeg);
                        pictureBox.Image = bm;
                    }
                }
                catch (System.IO.IOException) 
                { 
                    Invoke(new Action(() => { 
                        msgListBox.Items.Add("通信が切断されました。");
                        ClientReset();
                    }));
                }
                catch (System.InvalidOperationException)
                {
                    Invoke(new Action(() => {
                        msgListBox.Items.Add("通信が確立されていません。");
                        ClientReset();
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() => {
                        msgListBox.Items.Add(ex.Message);
                        ClientReset();
                    }));
                }
            });
        }


        // 画面ダブルクリック
        private async void pictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!LockFlg.ConectFlg) return;

            bool leftButtonFlg = MouseButtons.None != (e.Button & MouseButtons.Left);
            bool rightButtonFlg = MouseButtons.None != (e.Button & MouseButtons.Right);

            if (leftButtonFlg)
            {
                // 1920 x 1080モニターを想定
                // ピクチャーボックス(画面)をクリックした時の座標をサーバー画面に反映させる為の計算
                string x = Convert.ToString((int)((1920.0 / pictureBox.Width) * e.X));
                string y = Convert.ToString((int)((1080.0 / pictureBox.Height) * e.Y));
                await sendMessageAsync(new Protocol(TypeProtocol.MonitorDblClickLeft), $"{x}:{y}");
            }

            if (rightButtonFlg)
            {
                string x = Convert.ToString((int)((1920.0 / pictureBox.Width) * e.X));
                string y = Convert.ToString((int)((1080.0 / pictureBox.Height) * e.Y));
                await sendMessageAsync(new Protocol(TypeProtocol.MonitorDblClickRight), $"{x}:{y}");
            }
        }

        // 画面上 クリックボタン押す
        private async void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (!LockFlg.ConectFlg) return;

            bool leftButtonFlg = MouseButtons.None != (e.Button & MouseButtons.Left);
            bool rightButtonFlg = MouseButtons.None != (e.Button & MouseButtons.Right);

            if (leftButtonFlg)
            {
                string x = Convert.ToString((int)((1920.0 / pictureBox.Width) * e.X));
                string y = Convert.ToString((int)((1080.0 / pictureBox.Height) * e.Y));
                await sendMessageAsync(new Protocol(TypeProtocol.MonitorClickLeftDown), $"{x}:{y}");
                return;
            }


            if (rightButtonFlg)
            {
                string x = Convert.ToString((int)((1920.0 / pictureBox.Width) * e.X));
                string y = Convert.ToString((int)((1080.0 / pictureBox.Height) * e.Y));
                await sendMessageAsync(new Protocol(TypeProtocol.MonitorClickRightDown), $"{x}:{y}");
                return;
            }
        }

        // 画面上 クリックボタン離す
        private async void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (!LockFlg.ConectFlg) return;

            bool leftButtonFlg = MouseButtons.None != (e.Button & MouseButtons.Left);
            bool rightButtonFlg = MouseButtons.None != (e.Button & MouseButtons.Right);

            if (leftButtonFlg)
            {
                string x = Convert.ToString((int)((1920.0 / pictureBox.Width) * e.X));
                string y = Convert.ToString((int)((1080.0 / pictureBox.Height) * e.Y));
                await sendMessageAsync(new Protocol(TypeProtocol.MonitorClickLeftUp), $"{x}:{y}");
            }


            if (rightButtonFlg)
            {
                string x = Convert.ToString((int)((1920.0 / pictureBox.Width) * e.X));
                string y = Convert.ToString((int)((1080.0 / pictureBox.Height) * e.Y));
                await sendMessageAsync(new Protocol(TypeProtocol.MonitorClickRightUp), $"{x}:{y}");
            }
        }
    }
}

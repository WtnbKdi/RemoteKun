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
        Resolution Res; // 画面解像度 

        public MainForm()
        {
            InitializeComponent();
            LockFlg.ConectFlg = false;
            LockFlg.SendMonitorFlg = false;
            Res = new Resolution();
            this.pictureBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseWheel);
        }
        
        static class ErrorMessage
        {
            public static readonly string IPAddr = "正しいIPアドレスを入力してください。";
            public static readonly string Port = "正しいポート番号を入力してください。";
        }

        class Oder
        {
            public Oder() { }
            public Oder(string type) { Type = type; }
            public string Type { get; private set; }
        }


        class Resolution 
        {
            // 画面解像度 フォルト1920x1080
            public int X { get; } = 1920;
            public int Y { get; } = 1080;
            public Resolution() { }
            public Resolution(int x, int y) 
            {
                this.X = x;
                this.Y = y;
            }
        }


        // サーバーへ対しての命令, 送信の種類
        class OderKind
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
            LockFlg.SendMonitorFlg = false;
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
                statusLabel.Text = "サーバーに接続しました。";
            }
            catch (System.Net.Sockets.SocketException)
            {
                statusLabel.Text = "サーバーに接続できませんでした。";
                ClientReset();
            }
            catch (Exception)
            {
                ClientReset();
            }
        }

        // メッセージ送信
        async Task sendMessageAsync(Oder ptl, string sendMsg = null)
        {
            string msg = ptl.Type + sendMsg;
            byte[] sendBuff = Encoding.UTF8.GetBytes(msg);
            ns = client.GetStream();
            await ns.WriteAsync(sendBuff, 0, sendBuff.Length);
        }

        // メッセージ送信ボタン
        private async void sendMsgButton_Click(object sender, EventArgs e)
        {
            if (!LockFlg.ConectFlg) return;
            await sendMessageAsync(new Oder(OderKind.Message), msgTextBox.Text);
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
                await sendMessageAsync(new Oder(OderKind.StopGetMonitor));
                return;
            }

            pictureBox.Enabled = true;
            await Task.Run(async () => {
                await sendMessageAsync(new Oder(OderKind.GetMonitor)); // 画面をリクエスト
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
                        statusLabel.Text = "通信が切断されました。";
                    }));
                }
                catch (System.InvalidOperationException)
                {
                    Invoke(new Action(() => {
                        statusLabel.Text = "通信が確立されていません。";
                    }));
                }
                catch (Exception) { }
                finally
                {
                    ClientReset();
                }
            });
        }

        // ピクチャーボックスのマウス座標をサーバーのマウス座標へ変換
        (string x, string y) ConvertPoint(int x, int y)
        {
            int resultX = (int)(((float)Res.X / (float)pictureBox.Width) * (float)x);
            int resultY = (int)(((float)Res.Y / (float)pictureBox.Height) * (float)y);
            return (Convert.ToString(resultX), Convert.ToString(resultY));
        }

        async Task SendPointAsync(string protocol, MouseEventArgs e)
        {
            (string x, string y) = ConvertPoint(e.X, e.Y);
            await sendMessageAsync(new Oder(protocol), $"{x}:{y}");
        }

        private async void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (!LockFlg.ConectFlg) return;
            if (MouseButtons.None != (e.Button & MouseButtons.Left))
                await SendPointAsync(OderKind.MonitorClickLeftDown, e);
            if (MouseButtons.None != (e.Button & MouseButtons.Right))
                await SendPointAsync(OderKind.MonitorClickRightDown, e);
        }

        private async void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (!LockFlg.ConectFlg) return;
            if (MouseButtons.None != (e.Button & MouseButtons.Left))
                await SendPointAsync(OderKind.MonitorClickLeftUp, e);
            if (MouseButtons.None != (e.Button & MouseButtons.Right))
                await SendPointAsync(OderKind.MonitorClickRightUp, e);
        }

        private async void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!LockFlg.ConectFlg) return;
            await SendPointAsync(OderKind.MonitorMouseMove, e);
        }

        private async void pictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!LockFlg.ConectFlg) return;
            if (e.Delta < 0)
                await sendMessageAsync(new Oder(OderKind.MouseWheelDown));
            else
                await sendMessageAsync(new Oder(OderKind.MouseWheelUp));
        }
    }
}

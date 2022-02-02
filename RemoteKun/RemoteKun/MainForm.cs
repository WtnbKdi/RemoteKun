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
            Flag.IsConnected = false;
            Flag.IsReceivingMonitor = false;
            Res = new Resolution();
            this.pictureBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseWheel);
        }
        
        static class ErrorMessage
        {
            public static readonly string IPAddr = "正しいIPアドレスを入力してください。";
            public static readonly string Port = "正しいポート番号を入力してください。";
        }

        class Command
        {
            public Command() { }
            public Command(string type) { Type = type; }
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
        class CommandKind
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

        static class Flag
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
            Flag.IsReceivingMonitor = false;
            Flag.IsConnected = false;
            Flag.IsConnecting = false;
            startButton.Text = "開始";
            client?.Close();
            client?.Dispose();
            client = null;
        }

        // 開始
        private async void startButton_Click(object sender, EventArgs e)
        {
            Flag.IsConnecting = Flag.IsConnecting ? false : true;
            string ipAddr = ipAddrTextBox.Text;
            string port = portTextBox.Text;

            if (Flag.IsConnecting) startButton.Text = "終了";
            else startButton.Text = "開始";

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

            if (!Flag.IsConnecting)
            {
                ClientReset();
                return;
            }

            client = new TcpClient();

            try { await client.ConnectAsync(ipAddr, Convert.ToInt32(port)); }
            catch (System.Net.Sockets.SocketException)
            {
                statusLabel.Text = "サーバーに接続できませんでした。";
                ClientReset();
                return;
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
                ClientReset();
                return;
            }
            statusLabel.Text = "サーバーに接続しました。";
            Flag.IsConnected = true;
        }

        // 命令送信
        async Task sendOrderAsync(Command ptl, string sendMsg = null)
        {
            string msg = ptl.Type + sendMsg;
            byte[] sendBuff = Encoding.UTF8.GetBytes(msg);
            ns = client.GetStream();
            await ns.WriteAsync(sendBuff, 0, sendBuff.Length);
        }

        // メッセージ送信ボタン
        private async void sendMsgButton_Click(object sender, EventArgs e)
        {
            if (!Flag.IsConnected) return;
            await sendOrderAsync(new Command(CommandKind.Message), msgTextBox.Text);
        }

        // 画面リクエストボタン
        private async void monitorReqButton_Click(object sender, EventArgs e)
        {
            if (!Flag.IsConnected) return;

            int sendSize = 8 * 32 * 20000; 
            byte[] image = null;
            Bitmap monitor;

            Flag.IsReceivingMonitor = Flag.IsReceivingMonitor ? false : true;
            if(!Flag.IsReceivingMonitor)
            {
                pictureBox.Enabled = false;
                await sendOrderAsync(new Command(CommandKind.StopGetMonitor));
                return;
            }
            pictureBox.Enabled = true;
            await Task.Run(async () => {
                await sendOrderAsync(new Command(CommandKind.GetMonitor)); // 画面をリクエスト
                try
                {
                    while (Flag.IsReceivingMonitor)
                    {
                        image = new byte[sendSize];
                        await ns.ReadAsync(image, 0, image.Length);
                        monitor = new Bitmap(new MemoryStream(image));
                        monitor.Save(new MemoryStream(image), System.Drawing.Imaging.ImageFormat.Jpeg);
                        pictureBox.Image = monitor;
                    }
                }
                catch (System.IO.IOException) 
                {
                    Invoke(new Action(() => statusLabel.Text = "通信が切断されました。"));
                    ClientReset();
                    return;
                }
                catch (System.InvalidOperationException)
                {
                    Invoke(new Action(() => statusLabel.Text = "通信が確立されていません。"));
                    ClientReset();
                    return;
                }
                catch (Exception ex) 
                {
                    Invoke(new Action(() => statusLabel.Text = ex.Message));
                    ClientReset();
                    return;
                }
                finally { GC.Collect(); }
            });
        }

        // ピクチャーボックスのマウス座標をサーバーのマウス座標へ変換
        (string x, string y) ConvertPoint(int x, int y)
        {
            int resultX = (int)(((float)Res.X / (float)pictureBox.Width) * (float)x);
            int resultY = (int)(((float)Res.Y / (float)pictureBox.Height) * (float)y);
            return (Convert.ToString(resultX), Convert.ToString(resultY));
        }

        // 座標送信
        async Task SendPointAsync(string Command, MouseEventArgs e)
        {
            (string x, string y) = ConvertPoint(e.X, e.Y);
            await sendOrderAsync(new Command(Command), $"{x}:{y}");
        }

        // ピクチャーボックス上でマウスボタンを押す
        private async void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Flag.IsConnected) return;
            if (MouseButtons.None != (e.Button & MouseButtons.Left)) // 左クリックの場合
                await SendPointAsync(CommandKind.MonitorClickLeftDown, e);
            if (MouseButtons.None != (e.Button & MouseButtons.Right)) 
                await SendPointAsync(CommandKind.MonitorClickRightDown, e);
        }
        // ピクチャーボックス上でマウスボタンを離す
        private async void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (!Flag.IsConnected) return;
            if (MouseButtons.None != (e.Button & MouseButtons.Left))
                await SendPointAsync(CommandKind.MonitorClickLeftUp, e);
            if (MouseButtons.None != (e.Button & MouseButtons.Right))
                await SendPointAsync(CommandKind.MonitorClickRightUp, e);
        }

        // ピクチャーボックス上でマウスポインタを移動させたとき
        private async void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Flag.IsConnected) return;
            await SendPointAsync(CommandKind.MonitorMouseMove, e);
        }

        // ピクチャーボックス上でマウスホイールを動かしたとき
        private async void pictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!Flag.IsConnected) return;
            if (e.Delta < 0)
                await sendOrderAsync(new Command(CommandKind.MouseWheelDown));
            else
                await sendOrderAsync(new Command(CommandKind.MouseWheelUp));
        }
    }
}

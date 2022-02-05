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
        Resolution Res   = null; // 画面解像度 

        public MainForm()
        {
            InitializeComponent();
            Flag.IsConnected = false;
            Flag.IsReceivingMonitor = false;
            Res = new Resolution();
            this.pictureBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseWheel);
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
            ns = client.GetStream();
        }

        // 命令送信
        async Task sendCommandAsync(Command ptl, string sendMsg = null)
        {
            string msg = ptl.Type + sendMsg;
            byte[] sendBuff = Encoding.UTF8.GetBytes(msg);
            await ns.WriteAsync(sendBuff, 0, sendBuff.Length);
        }

        // メッセージ送信ボタン
        private async void sendMsgButton_Click(object sender, EventArgs e)
        {
            if (!Flag.IsConnected) return;
            await sendCommandAsync(new Command(CommandKind.Message), msgTextBox.Text);
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
                await sendCommandAsync(new Command(CommandKind.StopGetMonitor));
                return;
            }
            pictureBox.Enabled = true;
            await Task.Run(async () => {
                await sendCommandAsync(new Command(CommandKind.GetMonitor)); // 画面をリクエスト
                try
                {
                    while (Flag.IsReceivingMonitor)
                    {
                        // 受け取った画面データをピクチャーボックスに表示
                        image = new byte[sendSize];
                        await ns.ReadAsync(image, 0, image.Length);
                        monitor = new Bitmap(new MemoryStream(image));
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
            await sendCommandAsync(new Command(Command), $"{x}:{y}");
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
                await sendCommandAsync(new Command(CommandKind.MouseWheelDown));
            else
                await sendCommandAsync(new Command(CommandKind.MouseWheelUp));
        }
    }
}

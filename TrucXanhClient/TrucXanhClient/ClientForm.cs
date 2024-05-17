using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrucXanhClient
{
    public partial class ClientForm : Form
    {
        private Socket _clientSocket;
        private byte[] _buffer = new byte[1024];
        private string clientUserName = "";
        private int serverPort = 2010;
        private bool isConnected = false;
        private bool isInGame = false;
        private bool isGameStart = false;
        private bool isMyTurn = false;
        private bool checkingFlippingCard = false;
        private bool isEndGameOut = false;

        private bool showingClosing = true;
        private bool lostConnect = false;

        private Dictionary<string, Image> ImgCards = new Dictionary<string, Image>();

        /************** card handle **************/
        private List<bool> CardHandle = new List<bool>()
        {
            false, false, false, false, false, false,
        };

        private List<int> cardFlipping = new List<int>();

        private Label label1;
        private TextBox textBox1;
        private Button connectBtn;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Label pRank1;
        private Label pRank2;
        private Label pRank3;
        private GroupBox groupBox3;
        private TextBox logBox;
        private Label titleLabel;
        private PictureBox pictureBox6;
        private PictureBox pictureBox3;
        private PictureBox pictureBox5;
        private PictureBox pictureBox4;
        private PictureBox pictureBox2;
        private PictureBox pictureBox1;
        private Label phaseLabel;

        public ClientForm()
        {
            initForm();
            this.FormClosing += new FormClosingEventHandler(ClientForm_FormClosing);
        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            if (checkUsername(textBox1.Text))
            {
                if (isConnected)
                {
                    IPEndPoint clientEndPoint = _clientSocket.LocalEndPoint as IPEndPoint;
                    string clientIP = clientEndPoint.Address.ToString();
                    int clientPort = clientEndPoint.Port;

                    string connObj = @"
                    {
                        ""type"": ""JOIN"",
                        ""client"": {
                            ""ip"": """ + clientIP + @""",
                            ""port"": """ + clientPort + @"""
                        },
                        ""name"": """ + textBox1.Text + @"""
                    }";

                    sendMessage(connObj);
                }
                else
                {
                    _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort);

                    try
                    {
                        _clientSocket.BeginConnect(endPoint, new AsyncCallback(ConnectCallback), null);
                        isConnected = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Lỗi kết nối 1!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng đặt tên theo quy tắc:\n- Tên chỉ chứa các ký tự chữ và số\n- Ít nhất 6 ký tự và nhiều nhất 10 ký tự", "Tên không hợp lệ!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool checkUsername(string text)
        {
            string pattern = @"^[a-zA-Z0-9]{6,10}$";
            return Regex.IsMatch(text, pattern);
        }

        private void sendMessage(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    _clientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
                }
            }
            catch (SocketException ex)
            {
                lostConnect = true;
                MessageBox.Show("Mat ket noi voi may chu", "Co loi!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void AppendMessage(string message)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendMessage), new object[] { message });
                return;
            }

            logBox.AppendText(message + Environment.NewLine);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _clientSocket.EndConnect(ar);

                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

                IPEndPoint clientEndPoint = _clientSocket.LocalEndPoint as IPEndPoint;
                string clientIP = clientEndPoint.Address.ToString();
                int clientPort = clientEndPoint.Port;

                string connObj = @"
                    {
                        ""type"": ""JOIN"",
                        ""client"": {
                            ""ip"": """ + clientIP + @""",
                            ""port"": """ + clientPort + @"""
                        },
                        ""name"": """ + textBox1.Text + @"""
                    }";

                sendMessage(connObj.Replace(" ", ""));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi kết nối 2!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (this.InvokeRequired)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action<IAsyncResult>(ReceiveCallback), new object[] { ar });
                }
                return;
            }


            try
            {
                if (_clientSocket != null && _clientSocket.Connected)
                {
                    int received = _clientSocket.EndReceive(ar);
                    if (received > 0) // kiểm tra độ dài của dữ liệu
                    {
                        byte[] dataBuffer = new byte[received];
                        Array.Copy(_buffer, dataBuffer, received);

                        string text = Encoding.UTF8.GetString(dataBuffer);

                        if (text != null && text != "")
                        {
                            messageHandle(text);
                        }
                    }

                    _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                }
            }
            catch (SocketException ex)
            {
                lostConnect = true;
                MessageBox.Show("Mat ket noi voi may chu 2", "Co loi!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Close();

            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            if (_clientSocket != null && _clientSocket.Connected)
            {
                _clientSocket.EndSend(ar);
            }
        }

        private async void messageHandle(string text)
        {
            if (this.InvokeRequired)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action<string>(messageHandle), new object[] { text });
                }
                return;
            }

            dynamic obj = JsonConvert.DeserializeObject(text);

            int errcode = obj.err.code;
            string type = obj.type;

            if (errcode < 0)
            {
                if (type.Equals("JOIN"))
                {
                    isConnected = false;

                    _clientSocket.Shutdown(SocketShutdown.Both);
                    _clientSocket.Close();
                }

                string title = obj.msgBox.title;
                string content = obj.msgBox.text;
                MessageBox.Show(content, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (type.Equals("JOIN"))
            {
                string title = obj.msgBox.title;
                string content = obj.msgBox.text;

                //MessageBox.Show(content, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                string myClientName = obj.name;
                clientUserName = myClientName;
                renderForm();
                isInGame = true;
                this.logBox.Clear();
                AppendMessage(timeFormated() + " Bạn đã kết nối thành công với tên: " + myClientName);
            }
            else if (type.Equals("GET_IN_GAME_PLAYERS"))
            {
                //AppendMessage(text);
                int playersSize = obj.size;
                updateListPlayer(playersSize, obj);
            }
            else if (type.Equals("GAME_START"))
            {
                AppendMessage(timeFormated() + " Trò chơi sẽ bắt trong 3 giây.");
                string urlCard1 = obj.data.img1;
                string urlCard2 = obj.data.img2;
                string urlCard3 = obj.data.img3;

                InsertImgFromUrl(urlCard1);
                InsertImgFromUrl(urlCard2);
                InsertImgFromUrl(urlCard3);
                InsertImgFromUrl("https://i.imgur.com/Z5bWBwD.png");

                cooldown = 3;

                countdownTimer = new System.Windows.Forms.Timer();
                countdownTimer.Interval = 1000;
                countdownTimer.Tick += CountdownHandlerAsync;
                countdownTimer.Start();
            }
            else if (type.Equals("DISCONNECT"))
            {
                if (isGameStart)
                {
                    if (showingClosing)
                    {
                        MessageBox.Show("Huỷ trận, vì có người đã thoát ra trong lúc chơi.", "Có lỗi!!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        showingClosing = false;
                    }

                    try
                    {
                        this.Close();
                    }
                    catch (Exception ex) { }

                    string getInGamePlayersParams = @"
                    {
                        ""type"": ""GET_IN_GAME_PLAYERS"",
                        ""name"": """ + clientUserName + @"""
                    }";

                    sendMessage(getInGamePlayersParams);
                }
                else if (isEndGameOut)
                {
                    try
                    {
                        _clientSocket.Close();
                    }
                    catch { }
                    this.Close();
                }
                else
                {
                    string getInGamePlayersParams = @"
                    {
                        ""type"": ""GET_IN_GAME_PLAYERS"",
                        ""name"": """ + clientUserName + @"""
                    }";

                    string playername = obj.name;

                    AppendMessage(timeFormated() + " " + playername + " đã thoát.");
                    sendMessage(getInGamePlayersParams);
                }
            }
            else if (type.Equals("SET_PLAYER_TURN"))
            {
                checkingFlippingCard = false;
                cardFlipping.Clear();
                phaseLabel.Visible = true;
                string phasePlayer = obj.name;
                string phasePlayerIP = obj.client.ip;
                int phasePlayerPort = obj.client.port;

                IPEndPoint selfEP = _clientSocket.LocalEndPoint as IPEndPoint;
                string selfIP = selfEP.Address.ToString();
                int selfPort = selfEP.Port;

                bool isSelf = selfIP == phasePlayerIP && phasePlayerPort == selfPort && phasePlayer == clientUserName;
                AppendMessage(timeFormated() + " Đã đến lượt của " + (isSelf ? "bạn" : "người chơi: " + phasePlayer) + ".");
                isMyTurn = isSelf;
                phaseLabel.Text = "Lượt của: " + (isSelf ? phasePlayer + " (bạn)" : phasePlayer);
            }
            else if (type.Equals("ERR_FLIP_CARD"))
            {
                if (isMyTurn) checkingFlippingCard = false;
            }
            else if (type.Equals("SUCCESS_FLIP_CARD"))
            {
                int slot = obj.slot;
                string urlImg = obj.urlImg;
                string playername = obj.name;

                if (isMyTurn)
                {
                    cardFlipping.Add(slot);
                    AppendMessage(timeFormated() + " Bạn đã lật thẻ vị trí " + slot + " thành công.");
                }
                else
                {
                    AppendMessage(timeFormated() + " " + playername + " đã lật thẻ vị trí " + slot + " thành công.");
                }

                await LoadImageFromUrlAsync(urlImg, slot == 1 ? pictureBox1 : slot == 2 ? pictureBox2 : slot == 3 ? pictureBox3 : slot == 4 ? pictureBox4 : slot == 5 ? pictureBox5 : pictureBox6);

                if (cardFlipping.Count == 2)
                {
                    string check2cardObj = @"
                    {
                        ""err"": { 
                            ""code"": 1,
                            ""desc"": ""So sánh 2 thẻ""
                        },
                        ""type"": ""COMPARE_2_CARDS"",
                        ""name"": """ + clientUserName + @"""
                    }";

                    AppendMessage(timeFormated() + " Đang so sánh 2 thẻ.");
                    sendMessage(check2cardObj);
                }
                else
                {
                    checkingFlippingCard = false;
                }
            }
            else if (type.Equals("DIF_COMP_CARD") || type.Equals("SAME_COMP_CARD"))
            {
                string playername = obj.name;
                if (type.Equals("DIF_COMP_CARD"))
                {
                    AppendMessage(timeFormated() + " " + playername + " đã mất lượt do mở 2 thẻ khác nhau.");
                }
                else
                {
                    AppendMessage(timeFormated() + " " + playername + " đã được +10 điểm và được tiếp tục lượt chơi.");
                }

                cardFlipping.Clear();

                int sizeRest = obj.size_rest;

                for (int i = 0; i < 6; i++) CardHandle[i] = true;

                for (int i = 0; i < sizeRest; i++)
                {
                    int slot = obj.rest[i];
                    CardHandle[slot] = false;
                }
                List<Task> imageTasks = new List<Task>();
                List<PictureBox> pictureBoxes = new List<PictureBox>();
                checkingFlippingCard = false;
                for (int i = 0; i < 6; i++)
                {
                    if (CardHandle[i] == false)
                    {
                        pictureBoxes.Add(i == 0 ? pictureBox1 : i == 1 ? pictureBox2 : i == 2 ? pictureBox3 : i == 3 ? pictureBox4 : i == 4 ? pictureBox5 : pictureBox6);
                    }
                }

                foreach (PictureBox pictureBox in pictureBoxes)
                {
                    imageTasks.Add(LoadImageFromUrlAsync("https://i.imgur.com/Z5bWBwD.png", pictureBox));
                }

                await Task.WhenAll(imageTasks);
                checkingFlippingCard = false;
            }
            else if (type.Equals("END_GAME"))
            {
                isGameStart = false;
                isInGame = false;
                isEndGameOut = true;

                CardHandle = new List<bool>()
                {
                    false, false, false, false, false, false,
                };

                string winner = obj.winner;
                int points = obj.points;
                AppendMessage(timeFormated() + " Đã mở hết thẻ, trò chơi kết thúc.");
                DialogResult diaNotify = MessageBox.Show("Người thắng cuộc: " + winner + "\nTổng điểm: " + points, "Trò chơi kết thúc!!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine(diaNotify.ToString());
                if (diaNotify.ToString() == "OK")
                {
                    DialogResult diaRes = MessageBox.Show("Bạn có muốn chơi lại?", "Trúc xanh", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    Console.WriteLine(diaRes.ToString());
                    if (diaRes.ToString() == "Yes")
                    {
                        IPEndPoint clientEndPoint = _clientSocket.LocalEndPoint as IPEndPoint;
                        string clientIP = clientEndPoint.Address.ToString();
                        int clientPort = clientEndPoint.Port;

                        string connObj = @"
                        {
                            ""type"": ""JOIN"",
                            ""client"": {
                                ""ip"": """ + clientIP + @""",
                                ""port"": """ + clientPort + @"""
                            },
                            ""name"": """ + clientUserName + @"""
                        }";

                        sendMessage(connObj.Replace(" ", ""));
                    }
                    else
                    {
                        Close();
                    }
                }
            }
        }

        private string timeFormated()
        {
            TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            DateTimeOffset vietnamNow = utcNow.ToOffset(vietnamTimeZone.GetUtcOffset(utcNow));
            string formattedDateTime = vietnamNow.ToString("dd/MM/yyyy HH:mm:ss");
            return "[" + formattedDateTime + "]";
        }

        private void updateListPlayer(int playersSize, dynamic obj)
        {
            if (this.InvokeRequired)
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action<int, object>(updateListPlayer), new object[] { playersSize, obj });
                }
                return;
            }

            pRank1.Visible = false;
            pRank2.Visible = false;
            pRank3.Visible = false;

            titleLabel.Visible = !isGameStart;

            for (int i = 0; i < playersSize; i++)
            {
                string plname = obj.data[i].name;
                int plorder = obj.data[i].order;
                int plpoints = obj.data[i].point;

                bool isSelf = plname == clientUserName;

                switch (i)
                {
                    case 0:
                        {
                            pRank1.Text = "1. " + (isSelf ? plname + " (Bạn)" : plname) + " - " + plpoints;
                            pRank1.Visible = true;
                        }
                        break;
                    case 1:
                        {
                            pRank2.Text = "2. " + (isSelf ? plname + " (Bạn)" : plname) + " - " + plpoints;
                            pRank2.Visible = true;
                        }
                        break;
                    case 2:
                        {
                            pRank3.Text = "3. " + (isSelf ? plname + " (Bạn)" : plname) + " - " + plpoints;
                            pRank3.Visible = true;
                        }
                        break;
                }
            }
        }

        private int cooldown = 3, cooldown2 = 1;
        private System.Windows.Forms.Timer countdownTimer, upTheTimer;

        private async void CountdownHandlerAsync(object sender, EventArgs e)
        {
            if (cooldown > 0)
            {
                titleLabel.Text = cooldown.ToString();
                cooldown--;
            }
            else
            {
                countdownTimer.Stop();
                titleLabel.Text = "Bắt đầu!";
                isGameStart = true;
                pictureBox1.Visible = true;
                pictureBox2.Visible = true;
                pictureBox3.Visible = true;
                pictureBox4.Visible = true;
                pictureBox5.Visible = true;
                pictureBox6.Visible = true;

                List<Task> imageTasks = new List<Task>();

                foreach (PictureBox pictureBox in new List<PictureBox> { pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5, pictureBox6 })
                {
                    imageTasks.Add(LoadImageFromUrlAsync("https://i.imgur.com/Z5bWBwD.png", pictureBox));
                }

                await Task.WhenAll(imageTasks);

                titleLabel.Visible = false;
            }
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_clientSocket != null && _clientSocket.Connected)
            {
                try
                {
                    if (lostConnect)
                    {
                        _clientSocket.Shutdown(SocketShutdown.Both);
                        _clientSocket.Close();
                        return;
                    }

                    if (isEndGameOut)
                    {
                        string disconnectText = @"
                            {
                                ""err"": { 
                                    ""code"": 1,
                                    ""desc"": ""Người chơi ngắt kết nối""
                                },
                                ""type"": ""DISCONNECT_BY_END_GAME"",
                                ""name"": """ + clientUserName + @"""
                            }";
                        sendMessage(disconnectText);
                    }
                    else
                    {
                        isInGame = false;
                        string disconnectText = @"
                            {
                                ""err"": { 
                                    ""code"": 1,
                                    ""desc"": ""Người chơi ngắt kết nối""
                                },
                                ""type"": ""DISCONNECT"",
                                ""name"": """ + clientUserName + @"""
                            }";
                        sendMessage(disconnectText);
                    }

                    _clientSocket.Shutdown(SocketShutdown.Both);
                    _clientSocket.Close();
                }
                catch
                {
                    // Xử lý ngoại lệ nếu cần
                }
            }
        }

        private void requestOpenCard(int slot)
        {
            if (isMyTurn && !checkingFlippingCard && !CardHandle[slot - 1])
            {
                string flipCardObj = @"
                    {
                        ""err"": { 
                            ""code"": 1,
                            ""desc"": ""Lật thẻ""
                        },
                        ""type"": ""FLIP_CARD"",
                        ""slot"": " + slot + @",
                        ""name"": """ + clientUserName + @"""
                    }";
                sendMessage(flipCardObj);
                checkingFlippingCard = true;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            requestOpenCard(1);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            requestOpenCard(2);
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            requestOpenCard(3);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            requestOpenCard(4);
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            requestOpenCard(5);
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            requestOpenCard(6);
        }

        private async Task LoadImageFromUrlAsync(string url, PictureBox pictureBox)
        {
            if (!ImgCards.ContainsKey(url))
            {
                await InsertImgFromUrl(url);
            }

            Image image = ImgCards[url];
            pictureBox.Image = new Bitmap(image);
        }

        private async Task InsertImgFromUrl(string url)
        {
            if (!ImgCards.ContainsKey(url))
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        byte[] imageData = await response.Content.ReadAsByteArrayAsync();
                        using (MemoryStream memoryStream = new MemoryStream(imageData))
                        {
                            Image image = Image.FromStream(memoryStream);
                            ImgCards.Add(url, image);
                        }
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show("Không thể tải ảnh từ URL. Lỗi: " + ex.Message);
                    }
                }
            }
        }

        private void initForm()
        {
            this.label1 = new Label();
            this.textBox1 = new TextBox();
            this.connectBtn = new Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new Font("Arial", 10F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new Point(12, 50);
            this.label1.Name = "label1";
            this.label1.Size = new Size(117, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Tên người chơi:";
            // 
            // textBox1
            // 
            this.textBox1.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new Point(135, 47);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Size(187, 23);
            this.textBox1.TabIndex = 1;
            // 
            // connectBtn
            // 
            this.connectBtn.Cursor = Cursors.Hand;
            this.connectBtn.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.connectBtn.Location = new Point(104, 142);
            this.connectBtn.Name = "connectBtn";
            this.connectBtn.Size = new Size(123, 32);
            this.connectBtn.TabIndex = 2;
            this.connectBtn.Text = "THAM GIA";
            this.connectBtn.UseVisualStyleBackColor = true;
            this.connectBtn.Click += new System.EventHandler(this.connectBtn_Click);
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(334, 219);
            this.Controls.Add(this.connectBtn);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ClientForm";
            this.Text = "Client - Trò chơi Trúc Xanh";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void renderForm()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(renderForm));
                return;
            }

            this.label1.Visible = false;
            this.textBox1.Visible = false;
            this.connectBtn.Visible = false;

            if (groupBox1 != null)
            {
                this.titleLabel.Text = "Đang chờ thêm người chơi kết nối...";
                this.titleLabel.Visible = true;
                this.phaseLabel.Visible = false;
                this.pictureBox6.Visible = false;
                this.pictureBox3.Visible = false;
                this.pictureBox5.Visible = false;
                this.pictureBox4.Visible = false;
                this.pictureBox2.Visible = false;
                this.pictureBox1.Visible = false;
            }
            else
            {
                this.groupBox1 = new GroupBox();
                this.pRank3 = new Label();
                this.pRank2 = new Label();
                this.pRank1 = new Label();
                this.groupBox2 = new GroupBox();
                this.phaseLabel = new Label();
                this.titleLabel = new Label();
                this.pictureBox6 = new PictureBox();
                this.pictureBox3 = new PictureBox();
                this.pictureBox5 = new PictureBox();
                this.pictureBox4 = new PictureBox();
                this.pictureBox2 = new PictureBox();
                this.pictureBox1 = new PictureBox();
                this.groupBox3 = new GroupBox();
                this.logBox = new TextBox();
                this.groupBox1.SuspendLayout();
                this.groupBox2.SuspendLayout();
                ((ISupportInitialize)(this.pictureBox6)).BeginInit();
                ((ISupportInitialize)(this.pictureBox3)).BeginInit();
                ((ISupportInitialize)(this.pictureBox5)).BeginInit();
                ((ISupportInitialize)(this.pictureBox4)).BeginInit();
                ((ISupportInitialize)(this.pictureBox2)).BeginInit();
                ((ISupportInitialize)(this.pictureBox1)).BeginInit();
                this.groupBox3.SuspendLayout();
            }
            this.SuspendLayout();

            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pRank3);
            this.groupBox1.Controls.Add(this.pRank2);
            this.groupBox1.Controls.Add(this.pRank1);
            this.groupBox1.Location = new Point(381, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(241, 102);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Danh sách người chơi";
            // 
            // pRank3
            // 
            this.pRank3.AutoSize = true;
            this.pRank3.Location = new Point(15, 71);
            this.pRank3.Name = "pRank3";
            this.pRank3.Size = new Size(46, 17);
            this.pRank3.TabIndex = 2;
            this.pRank3.Text = "label4";
            this.pRank3.Visible = false;
            // 
            // pRank2
            // 
            this.pRank2.AutoSize = true;
            this.pRank2.Location = new Point(15, 49);
            this.pRank2.Name = "pRank2";
            this.pRank2.Size = new Size(46, 17);
            this.pRank2.TabIndex = 1;
            this.pRank2.Text = "label3";
            this.pRank2.Visible = false;
            // 
            // pRank1
            // 
            this.pRank1.AutoSize = true;
            this.pRank1.Location = new Point(15, 27);
            this.pRank1.Name = "pRank1";
            this.pRank1.Size = new Size(46, 17);
            this.pRank1.TabIndex = 0;
            this.pRank1.Text = "label2";
            this.pRank1.Visible = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.phaseLabel);
            this.groupBox2.Controls.Add(this.titleLabel);
            this.groupBox2.Controls.Add(this.pictureBox6);
            this.groupBox2.Controls.Add(this.pictureBox3);
            this.groupBox2.Controls.Add(this.pictureBox5);
            this.groupBox2.Controls.Add(this.pictureBox4);
            this.groupBox2.Controls.Add(this.pictureBox2);
            this.groupBox2.Controls.Add(this.pictureBox1);
            this.groupBox2.Location = new Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new Size(363, 344);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            // 
            // phaseLabel
            // 
            this.phaseLabel.Font = new Font("Arial", 14F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.phaseLabel.Location = new Point(6, 27);
            this.phaseLabel.Name = "phaseLabel";
            this.phaseLabel.Size = new Size(351, 22);
            this.phaseLabel.TabIndex = 7;
            this.phaseLabel.Text = "Lượt của:";
            this.phaseLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.phaseLabel.Visible = false;
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new Font("Arial", 14F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new Point(10, 173);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new Size(351, 22);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Đang chờ thêm người chơi kết nối...";
            this.titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.titleLabel.Visible = false;
            // 
            // pictureBox6
            // 
            this.pictureBox6.Cursor = Cursors.Hand;
            this.pictureBox6.Location = new Point(245, 197);
            this.pictureBox6.Name = "pictureBox6";
            this.pictureBox6.Size = new Size(93, 99);
            this.pictureBox6.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox6.TabIndex = 6;
            this.pictureBox6.TabStop = false;
            this.pictureBox6.Visible = false;
            this.pictureBox6.Click += new EventHandler(this.pictureBox6_Click);
            // 
            // pictureBox3
            // 
            this.pictureBox3.Cursor = Cursors.Hand;
            this.pictureBox3.Location = new Point(245, 85);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new Size(93, 99);
            this.pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 5;
            this.pictureBox3.TabStop = false;
            this.pictureBox3.Visible = false;
            this.pictureBox3.Click += new EventHandler(this.pictureBox3_Click);
            // 
            // pictureBox5
            // 
            this.pictureBox5.Cursor = Cursors.Hand;
            this.pictureBox5.Location = new Point(135, 197);
            this.pictureBox5.Name = "pictureBox5";
            this.pictureBox5.Size = new Size(93, 99);
            this.pictureBox5.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox5.TabIndex = 4;
            this.pictureBox5.TabStop = false;
            this.pictureBox5.Visible = false;
            this.pictureBox5.Click += new EventHandler(this.pictureBox5_Click);
            // 
            // pictureBox4
            // 
            this.pictureBox4.Cursor = Cursors.Hand;
            this.pictureBox4.Location = new Point(26, 197);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new Size(93, 99);
            this.pictureBox4.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox4.TabIndex = 3;
            this.pictureBox4.TabStop = false;
            this.pictureBox4.Visible = false;
            this.pictureBox4.Click += new EventHandler(this.pictureBox4_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Cursor = Cursors.Hand;
            this.pictureBox2.Location = new Point(135, 85);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new Size(93, 99);
            this.pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 2;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Visible = false;
            this.pictureBox2.Click += new EventHandler(this.pictureBox2_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Cursor = Cursors.Hand;
            this.pictureBox1.Location = new Point(26, 85);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Size(93, 99);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Visible = false;
            this.pictureBox1.Click += new EventHandler(this.pictureBox1_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.logBox);
            this.groupBox3.Location = new Point(381, 120);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new Size(241, 236);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Logs";
            // 
            // logBox
            // 
            this.logBox.Location = new Point(6, 19);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = ScrollBars.Vertical;
            this.logBox.Size = new Size(229, 211);
            this.logBox.TabIndex = 0;
            // 
            // ClientForm
            // 
            this.ClientSize = new Size(633, 367);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "ClientForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            ((ISupportInitialize)(this.pictureBox6)).EndInit();
            ((ISupportInitialize)(this.pictureBox3)).EndInit();
            ((ISupportInitialize)(this.pictureBox5)).EndInit();
            ((ISupportInitialize)(this.pictureBox4)).EndInit();
            ((ISupportInitialize)(this.pictureBox2)).EndInit();
            ((ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}

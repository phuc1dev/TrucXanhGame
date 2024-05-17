using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;

namespace TrucXanhServer
{
    public partial class ServerForm : Form
    {
        private GroupBox groupBox1;
        private Button startBtn;
        private GroupBox groupBox2;
        private TextBox textBox1;
        private Label pRank1;
        private Label pRank3;
        private Label pRank2;
        private Label ipLabel;
        private Label portLabel;

        public ServerForm()
        {
            InitializeComponent();
            pRank1.Visible = false;
            pRank2.Visible = false;
            pRank3.Visible = false;

            this.ActiveControl = pRank1;
        }

        private string currentPlayer = "";

        private List<string> urlCardList = new List<string>() {
            "https://i.imgur.com/Md7gkrk.jpg",
            "https://i.imgur.com/1gzzps2.jpg",
            "https://i.imgur.com/BQXv914.jpg"
        };

        private static List<CardManager> playerCardFlip = new List<CardManager>();
        private static List<CardManager> cardManagers = new List<CardManager>();
        private static List<ClientManager> clientManagers = new List<ClientManager>();
        private static List<Socket> _clients = new List<Socket>();
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private byte[] _buffer = new byte[1024];
        
        private bool isServerRunning = false;
        private bool isGameStart = false;

        private int port = 2010;

        private int currentPhase = 0;

        private void startBtn_Click(object sender, EventArgs e)
        {
            this.ActiveControl = pRank1;
            startBtn.Enabled = false;
            startServer();
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            this.ActiveControl = pRank1;
        }

        private void ServerForm_Load(object sender, EventArgs e)
        {
            WebClient client = new WebClient();
            byte[] data = client.DownloadData("https://i.imgur.com/B2MkYSI.png");

            // Lưu ảnh vào một tệp trên máy tính
            string filePath = Path.Combine(Path.GetTempPath(), "iconServer.ico");
            File.WriteAllBytes(filePath, data);

            // Đặt icon cho form
            Icon icon = Icon.FromHandle(new Bitmap(filePath).GetHicon());
            this.Icon = icon;

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipLabel.Text += ip.ToString();
                }
            }

            portLabel.Text += "2010";
        }

        private void startServer()
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            _serverSocket.Listen(5);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            isServerRunning = true;
            AppendMessage(timeFormated() + " Server đã khởi động và đang chờ kết nối...");
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            if (!isServerRunning)
            {
                return;
            }

            Socket clientSocket = _serverSocket.EndAccept(ar);
            _clients.Add(clientSocket);

            SocketState state = new SocketState(clientSocket);
            byte[] buffer = new byte[1024];
            state.Buffer = buffer;

            clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);

            IPEndPoint clientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
            string clientIP = clientEndPoint.Address.ToString();
            int clientPort = clientEndPoint.Port;

            //client ket noi

            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

        }

        private void BroadcastMessage(Socket sender, string message)
        {
            foreach (ClientManager cm in clientManagers)
            {
                Socket client = cm.clientSocket;
                if (client != sender)
                {
                    try
                    {
                        client.Send(Encoding.UTF8.GetBytes(message));
                    }
                    catch
                    {
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        _clients.Remove(client);
                        clientManagers.Remove(cm);

                        updateRankBoard();
                        //client ngat ket noi
                    }
                }
            }
        }

        private void msgToClient(Socket client, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                client.Send(Encoding.UTF8.GetBytes(message));
            }
        }

        private void AppendMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendMessage), new object[] { message });
                return;
            }
            textBox1.AppendText(message + Environment.NewLine);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            SocketState state = (SocketState)ar.AsyncState;
            Socket clientSocket = state.ClientSocket;
            int received = 0;

            try
            {
                received = clientSocket.EndReceive(ar);
            }
            catch (SocketException)
            {
                clientManagers.Remove(clientManagers.Find(c => c.clientSocket == clientSocket));
                clientSocket.Close();
                _clients.Remove(clientSocket);
                updateRankBoard();
                return;
            }

            byte[] dataBuffer = new byte[received];
            Array.Copy(state.Buffer, 0, dataBuffer, 0, received);

            string text = Encoding.UTF8.GetString(dataBuffer);

            if (text != null && text != "")
            {
                dynamic obj = JsonConvert.DeserializeObject(text);
                string type = obj.type;

                if (type.Equals("DISCONNECT"))
                {
                    string name = obj.name;
                    clientManagers.Remove(clientManagers.Find(c => c.name == name));
                    if (isGameStart)
                    {
                        if (clientManagers.Count == 1)
                        {
                            isGameStart = false;
                            currentPhase = 0;
                            AppendMessage(timeFormated() + " Tất cả người chơi đã ngắt kết nối, đã reset lại trò chơi!");
                        }
                        else
                        {
                            AppendMessage(timeFormated() + " " + name + " đã ngắt kết nối trò chơi.");
                        }
                    }

                    clientSocket.Close();
                    _clients.Remove(clientSocket);
                    updateRankBoard();
                    BroadcastMessage(_serverSocket, text);
                    return;
                }
                else if (type.Equals("DISCONNECT_BY_END_GAME"))
                {
                    string name = obj.name;
                    clientManagers.Remove(clientManagers.Find(c => c.name == name));

                    clientSocket.Close();
                    _clients.Remove(clientSocket);
                    updateRankBoard();
                    AppendMessage(timeFormated() + " " + name + " đã ngắt kết nối vì trò chơi kết thúc.");
                    return;
                }

                messageHandle(clientSocket, text);
            }

            byte[] buffer = new byte[1024];
            state.Buffer = buffer;
            clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
        }

        public static bool checkUsername(string text)
        {
            string pattern = @"^[a-zA-Z0-9]{6,10}$";
            return Regex.IsMatch(text, pattern);
        }

        private void messageHandle(Socket client, string text)
        {
            dynamic obj = JsonConvert.DeserializeObject(text);
            string type = obj.type;

            switch (type)
            {
                case "JOIN":
                    {
                        string nameClient = obj.name;
                        if (checkUsername(nameClient))
                        {
                            if (!isGameStart) {
                                if (clientManagers.Count < 3)
                                {
                                    if (!clientManagers.Exists(c => c.name == nameClient))
                                    {
                                        clientManagers.Add(new ClientManager(client, nameClient, 0, clientManagers.Count + 1));

                                        string returnObj = @"
                                            {
                                              ""err"": { 
                                                ""code"": 1,
                                                ""desc"": ""Đã kết nối với tên " + nameClient + @".""
                                              },
                                              ""msgBox"": {
                                                ""title"": ""Thành công!!!"",
                                                ""text"": ""Chào mừng người chơi: " + nameClient + @".""
                                              },
                                              ""type"": ""JOIN"",
                                              ""name"": """ + nameClient + @"""
                                            }";
                                        msgToClient(client, returnObj);
                                        AppendMessage(timeFormated() + " " + nameClient + " đã kết nối với máy chủ.");
                                        updateRankBoard();

                                        List<ClientManager> clientManagerSort = sapXepRank(clientManagers);
                                        BroadcastMessage(_serverSocket, msgListPlayers(clientManagerSort));

                                        if (clientManagers.Count == 3)
                                        {
                                            AppendMessage(timeFormated() + " Đã đủ 3 người chơi kết nối, bắt đầu đếm ngược.");
                                            new System.Threading.Timer(startCooldownGame, null, 2000, Timeout.Infinite);
                                        }
                                    }
                                    else
                                    {
                                        string returnObj = @"
                                            {
                                                ""err"": { 
                                                    ""code"": -3,
                                                    ""desc"": ""Tên đã tồn tại.""
                                                },
                                                ""msgBox"": {
                                                    ""title"": ""Tên không phù hợp!!!"",
                                                    ""text"": ""Tên đã được người khác sử dụng, vui lòng chọn tên khác.""
                                                },
                                                ""type"": ""JOIN""
                                            }";
                                        msgToClient(client, returnObj);
                                        AppendMessage(timeFormated() + " Người chơi kết nối thất bại do sử dụng tên đã tồn tại.");
                                    }
                                }
                                else
                                {
                                    string returnObj = @"
                                        {
                                            ""err"": { 
                                                ""code"": -2,
                                                ""desc"": ""Số lượng người chơi đã đủ.""
                                            },
                                            ""msgBox"": {
                                                ""title"": ""Không thể kết nối!!!"",
                                                ""text"": ""Số lượng người chơi đã đủ, vui lòng quay trở lại sau.""
                                            },
                                            ""type"": ""JOIN""
                                        }";
                                    AppendMessage(timeFormated() + " Người chơi kết nối thất bại do số lượng đã đủ.");
                                    msgToClient(client, returnObj);
                                }
                            } else
                            {
                                string returnObj = @"
                                    {
                                        ""err"": { 
                                            ""code"": -4,
                                            ""desc"": ""Trò chơi đã bắt đầu, không thể kết nối.""
                                        },
                                        ""msgBox"": {
                                            ""title"": ""Không thể kết nối!!!"",
                                            ""text"": ""Trò chơi đã bắt đầu rồi, vui lòng quay trở lại sau.""
                                        },
                                        ""type"": ""JOIN""
                                    }";
                                AppendMessage(timeFormated() + " Người chơi kết nối thất bại do trò chơi đã bắt đầu.");
                                msgToClient(client, returnObj);
                            }
                        }
                        else
                        {
                            string returnObj = @"
                                {
                                    ""err"": { 
                                        ""code"": -1,
                                        ""desc"": ""Tên đã tồn tại.""
                                    },
                                    ""msgBox"": {
                                        ""title"": ""Tên không phù hợp!!!"",
                                        ""text"": ""Vui lòng đặt tên theo quy tắc:\n- Chỉ chứa các ký tự chữ và số\n- Ít nhất 6 ký tự và nhiều nhất 10 ký tự""
                                    },
                                    ""type"": ""JOIN""
                                }";
                            AppendMessage(timeFormated() + " Người chơi kết nối thất bại do sử dụng tên không đúng yêu cầu.");
                            msgToClient(client, returnObj);
                        }
                    }
                    break;
                case "GET_IN_GAME_PLAYERS":
                    {
                        List<ClientManager> clientManagerSort = sapXepRank(clientManagers);
                        msgToClient(client, msgListPlayers(clientManagerSort));
                        updateRankBoard();
                        string playername = obj.name;
                        AppendMessage(timeFormated() + " " + playername + " đã yêu cầu lấy danh sách người chơi.");
                    }
                    break;
                case "FLIP_CARD":
                    {
                        int slot = obj.slot;
                        string playername = obj.name;

                        AppendMessage(timeFormated() + " " + playername + " đã yêu cầu lật thẻ vị trí số " + (slot + 1) + ".");

                        if (playerCardFlip.Count < 2)
                        {
                            if (playerCardFlip.Exists(c => c.slot == slot - 1))
                            {
                                string errorObj = @"
                                {
                                    ""err"": { 
                                        ""code"": 1,
                                        ""desc"": ""Không thể lật thẻ vì vị trí đã được lật lên.""
                                    },
                                    ""type"": ""ERR_FLIP_CARD""
                                }";
                                AppendMessage(timeFormated() + " " + playername + " lật thẻ thất bại do vị trí trùng với thẻ vừa chọn.");
                                BroadcastMessage(_serverSocket, errorObj);
                            } else if (cardManagers[slot - 1].open)
                            {
                                string errorObj = @"
                                {
                                    ""err"": { 
                                        ""code"": 1,
                                        ""desc"": ""Không thể lật thẻ vì vị trí đã được lật lên.""
                                    },
                                    ""type"": ""ERR_FLIP_CARD""
                                }";
                                AppendMessage(timeFormated() + " " + playername + " lật thẻ thất bại do thẻ đã được tìm ra trước đó.");
                                BroadcastMessage(_serverSocket, errorObj);
                            } else
                            {
                                CardManager card = cardManagers[slot - 1];
                                playerCardFlip.Add(card);

                                string successObj = @"
                                {
                                    ""err"": { 
                                        ""code"": 1,
                                        ""desc"": ""Lật thẻ thành công.""
                                    },
                                    ""slot"": " + slot + @",
                                    ""urlImg"": """ + card.urlImg + @""",
                                    ""name"": """ + playername + @""",
                                    ""type"": ""SUCCESS_FLIP_CARD""
                                }";

                                AppendMessage(timeFormated() + " " + playername + " lật thẻ thành công vị trí số " + (slot + 1) + ".");
                                BroadcastMessage(_serverSocket, successObj);
                            }
                        } else
                        {
                            string errorObj = @"
                                {
                                    ""err"": { 
                                        ""code"": 1,
                                        ""desc"": ""Chỉ được mở 2 thẻ khác nhau trong 1 lượt.""
                                    },
                                    ""type"": ""ERR_FLIP_CARD""
                                }";
                            AppendMessage(timeFormated() + " " + playername + " lật thẻ thất bại do vượt quá lượt mở.");
                            BroadcastMessage(_serverSocket, errorObj);
                        }
                    }
                    break;
                case "COMPARE_2_CARDS":
                    {
                        string playername = obj.name;
                        currentPlayer = playername;
                        new System.Threading.Timer(sendCheckCards, null, 1200, Timeout.Infinite);
                        
                    }
                    break;
            }
        }

        private bool check2cards(CardManager c1, CardManager c2)
        {
            return c1.data == c2.data;
        }

        private void startCooldownGame(object state)
        {
            string startGameObj = @"
            {
                ""err"": { 
                    ""code"": 1,
                    ""desc"": ""Bắt đầu trò chơi""
                },
                ""cooldown"": 3,
                ""data"": {
                    ""img1"": """ + urlCardList[0] + @""",
                    ""img2"": """ + urlCardList[1] + @""",
                    ""img3"": """ + urlCardList[2] + @""",
                },
                ""type"": ""GAME_START""
            }";

            BroadcastMessage(_serverSocket, startGameObj);
            isGameStart = true;
            initRandomCards();
            new System.Threading.Timer(setFirstPhaseWhenStart, null, 4500, Timeout.Infinite);
            if (state != null)
            {
                System.Threading.Timer timer = (System.Threading.Timer)state;
                timer.Dispose();
            }
        }

        private void sendCheckCards(object state)
        {
            string playername = currentPlayer;
            AppendMessage(timeFormated() + " " + playername + " đã lật xong 2 thẻ và yêu cầu so sánh.");
            if (playerCardFlip.Count < 2)
            {
                string errorObj = @"
                                {
                                    ""err"": { 
                                        ""code"": 1,
                                        ""desc"": ""Chưa mở đủ thẻ để so sánh.""
                                    },
                                    ""type"": ""ERR_COMP_CARD""
                                }";
                AppendMessage(timeFormated() + " " + playername + " yêu cầu so sánh thẻ trong khi chưa lật đủ 2 thẻ.");
                BroadcastMessage(_serverSocket, errorObj);
            }
            else
            {
                if (check2cards(playerCardFlip[0], playerCardFlip[1]))
                {
                    cardManagers[playerCardFlip[0].slot].open = true;
                    cardManagers[playerCardFlip[1].slot].open = true;

                    int[] listSlotRest = cardManagers.Where(c => !c.open).Select(c => c.slot).ToArray();
                    string rest = string.Join(", ", listSlotRest);

                    string sameObj = @"
                                {
                                    ""err"": { 
                                        ""code"": 1,
                                        ""desc"": ""Kết quả so sánh là GIỐNG NHAU.""
                                    },
                                    ""slot"": [" + playerCardFlip[0].slot + @", " + playerCardFlip[1].slot + @"],
                                    ""size_rest"": " + listSlotRest.Length + @",
                                    ""rest"": [" + rest + @"],
                                    ""name"": """ + playername + @""",
                                    ""type"": ""SAME_COMP_CARD""
                                }";

                    clientManagers[currentPhase].points += 10;
                    AppendMessage(timeFormated() + " " + playername + " đã đoán đúng, được cộng 10 điểm và tiếp tục lượt chơi.");
                    BroadcastMessage(_serverSocket, sameObj);
                }
                else
                {
                    int[] listSlotRest = cardManagers.Where(c => !c.open).Select(c => c.slot).ToArray();
                    string rest = string.Join(", ", listSlotRest);

                    string errorObj = @"
                                {
                                    ""err"": { 
                                        ""code"": 1,
                                        ""desc"": ""Kết quả so sánh là KHÁC NHAU.""
                                    },
                                    ""slot"": [" + playerCardFlip[0].slot + @", " + playerCardFlip[1].slot + @"],
                                    ""size_rest"": " + listSlotRest.Length + @",
                                    ""rest"": [" + rest + @"],
                                    ""name"": """ + playername + @""",
                                    ""type"": ""DIF_COMP_CARD""
                                }";

                    AppendMessage(timeFormated() + " " + playername + " đã đoán sai và mất lượt.");
                    BroadcastMessage(_serverSocket, errorObj);

                    currentPhase++;
                    if (currentPhase > 2) currentPhase = 0;
                    //lớn hơn 2 => 3, mà game chỉ 3 người chơi => 3 sẽ out index (ngoài danh sách người chơi)
                    //nên khi đi hết lượt thì sẽ vòng lại người đầu tiên
                }

                playerCardFlip.Clear();

                //ngay day
                new System.Threading.Timer(waitAndUpdatePoint, null, 500, Timeout.Infinite);

                int restCard = cardManagers.Where(c => !c.open).Select(c => c.slot).ToArray().Length;

                if (restCard == 0)
                {
                    ClientManager winner = clientManagers.OrderByDescending(c => c.points).FirstOrDefault();

                    isGameStart = false;
                    currentPhase = 0;
                    playerCardFlip.Clear();

                    string endObj = @"
                                    {
                                        ""err"": { 
                                            ""code"": 1,
                                            ""desc"": ""Trò chơi kết thúc, đã mở hết các thẻ""
                                        },
                                        ""winner"": """ + winner.name + @""",
                                        ""points"": """ + winner.points + @""",
                                        ""type"": ""END_GAME""
                                    }";

                    AppendMessage(timeFormated() + " Trò chơi kết thúc do thẻ đã được mở hết, chiến thắng thuộc về " + winner.name + " với số điểm " + winner.points + ".");
                    BroadcastMessage(_serverSocket, endObj);
                    clientManagers.Clear();
                }
                else
                {
                    AppendMessage(timeFormated() + "  Đã chuyển lượt cho: " + clientManagers[currentPhase].name + ".");
                    setPhase(currentPhase);
                }
            }
            if (state != null)
            {
                System.Threading.Timer timer = (System.Threading.Timer)state;
                timer.Dispose();
            }
        }

        private void setFirstPhaseWhenStart(object state)
        {
            AppendMessage(timeFormated() + "Trò chơi bắt đầu, lượt đầu tiên thuộc về " + clientManagers[currentPhase].name + ".");
            setPhase(currentPhase);
            if (state != null)
            {
                System.Threading.Timer timer = (System.Threading.Timer)state;
                timer.Dispose();
            }
        }

        private void waitAndUpdatePoint(object state)
        {
            List<ClientManager> clientManagerSort = sapXepRank(clientManagers);
            //ngay day
            BroadcastMessage(_serverSocket, msgListPlayers(clientManagerSort));

            if (state != null)
            {
                System.Threading.Timer timer = (System.Threading.Timer)state;
                timer.Dispose();
            }
        }


        private void setPhase(int order)
        {
            playerCardFlip.Clear();

            ClientManager cm = clientManagers[order];
            Socket clt = cm.clientSocket;

            IPEndPoint cltEP = clt.RemoteEndPoint as IPEndPoint;
            string cltIP = cltEP.Address.ToString();
            int cltPort = cltEP.Port;

            string phaseObj = @"
            {
                ""err"": { 
                    ""code"": 1,
                    ""desc"": ""Chỉ định lượt cho người chơi""
                },
                ""name"": """ + cm.name + @""",
                ""client"": {
                    ""ip"": """ + cltIP + @""",
                    ""port"": """ + cltPort + @"""
                },
                ""type"": ""SET_PLAYER_TURN""
            }";
            BroadcastMessage(_serverSocket, phaseObj);
        }

        private string msgListPlayers(List<ClientManager> clientManagerSort)
        {
            string data = "";

            for (int i = 0; i < clientManagerSort.Count; i++)
            {
                ClientManager clt = clientManagerSort[i];

                if (i != 0) data += ",";

                string cltname = clt.name;
                int cltorder = clt.order;
                int cltpoints = clt.points;

                data += @"{
                    ""name"": """ + cltname + @""",
                    ""order"": " + cltorder + @",
                    ""point"": " + cltpoints + @"
                }";
            }

            string returnObj = @"
                {
                    ""err"": {
                        ""code"": 1,
                        ""desc"": ""Lấy danh sách người chơi thành công.""
                    },
                    ""type"": ""GET_IN_GAME_PLAYERS"",
                    ""size"": " + clientManagerSort.Count + @",
                    ""data"": [
                        " + data + @"
                    ]
                }";
            return returnObj;
        }

        private void updateRankBoard()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(updateRankBoard));
                return;
            }

            List<ClientManager> clientManagerSort = sapXepRank(clientManagers);
            pRank1.Visible = false;
            pRank2.Visible = false;
            pRank3.Visible = false;
            for (int i = 0; i < clientManagerSort.Count; i++)
            {
                ClientManager clt = clientManagerSort[i];
                string cltname = clt.name;
                int cltorder = clt.order;
                int cltpoints = clt.points;

                switch (i)
                {
                    case 0:
                        {
                            pRank1.Text = "1. " + cltname + " - " + cltpoints;
                            pRank1.Visible = true;
                        }
                        break;
                    case 1:
                        {
                            pRank2.Text = "2. " + cltname + " - " + cltpoints;
                            pRank2.Visible = true;
                        }
                        break;
                    case 2:
                        {
                            pRank3.Text = "3. " + cltname + " - " + cltpoints;
                            pRank3.Visible = true;
                        }
                        break;
                }
            }
        }

        private List<ClientManager> sapXepRank(List<ClientManager> clManager)
        {
            List<ClientManager> copyClientManagers = new List<ClientManager>();
            copyClientManagers = clManager;

            /*copyClientManagers.Sort((x, y) =>
            {
                int pointCompare = y.points.CompareTo(x.points);
                return pointCompare == 0 ? x.order.CompareTo(y.order) : pointCompare;
            });*/

            copyClientManagers.Sort((x, y) => x.order.CompareTo(y.order));
            return copyClientManagers;
        }

        private void initRandomCards()
        {
            if (this.isGameStart)
            {
                Random r = new Random((int)DateTime.Now.Ticks);
                cardManagers.Clear();
                List<int> indexes = new List<int>();

                // có 3 thẻ, mỗi thẻ xuất hiện 2 lần => 6 thẻ cặp với nhau
                int numOfDuplicates = 2;

                for (int i = 0; i < urlCardList.Count; i++)
                {
                    for (int j = 0; j < numOfDuplicates; j++)
                    {
                        indexes.Add(i);
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    int randomIndex = r.Next(0, indexes.Count);
                    int selectedValue = indexes[randomIndex];
                    indexes.RemoveAt(randomIndex);
                    cardManagers.Add(new CardManager(urlCardList[selectedValue], selectedValue + "", false, i));
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
    }
}

using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace GameServer
{
    public class ClientState
    {
        public Socket socket;
        public string ip = "";
        public byte[] readBuff = new byte[1024];

        public string name = "";
        public int roomId = -1;

    }

    public class GameDate
    {
        public int money;
        public int color;
        public int playOrder;
        public int position;
        public bool isPoCan;
        public bool isGuaJi;
        public List<int> property = new();

        public GameDate(int money, int color, int order, int position)
        {
            this.money = money;
            this.color = color;
            this.playOrder = order;
            this.position = position;
            isPoCan = false;
            isGuaJi = false;
        }
    }

    public class House
    {
        public int id;
        public string name;
        public int price;
        public int level;
        public int maxLevel;
        public int up;
        public string playerName = "";
        /// <summary>
        /// 租金
        /// </summary>
        public int[] rent;
        public int state;
        public House(int id, string name, int maxLevel, int price, int up, int[] rent)
        {
            this.id = id;
            this.name = name;
            this.maxLevel = maxLevel;
            this.price = price;
            this.up = up;
            this.level = 0;
            this.rent = rent;
            this.state = -1;//默认为未买入
        }
    }

    /// <summary>
    /// 惊喜
    /// </summary>
    public class Treasure
    {
        public int id;
        public string name;
        public string desc;
        public Treasure(int id, string name, string desc)
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
        }
    }
    /// <summary>
    /// 命运
    /// </summary>
    public class Fate
    {
        public int id;
        public string name;
        public string desc;
        public Fate(int id, string name, string desc)
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
        }
    }

    public class Room
    {
        /// <summary>
        /// 房间号
        /// </summary>
        public int id;
        /// <summary>
        /// 房主
        /// </summary>
        public string playerName = "";
        /// <summary>
        /// 房间密码
        /// </summary>
        public string pawd = "";
        /// <summary>
        /// 当前房间人数，包括房主
        /// </summary>
        public int curNum;
        //public int maxNum;
        /// <summary>
        /// 是否开局
        /// </summary>
        public bool isBegin;
        /// <summary>
        /// 回合数
        /// </summary>
        public int count;
        public int curOrder;
        /// <summary>
        /// 对局内数据
        /// </summary>
        public Dictionary<string, GameDate> gameDate = new Dictionary<string, GameDate>();
        public string content = "";
        public Dictionary<int, House> houses = new Dictionary<int, House>();
        /// <summary>
        /// 宝箱
        /// </summary>
        public Dictionary<int, Treasure> treasures = new Dictionary<int, Treasure>();
        /// <summary>
        /// 命运
        /// </summary>
        public Dictionary<int, Fate> fates = new Dictionary<int, Fate>();
        /// <summary>
        /// 计时器
        /// </summary>
        public int time;
    }

    class MainClass
    {
        //监听Socket
        public static Socket? listenfd;
        /// <summary>
        /// 所有客户端Socket及状态信息
        /// </summary>
        public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();
        public static Dictionary<string, ClientState> allPlayers = new Dictionary<string, ClientState>();
        public static void Main(string[] args)
        {
            //Socket
            listenfd = new Socket(AddressFamily.InterNetwork,
                            SocketType.Stream, ProtocolType.Tcp);
            //Bind
            IPAddress ipAdr = IPAddress.Parse("0.0.0.0");
            IPEndPoint ipEp = new IPEndPoint(ipAdr, 9888);
            listenfd.Bind(ipEp);
            //Listen
            listenfd.Listen(0);
            Console.WriteLine(System.DateTime.Now.ToString("G") + " [服务器]启动成功\n大富翁服务器 版本1.0.13");
            /// <summary>
            /// 计时器
            /// </summary>
            Timer timer = new(MsgHandler.TimerCallback, null, 0, 1000);
            //checkRead
            List<Socket> checkRead = new List<Socket>();
            //主循环
            while (true)
            {
                //填充checkRead列表
                checkRead.Clear();
                checkRead.Add(listenfd);
                foreach (ClientState s in clients.Values)
                {
                    checkRead.Add(s.socket);
                }
                //select
                Socket.Select(checkRead, null, null, 1000);
                //检查可读对象
                foreach (Socket s in checkRead)
                {
                    if (s != null)
                    {
                        if (s == listenfd)
                        {
                            ReadListenfd(s);
                        }
                        else
                        {
                            ReadClientfd(s);
                        }
                    }

                }
            }
        }
        //读取Listenfd
        public static void ReadListenfd(Socket listenfd)
        {
            Console.WriteLine(System.DateTime.Now.ToString("G") + " Accept");
            Socket clientfd = listenfd.Accept();
            ClientState state = new ClientState();
            state.socket = clientfd;
            clients.Add(clientfd, state);
        }
        //读取Clientfd
        public static bool ReadClientfd(Socket clientfd)
        {
            if (clients.ContainsKey(clientfd))
            {
                ClientState state = clients[clientfd];
                //接收
                int count;
                try
                {
                    count = clientfd.Receive(state.readBuff);
                }
                catch (SocketException ex)
                {
                    MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
                    object[] ob = { state };
                    mei.Invoke(null, ob);

                    clientfd.Close();
                    clients.Remove(clientfd);
                    Console.WriteLine(System.DateTime.Now.ToString("G") + " Receive SocketException " + ex.ToString());
                    return false;
                }
                //客户端关闭
                if (count <= 0)
                {
                    MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
                    object[] ob = { state };
                    mei.Invoke(null, ob);

                    clientfd.Close();
                    clients.Remove(clientfd);
                    Console.WriteLine(System.DateTime.Now.ToString("G") + " Socket Close");
                    return false;
                }
                //消息处理
                string recvStr =
                        System.Text.Encoding.Default.GetString(state.readBuff, 0, count);
                string[] split = recvStr.Split('|');

                Console.WriteLine(System.DateTime.Now.ToString("G") + " Recv " + recvStr);
                string msgName = split[0];
                if (split.Length > 1)
                {
                    string msgArgs = split[1];
                    if (msgName != "")
                    {
                        string funName = "Msg" + msgName;
                        MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
                        object[] o = { state, msgArgs };
                        mi.Invoke(null, o);
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }
        //发送
        public static void Send(ClientState cs, string sendStr)
        {
            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
            if (cs.socket.Connected)
            {
                cs.socket.Send(sendBytes);
            }
        }

    }

}
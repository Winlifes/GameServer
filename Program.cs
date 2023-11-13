using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace GameServer
{
    public class ClientState
    {
        public Socket socket;
        public string ip;
        public byte[] readBuff = new byte[1024];

        public string name;
        public int roomId = -1;
        
        public GameDate? date = null;
        
    }

    public class GameDate
    {
        public int money;
        public int color;
        public int playOrder;
    
        public GameDate(int money, int color, int order)
        {
            this.money = money;
            this.color = color;
            this.playOrder = order;
        }
    }

    public class House
    {
        public int id;
        public string name;
        public int level;
        public int[] rent;
        public bool state;
        public House(int id, string name, int[] rent)
        {
            this.id = id;
            this.name = name;
            this.level = 0;
            this.rent = rent;
            state = true;
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
        public string playerName;
        /// <summary>
        /// 房间密码
        /// </summary>
        public string pawd;
        /// <summary>
        /// 当前房间人数，包括房主
        /// </summary>
        public int curNum;
        //public int maxNum;
        /// <summary>
        /// 房间状态
        /// </summary>
        public bool roomState;
        public Dictionary<House, ClientState> houses = new Dictionary<House, ClientState>();

    }

    class MainClass
    {
        //监听Socket
        public static Socket listenfd;
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
            Console.WriteLine("[服务器]启动成功\n大富翁服务器测试版本1.0");
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
        //读取Listenfd
        public static void ReadListenfd(Socket listenfd)
        {
            Console.WriteLine("Accept");
            Socket clientfd = listenfd.Accept();
            ClientState state = new ClientState();
            state.socket = clientfd;
            clients.Add(clientfd, state);
        }
        //读取Clientfd
        public static bool ReadClientfd(Socket clientfd)
        {
            ClientState state = clients[clientfd];
            //接收
            int count = 0;
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
                Console.WriteLine("Receive SocketException " + ex.ToString());
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
                Console.WriteLine("Socket Close");
                return false;
            }
            //消息处理
            string recvStr =
                    System.Text.Encoding.Default.GetString(state.readBuff, 0, count);
            string[] split = recvStr.Split('|');
            Console.WriteLine("Recv " + recvStr);
            string msgName = split[0];
            string msgArgs = split[1];
            string funName = "Msg" + msgName;
            MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
            object[] o = { state, msgArgs };
            mi.Invoke(null, o);
            return true;
        }
        //发送
        public static void Send(ClientState cs, string sendStr)
        {
            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
            cs.socket.Send(sendBytes);
        }


    }
}
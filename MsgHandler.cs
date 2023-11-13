namespace GameServer
{
    internal class MsgHandler
    {
        /// <summary>
        /// 所有房间信息
        /// </summary>
        public static Dictionary<int, Room> rooms = new Dictionary<int, Room>();
        /// <summary>
        /// 每个房间内有哪些玩家，房主除外
        /// </summary>
        public static Dictionary<int, List<string>> roomPlayers = new Dictionary<int, List<string>>();
        /// <summary>
        /// 当前客户端最新版本
        /// </summary>
        static readonly string curVerion = "1.0.0";
        /// <summary>
        /// 目前以分配到的房间id
        /// </summary>
        private static int curid = 0;

        /// <summary>
        /// 进入大厅
        /// </summary>
        /// <param name="c"></param>
        /// <param name="msgArgs"></param>
        public static void MsgEnter(ClientState c, string msgArgs)
        {
            //解析参数
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            string name = split[1];
            string verion = split[2];


            if (verion == curVerion)
            {
                foreach (ClientState cs in MainClass.clients.Values)
                {
                    if (cs.name == name)
                    {
                        MainClass.clients.Remove(c.socket);
                        return;
                    }

                }
                //赋值
                c.ip = desc;
                c.name = name;
                MainClass.allPlayers.Add(c.name, c);
                //广播
                string sendStr = "Enter|" + msgArgs;
                foreach (ClientState cs in MainClass.clients.Values)
                {
                    MainClass.Send(cs, sendStr);
                }
            }
            else
            {
                string sendStr = "Update|" + msgArgs;
                MainClass.Send(c, sendStr);
                MainClass.clients.Remove(c.socket);
            }

        }

        public static void MsgList(ClientState c, string msgArgs)
        {
            string sendStr = "List|";
            foreach (ClientState cs in MainClass.clients.Values)
            {
                sendStr += cs.ip.ToString() + ",";
                sendStr += cs.name + ",";
            }
            MainClass.Send(c, sendStr);
        }

        public static void MsgCreate(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            string name = split[1];
            string pswd = split[2];

            if (rooms.Count <= 9)
            {
                //赋值
                Room room = new Room();
                curid++;
                room.id = curid;
                room.playerName = name;
                room.pawd = pswd;
                room.curNum = 1;
                rooms.Add(room.id, room);
                roomPlayers.Add(room.id, new List<string>());
                c.roomId = curid;

                //广播
                string sendStr = "Create|" + desc + "," + name + "," + room.id + ',';
                foreach (ClientState cs in MainClass.clients.Values)
                {
                    MainClass.Send(cs, sendStr);
                }
            }
            else Console.WriteLine("房间已满,无法创建更多房间");


        }

        public static void MsgRoomList(ClientState c, string msgArgs)
        {
            string sendStr = "RoomList|";
            foreach (Room rm in rooms.Values)
            {
                sendStr += rm.id + ",";
                sendStr += rm.playerName + ",";
                sendStr += rm.curNum + ",";

            }
            MainClass.Send(c, sendStr);
        }

        public static void MsgRoomPlayerList(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            int roomId = int.Parse(split[0]);

            string sendStr = "RoomPlayerList|";
            sendStr += roomId + ",";
            sendStr += roomPlayers[roomId].Count + ",";
            List<string> players = roomPlayers[roomId];
            foreach (string player in players)
            {
                sendStr += player + ",";
            }
            MainClass.Send(c, sendStr);
        }

        public static void MsgExit(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            string name = split[1];
            int roomId = int.Parse(split[2]);

            roomPlayers[roomId].Remove(name);
            rooms[roomId].curNum--;
            c.roomId = -1;

            if (name == rooms[roomId].playerName) //如果房主退出，房主权限继承下一个人
            {
                if (rooms[roomId].curNum > 0)
                {
                    rooms[roomId].playerName = roomPlayers[roomId][0];
                    roomPlayers[roomId].Remove(rooms[roomId].playerName);

                    //房主变更
                    //广播
                    string sendStr1 = "RoomPlayerChange|" + msgArgs + rooms[roomId].playerName;
                    foreach (ClientState cs in MainClass.clients.Values)
                    {
                        MainClass.Send(cs, sendStr1);
                    }
                }
                else
                {
                    //关闭房间,原房间人员移除
                    rooms.Remove(roomId);
                    roomPlayers[roomId].Clear();
                    //广播
                    string sendStr1 = "Destroy|" + "," + desc + "," + roomId + ",";
                    foreach (ClientState cs in MainClass.clients.Values)
                    {
                        MainClass.Send(cs, sendStr1);
                    }
                }

            }
            else
            {
                //房间其他人退出房间
                //广播
                string sendStr = "Exit|" + msgArgs;
                foreach (ClientState cs in MainClass.clients.Values)
                {
                    MainClass.Send(cs, sendStr);
                }
            }
            

            

        }

        public static void MsgJoin(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            string name = split[1];
            int roomId = int.Parse(split[2]);
            string pawd = split[3];

            if(pawd == rooms[roomId].pawd)
            {
                roomPlayers[roomId].Add(name);
                rooms[roomId].curNum++;
                c.roomId = roomId;
                //广播
                string sendStr = "Join|" + desc + "," + name + "," + roomId + ",";
                foreach (ClientState cs in MainClass.clients.Values)
                {
                    MainClass.Send(cs, sendStr);
                }
            }
            
        }
        /// <summary>
        /// 开始一局游戏
        /// </summary>
        /// <param name="c"></param>
        /// <param name="msgArgs"></param>
        public static void MsgBegin(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            rooms[roomId].roomState = true;
            //生成随机数，颜色，开始顺序
            int count = roomPlayers[roomId].Count + 1;
            int[] ints = GetRandomArray(count, 1, count);

            //房主申请开始游戏，服务端更新房间状态，且为每个房间内的玩家随机分配颜色和开始顺序，制定初始资金）
            string playName = rooms[roomId].playerName;
            ClientState clientState = MainClass.allPlayers[playName];
            string sendStr = "GameBegin|";
            if (clientState != null)
            {
                //先处理房主信息
                clientState.date = new GameDate(1000, ints[0], ints[0]);
                sendStr += roomId + ",";
                sendStr += playName + "," + clientState.date.color + "," + clientState.date.playOrder + "," + clientState.date.money + ",";

            }
            for (int i = 0; i < roomPlayers[roomId].Count; i++)
            {
                string name = roomPlayers[roomId][i];
                ClientState cs = MainClass.allPlayers[name];
                if (cs != null)
                {
                    //在处理房间内其他玩家
                    cs.date = new GameDate(1000, ints[i + 1], ints[i + 1]);
                    sendStr += name + "," + cs.date.color + "," + cs.date.playOrder + "," + cs.date.money + ",";
                }

            }
            foreach (ClientState cs in MainClass.clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
            //需向每个客户端发送消息（GameBegin|房间号，玩家名字，颜色，开始顺序，初始资金，

            //玩家名字，颜色，开始顺序，初始资金，  房间内有几个玩家就发几个


        }
        /// <summary>
        /// 聊天系统
        /// </summary>
        /// <param name="c"></param>
        /// <param name="msgArgs"></param>
        public static void MsgSendInfo(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];
            string text = split[3];
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息

            //(SendInfo|玩家ip,玩家名字，玩家发出的文本）

        }





        public static void MsgMove(ClientState c, string msgArgs)
        {
            //解析参数
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float x = float.Parse(split[1]);
            float y = float.Parse(split[2]);
            float z = float.Parse(split[3]);
            //赋值
            //c.x = x;
            //c.y = y;
            //c.z = z;
            //广播
            string sendStr = "Move|" + msgArgs;
            foreach (ClientState cs in MainClass.clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
        }

        public static void MsgAttack(ClientState c, string msgArgs)
        {
            //广播
            string sendStr = "Attack|" + msgArgs;
            foreach (ClientState cs in MainClass.clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
        }

        public static void MsgHit(ClientState c, string msgArgs)
        {
            //解析参数
            string[] split = msgArgs.Split(',');
            string attDesc = split[0];
            string hitDesc = split[1];
            //被攻击
            ClientState hitCS = null;
            foreach (ClientState cs in MainClass.clients.Values)
            {
                if (cs.ip == hitDesc)
                    hitCS = cs;
            }
            if (hitCS == null)
                return;

        }




        public static int[] GetRandomArray(int Number, int minNum, int maxNum)
        {
            int j;
            int[] b = new int[Number];
            Random r = new();
            for (j = 0; j < Number; j++)
            {
                int i = r.Next(minNum, maxNum + 1);
                int num = 0;
                for (int k = 0; k < j; k++)
                {
                    if (b[k] == i)
                    {
                        num++;
                    }
                }
                if (num == 0)
                {
                    b[j] = i;
                }
                else
                {
                    j--;
                }
            }
            return b;
        }
    }
}

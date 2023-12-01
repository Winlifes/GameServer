
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
                        string sendStr1 = "NameRepeat|" + msgArgs;
                        MainClass.Send(c, sendStr1);
                        MainClass.clients.Remove(c.socket);
                        return;
                    }

                }
                //赋值
                c.ip = desc;
                c.name = name;
                MainClass.allPlayers.Add(c.name, c);
                //广播
                string sendStr = "Enter|" + desc + "," + name + "," + verion + ",";
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
                sendStr += rm.isBegin + ",";
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
                    string sendStr1 = "Destroy|" + desc + "," + roomId + ",";
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

            if (pawd == rooms[roomId].pawd)
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
            else
            {
                string sendStr = "PawdWrong|" + desc + "," + name + "," + roomId + ",";
                MainClass.Send(c, sendStr);
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
            rooms[roomId].time = 0;
            rooms[roomId].isBegin = true;
            rooms[roomId].count = 1;
            rooms[roomId].curOrder = 1;
            //初始化土地
            HouseInit(roomId);
            TreasureInit(roomId);
            FateInit(roomId);
            //生成随机数，颜色，开始顺序
            int count = roomPlayers[roomId].Count + 1;
            int[] ints = GetRandomArray(count, 1, count);

            //房主申请开始游戏，服务端更新房间状态，且为每个房间内的玩家随机分配颜色和开始顺序，制定初始资金）
            string playName = rooms[roomId].playerName;
            string sendStr = "GameBegin|";
            //先处理房主信息
            GameDate date = new GameDate(2000, ints[0], ints[0], 1);
            rooms[roomId].gameDate.Add(playName, date);
            sendStr += roomId + ",";
            sendStr += playName + "," + rooms[roomId].gameDate[playName].color + "," + rooms[roomId].gameDate[playName].playOrder + "," + rooms[roomId].gameDate[playName].money + ",";
            //需向每个客户端发送消息（GameBegin|房间号，玩家名字，颜色，开始顺序，初始资金，
            //玩家名字，颜色，开始顺序，初始资金，  房间内有几个玩家就发几个
            for (int i = 0; i < roomPlayers[roomId].Count; i++)
            {
                string name = roomPlayers[roomId][i];
                //在处理房间内其他玩家
                GameDate date1 = new GameDate(2000, ints[i + 1], ints[i + 1], 1);
                rooms[roomId].gameDate.Add(name, date1);
                sendStr += name + "," + rooms[roomId].gameDate[name].color + "," + rooms[roomId].gameDate[name].playOrder + "," + rooms[roomId].gameDate[name].money + ",";

            }
            foreach (ClientState cs in MainClass.clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
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

            rooms[roomId].content += "\n" + name + " : " + text;
            string sendStr = "SendInfo|" + msgArgs;
            //房主
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }
            //(SendInfo|玩家ip,房间号，玩家名字，玩家发出的文本）

        }

        public static void MsgYao(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];

            int num = new Random().Next(1, 7);//步数
            int num1 = -1;//命运，宝箱
            int num2 = -2;//命运随机数
            rooms[roomId].gameDate[name].position += num;
            if (rooms[roomId].gameDate[name].position / 41 >= 1)
            {
                rooms[roomId].gameDate[name].position %= 41;
                rooms[roomId].gameDate[name].position++;
                //走完一圈拿1000
                rooms[roomId].gameDate[name].money += 1000;
            }

            int x = rooms[roomId].gameDate[name].position;
            if (x == 1 || x == 11 || x == 21)
            {
                //无事发生
            }
            else if (x == 6 || x == 16 || x == 26 || x == 36)
            {
                //车站
                
            }
            else if (x == 5 || x == 17 || x == 27 || x == 35)
            {
                //宝箱
                num1 = new Random().Next(1, 3);
                TreasureAction(num1, roomId, name);
            }
            else if (x == 7 || x == 15 || x == 25 || x == 37)
            {
                //命运
                num1 = new Random().Next(1, 3);
                num2 = FateAction(num1, roomId, name);
            }
            else if (x == 31)
            {
                //坐牢
                rooms[roomId].gameDate[name].position = 11;

            }
            else
            {
                //房子
                if (rooms[roomId].houses[x].state == 0 && rooms[roomId].houses[x].playerName != name && rooms[roomId].gameDate[rooms[roomId].houses[x].playerName].position != 0)//有人买且不是自己的
                {
                    rooms[roomId].gameDate[name].money -= rooms[roomId].houses[x].rent[rooms[roomId].houses[x].level];
                    string roomName = rooms[roomId].houses[x].playerName;
                    rooms[roomId].gameDate[roomName].money += rooms[roomId].houses[x].rent[rooms[roomId].houses[x].level];
                }
                
            }

            string sendStr = "Yao|" + msgArgs + num + "," + num1 + "," + num2 + ",";
            //房主
            if(MainClass.allPlayers.ContainsKey(rooms[roomId].playerName))
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }
        }

        public static void MsgBuy(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];
            int position = int.Parse(split[3]);

            rooms[roomId].gameDate[name].money -= rooms[roomId].houses[position].price;//扣钱
            rooms[roomId].houses[position].state = 0;//已被买入
            rooms[roomId].houses[position].playerName = name;//写上地主名
            rooms[roomId].gameDate[name].property.Add(position);//加入财产

            string sendStr = "Buy|" + msgArgs;
            //房主
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }
            //(SendInfo|玩家ip,房间号，玩家名字，玩家发出的文本）

        }

        public static void MsgBuild(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];
            int position = int.Parse(split[3]);

            rooms[roomId].gameDate[name].money -= rooms[roomId].houses[position].up;//扣钱
            rooms[roomId].houses[position].level++;

            string sendStr = "Build|" + msgArgs;
            //房主
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }

        }

        public static void MsgSale(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];
            int position = int.Parse(split[3]);

            rooms[roomId].gameDate[name].money += rooms[roomId].houses[position].up / 2;//加钱
            rooms[roomId].houses[position].level--;

            string sendStr = "Sale|" + msgArgs;
            //房主
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }

        }

        public static void MsgPawn(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];
            int count = int.Parse(split[3]);
            for (int i = 0; i < count; i++)
            {
                int houseId = int.Parse(split[i + 4]);
                rooms[roomId].houses[houseId].state = 1;
                rooms[roomId].gameDate[name].money += rooms[roomId].houses[houseId].price / 2;
            }


            string sendStr = "Pawn|" + msgArgs;
            //房主
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }

        }

        public static void MsgRansom(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];
            int count = int.Parse(split[3]);
            for (int i = 0; i < count; i++)
            {
                int houseId = int.Parse(split[i + 4]);
                rooms[roomId].houses[houseId].state = 0;
                rooms[roomId].gameDate[name].money -= rooms[roomId].houses[houseId].price / 2;
            }


            string sendStr = "Ransom|" + msgArgs;
            //房主
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }

        }

        public static void MsgVehicle(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];
            int x = int.Parse(split[3]);

            rooms[roomId].gameDate[name].money -= 500;
            rooms[roomId].gameDate[name].position = x;

            string sendStr = "Vehicle|" + msgArgs;
            //房主
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }
        }

        public static void MsgSkip(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];

            if (rooms[roomId].gameDate[name].money < 0)//当前破产玩家数据刷新
            {
                rooms[roomId].gameDate[name].isPoCan = true;
                foreach (int i in rooms[roomId].gameDate[name].property)
                {
                    rooms[roomId].houses[i].state = -1;
                    rooms[roomId].houses[i].level = 0;
                    rooms[roomId].houses[i].playerName = "";
                }
                rooms[roomId].gameDate[name].property.Clear();
            }

            rooms[roomId].time = 0;
            rooms[roomId].count++;
            int nextOrder = (rooms[roomId].gameDate[name].playOrder % rooms[roomId].curNum) + 1;
            string nextName = "";
            foreach (string n in rooms[roomId].gameDate.Keys)
            {
                if (rooms[roomId].gameDate[n].playOrder == nextOrder)
                {
                    nextName = n;
                }
            }

            while (rooms[roomId].gameDate[nextName].isPoCan)//遇到破产的轮下一个
            {
                nextOrder = (nextOrder % rooms[roomId].curNum) + 1;
                foreach (string n in rooms[roomId].gameDate.Keys)
                {
                    if (rooms[roomId].gameDate[n].playOrder == nextOrder)
                    {
                        nextName = n;
                    }
                }
            }

            rooms[roomId].curOrder = nextOrder;


            string sendStr = "Skip|" + msgArgs;
            sendStr += rooms[roomId].curOrder + ",";

            //房主
            MainClass.Send(MainClass.allPlayers[rooms[roomId].playerName], sendStr);
            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
            foreach (string n in roomPlayers[roomId])
            {
                MainClass.Send(MainClass.allPlayers[n], sendStr);
            }

            if (MainClass.allPlayers.Count <= 0 && roomPlayers[roomId].Count <= 0)
            {
                rooms.Remove(roomId);
            }
        }

        public static void MsgGameOver(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            int roomId = int.Parse(split[1]);
            string name = split[2];
            //对局结束后初始化

            foreach (string s in rooms[roomId].gameDate.Keys)
            {
                if (rooms[roomId].gameDate[s].isGuaJi)
                {
                    roomPlayers[roomId].Remove(s);
                    rooms[roomId].curNum--;
                    c.roomId = -1;

                    string sendStr1 = "Exit|" + "desc," + s + "," + roomId + ",";

                    foreach (ClientState cs in MainClass.clients.Values)
                    {
                        MainClass.Send(cs, sendStr1);
                    }
                }
            }

            rooms[roomId].isBegin = false;
            rooms[roomId].gameDate.Clear();
            rooms[roomId].houses.Clear();
            rooms[roomId].treasures.Clear();
            rooms[roomId].fates.Clear();
            rooms[roomId].content = "";
            rooms[roomId].time = 0;
            rooms[roomId].count = 0;
            rooms[roomId].curOrder = 0;
            string sendStr = "GameOver|" + msgArgs;

            foreach (ClientState cs in MainClass.clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }

        }


        private static int[] GetRandomArray(int Number, int minNum, int maxNum)
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

        public static void TimerCallback(object state)
        {
            // 在定时器触发时执行的逻辑
            foreach (Room r in rooms.Values)
            {
                if (r.isBegin)
                {
                    r.time++;
                    if (r.time >= 30)
                    {
                        string name = "";
                        foreach (string n in r.gameDate.Keys)
                        {
                            if (r.gameDate[n].playOrder == r.curOrder)
                            {
                                name = n;
                            }
                        }
                        if (r.gameDate[name].money < 0)//当前破产玩家数据刷新
                        {
                            r.gameDate[name].isPoCan = true;
                            foreach (int i in r.gameDate[name].property)
                            {
                                r.houses[i].state = -1;
                                r.houses[i].level = 0;
                                r.houses[i].playerName = "";
                            }
                            r.gameDate[name].property.Clear();
                        }


                        //自动切下一个回合
                        r.time = 0;
                        r.curOrder = (r.curOrder % r.curNum) + 1;
                        string nextName = "";
                        foreach (string n in r.gameDate.Keys)
                        {
                            if (r.gameDate[n].playOrder == r.curOrder)
                            {
                                nextName = n;
                            }
                        }

                        while (r.gameDate[nextName].isPoCan)//遇到破产的轮下一个
                        {
                            r.curOrder = (r.curOrder % r.curNum) + 1;
                            foreach (string n in r.gameDate.Keys)
                            {
                                if (r.gameDate[n].playOrder == r.curOrder)
                                {
                                    nextName = n;
                                }
                            }
                        }

                        r.count++;
                        string sendStr = "ForceSkip|" + r.id + "," + r.curOrder + "," + name + ",";


                        if (!MainClass.allPlayers.ContainsKey(rooms[r.id].playerName) && roomPlayers[r.id].Count <= 0)
                        {
                            rooms.Remove(r.id);
                            Console.WriteLine(System.DateTime.Now.ToString("G") + "Destroy Room " +  r.id);
                            string sendStr1 = "Destroy|" + "desc," + r.id + ",";
                            foreach (ClientState cs in MainClass.clients.Values)
                            {
                                MainClass.Send(cs, sendStr1);
                            }
                        }
                        else
                        {
                            //房主
                            if (MainClass.allPlayers.ContainsKey(rooms[r.id].playerName))
                            {
                                MainClass.Send(MainClass.allPlayers[r.playerName], sendStr);
                            }
                            //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
                            foreach (string n in roomPlayers[r.id])
                            {
                                MainClass.Send(MainClass.allPlayers[n], sendStr);
                            }
                        }
                        
                    }
                }
            }
        }

        private static void HouseInit(int roomId)
        {
            rooms[roomId].houses.Add(2, new House(2, "教廷", 3, 200, 100, new int[] { 100, 200, 400, 800 }));
            rooms[roomId].houses.Add(3, new House(3, "利比亚", 3, 50, 50, new int[] { 25, 50, 100, 200 }));
            rooms[roomId].houses.Add(4, new House(4, "苏丹", 3, 60, 50, new int[] { 30, 60, 120, 240 }));
            rooms[roomId].houses.Add(6, new House(6, "日本站", 0, 100, 0, new int[] { 50 }));
            rooms[roomId].houses.Add(8, new House(8, "土耳其", 3, 100, 50, new int[] { 50, 100, 200, 400 }));
            rooms[roomId].houses.Add(9, new House(9, "希腊", 3, 100, 50, new int[] { 50, 100, 200, 400 }));
            rooms[roomId].houses.Add(10, new House(10, "保加利亚", 3, 120, 80, new int[] { 60, 120, 240, 480 }));
            rooms[roomId].houses.Add(12, new House(12, "波兰", 3, 150, 80, new int[] { 75, 150, 300, 600 }));
            rooms[roomId].houses.Add(13, new House(13, "俄罗斯", 3, 250, 100, new int[] { 125, 250, 500, 800 }));
            rooms[roomId].houses.Add(14, new House(14, "乌克兰", 3, 180, 80, new int[] { 90, 180, 360, 720 }));
            rooms[roomId].houses.Add(16, new House(16, "西班牙站", 0, 100, 0, new int[] { 50 }));
            rooms[roomId].houses.Add(18, new House(18, "立陶宛", 3, 200, 100, new int[] { 100, 200, 400, 800 }));
            rooms[roomId].houses.Add(19, new House(19, "拉脱维亚", 3, 200, 100, new int[] { 100, 200, 400, 800 }));
            rooms[roomId].houses.Add(20, new House(20, "艾欧尼亚", 3, 220, 100, new int[] { 110, 220, 440, 800 }));
            rooms[roomId].houses.Add(22, new House(22, "挪威", 3, 220, 100, new int[] { 110, 220, 440, 800 }));
            rooms[roomId].houses.Add(23, new House(23, "瑞典", 3, 220, 100, new int[] { 110, 220, 440, 800 }));
            rooms[roomId].houses.Add(24, new House(24, "芬兰", 3, 240, 100, new int[] { 120, 240, 480, 800 }));
            rooms[roomId].houses.Add(26, new House(26, "美国站", 0, 100, 0, new int[] { 50 }));
            rooms[roomId].houses.Add(28, new House(28, "德国", 3, 280, 100, new int[] { 140, 280, 560, 1000 }));
            rooms[roomId].houses.Add(29, new House(29, "法国", 3, 260, 100, new int[] { 130, 260, 520, 1000 }));
            rooms[roomId].houses.Add(30, new House(30, "英国", 3, 300, 150, new int[] { 150, 300, 600, 1200 }));
            rooms[roomId].houses.Add(32, new House(32, "加拿大", 3, 300, 150, new int[] { 150, 300, 600, 1200 }));
            rooms[roomId].houses.Add(33, new House(33, "美国", 3, 300, 150, new int[] { 150, 300, 600, 1200 }));
            rooms[roomId].houses.Add(34, new House(34, "墨西哥", 3, 320, 150, new int[] { 160, 320, 640, 1200 }));
            rooms[roomId].houses.Add(36, new House(36, "英国站", 0, 100, 0, new int[] { 50 }));
            rooms[roomId].houses.Add(38, new House(38, "迪拜", 3, 360, 150, new int[] { 180, 360, 720, 1200 }));
            rooms[roomId].houses.Add(39, new House(39, "夏威夷", 3, 400, 200, new int[] { 200, 400, 800, 1600 }));
            rooms[roomId].houses.Add(40, new House(40, "黑子之家", 3, 999, 500, new int[] { 500, 1000, 1500, 2000 }));
        }

        private static void TreasureInit(int roomId)
        {
            rooms[roomId].treasures.Add(1, new Treasure(1, "今天生日", "每人给你￥100零花钱"));
            rooms[roomId].treasures.Add(2, new Treasure(2, "做慈善", "你给每人￥100救济金"));
        }

        private static void TreasureAction(int num, int roomId, string name)
        {
            switch (num)
            {
                case 1:
                    foreach (string n in rooms[roomId].gameDate.Keys)
                    {
                        if (n != name && !rooms[roomId].gameDate[n].isPoCan)
                        {
                            rooms[roomId].gameDate[n].money -= 100;
                            rooms[roomId].gameDate[name].money += 100;
                        }
                    }
                    
                    return;
                case 2:
                    foreach (string n in rooms[roomId].gameDate.Keys)
                    {
                        if (n != name && !rooms[roomId].gameDate[n].isPoCan)
                        {
                            rooms[roomId].gameDate[n].money += 100;
                            rooms[roomId].gameDate[name].money -= 100;
                        }
                    }
                    
                    return;
                default:
                    
                    return;
            }
        }

        private static void FateInit(int roomId)
        {
            rooms[roomId].fates.Add(1, new Fate(1, "土地神到", "随机在你的地皮上建造一栋房子"));
            rooms[roomId].fates.Add(2, new Fate(2, "龙卷风来袭", "随机带走地图上的一栋房子"));
        }

        private static int FateAction(int num, int roomId, string name)
        {
            switch (num)
            {
                case 1:
                    int count = rooms[roomId].gameDate[name].property.Count;
                    int index = new Random().Next(0, count);
                    if (count > 0)
                    {
                        int houseId = rooms[roomId].gameDate[name].property[index];
                        if (rooms[roomId].houses[houseId].level < rooms[roomId].houses[houseId].maxLevel)
                        {
                            rooms[roomId].houses[houseId].level++;

                        }
                        else rooms[roomId].gameDate[name].money += 200;
                    }
                    else rooms[roomId].gameDate[name].money += 200;
                    return index;
                case 2:
                    List<int> ints = new List<int>();
                    foreach (int j in rooms[roomId].houses.Keys)
                    {
                        if (rooms[roomId].houses[j].state == 0)
                        {
                            ints.Add(j);
                        }
                    }
                    int c = ints.Count;
                    if (c > 0)
                    {
                        int i = new Random().Next(0, c);
                        int houseID = ints[i];
                        if (rooms[roomId].houses[houseID].level > 0)
                        {
                            rooms[roomId].houses[houseID].level--;

                        }
                        else
                        {
                            string n = rooms[roomId].houses[houseID].playerName;
                            rooms[roomId].gameDate[n].property.Remove(houseID);
                            rooms[roomId].houses[houseID].state = -1;
                            rooms[roomId].houses[houseID].level = 0;
                            rooms[roomId].houses[houseID].playerName = "";
                        }
                        return i;
                    }
                    else
                    {
                        Console.WriteLine( "目前没有人拥有地皮！");
                        return -1;
                    }
                default:
                    return -2;
            }
        }

    }
}

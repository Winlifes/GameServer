
namespace GameServer
{
    internal class EventHandler
    {
        public static void OnDisconnect(ClientState c)
        {
            if(c.ip != "")
            {
                string desc = c.ip;
                string sendStr = "Leave|" + desc + "," + c.name + ",";
                if(c.name != "")
                MainClass.allPlayers.Remove(c.name);
                if(c.socket != null)
                MainClass.clients.Remove(c.socket);
                if (c.roomId != -1)
                {
                    MsgHandler.roomPlayers[c.roomId].Remove(c.name);
                }
                foreach (ClientState cs in MainClass.clients.Values)
                {
                    MainClass.Send(cs, sendStr);
                }
                IsExitRoom(c);
            }
            
        }

        public static void IsExitRoom(ClientState c)
        {
            if (MsgHandler.rooms.ContainsKey(c.roomId))
            {
                if (MsgHandler.rooms[c.roomId].isBegin)
                {
                    MsgHandler.rooms[c.roomId].gameDate[c.name].isGuaJi = true;

                    //广播
                    string sendStr1 = "GuaJi|" + c.ip + "," + c.roomId + "," + c.name + ",";
                    //房主
                    if (MainClass.allPlayers.ContainsKey(MsgHandler.rooms[c.roomId].playerName))
                    {
                        MainClass.Send(MainClass.allPlayers[MsgHandler.rooms[c.roomId].playerName], sendStr1);
                    }
                    //客户端发出文本信息，服务端需向游戏中所有人广播该文本信息
                    foreach (string n in MsgHandler.roomPlayers[c.roomId])
                    {
                        MainClass.Send(MainClass.allPlayers[n], sendStr1);
                    }
                }
                else
                {
                    MsgHandler.roomPlayers[c.roomId].Remove(c.name);
                    MsgHandler.rooms[c.roomId].curNum--;
                    c.roomId = -1;
                    //广播
                    string sendStr1 = "Exit|" + c.ip + "," + c.name + "," + c.roomId + ",";
                    foreach (ClientState cs in MainClass.clients.Values)
                    {
                        MainClass.Send(cs, sendStr1);
                    }
                }
                
            }
            c.roomId = -1;
        }
                
            
    }
}


namespace GameServer
{
    internal class EventHandler
    {
        public static void OnDisconnect(ClientState c)
        {
            string desc = c.ip;
            string sendStr = "Leave|" + desc + "," + c.name + ",";
            MainClass.allPlayers.Remove(c.name);
            MainClass.clients.Remove(c.socket);
            if(c.roomId != -1)
            {
                MsgHandler.roomPlayers[c.roomId].Remove(c.name);
            }
            foreach (ClientState cs in MainClass.clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
            IsExitRoom(c);
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
                    foreach (ClientState cs in MainClass.clients.Values)
                    {
                        MainClass.Send(cs, sendStr1);
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

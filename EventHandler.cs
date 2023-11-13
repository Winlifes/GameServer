
namespace GameServer
{
    internal class EventHandler
    {
        public static void OnDisconnect(ClientState c)
        {
            string desc = c.ip;
            string sendStr = "Leave|" + desc + "," + c.name + ",";
            MainClass.allPlayers.Remove(c.name);
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
            c.roomId = -1;
        }
                
            
    }
}

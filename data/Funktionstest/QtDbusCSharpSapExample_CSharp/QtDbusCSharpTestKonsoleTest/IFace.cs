using System;
using DBus;

    [Interface("my.server.interface")]
    public interface ifaceServer
    {
        string execute(string qtSay);
        string pingSapDest();
    }

    public class Server : ifaceServer
    {
        public string execute(string qtSay)
        {
            Console.WriteLine("Qt Say {0}", qtSay);
            return ("execute ausgefuehrt");
        }
        public string pingSapDest()
        {
            return "Ping succesful";
        }
    }

using System;
using System.Linq;
using DBus;
using org.freedesktop.DBus;

public class Program
{
	public static void Main ()
	{
        //connect with D-Bus
        var bus = Bus.Session;
        ObjectPath path = new ObjectPath("/org/dbussharp/test");
        Server myserver = new Server();
        Bus.Session.Register(path,myserver);
        var BusName = "org.dbussharp.test";
        bus.RequestName(BusName,org.freedesktop.DBus.NameFlag.None);
        Console.WriteLine("Hauptprogramm\n");


  
        while (true)
        {
            bus.Iterate();
        }

    }
}

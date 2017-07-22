using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAP.Middleware.Connector;
using DBus;
using org.freedesktop.DBus;

/*
 * Ist der Einstiegspunkt
 */

namespace SapNetConn
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Verbindung mit der SAP
            DestConfig destCfg = new DestConfig();//Instanz erzeugen
            RfcDestinationManager.RegisterDestinationConfiguration(destCfg);//registriere Instanz im DestinationManager
            Console.Write(" Register destination...");
            // Destinationparameter setzen
            RfcConfigParameters configParams = new RfcConfigParameters();
            configParams.Add(RfcConfigParameters.Name,"Flash");
            configParams.Add(RfcConfigParameters.Language, "DE");
            configParams.Add(RfcConfigParameters.Client, "100");
            configParams.Add(RfcConfigParameters.AppServerHost, "mmm.nnn.ooo.3");
            // configParams.Add(RfcConfigParameters.PeakConnectionsLimit, "5");// Max Anzahl gleichzeitiger Verbindungen
            configParams.Add(RfcConfigParameters.ConnectionIdleTimeout, "600");// 600 seconds- 10 min
            configParams.Add(RfcConfigParameters.SystemNumber, "00");
            configParams.Add(RfcConfigParameters.User, "testexampleonly");
            configParams.Add(RfcConfigParameters.Password, "testexampleonly");
            destCfg.AddDestination(configParams);

            //  RfcDestination destination = RfcDestinationManager.GetDestination(strVerb); // hole destination Parameter
            RfcDestination destination = RfcDestinationManager.GetDestination("Flash");
            // Ausgabe der Parameter
            Console.WriteLine("\nConfigured Destination in main: {0} [ {1} ]", destination.Name, destination.Parameters.ToString());

            //connect with D-Bus
            var bus = Bus.Session;
            ObjectPath path = new ObjectPath("/com/swissbit/sap");
            SapServerQuery myserver = new SapServerQuery();
            Bus.Session.Register(path, myserver);
            var BusName = "com.swissbit.sap";
            bus.RequestName(BusName, org.freedesktop.DBus.NameFlag.None);
            if (bus.IsConnected)
            {
                Console.WriteLine("D-Bus connected\n");
                myserver.setDestConfig(ref destCfg);  
                myserver.setRfcDestination(ref destination);
            }
            else
            {
                Console.WriteLine("D-Bus disconnected");
            }
            while (true)
            {
                bus.Iterate();
            }
        }
    }
}

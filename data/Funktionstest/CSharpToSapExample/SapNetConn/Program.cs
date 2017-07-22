using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAP.Middleware.Connector;


/*
 * Ist der Einstiegspunkt
 */

namespace SapNetConn
{
    class Program
    {
        public SapQuery SapQuery
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

        public DestConfig DestConfig
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }
/*
        evt später inmplementieren
        public const string tabMara = "MARA";
        public const string tabMakt = "MAKT";
        public const string feldMatNr = "MATNR";
        public const string feldMatKurzText = "MAKTX";
*/
     static void Main(string[] args)
        {
            DestConfig destCfg = new DestConfig();//Instanz erzeugen
            RfcDestinationManager.RegisterDestinationConfiguration(destCfg);//registriere Instanz im DestinationManager
            Console.Write(" Verbindung :");
            String strVerb = Console.ReadLine();

            // Destinationparameter setzen
            RfcConfigParameters configParams = new RfcConfigParameters();
            if ("test".Equals(strVerb))
            {
                configParams.Add(RfcConfigParameters.Name, "test");
                configParams.Add(RfcConfigParameters.Language, "DE");
                configParams.Add(RfcConfigParameters.Client, "100");
                configParams.Add(RfcConfigParameters.AppServerHost, "mmm.nnn.ooo.3");
                configParams.Add(RfcConfigParameters.PeakConnectionsLimit, "5");// Max Anzahl Verbindungen
                configParams.Add(RfcConfigParameters.ConnectionIdleTimeout, "600");// 10 min
                configParams.Add(RfcConfigParameters.SystemNumber, "00");
                configParams.Add(RfcConfigParameters.User, "testexample");
                configParams.Add(RfcConfigParameters.Password, "testexample");
                destCfg.AddDestination(configParams);
            }
            else
            {
                configParams[RfcConfigParameters.Name] = strVerb;
                configParams.Add(RfcConfigParameters.Language, "DE");
                Console.WriteLine("Client : ");
                configParams[RfcConfigParameters.Client] = Console.ReadLine();
                Console.WriteLine("Server : ");
                configParams[RfcConfigParameters.AppServerHost] = Console.ReadLine();
                configParams.Add(RfcConfigParameters.PeakConnectionsLimit, "5");// Max Anzahl Verbindungen
                configParams.Add(RfcConfigParameters.ConnectionIdleTimeout, "600");// 10 min
                configParams.Add(RfcConfigParameters.SystemNumber, "00");
                Console.WriteLine("Nutzer : ");
                configParams[RfcConfigParameters.User] = Console.ReadLine();
                Console.WriteLine("password : ");
                configParams[RfcConfigParameters.Password] = Console.ReadLine();
                destCfg.AddDestination(configParams);
            }

          //  RfcDestination destination = RfcDestinationManager.GetDestination(strVerb); // hole destination Parameter
            RfcDestination destination = RfcDestinationManager.GetDestination(strVerb);
            // Ausgabe der Parameter
            Console.WriteLine("\nConfigured Destination in main: {0} [ {1} ]", destination.Name, destination.Parameters.ToString());

            //ping
            Console.WriteLine("für ping : Enter ");
            Console.ReadLine();
          //  destCfg.PingDestination(strVerb);
            Console.WriteLine("working with  : {0}", strVerb);
            destCfg.PingDestination(strVerb);

            Console.WriteLine("Auftragsnummer : ");
            string strAufNrToQuery = Console.ReadLine();
            SapQuery newQuery = new SapQuery();

            string strMatNr = "";
            string strGesMenge = "";
            string strGesFailMenge = "";
            newQuery.getMatNr(strAufNrToQuery, destination, ref strMatNr, ref strGesMenge, ref strGesFailMenge);
            string strAusMatNr = strMatNr.TrimStart('0');
            Console.WriteLine("Materialnummer:{0}, Gesamtmenge ist: {1}, Fail-Menge : {2}",strAusMatNr,strGesMenge, strGesFailMenge);
            Console.ReadLine();
            string strMatKurzText = newQuery.getMatKurzText(strMatNr,destination);
            Console.WriteLine("MatrKurzText :{0}", strMatKurzText);
            Console.ReadLine();
            Console.WriteLine("am ende der Funktion");
            Console.ReadLine();
        }
    }
}

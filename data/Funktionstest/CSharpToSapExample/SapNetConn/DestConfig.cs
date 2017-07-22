using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAP.Middleware.Connector;


/*
 * Diese Klasse verwaltet Konfiguration der Verbindung
 */

namespace SapNetConn
{
    public class DestConfig : IDestinationConfiguration
    {
        // Liste 
        private Dictionary<string, RfcConfigParameters> availableDestinations;

        // destination holen
        public DestConfig()
        {
            availableDestinations = new Dictionary<string, RfcConfigParameters>();
        }

        // zur Destination Parameter holen
        public RfcConfigParameters GetParameters(string destinationName)
        {
            RfcConfigParameters foundDestination;
            availableDestinations.TryGetValue(destinationName, out foundDestination);
            return foundDestination;
        }

        // Konfiguration wird nach dem Start nicht veraendert
        public bool ChangeEventsSupported()
        {
            return true;    // Doku SAP-Connector -> bessere Perfomance auch wenn nicht genutzt.
        }
        public event RfcDestinationManager.ConfigurationChangeHandler ConfigurationChanged; // muss vorhanden sein

        public void AddDestination(RfcConfigParameters parameters)
        {
            // name aus der Config holen
            string name = parameters[RfcConfigParameters.Name];
            if (availableDestinations.ContainsKey(name))
            {
                //checked for null on event handler, um NullReferenceException zu vermeiden (Aufruf bevor eine destination registriert)
                if (ConfigurationChanged != null)
                {
                    RfcConfigurationEventArgs eventArgs = new RfcConfigurationEventArgs(RfcConfigParameters.EventType.CHANGED, parameters);
                    Console.WriteLine("Firing change event {0} for destination {1}", eventArgs.ToString(), name);
                    ConfigurationChanged(name, eventArgs);
                }
            }
            // erstelle eine destination
            availableDestinations[name] = parameters;
        }

        // ping a destination
        public void PingDestination(string a)
        {
            try
            {
                RfcDestinationManager.GetDestination(a).Ping();
                Console.WriteLine("Ping successful");
            }
            catch (RfcInvalidParameterException ex)
            {
                Console.WriteLine("{0} : {1}", ex.GetType().Name, ex.Message);
            }
            catch (RfcBaseException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

    }

}

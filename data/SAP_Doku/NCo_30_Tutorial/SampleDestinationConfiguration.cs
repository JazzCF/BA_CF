using System;
using System.Collections.Generic;
using System.Text;
using SAP.Middleware.Connector;

namespace SAP.Middleware.Connector.Examples
{
    /* This example class demonstrates how to implement a Destination Configuration that resides in memory.
     * 
     * This example shows that it is rather simple to obtain destinations from an arbitrary storage. 
     * In productive environments, however, it is not recommended to store configuration data in code or only in memory.
     * Use a persistent data store for that purpose, e.g. a database or an LDAP directory. 
     */
    public class InMemoryDestinationConfiguration : IDestinationConfiguration
    {
        // For the purpose of this example destinations are stored in a Dictionary<string,RfcConfigParameters>
        // that maps a destination name to destination parameters.
        private Dictionary<string, RfcConfigParameters> availableDestinations;

        public InMemoryDestinationConfiguration()
        {
            availableDestinations = new Dictionary<string, RfcConfigParameters>();
        }

        /*
         * Method inherited from interface definition.
         * 
         * Gets the parameters for the destination specified through its name.
         * 
         * Note that this implementation returns null if the destination does not exist.
         * This is the preferred - since more efficient - way to handle the non-existence of
         * a destination. NCo3 will react appropriately, e.g. by throwing an exception if
         * necessary (if there is no sensible way to proceed without parameters, for instance).
         * 
         * It is, however, also legitimate to throw an exception of your choice, if the parameters
         * for a given destination cannot be supplied, which may incur a performance penalty as a
         * result of the .Net framework having to create an exception instance that is simply discarded.
         * 
         */
        public RfcConfigParameters GetParameters(string destinationName)
        {
            RfcConfigParameters foundDestination;
            availableDestinations.TryGetValue(destinationName, out foundDestination);
            return foundDestination;
        }

        /*
         * Method inherited from interface definition.
         * 
         * This implementation of a destination configuration supports events. Based on the return value of
         * this method, NCo3 will (or will not) register an RfcDestinationManager.ConfigurationChangeHandler.
         * In this case (return value is true) it will register such a handler.
         * 
         * It is the responsibility of this class, however, to actually fire the ConfigurationChanged event.
		 * 
		 * Tip: if you know that our configuration never changes during the runtime of your program, it is
		 * safe to return true here, even if you don't use any events. This will improve the performance,
		 * because you avoid that NCo3 constantly re-reads the config parameters (which it would, if you
		 * would return false here). As you know that the parameters will never change, these re-reads are
		 * unnecessary.
         */
        public bool ChangeEventsSupported()
        {
            return true;
        }

        /*
         * Event inherited from interface definition.
         * 
         * If change events are supported (which is the case since ChangeEventsSupported() returns true),
         * the NCO3 runtime will register an RfcDestinationManager.ConfigurationChangeHandler to
         * perform the necessary operations ensuing a modification or removal of a destination configuration.
         * 
         * It is the responsibility of this class, however, to actually fire the ConfigurationChanged event.
         */
        public event RfcDestinationManager.ConfigurationChangeHandler ConfigurationChanged;

        public void AddOrEditDestination(RfcConfigParameters parameters)
        {
            string name = parameters[RfcConfigParameters.Name];
            if (availableDestinations.ContainsKey(name))
            {
                // Fire a change event
				if (ConfigurationChanged != null)
				/* Always check for null on event handlers... If AddOrEditDestination() gets called before this
				 * instance of InMemoryDestinationConfiguration is registered with the RfcDestinationManager, we
				 * would get a NullReferenceException when trying to raise the event... Stupid concept.
				 * Why doesn't the .NET framework do this for me?
				 */
				{
					RfcConfigurationEventArgs eventArgs = new RfcConfigurationEventArgs(RfcConfigParameters.EventType.CHANGED, parameters);
					Console.WriteLine("Firing change event {0} for destination {1}", eventArgs.ToString(), name);
                    ConfigurationChanged(name, eventArgs);
                }
            }

			// Replace the current parameters of an existing destination or add a new one
            availableDestinations[name] = parameters;

            string tmp;
            Console.WriteLine("{1} destination {0} added/changed",
                name,
                parameters.TryGetValue(RfcConfigParameters.LogonGroup, out tmp) ? "Load balancing" : "Application server");
        }

        // Removes the destination specified by its name
        public void RemoveDestination(string name)
        {
            if (availableDestinations.Remove(name))
            {
                Console.WriteLine("Successfully removed destination {0}", name);
                if (ConfigurationChanged != null) // Always check for null
                {
                    Console.WriteLine("Firing deletion event for destination {0}", name);
                    ConfigurationChanged(name, new RfcConfigurationEventArgs(RfcConfigParameters.EventType.DELETED));
                }
            }
            else
            {
                Console.WriteLine("The destination could not be removed since it does not exist");
            }
        }
    }

    /*
     * This class, in combination with class InMemoryDestinationConfiguration, demonstrates how to register and use
     * a destination configuration other than the default configuration derived from the application's configuration file.
     */
    public class SampleDestinationConfiguration
    {
        private static InMemoryDestinationConfiguration inMemoryDestinationConfiguration = new InMemoryDestinationConfiguration();

        public static void SetUp()
        {
            // register the in-memory destination configuration -- called before executing any of the examples
            RfcDestinationManager.RegisterDestinationConfiguration(inMemoryDestinationConfiguration);
        }

        public static void TearDown()
        {
            // unregister the in-memory destination configuration -- called after we are done working with the examples 
            RfcDestinationManager.UnregisterDestinationConfiguration(inMemoryDestinationConfiguration);
        }

        /*
         * This example gets a destination by name. The destination has to be added before using ExampleAddOrChangeDestination.
         */
        public static void ExampleGetDestination()
        {
            string name = ReadName();
            try
            {
                Console.WriteLine(RfcDestinationManager.GetDestination(name).Parameters.ToString());
            }
            catch (RfcBaseException ex)
            {
                Console.WriteLine("{0} : {1}", ex.GetType().Name, ex.Message);
            }
        }

        /*
         * This example adds or changes a destination configuration.
         * 
         * For the sake of simplicity only crucial parameters and the trace level can be entered.
         * Others are set to certain values (like PoolSize=5 or Language="EN") or remain at their default values.
         */
        public static void ExampleAddOrChangeDestination()
        {
            RfcConfigParameters parameters = new RfcConfigParameters();
            parameters[RfcConfigParameters.Language] = "EN";
            parameters[RfcConfigParameters.PeakConnectionsLimit] = "5";
            parameters[RfcConfigParameters.ConnectionIdleTimeout] = "600"; // 600 seconds, i.e. 10 minutes
            parameters[RfcConfigParameters.Name] = ReadName();
            Console.Write("User: ");
            parameters[RfcConfigParameters.User] = Console.ReadLine();
            Console.Write("Password: ");
            parameters[RfcConfigParameters.Password] = Console.ReadLine();
            Console.Write("Client: ");
            parameters[RfcConfigParameters.Client] = Console.ReadLine();
            Console.Write("AS/MS Host: ");
            string server = Console.ReadLine();
            Console.Write("System Nr/ID: ");
            string sys = Console.ReadLine();
            int tmp;
            if (int.TryParse(sys, out tmp))
            {
                parameters[RfcConfigParameters.AppServerHost] = server;
                parameters[RfcConfigParameters.SystemNumber] = sys;
            }
            else
            {
                parameters[RfcConfigParameters.MessageServerHost] = server;
                parameters[RfcConfigParameters.SystemID] = sys;
                Console.Write("Logon Group: ");
                parameters[RfcConfigParameters.LogonGroup] = Console.ReadLine();
            }
            Console.Write("Trace: ");
            parameters[RfcConfigParameters.Trace] = Console.ReadLine();
            inMemoryDestinationConfiguration.AddOrEditDestination(parameters);
        }

        /*
         * This example removes a destination configuration.
         */
        public static void ExampleRemoveDestination()
        {
            inMemoryDestinationConfiguration.RemoveDestination(ReadName());
        }

        /*
         * This example pings a destination.
         */
        public static void ExamplePingDestination()
        {
            try
            {
                RfcDestinationManager.GetDestination(ReadName()).Ping();
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

        private static string ReadName()
        {
            Console.WriteLine();
            Console.Write("Name: ");
            return Console.ReadLine();
        }
    }
}

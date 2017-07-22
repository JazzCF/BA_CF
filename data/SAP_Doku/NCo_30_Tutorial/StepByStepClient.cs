using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SAP.Middleware.Connector;
using System.IO;

namespace SAP.Middleware.Connector.Examples
{
    public class StepByStepClient
    {
        // used by example "multi-threaded stateful calls with default session provider"
        static int pendingThreads = 0;

        // used by example "multi-threaded stateful calls with custom session provider"
        static ExampleSessionProvider exampleSessionProvider = null;
        static List<int> callResults = null;

        //
        // See the App.config file for the definition of the following RFC destinations.
        // Modify the logon parameters in that file to match a SAP system in your local network.
        // The available configuration parameters are described in the documentation of the
        // class RfcConfigParameters.
        // 
		const string ABAP_APP_SERVER = "NCO_TESTS";
        const string ABAP_MESSAGE_SERVER = "NCO_LB_TESTS";

        //
        // The .Net Connector 3.0 introduces a new destination-oriented concept. Applications work with
        // destination instances, which are configured per default in the application configuration file
        // (app.config) or which can alternatively be defined by explicitly registering an IDestinationConfiguration
        // object. A destination identifies the backend to which connections can be opened.
        // The .Net Connector runtime takes care of the connection management and its lifecycle.
        // So the application can concentrate on the business logic.
        // Note: A destination is not just a replacement for a connection. The .Net Connector runtime decides
        // if and how many connections are currently used for a given destination. The application
        // never has to deal with connections themselves.
        // 
        // The first example obtains two destinations through the destination manager by supplying the
        // name of the required destination. A connection is made to the backend system - first specified
        // through an application server ASHOST and then through a message server MSHOST - by requesting
        // the system attributes and/or by calling Ping() on the destination.
        // In the second case the system attributes may be different from call to call due to load balancing.
        // 
        // Opening a connection is a relatively time-consuming task. In some situations it can lead to
        // performance improvements if a certain number of connections are retained (i.e. they are not closed)
        // for further requests. This is what connection pooling does. A pool (i.e., a set) of connections is maintained
        // based on the settings of configuration parameters POOL_SIZE and MAX_POOL_SIZE (see RfcConfigParameters
        // and in particular the API of properties PoolSize and MaxPoolSize of class RfcDestination).
        // Connection pooling is done transparently by the .Net Conenctor runtime. Application
        // code does not require any changes, only the destination configuration is extended
        // by pool option POOL_SIZE (and optionally MAX_POOL_SIZE). Note that by default pooling is enabled
        // with a default pool size of 10 and unlimited MAX_POOL_SIZE. So in order to disable pooling
        // (in the sense of retaining currently unused connections) POOL_SIZE has to be set to 0.
        // If on top of that the number of connections that can be used simultaneously at any time needs
        // to be limited, MAX_POOL_SIZE has to be set to the desired maximal number of concurrently active
        // connections.
        //
		public static void ExamplePingDestination()
        {
			RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
			destination.Ping();
			Console.WriteLine("\nAttributes (application server logon):");
			Console.WriteLine(AttributesToString(destination.SystemAttributes));
			Console.WriteLine();


            destination = RfcDestinationManager.GetDestination(ABAP_MESSAGE_SERVER);
            destination.Ping();
            Console.WriteLine("\nAttributes (message server logon):");
            Console.WriteLine(AttributesToString(destination.SystemAttributes));
            Console.WriteLine();
        }

        
        // This example shows how to invoke the simple function module STFC_CONNECTION on an ABAP Application Server.
        
        // The .Net Connector provides IRfcFunction - a generic container for remote functions 
        // and their parameters as well as exceptions. After providing the required (import) parameters
        // such a function can be called on a backend defined by a destination.
        
        // The metadata of a function (IRfcFunction) defines the parameters and exceptions of the respective
        // function. This metadata is fetched from an ABAP application server's DDIC on demand and cached by
        // the .NET Connector in the so-called repository. A given destination defines which application server
        // to use for DDIC look-up. That destination is also used to gain access to the repository. Typically,
        // the destination for metadata look-up and for invoking the function produced from that metadata is the
        // same, but it does not have to be. Note that the metadata of each IRfcFunction is accessible via
        // property Metadata. Furthermore, metadata is unique in the sense that there is only one instance
        // of RfcFunctionMetadata for any given function name. (The same is true for all other kinds of metadata.)
        
        // There is no real need for an application to deal with metadata directly. For a given destination and
        // repository a function can be created without referring to metadata. The necessary metadata look-up
        // (either fetching from cache or a request to the application server) happens behind the scenes.
        // Nonetheless the metadata is available from the repository or from an IRfcFunction should it ever become
        // necessary to handle metadata for whatever reason (see RfcFunctionMetadata's API documentation for details).
        
        public static void ExampleSimpleCall()
        {
            // Get destination instance. The destination is configured in app.config.
            RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
            // you may, or may not, want the default trace level to match the trace level of the destination -- comment or uncomment the following line
            // RfcTrace.DefaultTraceLevel = destination.Parameters.GetTraceLevelAsUint();

            IRfcFunction rfcFunction =
                destination.Repository.          //get metadata repository associated with the destination
                  CreateFunction("STFC_CONNECTION");  //fetch or get cached function metadata and create a function container based on the function metadata

            // Set the import parameters, in this one import parameter REQUTEXT. The parameter is CHAR 255, but the .Net Connector runtime always trys to find
            // a suitable conversion between C# data types and ABAP data types.
            rfcFunction.SetValue("REQUTEXT", "Hello SAP");

            // Make the remote call
            rfcFunction.Invoke(destination);

            // Show result
            ShowFunction(rfcFunction);

            // A function can be reused to make another call, but be mindful about parameters that are affected by previous calls to a function,
            // in particular changing and table parameters, which may modify the behavior and result of subsequent calls.
            // Also, the destination on which a function is called may vary; here you have to watch out for possible metadata inconsistencies
            // in case the system delivering the metadata and the system to which the call is sent define the chosen function's metadata differently.
            rfcFunction.SetValue("REQUTEXT", "Hello again!");
            rfcFunction.Invoke(RfcDestinationManager.GetDestination(ABAP_APP_SERVER));
            ShowFunction(rfcFunction);

            // Metadata is unqiue
            RfcFunctionMetadata functionMetadata = destination.Repository.GetFunctionMetadata("STFC_CONNECTION");
            Console.WriteLine(
                "\nThe statement that the metadata associated with a function and the metadata retrieved from the respective repository are identical is always {0}.",
                (rfcFunction.Metadata == functionMetadata).ToString().ToUpper());

            // Besides using parameter names it is also possible to use indices. This is the more efficient way to set a parameter,
            // but it requires reliable knowledge on the mapping between names and indices. When in doubt use the metadata method
            // NameToIndex(string) or TryNameToIndex(string):
            int requTextIndex = functionMetadata.NameToIndex("REQUTEXT");

            // A function can also be created from the metadata, which actually is the only way to create it.
            // CreateFunction(string) on the repository is merely a convenience method that bypasses or
            // rather hides the metadata getter.
            IRfcFunction function2 = functionMetadata.CreateFunction();
            function2.SetValue(requTextIndex, "GOOD-BYE!");
            function2.Invoke(destination);
            ShowFunction(function2);
        }

        
        //This example demonstrates how to work with structures and tables.
        
        //Structures and functions are in essence very similar. They are both containers for elements,
        //namely parameters in the case of a function and fields in the case of a structure. Hence
        //setting and getting field values is analogous to setting and getting parameter values.
        //Also, each structure (an IRfcStructure) is based on unique metadata (RfcStructureMetadata)
        //just like functions.
        
        //Naturally functions and structures differ in terms of some aspects. Only functions can be
        //invoked, and only structures can appear as parameter or field values.
        
        //A table (IRfcTable) essentially is a list or array of structures (IRfcStructure).
        //Each row of the table is represented by a structure. By employing the CurrentIndex
        //to designate a particular row it is possible to work with a table as if it were a
        //structure. CurrentIndex can be viewed as cursor pointing to a certain row of the table,
        //allowing to navigate through the table to set or get field values. Alternatively,
        //a row - i.e., a IRfcStructure - can be retrieved based on its (zero-based) row index.
        
        public static void ExampleWorkWithStructuresAndTables()
        {
            RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
			// Create and invoke function module STFC_STRUCTURE
            IRfcFunction function = destination.Repository.CreateFunction("STFC_STRUCTURE");

			// Set some input values for the structure.
			IRfcStructure importStructure = function.GetStructure("IMPORTSTRUCT");
			importStructure.SetValue("RFCFLOAT", 17.0932);
			importStructure.SetValue("RFCCHAR1", 'a');
			importStructure.SetValue("RFCINT2", (short)17283);
			importStructure.SetValue("RFCINT1", (byte)127);
			importStructure.SetValue("RFCCHAR4", "hugo");
			importStructure.SetValue("RFCINT4", -4367283);
			importStructure.SetValue("RFCHEX3", new byte[3]{1, 2, 3});
			importStructure.SetValue("RFCCHAR2", "дц");
			importStructure.SetValue("RFCTIME", DateTime.Now);
			importStructure.SetValue("RFCDATE", new DateTime(1685, 3, 31));

			// Fill a few lines into the table.
			IRfcTable table = function.GetTable("RFCTABLE");
			table.Append(7);
			for (int i=0; i<table.Count; ++i) {
				table.CurrentIndex = i;
				table.SetValue(3, i);
                // Field at index 3 is "RFCINT1". In a loop over a large table it makes sense to access the fields by index
                // rather than by name, as that is much faster. Just look up the structure of the table in the DDIC and
                // note, which field has which index.
                // I leave the other fields empty for now. 
			}

			// Fire the thing off to the ABAP side
			function.Invoke(destination);

			// Get the structure ECHOSTRUCT
			IRfcStructure echoStructure = function.GetStructure("ECHOSTRUCT");

            // Show echo structure
            Console.WriteLine();
			Console.WriteLine("Loop over the fields of structure ECHOSTRUCT using standard 'for' loop:");
            //For each field in the structure ...
            for (int i = 0; i < echoStructure.Metadata.FieldCount; i++)
            {
                // ... print out the name and the value in string representation
                Console.WriteLine("{0}:\t{1}", echoStructure.Metadata[i].Name, echoStructure.GetString(i));
            }
            Console.WriteLine();

            // Note that it is also possible to iterate through the fields of a structure (as well as parameters of a function or elements of
            // any container for that matter) using the foreach loop. For all containers (IRfcStructure/IRfcTable, IRfcFunction, IRfcAbapObject)
            // there are corresponding elements (IRfcField, IRfcParameter, IRfcAttribute) that can be used to work with a particular element
            // in a more convenient way. Note, however, that except for IRfcFunction these objects will be generated every time they are requested.
            // So there is a certain performance penalty when choosing this type of loop or whenever working with IRfcField or IRfcAttribute.
            // (IRfcStructure and IRfcAbapObject only store the values as an array of System.Object, whereas IRfcFunction on top of parameter
            // values has to store the active flag for each parameter which warrants the use of an array of IRfcParameter to represent parameters.)
			Console.WriteLine("Loop over the fields of structure ECHOSTRUCT using 'foreach' loop:");
            foreach (IRfcField field in echoStructure)
            {
                Console.WriteLine("{0}:\t{1}", field.Metadata.Name, field.GetString());
            }

            //Now we loop over the RFCTABLE table. The ABAP side should have appended one line to it, using the values
            //givin in our IMPORTSTRUCT, so we should now have 8 lines.
            //Show number of rows and line type.
			Console.WriteLine();
			Console.WriteLine("Table has {0} rows and line type {1}.", table.RowCount, table.Metadata.LineType.Name);

            //Write table header. We only use the first 10 fields, so it can easily be printed to console. Make sure your
            //console is at least 120 characters wide. */
			Console.WriteLine();
            for (int j = 0; j < 10; ++j)
            {
                Console.Write("{0,-10}  ", table.Metadata.LineType[j].Name);
            }
			// Write table data:
			Console.WriteLine();
			for (int i=0; i<table.Count; ++i) {
				table.CurrentIndex = i;
				for (int j=0; j<10; ++j)
					Console.Write("{0,-10}  ", table.GetString(j));
				Console.WriteLine();
			}

            // Instead of looping over a table, we can also access a particular row (in this case the last row) directly without touching CurrentIndex
			IRfcStructure lastRow = table[table.RowCount - 1];
			Console.WriteLine();
			Console.WriteLine(
				"Last row: {0} RFCFLOAT={1} RFCINT2={2} RFCINT4={3} RFCTIME={4} RFCDATE={5}",
				(table.RowCount - 1).ToString(),
				lastRow.GetString("RFCFLOAT"),
				lastRow.GetString("RFCINT2"),
				lastRow.GetString("RFCINT4"),
				lastRow.GetString("RFCTIME"),
				lastRow.GetString("RFCDATE"));
        }


        //
        //This example shows how to achieve transactional security when sending a tRFC into the backend.
        //
		public static void ExampleTrfcClient() {
			RfcTransaction trans = null;
            string data = "";
			TidStore tidStore = new TidStore("clientTidStore", false);
            bool quit = false;
		try{
	            while (!quit){
	                Console.Write("Resend an existing LUW, create a new one or quit? [r/c/q] ");
	                String answer = Console.ReadLine();

	                switch (answer)
	                {
	                    case "r":
	                        trans = OpenTransaction(tidStore, out data);
	                        break;
	                    case "c":
	                        trans = CreateTransaction(tidStore, out data);
	                        break;
	                    default:
	                        quit = true;
	                        trans = null;
	                        break;
	                }

	                // Now we try to send that thing off into the backend.
	                if (trans != null)
	                {
	                    SubmitTransaction(trans, tidStore, data);
	                }
	            }
		}
		finally{
			tidStore.Close();
		}
		}

        static RfcTransaction OpenTransaction(TidStore tidStore, out string data)
        {
            RfcTransaction trans = null;
            data = "";
            if (tidStore.Size == 0)
            {
                Console.WriteLine("No old LUWs exist. Let's create a new one instead...");
                return CreateTransaction(tidStore, out data);
            }

            // Let the user choose one of the previously failed LUWs:
            tidStore.PrintOverview();
            string tid = ChooseTid(tidStore);
            if(string.IsNullOrEmpty(tid))
            {
                Console.WriteLine("Sorry, the entered TID does not exist, please try again.");
                tid = ChooseTid(tidStore);
            }

            if (string.IsNullOrEmpty(tid))
            {
                Console.WriteLine("Sorry, the entered TID does not exist, Bye!.");
                return null;
            }

            // Read the payload back in
            FileStream dataFile = new FileStream(tid, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(dataFile);
            data = reader.ReadLine();
            dataFile.Close();
            // Recreate the transaction with the existing TID.
            trans = new RfcTransaction(new RfcTID(tid));

            return trans;
        }

        private static string ChooseTid(TidStore tidStore)
        {
            Console.Write("Please choose an existing TID: ");
            string tid = Console.ReadLine();
            try
            {
                // Double check, whether that TID really exists:
                string errorMessage = "";
                tidStore.GetStatus(tid, out errorMessage);
            }
            catch (ArgumentException)
            {
                tid = "";
            }
            return tid;
        }

        static RfcTransaction CreateTransaction(TidStore tidStore, out string data)
        {
            RfcTransaction trans = new RfcTransaction(); // This creates a fresh TID.;
            Console.Write("Please enter some input data: ");
            data = Console.ReadLine();

            // Persist the payload, so it can later be resend, in case something goes wrong the first time:
            FileStream dataFile = new FileStream(trans.Tid.TID, FileMode.Create, FileAccess.ReadWrite);
            byte[] utf8Data = Encoding.UTF8.GetBytes(data);
            dataFile.Write(utf8Data, 0, utf8Data.Length);
            dataFile.Close();
            tidStore.CreateEntry(trans.Tid.TID);
            return trans;
        }

        private static void SubmitTransaction(RfcTransaction trans, TidStore tidStore, String data)
        {
            try
            {
                RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
                IRfcFunction stfc_write_to_tcpic = destination.Repository.CreateFunction("STFC_WRITE_TO_TCPIC");
                // In some releases, STFC_WRITE_TO_TCPIC does not have this parameter. Uncomment the following line, if it does:
                //stfc_write_to_tcpic.SetParameterActive("RESTART_QNAME", false);
                IRfcTable dataTable = stfc_write_to_tcpic.GetTable("TCPICDAT");
                dataTable.Append();
                dataTable.SetValue(0, data);

                // Insert the function module into the transaction:
                trans.AddFunction(stfc_write_to_tcpic);

                // In order to demonstrate that a tRFC LUW can consist of several function modules,
                // we clone the function object, modify the data in a fixed way and add a second
                // FM to the transaction.
                // Of course in this particular case we could as well add a second line to the
                // original function object, but this is to demonstrate that several different
                // function modules (even of different type) can be added to one LUW and treated as
                // an "atomic unit".
                //
                stfc_write_to_tcpic = (IRfcFunction)stfc_write_to_tcpic.Clone();
                dataTable = stfc_write_to_tcpic.GetTable("TCPICDAT");
                dataTable.SetValue(0, data + " -- data of the second function module");
                trans.AddFunction(stfc_write_to_tcpic);

                // If you want to make this example a bit interesting, you could change the logon parameters in
                // App.config in the following way:
                // Make sure that ABAP_MESSAGE_SERVER has valid logon parameters, so that it can successfully used
                // for DDIC lookups.
                // In ABAP_APP_SERVER, add a value REPOSITORY_DESTINATION= so that the DDIC lookup in
                // CreateFunction("STFC_WRITE_TO_TCPIC") can be done successfully. Then make the user/hostname etc invalid,
                // so that we get and error in the following line (RfcLogonException, RfcCommunicationException, etc).
                // afterwards you can correct that mistake, re-run the program and send the transaction a second time.
                trans.Commit(destination);
                tidStore.SetStatus(trans.Tid.TID, TidStatus.Committed, null);

                File.Delete(trans.Tid.TID);

                // Only now, after everything was successful - including the deletion of the data on our side, so that
                // we can be absolutely sure that this transaction will never be repeated from our side - do we confirm
                // the TID in the backend. From then on the backend would no longer be protected against a duplicate
                // update.
                destination.ConfirmTransactionID(trans.Tid);
                tidStore.DeleteEntry(trans.Tid.TID);
                // Now use SE16 to check, whether two rows have been added to table TCPIC.
            }
            catch (Exception e)
            {
                tidStore.SetStatus(trans.Tid.TID, TidStatus.RolledBack, e.Message);
            }
        }

        //
        // This example shows how to achieve transactional security when sending a bgRFC unit into the backend.
        //
		public static void ExampleBgrfcClient() {
			RfcBackgroundUnit unit = null;
            string data = "";
			TidStore tidStore = new TidStore("clientTidStore", true);
            bool quit = false;
			try {
				while (!quit) {
					Console.Write("Resend an existing LUW, create a new one or quit? [r/c/q] ");
					String answer = Console.ReadLine();

					switch (answer) {
						case "r":
							unit = OpenBgUnit(tidStore, out data);
							break;

						case "c":
							unit = CreateBgUnit(tidStore, out data);
							break;
						default:
							quit = true;
							unit = null;
							break;
					}

					// Now we try to send that thing off into the backend.
					if (unit != null) {
						SubmitBgUnit(unit, tidStore, data);
					}
				}
			}
			finally {
				tidStore.Close();
			}
		}

        static RfcBackgroundUnit OpenBgUnit(TidStore tidStore, out string data)
        {
            
            if (tidStore.Size == 0)
            {
                Console.WriteLine("No old LUWs exist. Let's create a new one instead...");
                return CreateBgUnit(tidStore, out data);
            }
            data = "";
            // Let the user choose one of the previously failed LUWs:
            tidStore.PrintOverview();
            string uid = ChooseUnitId(tidStore);
            if (string.IsNullOrEmpty(uid))
            {
				Console.WriteLine("Sorry, the entered Unit ID does not exist, please try again.");
                uid = ChooseTid(tidStore);
            }

            if (string.IsNullOrEmpty(uid))
            {
				Console.WriteLine("Sorry, the entered Unit ID does not exist, Bye!");
                return null;
            }

            // Read the payload back in
            FileStream dataFile = new FileStream(uid, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(dataFile);
            data = reader.ReadLine();
            dataFile.Close();
            // Recreate the transaction with the existing uid.
            RfcBackgroundUnit unit = new RfcBackgroundUnit(new RfcUnitID(new Guid(uid), RfcUnitType.TRANSACTIONAL));

            return unit;
        }

        private static string ChooseUnitId(TidStore tidStore)
        {
            Console.Write("Please choose an existing Unit ID: ");
            string uid = Console.ReadLine();
            try
            {
                // Double check, whether that TID really exists:
                string errorMessage = "";
                tidStore.GetStatus(uid, out errorMessage);
            }
            catch (ArgumentException)
            {
                uid = "";
            }
            return uid;
        }

        static RfcBackgroundUnit CreateBgUnit(TidStore tidStore, out string data)
        {
            RfcBackgroundUnit unit = new RfcBackgroundUnit(new RfcUnitID(RfcUnitType.TRANSACTIONAL)); // This creates a fresh GUID.
            string uid = unit.UnitID.Uuid.ToString("N");
            Console.Write("Please enter some input data: ");
            data = Console.ReadLine();

            // Persist the payload, so it can later be resend, in case something goes wrong the first time:
            FileStream dataFile = new FileStream(uid, FileMode.Create, FileAccess.ReadWrite);
            byte[] utf8Data = Encoding.UTF8.GetBytes(data);
            dataFile.Write(utf8Data, 0, utf8Data.Length);
            dataFile.Close();

            tidStore.CreateEntry(uid);

            return unit;
        }

        private static void SubmitBgUnit(RfcBackgroundUnit unit, TidStore tidStore, String data)
        {
            string uid = unit.UnitID.Uuid.ToString("N");
            try
            {
                RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
                IRfcFunction stfc_write_to_tcpic = destination.Repository.CreateFunction("STFC_WRITE_TO_TCPIC");
                // In some releases, STFC_WRITE_TO_TCPIC does not have this parameter. Uncomment the following line, if it does:
                //stfc_write_to_tcpic.SetParameterActive("RESTART_QNAME", false);
                IRfcTable dataTable = stfc_write_to_tcpic.GetTable("TCPICDAT");
                dataTable.Append();
                dataTable.SetValue(0, data);

                // Insert the function module into the background unit:
                unit.AddFunction(stfc_write_to_tcpic);

                // In order to demonstrate that a bgRFC LUW can consist of several function modules,
                // we clone the function object, modify the data in a fixed way and add a second
                // FM to the transaction.
                // Of course in this particular case we could as well add a second line to the
                // original function object, but this is to demonstrate that several different
                // function modules (even of different type) can be added to one LUW and treated as
                // an "atomic unit".
                //
                stfc_write_to_tcpic = (IRfcFunction)stfc_write_to_tcpic.Clone();
                dataTable = stfc_write_to_tcpic.GetTable("TCPICDAT");
                dataTable.SetValue(0, data + " -- data of the second function module");
                unit.AddFunction(stfc_write_to_tcpic);

                // If you want to make this example a bit interesting, you could change the logon parameters in
                // App.config in the following way:
                // Make sure that ABAP_MESSAGE_SERVER has valid logon parameters, so that it can successfully used
                // for DDIC lookups.
                // In ABAP_APP_SERVER, add a value REPOSITORY_DESTINATION= so that the DDIC lookup in
                // CreateFunction("STFC_WRITE_TO_TCPIC") can be done successfully. Then make the user/hostname etc invalid,
                // so that we get and error in the following line (RfcLogonException, RfcCommunicationException, etc).
                // afterwards you can correct that mistake, re-run the program and send the transaction a second time.
                unit.Commit(destination);
             
                tidStore.SetStatus(uid, TidStatus.Committed, null);

                File.Delete(uid);

                // Only now, after everything was successful - including the deletion of the data on our side, so that
                // we can be absolutely sure that this transaction will never be repeated from our side - do we confirm
                // the TID in the backend. From then on the backend would no longer be protected against a duplicate
                // update.
                destination.ConfirmUnitID(unit.UnitID);
                tidStore.DeleteEntry(uid);
                // Now use SE16 to check, whether two rows have been added to table TCPIC.
            }
            catch (Exception e)
            {
                tidStore.SetStatus(uid, TidStatus.RolledBack, e.Message);
            }
        }

		
         //This example demonstrates the "simple" stateful call sequence. All calls belonging to one
         //session are executed within the same thread. 
         
         //Note: this example uses Z_GET_COUNTER and Z_INCREMENT_COUNTER. Most ABAP systems
         //contain function modules GET_COUNTER and INCREMENT_COUNTER, which however, are not remote enabled.
         //Copy these functions to Z_GET_COUNTER and Z_INCREMENT_COUNTER (or implement as wrapper)
         //and mark them as remote enabled.
        
        public static void ExampleSimpleStatefulCalls()
        {
            RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
            IRfcFunction incrementCounter, getCounter;
            try
            {
                // create functions Z_INCREMENT_COUNTER and Z_GET_COUNTER
                incrementCounter = destination.Repository.CreateFunction("Z_INCREMENT_COUNTER");
                getCounter = destination.Repository.CreateFunction("Z_GET_COUNTER");
            }
            catch (RfcBaseException ex)
            {
                Console.WriteLine("\nThis example cannot run without function modules Z_INCREMENT_COUNTER and Z_GET_COUNTER ({0})", ex.Message);
                return;
            }

            const int loops = 5;

            // increment and get counter a number of times; counter will always be 0
            Console.WriteLine();
            Console.WriteLine("Without stateful call sequence:");
            for (int i = 0; i < loops; i++)
            {
                incrementCounter.Invoke(destination);
                getCounter.Invoke(destination);
                int counter = getCounter.GetInt("GET_VALUE");
                Console.WriteLine("Call {0} --> counter = {1} ({2} value)", i + 1, counter, counter == 0 ? "correct" : "WRONG");
            }

            Console.WriteLine();
			Console.WriteLine("With stateful call sequence:");
            RfcSessionManager.BeginContext(destination);
            // increment and get counter a number of times; counter value will grow by one with each iteration
            try
            {
                for (int i = 0; i < loops; i++)
                {
                    incrementCounter.Invoke(destination);
                    getCounter.Invoke(destination);
                    int counter = getCounter.GetInt("GET_VALUE");
                    Console.WriteLine("Call {0} --> counter = {1} ({2} value)", i + 1, counter, counter == i + 1 ? "correct" : "WRONG");
                }
            }
            finally
            {
                // it is a good programming style to ensure EndContext is called no matter what happens after BeginContext
                RfcSessionManager.EndContext(destination);
            }
        }

        //
        // This examples demonstrates stateful calls in a multi-threaded environment. In this example a number of threads
        // execute stateful calls of the same function module on one and the same destination, each thread using a session
        // of its own. For this scenario the default session provider is sufficient since it is designed to associate a
        // session with each thread. As a matter of fact, the default session provider uses thread IDs as session IDs. Thus,
        // this scenario perfectly suits the default session provider, and there is no reason to implement a session provider.
        //
        public static void ExampleMultiThreadedStatefulCallsWithDefaultSessionProvider()
        {
            // first ensure the required function modules are available
            try
            {
                RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
                destination.Repository.GetFunctionMetadata("Z_INCREMENT_COUNTER");
                destination.Repository.GetFunctionMetadata("Z_GET_COUNTER");
            }
            catch (RfcBaseException ex)
            {
                Console.WriteLine("\nThis example cannot run without function modules Z_INCREMENT_COUNTER and Z_GET_COUNTER ({0})", ex.Message);
                return;
            }

            // start a number of threads, each executing stateful calls to the same destination in a session of its own
            const int threads = 5;
			pendingThreads = 0;
            for (int i = 0; i < threads; i++)
            {
                Interlocked.Increment(ref pendingThreads);
                new Thread(RunIncrementCounterInSessionPerThread).Start();
            }

            while (pendingThreads > 0)
            {
                Thread.CurrentThread.Join(500);
            }

            Console.WriteLine("\nAll threads terminated");
        }

        
        // This example demonstrates stateful calls in a multi-threaded environment. In this example different threads
        // are to call a function module in the same session. Using the default session provider results in stateless
        // calls since each thread is associated with a different session, and in particular the thread executing the
        // BeginContext method is different from all threads executing Z_INCREMENT_COUNTER and Z_GET_COUNTER.
        
        // A very simple custom session provider that manages a single given session ID allows to achieve the desired
        // effect that different threads execute stateful calls within the same session. Note that the application must
        // ensure that different threads associated with the same session must not execute in parallel, since this
        // is very likely to cause issues by concurrently using the same connection for different requests.
        public static void ExampleMultiThreadedStatefulCallsWithCustomSessionProvider()
        {
            // first ensure the required function modules are available
            try
            {
				RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
                destination.Repository.GetFunctionMetadata("Z_INCREMENT_COUNTER");
                destination.Repository.GetFunctionMetadata("Z_GET_COUNTER");
            }
            catch (RfcBaseException ex)
            {
                Console.WriteLine("\nThis example cannot run without function modules Z_INCREMENT_COUNTER and Z_GET_COUNTER ({0})", ex.Message);
                return;
            }

            // Use default session provider, which will produce a list of call results consisting of 0s only -- none of the threads executing
            // Z_INCREMENT_COUNTER nor Z_GET_COUNTER are tied to the session created in RunIncrementCounterInOneSession by a different thread;
            // those calls are effectively stateless.
            RunIncrementCounterInOneSession();
            Console.Write("\nCall Results for default session provider:");
            foreach (int i in callResults)
            {
                Console.Write(" {0}", i);
            }
            Console.WriteLine();

            // Register custom session provider
            if (exampleSessionProvider == null)
            {
                exampleSessionProvider = new ExampleSessionProvider();
            }
            else
            {
                exampleSessionProvider.SetSession(exampleSessionProvider.CreateSession());
            }
            RfcSessionManager.RegisterSessionProvider(exampleSessionProvider);

            // Use the custom session provider to repeat the above calls -- this time all threads will run in the same session, thus
            // producing a list of call results consisting of 1, 2, ...
            RunIncrementCounterInOneSession();
            Console.Write("Call Results for custom session provider :");
            foreach (int i in callResults)
            {
                Console.Write(" {0}", i);
            }
            Console.WriteLine();

            // Unregister custom session provider to revert to default session provider
            RfcSessionManager.UnregisterSessionProvider(exampleSessionProvider);
        }

        
        // This example demonstrates the use of custom destinations.
        
        // A custom destination can be derived from a configured destination. It allows to modify certain configuration parameters
        // without having to change the configuration file (XML). A custom destination is particulary useful in a scenario where
        // any number of (different) users require access to possibly just one backend system. Since it is not feasible to create
        // a destination for all possible users, simply configure one destination for each relevant backend system without providing
        // user and password. Whenever a user request is received, derive a custom destination from such a "raw" destination and
        // set user and password appropriately.
        public static void ExampleCustomDestination()
        {
            // NCO_RAW is assumed to be the raw destination that does not supply user nor password
            RfcDestination destination = RfcDestinationManager.GetDestination("NCO_RAW");
             Console.WriteLine("\nConfigured Destination: {0} [ {1} ]", destination.Name, destination.Parameters.ToString());
            RfcCustomDestination custDest = destination.CreateCustomDestination();
            try
            {
                custDest.Ping();
                Console.WriteLine("Error: {0} already has valid user and password!", destination.Name);
				return;
            }
            catch (RfcLogonException)
            {
                Console.WriteLine("Everything ok: unable to login before setting user and password");
            }

			Console.Write("User: ");
			custDest.User = Console.ReadLine();
			Console.Write("Password: ");
			custDest.Password = Console.ReadLine();
            try
            {
                custDest.Ping();
                Console.WriteLine("Ping successful after setting user and password");
            }
            catch (RfcBaseException)
            {
				Console.WriteLine("Ping fails after setting user and password: user/password for {0} invalid?", custDest.User);
            }
        }

		//
		// This example shows, how the various types of errors, that the ABAP side may raise, can be handled in
		// an NCo client program.
		//
		// The three exceptions RfcCommunicationException, RfcLogonException and RfcAbapRuntimeException should
		// always be handled, because these represent three problems that could always occur when trying to
		// execute an ABAP function module in the backend: there could be a network problem, the provided user
		// credentials could be invalid/expired (or the user locked) or the ABAP runtime of the backend system could run
		// into some kind of low level problem (out of memory, work process timeout, division by zero, database deadlock
		// etc.).
		// Then the next exceptions to catch depend on what the function module in question may throw. If you know
		// that the FM throws only plain ABAP exceptions (or ABAP exceptions with sy-fields), you only need to catch
		// RfcAbapException. If you know that your FM also uses ABAP messages or in modern systems even ABAP class-based
		// exceptions, you also need to handle RfcAbapMessageException and RfcAbapClassException.
		//
		public static void ExampleCatchErrors() {
			RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
			IRfcFunction rfc_raise_error = destination.Repository.CreateFunction("RFC_RAISE_ERROR");
			rfc_raise_error.SetValue("MESSAGETYPE", "E");

            // First we try a plain ABAP exception. This corresponds to ABAP keyword
            // RAISE RAISE_EXECPTION.
            // The result should be an exception of type "RAISE_EXCEPTION" (what a confusing name...)
            //
			try {
				rfc_raise_error.SetValue("METHOD", "2");
				rfc_raise_error.Invoke(destination);
			}
			catch (RfcAbapException abapException) {
				Console.WriteLine("Caught ABAP exception {0}", abapException.Key);
			}
			catch (RfcAbapRuntimeException) {
			}
			catch (RfcLogonException) {
			}
			catch (RfcCommunicationException) {
			}


            // Next is an ABAP exception with sy-fields. In ABAP this is thrown via
            // MESSAGE ID 'SR' TYPE 'E' NUMBER '006' WITH l_msgv1 RAISING RAISE_EXECPTION.
            // The result should be an exception of type "RAISE_EXCEPTION" and the sy-fields
            // filled as follows.
            //
			try {
				rfc_raise_error.SetValue("METHOD", "1");
				rfc_raise_error.Invoke(destination);
			}
			catch (RfcAbapException abapException) {
				Console.WriteLine("Caught ABAP exception {0}", abapException.Key);
				Console.WriteLine("Message parameters were filled as follows:\nSY-MSGTY={0}\nSY-MSGID={1}\nSY-MSGNO={2}\nSY-MSGV1={3}",
					abapException.AbapMessageType,
					abapException.AbapMessageClass,
					abapException.AbapMessageNumber,
					abapException.AbapMessageParameters[0]);
			}
			catch (RfcAbapRuntimeException) {
			}
			catch (RfcLogonException) {
			}
			catch (RfcCommunicationException) {
			}
		}

        static String AttributesToString(RfcSystemAttributes attr)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("             Destination: {0}\n", attr.Destination));
            sb.Append(string.Format("               Host Name: {0}\n", attr.HostName));
            sb.Append(string.Format("                    User: {0}\n", attr.User));
            sb.Append(string.Format("                  Client: {0}\n", attr.Client));
            sb.Append(string.Format("            ISO Language: {0}\n", attr.ISOLanguage));
            sb.Append(string.Format("                Language: {0}\n", attr.Language));
            sb.Append(string.Format("          Kernel Release: {0}\n", attr.KernelRelease));
            sb.Append(string.Format("               Code Page: {0}\n", attr.CodePage));
            sb.Append(string.Format("       Partner Code Page: {0}\n", attr.PartnerCodePage));
            sb.Append(string.Format("            Partner Host: {0}\n", attr.PartnerHost));
            sb.Append(string.Format("         Partner Release: {0}\n", (attr.PartnerRelease)));
            sb.Append(string.Format("  Partner Release Number: {0}\n", attr.PartnerReleaseNumber));
            sb.Append(string.Format("            Partner Type: {0}\n", attr.PartnerType));
            sb.Append(string.Format("                 Release: {0}\n", attr.Release));
            sb.Append(string.Format("                RFC Role: {0}\n", attr.RfcRole));
            sb.Append(string.Format("               System ID: {0}\n", attr.SystemID));
            sb.Append(string.Format("           System Number: {0}\n", attr.SystemNumber));
            return sb.ToString();
        }

        static void ShowFunction(IRfcFunction fct)
        {
            Console.WriteLine("\n{0}", fct.Metadata.Name);
            foreach (IRfcParameter parameter in fct)
            {
                Console.WriteLine("  {0}: {1}", parameter.Metadata.Name, parameter.GetString());
            }
        }

        static void RunIncrementCounterInSessionPerThread()
        {
            RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
            IRfcFunction incrementCounter = destination.Repository.CreateFunction("Z_INCREMENT_COUNTER");
            IRfcFunction getCounter = destination.Repository.CreateFunction("Z_GET_COUNTER");

            const int loops = 3;

            RfcSessionManager.BeginContext(destination);
            // increment and get counter a number of times; counter value will grow by one with each iteration
            try
            {
                int counter = 0;
                for (int i = 1; i <= loops; i++)
                {
                    incrementCounter.Invoke(destination);
                    getCounter.Invoke(destination);
                    counter = getCounter.GetInt("GET_VALUE");
                    if (counter != i)
                    {
                        lock (typeof(StepByStepClient))
                        {
                            Console.WriteLine("\nThread {0} gets wrong value {1} (expected: {2})", Thread.CurrentThread.ManagedThreadId, counter, i);
                        }
                    }
                }
                lock (typeof(StepByStepClient))
                {
                    Console.WriteLine("\nThread {0} terminates with counter = {1} ({2} value)", Thread.CurrentThread.ManagedThreadId, counter, counter == loops ? "correct" : "WRONG");
                }
            }
            finally
            {
                // It is a good programming style to ensure EndContext is called no matter what happens after BeginContext.
                // Otherwise there would be a serious connection leak, resulting in the connection pool either opening more and
                // more connections, or running out of connections when reaching MAX_POOL_SIZE. 
                RfcSessionManager.EndContext(destination);
            }
            // Decrement the number of pending threads before exiting this thread.
            Interlocked.Decrement(ref pendingThreads);
        }

        static void RunIncrementCounterInOneSession()
        {
            // clear or create list of call results
            if (callResults == null)
            {
                callResults = new List<int>();
            }
            else
            {
                callResults.Clear();
            }

            // A given number of threads will each invoke Z_INCREMENT_COUNTER once -- depending on the session provider,
            // the final values in callResults will vary.
			RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
            RfcSessionManager.BeginContext(destination);
            try
            {
                for (int i = 1; i <= 5; i++)
                {
                    new Thread(InvokeIncrementCounter).Start();
                    // Wait here until all the started thread completed the call and exits.
                    while (callResults.Count < i)
                    {
                        Thread.CurrentThread.Join(500);
                    }
                }

            }
            finally
            {
                // It is a good programming style to ensure EndContext is called no matter what happens after BeginContext.
                RfcSessionManager.EndContext(destination);
            }
        }

        static void InvokeIncrementCounter()
        {
			RfcDestination destination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER);
            destination.Repository.CreateFunction("Z_INCREMENT_COUNTER").Invoke(destination);
            lock (callResults)
            {
                IRfcFunction getCounter = destination.Repository.CreateFunction("Z_GET_COUNTER");
                getCounter.Invoke(destination);
                callResults.Add(getCounter.GetInt("GET_VALUE"));
             }
        }
    }

    
    // This is a rudimentary session provider that is used in the example on several threads
    // executing calls in the same session (ExampleMultiThreadedStatefulCallsWithCustomSessionProvider).
    
    // When using a session provider like this the application needs to ensure that session IDs are set
    // appropriately, and that no more than one thread can execute calls in a session at any given time.
    
    internal class ExampleSessionProvider : ISessionProvider
    {
        int sessionCounter = 0;

        string sessionID;

        internal ExampleSessionProvider()
        {
            this.sessionID = CreateSession();
        }

        internal void SetSession(string sessionID)
        {
            this.sessionID = sessionID;
        }

        #region ISessionProvider Members

        public String GetCurrentSession()
        {
            return sessionID;
        }

        public void ContextStarted()
        {
        }

        public void ContextFinished()
        {
        }

        public String CreateSession()
        {
            return "Session_" + Convert.ToString(Interlocked.Increment(ref sessionCounter));
        }

        public void PassivateSession(String sessionID)
        {
        }

        public void ActivateSession(String sessionID)
        {
        }

        public void DestroySession(String sessionID)
        {
            if (sessionID != null && sessionID.Equals(this.sessionID))
            {
                this.sessionID = null;
            }
        }

        public bool IsAlive(String sessionID)
        {
            return sessionID != null && sessionID.Equals(this.sessionID);
        }

        public bool ChangeEventsSupported()
        {
            return false;
        }

#pragma warning disable 0067
        public event RfcSessionManager.SessionChangeHandler SessionChanged;
#pragma warning restore 0067

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

using SAP.Middleware.Connector;

namespace SAP.Middleware.Connector.Examples
{
    //
    // The examples in this class rely on server configuration NCO_SERVER in App.config.
    // Modify GWHOST, GWSERV and possibly PROGRAM_ID to suit your system needs. Also, on ABAP side
    // a destination for a registered server program (SM59, type T, matching the program ID
    // of the configuration) has to be available through which requests can be sent to the server.
    //
    public class StepByStepServer
    {
        private const string NCO_SERVER_CONFIG_NAME = "NCO_SERVER";

        //
        // This example demonstrates a minimal server implementation using a static function handler.
        // 
        // Once the server is started STFC_CONNECTION can be called on the ABAP side using
        // the above mentioned SM59 destination. Any other function module will incur an error
        // (a SYSTEM_FAILURE) since only STFC_CONNECTION can be handled by the handler
        // supplied to the server.
        //
        public static void ExampleSimpleServer()
        {
            // Creates a server instance using the NCO_SERVER configuration and the given array of function handlers.
            // Static function handlers are suited for stateless functions;
            // several handlers can be passed to the server - in this case there is only one.
            RfcServer server = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, new Type[] { typeof(StfcConnectionStaticImpl) });
            // You may, or may not, want the default trace level to match the trace level of the server -- comment or uncomment the following line
            //RfcTrace.DefaultTraceLevel = server.Parameters.GetTraceLevelAsUint();
            // Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start();
            Console.WriteLine();
            Console.WriteLine("Server started: {0}", server.Parameters.ToString());
            Console.WriteLine("You can now send requests (through STFC_CONNECTION only) -- press ENTER to stop the server");
            Console.ReadLine();
            Console.WriteLine("Server shutting down...");
            // Shut down the server, aborting any active calls
            server.Shutdown(true);
        }

		//
		// This example demonstrates a server permitting stateful requests.
		// 
		// The easiest way to implement stateful scenarios is by using instance methods. In that case the .Net Connector runtime
		// will create a new instance of the handler class for each ABAP user session and keep the instance as long as the corresponding
		// stateful session is alive. This is the approach the current example will follow.
		// However, if creation/destruction of your classes implementing the "server functions" would be to expensive, you can as well
		// use static methods (avoiding object creation by the NCo runtime) and implement the necessary stateful behavior yourself
		// using RfcServerContext.SessionID inside the static server function implementation.
		//
		public static void ExampleStatefulServer()
        {
            // Creates a server instance using the NCO_SERVER configuration and the given function handler.
            // Stateful requests can be handled by instance methods only;
            // several handlers can be passed to the server - in this case there is only one.
            RfcServer server = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, new Type[] { typeof(StfcConnectionImpl) });
            // Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start();
            Console.WriteLine();
            Console.WriteLine("Server started: {0}", server.Parameters.ToString());
            Console.WriteLine("You can now send (three) stateful requests (through STFC_CONNECTION only) -- press ENTER to stop the server");
            Console.ReadLine();
            Console.WriteLine("Server shutting down...");
            // Shut down the server, aborting any active calls
            server.Shutdown(true);
        }

        //
        // This example demonstrates how to implement a generic function handler.
        // 
        // In a generic function handler the handling method (or several such methods) is not limited
        // to a particular function through the annotation. Instead, the annotation designates the method
        // as a default handler which takes on any request that is not handled otherwise.
        // 
        // In the example the static handler for STFC_CONNECTION is passed to the server together with
        // a generic handler. Thus STFC_CONNECTION will be handled by the handler specializing on that
        // very function, whereas the generic handler takes care of everything else.
        // 
        // In this example handlers for server and application errors are registered that produce console
        // output in case such errors occur. As the preceding examples showed, registering event handlers
        // of this kind is not necessary. It is, however, a way to be notified and to take appropriate
        // actions, should issues arise.
        // 
        // Note that the generic handler in this example does not really handle any (other) function
        // sensibly. It simply does nothing, which may in effect result in a successful call in many cases.
        //
        public static void ExampleGenericServer()
        {
            // Creates a server instance using the NCO_SERVER configuration and the function handler for STFC_CONNECTION and a generic handler
            RfcServer server = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, new Type[] { typeof(ServerFunctionGenericImpl), typeof(StfcConnectionStaticImpl) });
            // Register event handlers for internal and application errors
            server.RfcServerError += OnRfcServerError;
            server.RfcServerApplicationError += OnRfcServerApplicationError;
            // Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start();
            Console.WriteLine();
            Console.WriteLine("Server started: {0}", server.Parameters.ToString());
            Console.WriteLine("You can now send requests (through any function module) -- press ENTER to stop the server");
            Console.ReadLine();
            Console.WriteLine("Server shutting down...");
            // Shut down the server, aborting any active calls
            server.Shutdown(true);
            // Remove error handlers so that other examples using the same server can start from scratch
            server.RfcServerError -= OnRfcServerError;
            server.RfcServerApplicationError -= OnRfcServerApplicationError;
        }

		//
		// This example demonstrates how to return the different error types to ABAP.
		// These are described in more detail in the class RaiseErrorImpl, see comments there for more information.
		// In order to call this server, use an ABAP report like the one in Z_CALL_EXTERNAL_SERVER.abap available in this tutorial.
		// 
		//
		public static void ExampleThrowErrors() {
			// Creates a server instance using the NCO_SERVER configuration and the function handler for RFC_RAISE_ERROR
			RfcServer server = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, new Type[] { typeof(RaiseErrorImpl) });
			// Register event handlers for internal and application errors
			server.RfcServerError += OnRfcServerError;
			server.RfcServerApplicationError += OnRfcServerApplicationError;
			// Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
			server.Start();
			Console.WriteLine();
			Console.WriteLine("Server started: {0}", server.Parameters.ToString());
			Console.WriteLine("You can now send requests (using ABAP report Z_CALL_EXTERNAL_SERVER) -- press ENTER to stop the server");
			Console.ReadLine();
			Console.WriteLine("Server shutting down...");
			// Shut down the server, aborting any active calls
			server.Shutdown(true);
			// Remove error handlers so that other examples using the same server can start from scratch
			server.RfcServerError -= OnRfcServerError;
			server.RfcServerApplicationError -= OnRfcServerApplicationError;
		}

		//
		// This example demonstrates how to implement a TID handler and dispatch transactional calls (i.e., "call function in background task").
		// 
		// The following coding on the ABAP side will submit a request for execution of STFC_CONNECTION in a background task.
		// (Set up a destination NCO_SERVER - or whichever name you choose - as described above.)
		// 
		// CALL FUNCTION 'STFC_CONNECTION' IN BACKGROUND TASK DESTINATION 'NCO_SERVER'.
		//   EXPORTING
		//     requtext = 'A tRFC Call'.
		//
		// COMMIT WORK.
		//
		public static void ExampleTRfcServer()
        {
            // Creates a server instance using the NCO_SERVER configuration and the function handler for STFC_CONNECTION
            RfcServer server = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, new Type[] { typeof(StfcConnectionStaticImpl) });
            // Register event handlers for internal and application errors
            server.RfcServerError += OnRfcServerError;
            server.RfcServerApplicationError += OnRfcServerApplicationError;
            // Register transaction ID handler
			if (server.TransactionIDHandler == null) server.TransactionIDHandler = new MyTidHandler();
            // Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start();
            Console.WriteLine();
            Console.WriteLine("Server started: {0}", server.Parameters.ToString());
            Console.WriteLine("You can now send requests for STFC_CONNECTION (synchronous or in background task) -- press X to stop the server");
            while (Console.ReadLine() != "X");
            Console.WriteLine("Server shutting down...");
            // Shut down the server, aborting any active calls
            server.Shutdown(true);
            // Remove error and TID handlers so that other examples using the same server can start from scratch
            server.RfcServerError -= OnRfcServerError;
            server.RfcServerApplicationError -= OnRfcServerApplicationError;
            server.TransactionIDHandler = null;
        }

		public static void ExampleShowTRfcs() {
			MyTidHandler.ListLUWs();
		}


		//
		// This example demonstrates how to implement a UnitID handler and dispatch bgRFC calls.
		// 
		// The following coding on the ABAP side will submit a request for execution of STFC_CONNECTION in a background unit of type "T".
		// Sending background units of type "Q" requires a bit more work. See the ABAP programming manuals for that.
		// (Set up a destination NCO_SERVER - or whichever name you choose - as described above.)
		// 
		// DATA: l_dest TYPE REF TO if_bgrfc_destination_outbound,
		//       l_unit TYPE REF TO if_trfc_unit_outbound.
		// 
		// TRY.
		//   l_dest = cl_bgrfc_destination_outbound=>create( 'NCO_SERVER' ).
		// CATCH cx_bgrfc_invalid_destination.
		//   MESSAGE e103(bgrfc) WITH 'NCO_SERVER'.
		// ENDTRY.
		// 
		// l_unit = l_dest->create_trfc_unit( ).
		// 
		// CALL FUNCTION 'STFC_CONNECTION' IN BACKGROUND UNIT l_unit
		//   EXPORTING
		//     requtext = 'This time a bgRFC Call'.
		//
		// COMMIT WORK.
		//
		public static void ExampleBgRfcServer() {
			// Creates a server instance using the NCO_SERVER configuration and the function handler for STFC_CONNECTION
			RfcServer server = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, new Type[] { typeof(StfcConnectionStaticImpl) });
			// Register event handlers for internal and application errors
			server.RfcServerError += OnRfcServerError;
			server.RfcServerApplicationError += OnRfcServerApplicationError;
			// Register unit ID handler
			if (server.UnitIDHandler == null) server.UnitIDHandler = new MyUnitIDHandler();
			// Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
			server.Start();
			Console.WriteLine();
			Console.WriteLine("Server started: {0}", server.Parameters.ToString());
			Console.WriteLine("You can now send requests for STFC_CONNECTION (synchronous or in background task) -- press X to stop the server");
			while (Console.ReadLine() != "X") ;
			Console.WriteLine("Server shutting down...");
			// Shut down the server, aborting any active calls
			server.Shutdown(true);
			// Remove error and TID handlers so that other examples using the same server can start from scratch
			server.RfcServerError -= OnRfcServerError;
			server.RfcServerApplicationError -= OnRfcServerApplicationError;
			server.UnitIDHandler = null;
		}

		public static void ExampleShowBgRfcs() {
			MyUnitIDHandler.ListLUWs();
		}

        private static String AttributesToString(RfcSystemAttributes attr)
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

        //
        // This method is used as the event handler for internal server errors. Internal server errors are errors
        // that are not caused by the application, but are incurred by NCO3. Such errors include, but are not limited to,
        // communication errors (e.g., loss of connections), out of memory, etc.
        // 
        private static void OnRfcServerError(Object server, RfcServerErrorEventArgs errorEventData)
        {
            RfcServer rfcServer = server as RfcServer;
            Console.WriteLine();
            Console.WriteLine(">>>>> RfcServerError occurred in RFC server {0}:", rfcServer.Name);
            ShowErrorEventData(errorEventData);
        }

        //
        // This method is used as the event handler for application errors raised while a server processes a requested function.
        // As opposed to internal errors these errors occur in application coding outside NCO3. Note that the application can throw
        // whichever type of exception it sees fit. It will be wrapped in a RfcServerApplicationException and made available as its
        // inner exception.
        //
        private static void OnRfcServerApplicationError(Object server, RfcServerErrorEventArgs errorEventData)
        {
            RfcServer rfcServer = server as RfcServer;
            Console.WriteLine();
            Console.WriteLine(">>>>> RfcServerApplicationError occurred in RFC server {0}:", rfcServer.Name);
            ShowErrorEventData(errorEventData);
            RfcServerApplicationException appEx = errorEventData.Error as RfcServerApplicationException;
            if (appEx != null)
            {
                Console.WriteLine("Inner exception type: {0}", appEx.InnerException.GetType().Name);
                Console.WriteLine("Inner exception message: {0}", appEx.InnerException.Message);
            }
            else
            {
                Console.WriteLine("WARNING: errorEventData.Error is not an instance of RfcServerApplicationError");
            }
        }

        private static void ShowErrorEventData(RfcServerErrorEventArgs errorEventData)
        {
            if (errorEventData.ServerContextInfo != null)
            {
                Console.WriteLine("RFC Caller System ID: {0} ", errorEventData.ServerContextInfo.SystemAttributes.SystemID);
                Console.WriteLine("RFC function Name: {0} ", errorEventData.ServerContextInfo.FunctionName);
            }
            Console.WriteLine("Error type: {0}", errorEventData.Error.GetType().Name);
            Console.WriteLine("Error message: {0}", errorEventData.Error.Message);
        }

        //
        // This class represents a static function handler that can be used by a server to handle stateless calls.
        // It implements one function, namely STFC_CONNECTION. If more functions were to be handled
        // more static methods analogous to the available method could be implemented.
        // 
        class StfcConnectionStaticImpl
        {
            // The annotation binds the function (name) to its implementation
            [RfcServerFunction(Name = "STFC_CONNECTION")]
            public static void StfcConnection(RfcServerContext serverContext, IRfcFunction function)
            {
                MethodBase method = MethodInfo.GetCurrentMethod();
				String requtext = function.GetString("REQUTEXT");
                Console.WriteLine();
                Console.WriteLine("Method {2}.{0} processing RFC call {1}", method.Name, function.Metadata.Name, method.DeclaringType.ToString());
                Console.WriteLine("System Attributes:");
                Console.WriteLine(AttributesToString(serverContext.SystemAttributes));
				Console.WriteLine("Received REQUTEXT: {0}", requtext);

				// This class is also used in our tRFC & bgRFC Server examples, so let's do some more stuff here to make this more interesting...
				 
				if (serverContext.InTransaction) {
					bool bgRFC = false;
					if (serverContext.TransactionID != null) { // It's a tRFC/qRFC.
						Console.WriteLine("Currently running in tRFC LUW {0}", serverContext.TransactionID.TID);
					}
					else { // It's a bgRFC.
						Console.WriteLine("Currently running in bgRFC LUW {0}", serverContext.UnitID.Uuid.ToString("N").ToUpper());
						bgRFC = true;
					}
					if (requtext.Equals("Aborts")) {
						Console.WriteLine("Let's abort this LUW with an error...");
						String errorMessage = "Not in the mood for t/q/bgRFC. Try again later...";
						if (bgRFC) MyUnitIDHandler.SetError(serverContext.UnitID, errorMessage);
						else MyTidHandler.SetError(serverContext.TransactionID, errorMessage);
						throw new RfcAbapRuntimeException(errorMessage);
					}
				}
				else { // In this case we can send return parameters.
					function.SetValue("ECHOTEXT", function.GetString("REQUTEXT"));
					function.SetValue("RESPTEXT", "NCO3: Hello world.");
				}
            }
        }

        //
        // This class represents a function handler that can be used by a server to handle stateful (and also stateless) calls.
        // It implements one function, namely STFC_CONNECTION. If more functions were to be handled
        // more static methods analogous to the available method could be implemented.
        // 
        class StfcConnectionImpl
        {
            private int calls = 0;

            [RfcServerFunction(Name = "STFC_CONNECTION")]
            public void StfcConnection(RfcServerContext serverContext, IRfcFunction function)
            {
                MethodBase method = MethodInfo.GetCurrentMethod();
                Console.WriteLine();
                Console.WriteLine("Method {2}.{0} processing RFC call {1}", method.Name, function.Metadata.Name, method.DeclaringType.ToString());
                Console.WriteLine("System Attributes:");
                Console.WriteLine(AttributesToString(serverContext.SystemAttributes));

                function.SetValue("ECHOTEXT", function.GetString("REQUTEXT"));
                function.SetValue("RESPTEXT", "NCO3: call #" + (++calls).ToString());

                if (calls == 1)
                {
					serverContext.SetStateful(true); // Make connection stateful on first call.
                }
                else if (calls >= 3)
                {
                    serverContext.SetStateful(false); //This allows the server to close the connection from its side.
                }
            }
        }

		//
		// This class represents a function handler for the function module RFC_RAISE_ERROR.
		// It illustrates the various ways of returning an error message to the backend:
		// +  plain ABAP exception -- METHOD 2
		// +  ABAP exception with SY message (SY_MSG fields filled) -- METHOD 1
		// +  ABAP message (SY_MSG fields) -- METHOD 4
		// +  plain SYSTEM_FAILURE -- METHOD 3
		// +  SYSTEM_FAILURE with SY message (SY_MSG fields filled) -- METHOD 0
		// +  ABAP class based exception (backend must be 7.11 or higher and put TRY - CATCH around the CALL FUNCTION) -- METHOD 5
		// 
		// An ABAP report illustrating how to catch these different errors on ABAP side, is also included in the
		// tutorial: see file Z_CALL_EXTERNAL_SERVER.abap
		//
		class RaiseErrorImpl {
			[RfcServerFunction(Name = "RFC_RAISE_ERROR")]
			public static void RfcRaiseError(RfcServerContext serverContext, IRfcFunction rfcFunction) {
				MethodBase method = MethodInfo.GetCurrentMethod();
				Console.WriteLine();
				Console.WriteLine("Method {2}.{0} processing RFC call {1}", method.Name, rfcFunction.Metadata.Name, method.DeclaringType.ToString());
				Console.WriteLine("System Attributes:");
				Console.WriteLine(AttributesToString(serverContext.SystemAttributes));

				int methodNumber = Int32.Parse(rfcFunction.GetString("METHOD"));

                switch (methodNumber)
                {
					case 0: //SYSTEM_FAILURE with SY message
						throw new RfcAbapRuntimeException(null, 'E', "LX", "114", new String[2] { "hugo.txt", serverContext.SystemAttributes.HostName });

					case 1: //ABAP exception with SY message
						throw new RfcAbapException("RAISE_EXCEPTION", 'E', "LX", "114", new String[2] { "hugo.txt", serverContext.SystemAttributes.HostName});

					case 2: //ABAP exception
						throw new RfcAbapException("RAISE_EXCEPTION");

					case 3: //SYSTEM_FAILURE
						throw new RfcAbapRuntimeException("I'm very sorry, but something went wrong overhere...");

					case 4: //ABAP message
						throw new RfcAbapMessageException(null, "X", "LX", "114", "hugo.txt", serverContext.SystemAttributes.HostName, null, null);

					case 5: //ABAP class based exception
						IRfcAbapObject exceptionObject = serverContext.Repository.GetAbapObjectMetadata("").CreateAbapObject();
						exceptionObject.SetValue("SOURCE_TYPENAME", "Hello ABAP");
						exceptionObject.SetValue("TARGET_TYPENAME", "How are you?");
						throw new RfcAbapClassException("NCo3 step-by-step server tutorial message", exceptionObject);
				}
			}
		}

        //
        // This class represents a generic function handler that can be used by a server to handle any kind of (stateless) calls.
        // It implements a default handler that receives every function not handled by any other handler available to the server.
        // 
        class ServerFunctionGenericImpl
        {
            [RfcServerFunction(Default = true)]
            public static void GenericHandler(RfcServerContext serverContext, IRfcFunction rfcFunction)
            {
                MethodBase method = MethodInfo.GetCurrentMethod();
                Console.WriteLine();
                Console.WriteLine("Method {2}.{0} processing RFC call {1}", method.Name, rfcFunction.Metadata.Name, method.DeclaringType.ToString());
                Console.WriteLine("System Attributes:");
                Console.WriteLine(AttributesToString(serverContext.SystemAttributes));

				for (int i=0; i<rfcFunction.Metadata.ParameterCount; ++i) {
					if (0 != (rfcFunction.Metadata[i].Direction & RfcDirection.IMPORT)){ //  || rfcFunction.Metadata[i].Direction == RfcDirection.CHANGING) {
						switch (rfcFunction.Metadata[i].DataType){
							case RfcDataType.STRUCTURE:
								Console.WriteLine("Received structure of name {0} and type {1}", rfcFunction.Metadata[i].Name, rfcFunction.Metadata[i].ValueMetadataAsStructureMetadata.Name);
								// We could additionally loop through the fields of structures and tables here, but this shall suffice for our simple example.
								break;
							case RfcDataType.TABLE:
								Console.WriteLine("Received table of name {0} and type {1}", rfcFunction.Metadata[i].Name, rfcFunction.Metadata[i].ValueMetadataAsTableMetadata.Name);
								break;
							default:
								Console.WriteLine("{0} = {1}", rfcFunction.Metadata[i].Name, rfcFunction.GetString(i));
								break;
						}
					}
				}
            }
        }

        class MyTidHandler : ITransactionIDHandler
        {
            // Only for tests. In real life scenarios, use a database to store the transaction state!
			private static TidStore tidStore = new TidStore("serverTidStore", false);

            public bool CheckTransactionID(RfcServerContextInfo serverContext, RfcTID tid)
            {
                Console.WriteLine();
                Console.Write("TRFC: Checking transaction ID {0} --> ", tid.TID);
				TidStatus status = tidStore.CreateEntry(tid.TID);

				switch (status) {
					case TidStatus.Created:
					case TidStatus.RolledBack:
						// In these case we have to execute the tRFC LUW.
						Console.WriteLine("New transaction or one that had failed previously");
						return true;
					default:
						// In the remaining cases we have already executed this LUW successfully, so we 
						// can (or rather have to) skip a second execution and send an OK code to R/3 immediately.
						Console.WriteLine("Already executed successfully");
						return false;
				}
                // "true" means that NCo will now execute the transaction, "false" means
                // that we have already executed this transaction previously, so NCo will
                // skip the function execution step and will immediately return an OK code to R/3.
			    // In a real life scenario, if DB is down/unavailable, throw an exception at this point.
			    // The .Net Connector will then abort the current tRFC request and the R/3 backend will
			    // try again later.
				
			}

            // Clean up the resources. Backend will never send this transaction again, so no need to
			// protect against it any longer.
            public void ConfirmTransactionID(RfcServerContextInfo serverContext, RfcTID tid)
            {
                Console.WriteLine();
				Console.WriteLine("TRFC: Confirm transaction ID {0}", tid.TID);

				try {
					// Our implementation keeps the ones that failed, so an admin can later view error messages.
                    string error = "";
                    TidStatus status = tidStore.GetStatus(tid.TID, out error); 			
					if (status == TidStatus.RolledBack)
                        return;
					tidStore.DeleteEntry(tid.TID);
				}
				catch (Exception) { }
            }


            // React to commit, e.g. commit to the database;
            // Throw an exception if committing failed
            public void Commit(RfcServerContextInfo serverContext, RfcTID tid)
            {
                Console.WriteLine();
				Console.WriteLine("TRFC: Commit transaction ID {0}", tid.TID);
				// Do whatever is necessary to persist the data/changes of the function modules belonging to this LUW.
				// Throw an exception, if that fails.
				// If we know, that the LUWs that we are processing, consist of only a single function module,
				// the processing server function may already persist everything at the end and set the status
				// for that TID to Executed.
                //
				// No exception after this point  
                try
                {
					tidStore.SetStatus(tid.TID, TidStatus.Committed, null);
                }
				catch (Exception){}
            }

            // React to rollback, e.g. rollback on the database
            public void Rollback(RfcServerContextInfo serverContext, RfcTID tid)
            {
                Console.WriteLine();
				Console.WriteLine("TRFC: Rollback transaction ID {0}", tid.TID);
				// Roll back all changes of the previous function modules in this LUW.
				// If this LUW contains only one function module, we could already do this in the processing
				// server function.
                // We assume that the error message for this TID has already been added to the TidStore at the
				// point where the error happened.
                //
				tidStore.SetStatus(tid.TID, TidStatus.RolledBack, null);
            }

			// This is only a convenience method, because I like to keep track of the last error, with which
			// a transaction failed, in the status management. Makes it easier for administrators to fix the
			// problem and then retry that LUW.
			//
			public static void SetError(RfcTID tid, String errorMessage) {
				try {
					tidStore.SetStatus(tid.TID, TidStatus.RolledBack, errorMessage);
				}
				catch (Exception){}
			}

			public static void ListLUWs() {
				tidStore.PrintOverview();
			}
        }


        class MyUnitIDHandler : IUnitIDHandler
        {
            // Only for tests. In real life scenarios, use a database to store the transaction state!
			private static TidStore unitIDStore = new TidStore("serverUnitIdStore", true);

            public bool CheckUnitID(RfcServerContextInfo serverContext, RfcUnitID tid)
            {
                Console.WriteLine();
                Console.Write("bgRFC: Checking unit ID {0} --> ", tid.Uuid.ToString("N").ToUpper());
				TidStatus status = unitIDStore.CreateEntry(tid.Uuid.ToString("N").ToUpper());

				switch (status) {
					case TidStatus.Created:
					case TidStatus.RolledBack:
						// In these case we have to execute the bgRFC LUW.
						Console.WriteLine("New unit or one that had failed previously");
						return true;
					default:
						// In the remaining cases we have already executed this LUW successfully, so we 
						// can (or rather have to) skip a second execution and send an OK code to R/3 immediately.
						Console.WriteLine("Already executed successfully");
						return false;
				}
                // "true" means that NCo will now execute the transaction, "false" means
                // that we have already executed this transaction previously, so NCo will
                // skip the function execution step and will immediately return an OK code to R/3.
				// In a real life scenario, if DB is down/unavailable, throw an exception at this point.
				// The .Net Connector will then abort the current bgRFC request and the R/3 backend will
				// try again later.
				//
			}

            // Clean up the resources. Backend will never send this LUW again, so no need to
			// protect against it any longer.
			public void ConfirmUnitID(RfcServerContextInfo serverContext, RfcUnitID tid)
            {
                Console.WriteLine();
				Console.WriteLine("TRFC: Confirm unit ID {0}", tid.Uuid.ToString("N").ToUpper());
				try {
					// Our implementation keeps the ones that failed, so an admin can later view error messages.
		
					String error = "";
                    TidStatus status = unitIDStore.GetStatus(tid.Uuid.ToString("N").ToUpper(), out error);
					if (status == TidStatus.RolledBack)
                        return;
                    unitIDStore.DeleteEntry(tid.Uuid.ToString("N").ToUpper());
				}
				catch (Exception) { }
            }


            // React to commit, e.g. commit to the database;
            // Throw an exception if committing failed
			public void Commit(RfcServerContextInfo serverContext, RfcUnitID tid)
            {
                Console.WriteLine();
				Console.WriteLine("bgRFC: Commit unit ID {0}", tid.Uuid.ToString("N").ToUpper());
				// Do whatever is necessary to persist the data/changes of the function modules belonging to this LUW.
				// Throw an exception, if that fails.
				// If we know, that the LUWs that we are processing, consist of only a single function module,
				// the processing server function may already persist everything at the end and set the status
				// for that UnitID to Executed. */
                //
				// No exception after this point */ 
                try
                {
					unitIDStore.SetStatus(tid.Uuid.ToString("N").ToUpper(), TidStatus.Committed, null);
                }
				catch (Exception){}
            }

            // React to rollback, e.g. rollback on the database
			public void Rollback(RfcServerContextInfo serverContext, RfcUnitID tid)
            {
                Console.WriteLine();
				Console.WriteLine("bgRFC: Rollback unit ID {0}", tid.Uuid.ToString("N").ToUpper());
				// Roll back all changes of the previous function modules in this LUW.
				// If this LUW contains only one function module, we could already do this in the processing
				// server function.
                // We assume that the error message for this UnitID has already been added to the TidStore at the
				// point where the error happened.
                //
				unitIDStore.SetStatus(tid.Uuid.ToString("N").ToUpper(), TidStatus.RolledBack, null);
            }

			// The backend's bgRFC runtime may at certain points ask us about the status of a bgRFC unit.
			public RfcUnitState GetUnitState(RfcServerContextInfo serverContext, RfcUnitID tid) {
				Console.WriteLine();
				Console.WriteLine("bgRFC: Get unit state for unit ID {0}", tid.Uuid.ToString("N").ToUpper());
				try {
                    String error = "";
					TidStatus status = unitIDStore.GetStatus(tid.Uuid.ToString("N").ToUpper(), out error);
					switch (status) {
						case TidStatus.Confirmed: return RfcUnitState.CONFIRMED;
						case TidStatus.Committed: return RfcUnitState.COMMITTED;
						case TidStatus.RolledBack: return RfcUnitState.ROLLED_BACK;
						default: return RfcUnitState.IN_PROCESS;
					}
				}
				catch (ArgumentException) {
					return RfcUnitState.NOT_FOUND;
				}
			}

			// This is only a convenience method, because I like to keep track of the last error, with which
			// a transaction failed, in the status management. Makes it easier for administrators to fix the
			// problem and then retry that LUW.
			//
			public static void SetError(RfcUnitID tid, String errorMessage) {
				try {
					unitIDStore.SetStatus(tid.Uuid.ToString("N").ToUpper(), TidStatus.RolledBack, errorMessage);
				}
				catch (Exception){}
			}

			public static void ListLUWs() {
				unitIDStore.PrintOverview();
			}
        }
    }
}

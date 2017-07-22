Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Threading
Imports System.Reflection
Imports System.Diagnostics
Imports SAP.Middleware.Connector

Namespace SAP.Middleware.Connector.Examples
    ''
    ''The examples in this class rely on server configuration NCO_SERVER in App.config.
    ''Modify GWHOST, GWSERV and possibly PROGRAM_ID to suit your system needs. Also, on ABAP side
    ''a destination for a registered server program (SM59, type T, matching the program ID
    ''of the configuration) has to be available through which requests can be sent to the server.
    '
    Public Class StepByStepServer
        Private Const NCO_SERVER_CONFIG_NAME As String = "NCO_SERVER_B20"


        ''This example demonstrates a minimal server implementation Importsa Shared function handler.

        ''Once the server is started STFC_CONNECTION can be called on the ABAP side using
        ''the above mentioned SM59 destination. Any other function module will incur an error
        ''(a SYSTEM_FAILURE) since only STFC_CONNECTION can be handled by the handler
        ''supplied to the server.

        Public Shared Sub ExampleSimpleServer()
            '' Creates a server instance Importsthe NCO_SERVER configuration and the given array of function handlers.
            '' Shared function handlers are suited for stateless functions
            '' several handlers can be passed to the server - in this case there is only one.
            Dim server As RfcServer = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, New Type() {GetType(ServerFunctionStaticImpl)})
            '' You may, or may not, want the default trace level to match the trace level of the server -- comment or uncomment the following line
            ''RfcTrace.DefaultTraceLevel = server.Parameters.GetTraceLevelAsUint()
            '' Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start()
            Console.WriteLine()
            Console.WriteLine("Server started: {0}", server.Parameters.ToString())
            Console.WriteLine("You can now send requests (through STFC_CONNECTION only) -- press ENTER to stop the server")
            Console.ReadLine()
            Console.WriteLine("Server shutting down...")
            '' Shut down the server, aborting any active calls
            server.Shutdown(True)
        End Sub


        ''This example demonstrates a server permitting stateful requests.

        ''The easiest way to implement stateful scenarios is by Importsinstance methods. In that case the .Net Connector runtime
        ''will create a new instance of the handler class for each ABAP user session and keep the instance as long as the corresponding
        ''stateful session is alive. This is the approach the current example will follow.
        ''However, if creation/destruction of your classes implementing the "server functions" would be to expensive, you can as well
        ''use Shared methods (avoiding object creation by the NCo runtime) and implement the necessary stateful behavior yourself
        ''ImportsRfcServerContext.SessionID inside the Shared server function implementation.

        Public Shared Sub ExampleStatefulServer()
            '' Creates a server instance Importsthe NCO_SERVER configuration and the given function handler.
            '' Stateful requests can be handled by instance methods only
            '' several handlers can be passed to the server - in this case there is only one.
            Dim server As RfcServer = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, New Type() {GetType(ServerFunctionImpl)})
            '' Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start()
            Console.WriteLine()
            Console.WriteLine("Server started: {0}", server.Parameters.ToString())
            Console.WriteLine("You can now send (three) stateful requests (through STFC_CONNECTION only) -- press ENTER to stop the server")
            Console.ReadLine()
            Console.WriteLine("Server shutting down...")
            '' Shut down the server, aborting any active calls
            server.Shutdown(True)
        End Sub

        ''This example demonstrates how to implement a generic function handler.

        ''In a generic function handler the handling method (or several such methods) is not limited
        ''to a particular function through the annotation. Instead, the annotation designates the method
        ''as a default handler which takes on any request that is not handled otherwise.

        ''In the example the Shared handler for STFC_CONNECTION is passed to the server together with
        ''a generic handler. Thus STFC_CONNECTION will be handled by the handler specializing on that
        ''very function, whereas the generic handler takes care of everything else.

        ''In this example handlers for server and application errors are registered that produce console
        ''output in case such errors occur. As the preceding examples showed, registering event handlers
        ''of this kind is not necessary. It is, however, a way to be notified and to take appropriate
        ''actions, should issues arise.

        ''Note that the generic handler in this example does not really handle any (other) function
        ''sensibly. It simply does nothing, which may in effect result in a successful call in many cases.

        Public Shared Sub ExampleGenericServer()
            '' Creates a server instance Importsthe NCO_SERVER configuration and the function handler for STFC_CONNECTION and a generic handler
            Dim server As RfcServer = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, New Type() {GetType(ServerFunctionGenericImpl), GetType(ServerFunctionStaticImpl)})
            '' Register event handlers for internal and application errors
            AddHandler server.RfcServerError, AddressOf OnRfcServerError
            AddHandler server.RfcServerApplicationError, AddressOf OnRfcServerApplicationError
            '' Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start()
            Console.WriteLine()
            Console.WriteLine("Server started: {0}", server.Parameters.ToString())
            Console.WriteLine("You can now send requests (through any function module) -- press ENTER to stop the server")
            Console.ReadLine()
            Console.WriteLine("Server shutting down...")
            '' Shut down the server, aborting any active calls
            server.Shutdown(True)
            '' Remove error handlers so that other examples Importsthe same server can start from scratch
            RemoveHandler server.RfcServerError, AddressOf OnRfcServerError
            RemoveHandler server.RfcServerApplicationError, AddressOf OnRfcServerApplicationError
        End Sub


        'This example demonstrates how to return the different error types to ABAP.
        'These are described in more detail in the class RaiseErrorImpl, see comments there for more information.
        'In order to call this server, use an ABAP report like the one in Z_CALL_EXTERNAL_SERVER.abap available in this tutorial.


        Public Shared Sub ExampleThrowErrors()
            '' Creates a server instance Importsthe NCO_SERVER configuration and the function handler for RFC_RAISE_ERROR
            Dim server As RfcServer = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, New Type() {GetType(RaiseErrorImpl)})
            '' Register event handlers for internal and application errors
            AddHandler server.RfcServerError, AddressOf OnRfcServerError
            AddHandler server.RfcServerApplicationError, AddressOf OnRfcServerApplicationError
            '' Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start()
            Console.WriteLine()
            Console.WriteLine("Server started: {0}", server.Parameters.ToString())
            Console.WriteLine("You can now send requests (ImportsABAP report Z_CALL_EXTERNAL_SERVER) -- press ENTER to stop the server")
            Console.ReadLine()
            Console.WriteLine("Server shutting down...")
            '' Shut down the server, aborting any active calls
            server.Shutdown(True)
            '' Remove error handlers so that other examples Importsthe same server can start from scratch
            RemoveHandler server.RfcServerError, AddressOf OnRfcServerError
            RemoveHandler server.RfcServerApplicationError, AddressOf OnRfcServerApplicationError
        End Sub


        ''This example demonstrates how to implement a TID handler and dispatch transactional calls (i.e., "call function in background task").

        ''The following coding on the ABAP side will submit a request for execution of STFC_CONNECTION in a background task.
        ''(Set up a destination NCO_SERVER - or whichever name you choose - as described above.)

        ''CALL FUNCTION 'STFC_CONNECTION' IN BACKGROUND TASK DESTINATION 'NCO_SERVER'.
        ''COMMIT WORK.

        Public Shared Sub ExampleTRfcServer()
            '' Creates a server instance Importsthe NCO_SERVER configuration and the function handler for STFC_CONNECTION
            Dim server As RfcServer = RfcServerManager.GetServer(NCO_SERVER_CONFIG_NAME, New Type() {GetType(ServerFunctionStaticImpl)})
            '' Register event handlers for internal and application errors
            AddHandler server.RfcServerError, AddressOf OnRfcServerError
            AddHandler server.RfcServerApplicationError, AddressOf OnRfcServerApplicationError
            '' Register transaction ID handler
            If server.TransactionIDHandler Is Nothing Then
                server.TransactionIDHandler = New MyTidHandler()
            End If

            '' Start the server instance, i.e. open the connection(s) as defined by parameter REG_COUNT (RfcConfigParameters.RegCount)
            server.Start()
            Console.WriteLine()
            Console.WriteLine("Server started: {0}", server.Parameters.ToString())
            Console.WriteLine("You can now send requests for STFC_CONNECTION (synchronous or in background task) -- press X to stop the server")
            While Not Console.ReadLine() = "X"
            End While
            Console.WriteLine("Server shutting down...")
            '' Shut down the server, aborting any active calls
            server.Shutdown(True)
            '' Remove error and TID handlers so that other examples Importsthe same server can start from scratch
            RemoveHandler server.RfcServerError, AddressOf OnRfcServerError
            RemoveHandler server.RfcServerApplicationError, AddressOf OnRfcServerApplicationError
            server.TransactionIDHandler = Nothing
        End Sub

        Public Shared Sub ExampleShowTRfcs()
            MyTidHandler.ListLUWs()
        End Sub

        Private Shared Function AttributesToString(ByVal attr As RfcSystemAttributes) As String

            Dim sb As StringBuilder = New StringBuilder()
            sb.Append(String.Format("             Destination: {0}" + vbNewLine, attr.Destination))
            sb.Append(String.Format("               Host Name: {0}" + vbNewLine, attr.HostName))
            sb.Append(String.Format("                    User: {0}" + vbNewLine, attr.User))
            sb.Append(String.Format("                  Client: {0}" + vbNewLine, attr.Client))
            sb.Append(String.Format("            ISO Language: {0}" + vbNewLine, attr.ISOLanguage))
            sb.Append(String.Format("                Language: {0}" + vbNewLine, attr.Language))
            sb.Append(String.Format("          Kernel Release: {0}" + vbNewLine, attr.KernelRelease))
            sb.Append(String.Format("               Code Page: {0}" + vbNewLine, attr.CodePage))
            sb.Append(String.Format("       Partner Code Page: {0}" + vbNewLine, attr.PartnerCodePage))
            sb.Append(String.Format("            Partner Host: {0}" + vbNewLine, attr.PartnerHost))
            sb.Append(String.Format("         Partner Release: {0}" + vbNewLine, (attr.PartnerRelease)))
            sb.Append(String.Format("  Partner Release Number: {0}" + vbNewLine, attr.PartnerReleaseNumber))
            sb.Append(String.Format("            Partner Type: {0}" + vbNewLine, attr.PartnerType))
            sb.Append(String.Format("                 Release: {0}" + vbNewLine, attr.Release))
            sb.Append(String.Format("                RFC Role: {0}" + vbNewLine, attr.RfcRole))
            sb.Append(String.Format("               System ID: {0}" + vbNewLine, attr.SystemID))
            sb.Append(String.Format("           System Number: {0}" + vbNewLine, attr.SystemNumber))
            Return sb.ToString()
        End Function

        'This method is used as the event handler for internal server errors. Internal server errors are errors
        'that are not caused by the application, but are incurred by NCO3. Such errors include, but are not limited to,
        'communication errors (e.g., loss of connections), out of memory, etc.

        Private Shared Sub OnRfcServerError(ByVal server As Object, ByVal errorEventData As RfcServerErrorEventArgs)

            Dim rfcserver As RfcServer = server
            Console.WriteLine(vbNewLine + ">>>>> RfcServerError occurred in RFC server {0}:", rfcserver.Name)
            ShowErrorEventData(errorEventData)
        End Sub


        ''This method is used as the event handler for application errors raised while a server processes a requested function.
        ''As opposed to internal errors these errors occur in application coding outside NCO3. Note that the application can throw
        ''whichever type of exception it sees fit. It will be wrapped in a RfcServerApplicationException and made available as its
        ''inner exception.

        Private Shared Sub OnRfcServerApplicationError(ByVal server As Object, ByVal errorEventData As RfcServerErrorEventArgs)
            Dim rfcserver As RfcServer = server

            Console.WriteLine(vbNewLine + ">>>>> RfcServerApplicationError occurred in RFC server {0}:", rfcserver.Name)
            ShowErrorEventData(errorEventData)
            Dim appEx As RfcServerApplicationException = errorEventData.Error
            If Not appEx Is Nothing Then
                Console.WriteLine("Inner exception type: {0}", appEx.InnerException.GetType().Name)
                Console.WriteLine("Inner exception message: {0}", appEx.InnerException.Message)

            Else

                Console.WriteLine("WARNING: errorEventData.Error is not an instance of RfcServerApplicationError")
            End If
        End Sub

        Private Shared Sub ShowErrorEventData(ByVal errorEventData As RfcServerErrorEventArgs)
            If Not errorEventData.ServerContextInfo Is Nothing Then
                Console.WriteLine("RFC Caller System ID: {0} ", errorEventData.ServerContextInfo.SystemAttributes.SystemID)
                Console.WriteLine("RFC function Name: {0} ", errorEventData.ServerContextInfo.FunctionName)
            End If
            Console.WriteLine("Error type: {0}", errorEventData.Error.GetType().Name)
            Console.WriteLine("Error message: {0}", errorEventData.Error.Message)
        End Sub



        ''This class represents a Shared function handler that can be used by a server to handle stateless calls.
        ''It implements one function, namely STFC_CONNECTION. If more functions were to be handled
        ''more Shared methods analogous to the available method could be implemented.

        Class ServerFunctionStaticImpl
            '' The annotation binds the function (name) to its implementation
            <RfcServerFunction(Name:="STFC_CONNECTION")> _
            Public Shared Sub StfcConnection(ByVal serverContext As RfcServerContext, ByVal rfcFunction As IRfcFunction)
                Dim method As MethodBase = MethodInfo.GetCurrentMethod()
                Console.WriteLine(vbNewLine + "Method {2}.{0} processing RFC call {1}", method.Name, rfcFunction.Metadata.Name, method.DeclaringType.ToString())
                Console.WriteLine("System Attributes:")
                Console.WriteLine(AttributesToString(serverContext.SystemAttributes))

                rfcFunction.SetValue("ECHOTEXT", rfcFunction.GetString("REQUTEXT"))
                rfcFunction.SetValue("RESPTEXT", "NCO3: Hello world.")

                ''This class is also used in our tRFC Server example, so let's do some more stuff here to make this more interesting...
                ''
                If serverContext.InTransaction Then
                    Console.WriteLine("Currently running in tRFC LUW {0}", serverContext.TransactionID.TID)
                    Console.WriteLine("Do you want to abort this LUW with an error?")
                    If Console.ReadLine().ToUpper().Equals("Y") Then
                        Dim errorMessage As String = "Not in the mood for tRFC. Try again later..."
                        MyTidHandler.SetError(serverContext.TransactionID, errorMessage)
                        Throw New RfcAbapMessageException(errorMessage, "E")
                    End If
                End If
            End Sub
        End Class


        ''This class represents a function handler that can be used by a server to handle stateful (and also stateless) calls.
        ''It implements one function, namely STFC_CONNECTION. If more functions were to be handled
        ''more Shared methods analogous to the available method could be implemented.
        Class ServerFunctionImpl
            Private calls As Integer = 0

            <RfcServerFunction(Name:="STFC_CONNECTION")> _
            Public Sub StfcConnection(ByVal serverContext As RfcServerContext, ByVal rfcFunction As IRfcFunction)
                Dim method As MethodBase = MethodInfo.GetCurrentMethod()
                Console.WriteLine(vbNewLine + "Method {2}.{0} processing RFC call {1}", method.Name, rfcFunction.Metadata.Name, method.DeclaringType.ToString())
                Console.WriteLine("System Attributes:")
                Console.WriteLine(AttributesToString(serverContext.SystemAttributes))

                rfcFunction.SetValue("ECHOTEXT", rfcFunction.GetString("REQUTEXT"))
                rfcFunction.SetValue("RESPTEXT", "NCO3: call #" + (++calls).ToString())

                If calls = 1 Then
                    serverContext.SetStateful(True) '' Make connection stateful on first call.
                ElseIf calls >= 3 Then
                    serverContext.SetStateful(False) ''This allows the server to close the connection from its side.
                End If

            End Sub
        End Class


        ''This class represents a function handler for the function module RFC_RAISE_ERROR.
        ''It illustrates the various ways of returning an error message to the backend:
        ''+  plain ABAP exception -- METHOD 2
        ''+  ABAP exception with SY message (SY_MSG fields filled) -- METHOD 1
        ''+  ABAP message (SY_MSG fields) -- METHOD 4
        ''+  plain SYSTEM_FAILURE -- METHOD 3
        ''+  SYSTEM_FAILURE with SY message (SY_MSG fields filled) -- METHOD 0
        ''+  ABAP class based exception (backend must be 7.11 or higher and put TRY - CATCH around the CALL FUNCTION) -- METHOD 5
        ''An ABAP report illustrating how to catch these different errors on ABAP side, is also included in the
        ''tutorial: see file Z_CALL_EXTERNAL_SERVER.abap

        Class RaiseErrorImpl
            <RfcServerFunction(Name:="RFC_RAISE_ERROR")> _
            Public Shared Sub RfcRaiseErrorserverContext(ByVal serverContext As RfcServerContext, ByVal rfcFunction As IRfcFunction)
                Dim method As MethodBase = MethodInfo.GetCurrentMethod()
                Console.WriteLine(vbNewLine + "Method {2}.{0} processing RFC call {1}", method.Name, rfcFunction.Metadata.Name, method.DeclaringType.ToString())
                Console.WriteLine("System Attributes:")
                Console.WriteLine(AttributesToString(serverContext.SystemAttributes))


                Select Case rfcFunction.GetString("METHOD")
                    Case "0"  ''SYSTEM_FAILURE with SY message
                        Throw New RfcAbapRuntimeException(Nothing, "E", "LX", "114", New String() {"hugo.txt", serverContext.SystemAttributes.HostName})
                    Case "1"  ''ABAP exception with SY message
                        Throw New RfcAbapException("RAISE_EXCEPTION", "E", "LX", "114", New String() {"hugo.txt", serverContext.SystemAttributes.HostName})

                    Case "2" ''ABAP exception
                        Throw New RfcAbapException("RAISE_EXCEPTION")

                    Case "3" ''SYSTEM_FAILURE
                        Throw New RfcAbapRuntimeException(Nothing, "I'm very sorry, but something went wrong overhere...", Nothing)

                    Case "4"  ''ABAP message
                        Throw New RfcAbapMessageException("How am I supposed to now this?!", "E", "LX", "114", "hugo.txt", serverContext.SystemAttributes.HostName, Nothing, Nothing)

                    Case "5" ''ABAP class based exception
                        Dim exceptionObjext As IRfcAbapObject = serverContext.Repository.GetAbapObjectMetadata("").CreateAbapObject()
                        exceptionObjext.SetValue("SOURCE_TYPENAME", "Hello ABAP")
                        exceptionObjext.SetValue("TARGET_TYPENAME", "How are you?")
                        Throw New RfcAbapClassException("NCo3 step-by-step server tutorial message", exceptionObjext)
                    Case Else
                End Select
            End Sub
        End Class

        ''This class represents a generic function handler that can be used by a server to handle any kind of (stateless) calls.
        ''It implements a default handler that receives every function not handled by any other handler available to the server.
        Class ServerFunctionGenericImpl
            <RfcServerFunction(Default:=True)> _
            Public Shared Sub GenericHandler(ByVal serverContext As RfcServerContext, ByVal rfcFunction As IRfcFunction)
                Dim method As MethodBase = MethodInfo.GetCurrentMethod()
                Console.WriteLine()
                Console.WriteLine(vbNewLine + "Method {2}.{0} processing RFC call {1}", method.Name, rfcFunction.Metadata.Name, method.DeclaringType.ToString())
                Console.WriteLine("System Attributes:")
                Console.WriteLine(AttributesToString(serverContext.SystemAttributes))

                For i As Integer = 0 To rfcFunction.Metadata.ParameterCount - 1
                    If Not 0 = (rfcFunction.Metadata(i).Direction & RfcDirection.IMPORT) Then ''

                        Select Case rfcFunction.Metadata(i).DataType
                            Case RfcDataType.STRUCTURE
                                Console.WriteLine("Received structure of name {0} and type {1}", rfcFunction.Metadata(i).Name, rfcFunction.Metadata(i).ValueMetadataAsStructureMetadata.Name)
                                '' We could additionally loop through the fields of structures and tables here, but this shall suffice for our simple example.

                            Case RfcDataType.TABLE
                                Console.WriteLine("Received table of name {0} and type {1}", rfcFunction.Metadata(i).Name, rfcFunction.Metadata(i).ValueMetadataAsTableMetadata.Name)

                            Case Else
                                Console.WriteLine("{0} = {1}", rfcFunction.Metadata(i).Name, rfcFunction.GetString(i))

                        End Select
                    End If

                Next
                If rfcFunction.Metadata.Name.Equals("STFC_CONNECTION") Then

                    Throw New Exception("STFC_CONNECTION should not be handled by " + method.DeclaringType.ToString())

                Else

                    ''handle the function

                End If
            End Sub
        End Class

        Class MyTidHandler
            Implements ITransactionIDHandler
            '' Only for tests. In real life scenarios, use a database to store the transaction state!
            Private Shared tidStore As New TidStore("sampleTidStore", False)

            Public Function CheckTransactionID(ByVal serverContext As RfcServerContextInfo, ByVal tid As RfcTID) As Boolean _
                Implements ITransactionIDHandler.CheckTransactionID

                Console.Write(vbNewLine + "TRFC: Checking transaction ID {0} --> ", tid.TID)
                Dim status As TidStatus = tidStore.CreateEntry(tid.TID)

                Select Case status
                    Case TidStatus.Created, TidStatus.RolledBack
                        '' In these case we have to execute the tRFC LUW.
                        Console.WriteLine("New transaction or one that had failed previously")
                        Return True
                    Case Else
                        '' In the remaining cases we have already executed this LUW successfully, so we 
                        '' can (or rather have to) skip a second execution and send an OK code to R/3 immediately.
                        Console.WriteLine("Already executed successfully")
                        Return False
                End Select
                '' "true" means that NCo will now execute the transaction, "false" means
                '' that we have already executed this transaction previously, so NCo will
                '' skip the function execution step and will immediately return an OK code to R/3.
                ''In a real life scenario, if DB is down/unavailable, throw an exception at this point.
                ''The .Net Connector will then abort the current tRFC request and the R/3 backend will
                ''try again later.
            End Function

            '' Clean up the resources. Backend will never send this transaction again, so no need to
            '' protect against it any longer.
            Public Sub ConfirmTransactionID(ByVal serverContext As RfcServerContextInfo, ByVal tid As RfcTID) Implements ITransactionIDHandler.ConfirmTransactionID
                Console.WriteLine()
                Console.WriteLine("TRFC: Confirm transaction ID {0}", tid.TID)
                tidStore.DeleteEntry(tid.TID)
            End Sub

            '' React to commit, e.g. commit to the database
            '' Throw an exception if committing failed
            Public Sub Commit(ByVal serverContext As RfcServerContextInfo, ByVal tid As RfcTID) Implements ITransactionIDHandler.Commit

                Console.WriteLine(vbNewLine + "TRFC: Commit transaction ID {0}", tid.TID)
                ''Do whatever is necessary to persist the data/changes of the function modules belonging to this LUW.
                ''Throw an exception, if that fails.
                ''If we know, that the LUWs that we are processing, consist of only a single function module,
                ''the processing server function may already persist everything at the end and set the status
                ''for that TID to Executed. 

                ''No exception after this point  
                Try
                    tidStore.SetStatus(tid.TID, TidStatus.Committed, Nothing)
                Catch
                End Try

            End Sub

            '' React to rollback, e.g. rollback on the database
            Public Sub Rollback(ByVal serverContext As RfcServerContextInfo, ByVal tid As RfcTID) Implements ITransactionIDHandler.Rollback
                Console.WriteLine(vbNewLine + "TRFC: Rollback transaction ID {0}", tid.TID)
                ''Roll back all changes of the previous function modules in this LUW.
                ''If this LUW contains only one function module, we could already do this in the processing
                ''server function.
                ''            We assume that the error message for this TID has already been added to the TidStore at the
                ''point where the error happened.

                tidStore.SetStatus(tid.TID, TidStatus.RolledBack, Nothing)
            End Sub

            ''This is only a convenience method, because I like to keep track of the last error, with which
            ''a transaction failed, in the status management. Makes it easier for administrators to fix the
            ''problem and then retry that LUW.
            Public Shared Sub SetError(ByVal tid As RfcTID, ByVal errorMessage As String)
                Try
                    tidStore.SetStatus(tid.TID, TidStatus.RolledBack, errorMessage)
                Catch ex As Exception
                End Try

            End Sub

            Public Shared Sub ListLUWs()
                tidStore.PrintOverview()
            End Sub
        End Class
    End Class
End Namespace

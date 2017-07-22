Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Text
Imports System.Threading
Imports SAP.Middleware.Connector

Namespace SAP.Middleware.Connector.Examples

    Public Class StepByStepClient

        '' used by example "multi-threaded stateful calls with default session provider"
        Private Shared pendingThreads As Integer = 0

        '' used by example "multi-threaded stateful calls with custom session provider"
        Private Shared exampleSessionProvider As ExampleSessionProvider
        Private Shared callResults As List(Of Integer)

        ''
        '' See the App.config file for the definition of the following RFC destinations.
        '' Modify the logon parameters in that file to match a SAP system in your local network.
        '' The available configuration parameters are described in the documentation of the
        '' class RfcConfigParameters.
        '' 
        Private Const ABAP_APP_SERVER As String = "NCO_TESTS"
        Private Const ABAP_MESSAGE_SERVER As String = "NCO_LB_TESTS"

        ''
        '' The .Net Connector 3.0 introduces a new destination-oriented concept. Applications work with
        '' destination instances, which are configured per default in the application configuration file
        '' (app.config) or which can alternatively be defined by explicitly registering an IDestinationConfiguration
        '' object. A destination identifies the backend to which connections can be opened.
        '' The .Net Connector runtime takes care of the connection management and its lifecycle.
        '' So the application can concentrate on the business logic.
        '' Note: A destination is not just a replacement for a connection. The .Net Connector runtime decides
        '' if and how many connections are currently used for a given destination. The application
        '' never has to deal with connections themselves.
        '' 
        '' The first example obtains two destinations through the destination manager by supplying the
        '' name of the required destination. A connection is made to the backend system - first specified
        '' through an application server ASHOST and then through a message server MSHOST - by requesting
        '' the system attributes and/or by calling Ping() on the destination.
        '' In the second case the system attributes may be different from call to call due to load balancing.
        '' 
        '' Opening a connection is a relatively time-consuming task. In some situations it can lead to
        '' performance improvements if a certain number of connections are retained (i.e. they are not closed)
        '' for further requests. This is what connection pooling does. A pool (i.e., a set) of connections is maintained
        '' based on the settings of configuration parameters POOL_SIZE and MAX_POOL_SIZE (see RfcConfigParameters
        '' and in particular the API of properties PoolSize and MaxPoolSize of class RfcDestination).
        '' Connection pooling is done transparently by the .Net Conenctor runtime. Application
        '' code does not require any changes, only the destination configuration is extended
        '' by pool option POOL_SIZE (and optionally MAX_POOL_SIZE). Note that by default pooling is enabled
        '' with a default pool size of 10 and unlimited MAX_POOL_SIZE. So in order to disable pooling
        '' (in the sense of retaining currently unused connections) POOL_SIZE has to be set to 0.
        '' If on top of that the number of connections that can be used simultaneously at any time needs
        '' to be limited, MAX_POOL_SIZE has to be set to the desired maximal number of concurrently active
        '' connections.
        ''
        Public Shared Sub ExampleConnect()
            Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
            destination.Ping()
            Console.WriteLine(vbNewLine + "Attributes (application server logon):")
            Console.WriteLine(AttributesToString(destination.SystemAttributes))
            Console.WriteLine()


            destination = RfcDestinationManager.GetDestination(ABAP_MESSAGE_SERVER)
            destination.Ping()
            Console.WriteLine(vbNewLine + "Attributes (message server logon):")
            Console.WriteLine(AttributesToString(destination.SystemAttributes))
            Console.WriteLine()
        End Sub


        Public Shared Sub ExampleSimpleCall()
            ''Get destination instance. The destination is configured in app.config.
            Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)

            '' you may, or may not, want the default trace level to match the trace level of the destination -- comment or uncomment the following line
            ''RfcTrace.DefaultTraceLevel = destination.Parameters.GetTraceLevelAsUint()

            ''get metadata repository associated with the destination
            ''fetch or get cached function metadata and create a function container based on the function metadata
            Dim rfcFunction As IRfcFunction = destination.Repository.CreateFunction("STFC_CONNECTION")

            '' Set the import parameters, in this one import parameter REQUTEXT. The parameter is CHAR 255, but the .Net Connector runtime always trys to find
            '' a suitable conversion between C# data types and ABAP data types.
            rfcFunction.SetValue("REQUTEXT", "Hello SAP")

            ''make the remote call
            rfcFunction.Invoke(destination)

            '' show result
            ShowFunction(rfcFunction)

            '' A function can be reused to make another call, but be mindful about parameters that are affected by previous calls to a function,
            '' in particular changing and table parameters, which may modify the behavior and result of subsequent calls.
            '' Also, the destination on which a function is called may vary here you have to watch out for possible metadata inconsistencies
            '' in case the system delivering the metadata and the system to which the call is sent define the chosen function's metadata differently.
            rfcFunction.SetValue("REQUTEXT", "Hello again!")
            rfcFunction.Invoke(RfcDestinationManager.GetDestination(ABAP_APP_SERVER))
            ShowFunction(rfcFunction)

            '' metadata is unqiue
            Dim functionMetadata As RfcFunctionMetadata = destination.Repository.GetFunctionMetadata("STFC_CONNECTION")
            Console.WriteLine(vbNewLine + "The statement that the metadata associated with a function and the metadata retrieved from the respective repository are identical is always {0}.", _
                (rfcFunction.Metadata.Equals(functionMetadata)).ToString().ToUpper())

            '' Besides using parameter names it is also possible to use indices. This is the more efficient way to set a parameter,
            '' but it requires reliable knowledge on the mapping between names and indices. When in doubt use the metadata method
            '' NameToIndex(string) or TryNameToIndex(string):
            Dim requTextIndex As Integer = functionMetadata.NameToIndex("REQUTEXT")

            '' A function can also be created from the metadata, which actually is the only way to create it.
            '' CreateFunction(string) on the repository is merely a convenience method that bypasses or
            '' rather hides the metadata getter.
            Dim rfcFunction2 As IRfcFunction = functionMetadata.CreateFunction()
            rfcFunction2.SetValue(requTextIndex, "GOOD-BYE!")
            rfcFunction2.Invoke(destination)
            ShowFunction(rfcFunction2)

            Console.WriteLine("Hit RETURN to exit")
            Console.ReadLine()
        End Sub

        ''This example demonstrates how to work with structures.

        ''Structures and functions are in essence very similar. They are both containers for elements,
        ''namely parameters in the case of a function and fields in the case of a structure. Hence
        ''setting and getting field values is analogous to setting and getting parameter values.
        ''Also, each structure (an IRfcStructure) is based on unique metadata (RfcStructureMetadata)
        ''just like functions.

        ''Naturally functions and structures differ in terms of some aspects. Only functions can be
        ''invoked, and only structures can appear as parameter or field values.

        ''Tables are in a way very similar to structures and will be discussed in the next example.

        Public Shared Sub ExampleWorkWithStructure()

            Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
            '' create and invoke function module RFC_SYSTEM_INFO
            Dim rfcFunction As IRfcFunction = destination.Repository.CreateFunction("RFC_SYSTEM_INFO")
            rfcFunction.Invoke(destination)

            '' get the structure RFCSI_EXPORT
            Dim exportStructure As IRfcStructure = rfcFunction.GetStructure("RFCSI_EXPORT")

            '' show structure
            Console.WriteLine(vbNewLine + "System info for {0} using 'For ... Next' loop:", destination.SystemAttributes.SystemID)
            ''For each field in the structure ...
            For i As Integer = 0 To exportStructure.Metadata.FieldCount - 1

                '' ... print out the name and the value in string representation
                Console.WriteLine("{0}:" + vbTab + "{1}", exportStructure.Metadata(i).Name, exportStructure.GetString(i))
            Next
            '' Note that it is also possible to iterate through the fields of a structure (as well as parameters of a function or elements of
            '' any container for that matter) using the foreach loop. For all containers (IRfcStructure/IRfcTable, IRfcFunction, IRfcAbapObject)
            '' there are corresponding elements (IRfcField, IRfcParameter, IRfcAttribute) that can be used to work with a particular element
            '' in a more convenient way. Note, however, that except for IRfcFunction these objects will be generated every time they are requested.
            '' So there is a certain performance penalty when choosing this type of loop or whenever working with IRfcField or IRfcAttribute.
            '' (IRfcStructure and IRfcAbapObject only store the values as an array of System.Object, whereas IRfcFunction on top of parameter
            '' values has to store the active flag for each parameter which warrants the use of an array of IRfcParameter to represent parameters.)
            Console.WriteLine(vbNewLine + "System info for {0} using 'For Each ... Next' loop:", destination.SystemAttributes.SystemID)
            For Each field As IRfcField In exportStructure
                Console.WriteLine("{0}:" + vbTab + "{1}", field.Metadata.Name, field.GetString())
            Next
        End Sub


        ''This example demonstrates how to work with tables.

        ''A table (IRfcTable) essentially is a list or array of structures (IRfcStructure).
        ''Each row of the table is represented by a structure. By employing the CurrentIndex
        ''to designate a particular row it is possible to work with a table as if it were a
        ''structure. CurrentIndex can be viewed as cursor pointing to a certain row of the table,
        ''allowing to navigate through the table to set or get field values. Alternatively,
        ''a row - i.e., a IRfcStructure - can be retrieved based on its (zero-based) row index.

        Public Shared Sub ExampleCallWithStructureAndTable()
            Try
                ''Get destination instance. The destination is configured in app.config.
                Dim dest As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
                ''create a function container based on the function metadata
                Dim func As IRfcFunction = dest.Repository.CreateFunction("STFC_STRUCTURE")
                Console.WriteLine(vbNewLine + "Example RFC call to function {0} using destination {1} ", func.Metadata.Name, dest.Name)
                Console.WriteLine("Preparing input parameters ...")

                ''prepare input parameters
                '' get the structure parameter IMPORTSTRUCT
                Dim impStruct As IRfcStructure = func.GetStructure("IMPORTSTRUCT")
                '' set the fields of the structure
                impStruct.SetValue("RFCFLOAT", 12345.678900000001)
                impStruct.SetValue("RFCINT4", 12345)
                impStruct.SetValue("RFCHEX3", (New Byte() {41, 42, 43}))
                impStruct.SetValue("RFCDATA1", "Hello World")
                impStruct.SetValue("RFCDATA2", "世界你好")

                '' populize the table parameter RFCTABLE
                '' get the table parameter
                Dim rfcTable As IRfcTable = func.GetTable("RFCTABLE")
                '' append 100 rows to the table
                Dim rows As Integer = 100
                For i As Integer = 0 To rows - 1
                    '' clone a structure from impStruct
                    Dim row As IRfcStructure = impStruct.Clone()
                    '' change the values in some fields
                    row.SetValue("RFCINT4", i + 1000)
                    row.SetValue("RFCDATA1", i.ToString() + row.GetString("RFCDATA1"))
                    row.SetValue("RFCDATA2", i.ToString() + row.GetString("RFCDATA2"))
                    '' append the cloned row to the table
                    rfcTable.Append(row)
                Next
                Console.WriteLine("Invoking RFC call...")
                '' invoke the RFC call

                func.Invoke(dest)

                Console.WriteLine("RFC call returned with the following results")

                Console.WriteLine("RESPTEXT = {0}", func.GetString("RESPTEXT"))
                Dim echoStruct As IRfcStructure = func.GetStructure("ECHOSTRUCT")
                Console.WriteLine("ECHOSTRUCT.RFCFLOAT = {0}", echoStruct.GetDouble("RFCFLOAT"))
                Console.WriteLine("ECHOSTRUCT.RFCINT4= {0}", echoStruct.GetInt("RFCINT4"))
                Console.WriteLine("ECHOSTRUCT.RFCDATA1 = {0}", echoStruct.GetString("RFCDATA1"))
                Console.WriteLine("ECHOSTRUCT.RFCDATA2 = {0}" + vbNewLine, echoStruct.GetString("RFCDATA2"))

                Console.WriteLine("Returned table RFCTABLE:")
                rfcTable = func.GetTable("RFCTABLE")
                For i As Integer = 0 To rfcTable.Count - 1
                    Console.WriteLine("RFCTABLE[{0}].RFCFLOAT = {1}", i, rfcTable(i).GetDouble("RFCFLOAT"))
                    Console.WriteLine("RFCTABLE[{0}].RFCINT4 = {1}", i, rfcTable(i).GetInt("RFCINT4"))
                    Console.WriteLine("RFCTABLE[{0}].RFCDATA1 = {1}", i, rfcTable(i).GetString("RFCDATA1"))
                    Console.WriteLine("RFCTABLE[{0}].RFCDATA2 = {1}", i, rfcTable(i).GetString("RFCDATA2"))
                Next

                Console.WriteLine("RFC call to {0} against system {1} succeded!" + vbNewLine, func.Metadata.Name, dest.SystemID)
            Catch ex As Exception
                Console.WriteLine("Example RFC call failed with error: {0}", ex.Message)
            End Try

            Console.WriteLine("Hit RETURN to exit")
            Console.ReadLine()
        End Sub

        Public Shared Sub ExampleWorkWithTable()

            Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
            '' create, set up, and invoke RFC_FUNCTION_SEARCH to retrieve all functions with names starting with RFC_
            Dim rfcFunction As IRfcFunction = destination.Repository.CreateFunction("RFC_FUNCTION_SEARCH")
            rfcFunction.SetValue("FUNCNAME", "RFC_*")
            rfcFunction.SetValue("LANGUAGE", "E")
            rfcFunction.Invoke(destination)

            '' all functions with names starting with RFC_ are returned in table parameter FUNCTIONS
            Dim table As IRfcTable = rfcFunction.GetTable("FUNCTIONS")

            '' show number of rows and line type
            Console.WriteLine()
            Console.WriteLine("Table has {0} rows and line type {1}.", table.RowCount, table.Metadata.LineType.Name)

            '' show the first ten (or less) rows
            Dim rowsShown As Integer = Math.Min(10, table.RowCount)
            Console.WriteLine(vbNewLine + "Showing the first {0} rows:", rowsShown)
            For i As Integer = 0 To rowsShown - 1
                table.CurrentIndex = i
                Console.WriteLine("{0} FUNCNAME={1} GROUPNAME={2} APPL={3} HOST={4} STEXT={5}", _
                    i.ToString(), _
                    table.GetString("funcname"), _
                    table.GetString("groupname"), _
                    table.GetString("appl"), _
                    table.GetString("host"), _
                    table.GetString("stext"))
            Next

            '' get a particular row (in this case the last row) directly without touching CurrentIndex
            Dim lastRow As IRfcStructure = table(table.RowCount - 1)
            Console.WriteLine(vbNewLine + _
                "Last row: {0} FUNCNAME={1} GROUPNAME={2} APPL={3} HOST={4} STEXT={5}", _
                (table.RowCount - 1).ToString(), _
                lastRow.GetString("funcname"), _
                lastRow.GetString("groupname"), _
                lastRow.GetString("appl"), _
                lastRow.GetString("host"), _
                lastRow.GetString("stext"))
        End Sub


        ''
        ''This example shows how to achieve transactional security when sending a tRFC into the backend.
        ''
        Public Shared Sub ExampleTrfcClient()
            Dim trans As RfcTransaction = Nothing
            Dim data As String = ""
            Dim tidStore As New TidStore("clientTidStore", False)
            Dim quit As Boolean = False
            Try
            While Not quit
                Console.Write("Resend an existing LUW, create a new one or quit? [r/c/q] ")
                Dim action As String = Console.ReadLine()

                Select action
                    Case "r"
                        trans = OpenTransaction(tidStore, data)
                    Case "c"
                        trans = CreateTransaction(tidStore, data)
                      Case Else
                        quit = True
	                        trans = Nothing
                End Select

                '' Now we try to send that thing off into the backend.
                If Not trans Is Nothing Then
                    SubmitTransaction(trans, tidStore, data)
                End If

            End While
            Finally
            tidStore.Close()
            End Try
        End Sub

        Shared Function OpenTransaction(ByVal tidStore As TidStore, ByRef data As String) As RfcTransaction

            Dim trans As RfcTransaction = Nothing
            Data = ""
            If tidStore.Size() = 0 Then
                Console.WriteLine("No old LUWs exist. Let's create a new one instead...")
                Return CreateTransaction(tidStore, data)
            End If

            '' Let the user choose one of the previously failed LUWs:
            tidStore.PrintOverview()
            Dim tid As String = ChooseTid(tidStore)

            If String.IsNullOrEmpty(tid) Then
                Console.WriteLine("Sorry, the entered TID does not exist, please try again.")
                tid = ChooseTid(tidStore)
            End If

            If String.IsNullOrEmpty(tid) Then

                Console.WriteLine("Sorry, the entered TID does not exist, Bye!.")
                Return Nothing
            End If


            '' Read the payload back in
            Dim dataFile As New FileStream(tid, FileMode.Open, FileAccess.Read)
            Dim reader As New StreamReader(dataFile)
            data = reader.ReadLine()
            dataFile.Close()
            '' Recreate the transaction with the existing TID.
            trans = New RfcTransaction(New RfcTID(tid))

            Return trans
        End Function

        Private Shared Function ChooseTid(ByVal tidStore As TidStore) As String

            Console.Write("Please choose an existing TID: ")
            Dim tid As String = Console.ReadLine()
            Try
                '' Double check, whether that TID really exists:
                Dim errorMessage As String = ""
                TidStore.GetStatus(tid, errorMessage)

            Catch ex As ArgumentException
                tid = ""
            End Try
            Return tid
        End Function


        Shared Function CreateTransaction(ByVal tidStore As TidStore, ByRef data As String) As RfcTransaction

            Dim trans As New RfcTransaction()   '' This creates a fresh TID.
            Console.Write("Please enter some input data: ")
            data = Console.ReadLine()

            '' Persist the payload, so it can later be resend, in case something goes wrong the first time:
            Dim dataFile As New FileStream(trans.Tid.TID, FileMode.Create, FileAccess.ReadWrite)
            Dim utf8Data As Byte() = Encoding.UTF8.GetBytes(data)
            dataFile.Write(utf8Data, 0, utf8Data.Length)
            dataFile.Close()
            TidStore.CreateEntry(trans.Tid.TID)
            Return trans
        End Function


        Private Shared Sub SubmitTransaction(ByVal trans As RfcTransaction, ByVal tidStore As TidStore, ByVal data As String)
            Try
                Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
                Dim stfc_write_to_tcpic As IRfcFunction = destination.Repository.CreateFunction("STFC_WRITE_TO_TCPIC")
                '' In some releases, STFC_WRITE_TO_TCPIC does not have this parameter. Uncomment the following line, if it does:
                ''stfc_write_to_tcpic.SetParameterActive("RESTART_QNAME", False)
                Dim DataTable As IRfcTable = stfc_write_to_tcpic.GetTable("TCPICDAT")
                DataTable.Append()
                DataTable.SetValue(0, data)

                '' Insert the function module into the transaction:
                trans.AddFunction(stfc_write_to_tcpic)

                '' In order to demonstrate that a tRFC LUW can consist of several function modules,
                '' we clone the function object, modify the data in a fixed way and add a second
                '' FM to the transaction.
                '' Of course in this particular case we could as well add a second line to the
                '' original function object, but this is to demonstrate that several different
                '' function modules (even of different type) can be added to one LUW and treated as
                '' an "atomic unit".
                ''
                stfc_write_to_tcpic = stfc_write_to_tcpic.Clone()
                DataTable = stfc_write_to_tcpic.GetTable("TCPICDAT")
                DataTable.SetValue(0, data + " -- data of the second function module")
                trans.AddFunction(stfc_write_to_tcpic)

                '' If you want to make this example a bit interesting, you could change the logon parameters in
                '' App.config in the following way:
                '' Make sure that ABAP_MESSAGE_SERVER has valid logon parameters, so that it can successfully used
                '' for DDIC lookups.
                '' In ABAP_APP_SERVER, add a value REPOSITORY_DESTINATION= so that the DDIC lookup in
                '' CreateFunction("STFC_WRITE_TO_TCPIC") can be done successfully. Then make the user/hostname etc invalid,
                '' so that we get and error in the following line (RfcLogonException, RfcCommunicationException, etc).
                '' afterwards you can correct that mistake, re-run the program and send the transaction a second time.
                trans.Commit(destination)
                tidStore.SetStatus(trans.Tid.TID, TidStatus.Committed, Nothing)

                File.Delete(trans.Tid.TID)

                '' Only now, after everything was successful - including the deletion of the data on our side, so that
                '' we can be absolutely sure that this transaction will never be repeated from our side - do we confirm
                '' the TID in the backend. From then on the backend would no longer be protected against a duplicate
                '' update.
                destination.ConfirmTransactionID(trans.Tid)
                tidStore.DeleteEntry(trans.Tid.TID)
                '' Now use SE16 to check, whether two rows have been added to table TCPIC.
            Catch e As Exception

                tidStore.SetStatus(trans.Tid.TID, TidStatus.RolledBack, e.Message)
            End Try

        End Sub


        ''
        '' This example shows how to achieve transactional security when sending a bgRFC unit into the backend.
        ''
        Public Shared Sub ExampleBgrfcClient()
            Dim unit As RfcBackgroundUnit = Nothing
            Dim data As String = ""
            Dim tidStore As New TidStore("clientTidStore", True)
            Dim quit As Boolean = False
            Try
            While Not quit

                Console.Write("Resend an existing LUW, create a new one or quit? [r/c/q] ")
                Dim action As String = Console.ReadLine()

                Select action

                    Case "r"
                        unit = OpenBgUnit(tidStore, data)
                    Case "c"
                        unit = CreateBgUnit(tidStore, data)

                    Case Else
                        quit = True
	                        unit = Nothing

                End Select


                '' Now we try to send that thing off into the backend.
                If Not unit Is Nothing Then
                    SubmitBgUnit(unit, tidStore, data)
                End If

            End While
	    Finally
            TidStore.Close()
            End Try
        End Sub

        Shared Function OpenBgUnit(ByVal tidStore As TidStore, ByRef data As String) As RfcBackgroundUnit

            If tidStore.Size = 0 Then
                Console.WriteLine("No old LUWs exist. Let's create a new one instead...")
                Return CreateBgUnit(tidStore, data)
            End If

            data = ""
            '' Let the user choose one of the previously failed LUWs:
            tidStore.PrintOverview()
            Dim uid As String = ChooseUnitId(tidStore)
            If String.IsNullOrEmpty(uid) Then

                Console.WriteLine("Sorry, the entered Unit ID does not exist, please try again.")
                uid = ChooseTid(tidStore)
            End If


            If String.IsNullOrEmpty(uid) Then
                Console.WriteLine("Sorry, the entered Unit ID does not exist, Bye!")
                Return Nothing
            End If


            '' Read the payload back in
            Dim dataFile As New FileStream(uid, FileMode.Open, FileAccess.Read)
            Dim reader As New StreamReader(dataFile)
            data = reader.ReadLine()
            dataFile.Close()
            '' Recreate the transaction with the existing uid.
            Dim unit As New RfcBackgroundUnit(New RfcUnitID(New Guid(uid), RfcUnitType.TRANSACTIONAL))
            Return unit
        End Function


        Private Shared Function ChooseUnitId(ByVal tidStore As TidStore) As String

            Console.Write("Please choose an existing Unit ID: ")
            Dim uid As String = Console.ReadLine()
            Try

                '' Double check, whether that TID really exists:
                Dim errorMessage As String = ""
                tidStore.GetStatus(uid, errorMessage)

            Catch e As ArgumentException

                uid = ""
            End Try

            Return uid
        End Function


        Shared Function CreateBgUnit(ByVal tidStore As TidStore, ByRef data As String) As RfcBackgroundUnit

            Dim unit As New RfcBackgroundUnit(New RfcUnitID(RfcUnitType.TRANSACTIONAL)) '' This creates a fresh GUID.
            Dim uid As String = unit.UnitID.Uuid.ToString("N")
            Console.Write("Please enter some input data: ")
            data = Console.ReadLine()

            '' Persist the payload, so it can later be resend, in case something goes wrong the first time:
            Dim dataFile As New FileStream(uid, FileMode.Create, FileAccess.ReadWrite)
            Dim utf8Data As Byte() = Encoding.UTF8.GetBytes(data)
            dataFile.Write(utf8Data, 0, utf8Data.Length)
            dataFile.Close()

            tidStore.CreateEntry(uid)

            Return unit
        End Function


        Private Shared Sub SubmitBgUnit(ByVal unit As RfcBackgroundUnit, ByVal tidStore As TidStore, ByVal data As String)

            Dim uid As String = unit.UnitID.Uuid.ToString("N")
            Try

                Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
                Dim stfc_write_to_tcpic As IRfcFunction = destination.Repository.CreateFunction("STFC_WRITE_TO_TCPIC")
                '' In some releases, STFC_WRITE_TO_TCPIC does not have this parameter. Uncomment the following line, if it does:
                '' stfc_write_to_tcpic.SetParameterActive("RESTART_QNAME", False)
                Dim dataTable As IRfcTable = stfc_write_to_tcpic.GetTable("TCPICDAT")
                dataTable.Append()
                dataTable.SetValue(0, data)

                '' Insert the function module into the background unit:
                unit.AddFunction(stfc_write_to_tcpic)

                '' In order to demonstrate that a bgRFC LUW can consist of several function modules,
                '' we clone the function object, modify the data in a fixed way and add a second
                '' FM to the transaction.
                '' Of course in this particular case we could as well add a second line to the
                '' original function object, but this is to demonstrate that several different
                '' function modules (even of different type) can be added to one LUW and treated as
                '' an "atomic unit".
                ''
                stfc_write_to_tcpic = stfc_write_to_tcpic.Clone()
                dataTable = stfc_write_to_tcpic.GetTable("TCPICDAT")
                dataTable.SetValue(0, data + " -- data of the second function module")
                unit.AddFunction(stfc_write_to_tcpic)

                '' If you want to make this example a bit interesting, you could change the logon parameters in
                '' App.config in the following way:
                '' Make sure that ABAP_MESSAGE_SERVER has valid logon parameters, so that it can successfully used
                '' for DDIC lookups.
                '' In ABAP_APP_SERVER, add a value REPOSITORY_DESTINATION= so that the DDIC lookup in
                '' CreateFunction("STFC_WRITE_TO_TCPIC") can be done successfully. Then make the user/hostname etc invalid,
                '' so that we get and error in the following line (RfcLogonException, RfcCommunicationException, etc).
                '' afterwards you can correct that mistake, re-run the program and send the transaction a second time.
                unit.Commit(destination)

                tidStore.SetStatus(uid, TidStatus.Committed, Nothing)

                File.Delete(uid)

                '' Only now, after everything was successful - including the deletion of the data on our side, so that
                '' we can be absolutely sure that this transaction will never be repeated from our side - do we confirm
                '' the TID in the backend. From then on the backend would no longer be protected against a duplicate
                '' update.
                destination.ConfirmUnitID(unit.UnitID)
                TidStore.DeleteEntry(uid)
                '' Now use SE16 to check, whether two rows have been added to table TCPIC.

            Catch e As Exception

                tidStore.SetStatus(uid, TidStatus.RolledBack, e.Message)
            End Try
        End Sub




        ''This example demonstrates the "simple" stateful call sequence. All calls belonging to one
        ''session are executed within the same thread. 

        ''Note: this example uses Z_GET_COUNTER and Z_INCREMENT_COUNTER. Most ABAP systems
        ''contain function modules GET_COUNTER and INCREMENT_COUNTER, which however, are not remote enabled.
        ''Copy these functions to Z_GET_COUNTER and Z_INCREMENT_COUNTER (or implement as wrapper)
        ''and mark them as remote enabled.

        Public Shared Sub ExampleSimpleStatefulCalls()
            Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
            Dim incrementCounter, getCounter As IRfcFunction
            Try
                '' create functions Z_INCREMENT_COUNTER and Z_GET_COUNTER
                incrementCounter = destination.Repository.CreateFunction("Z_INCREMENT_COUNTER")
                getCounter = destination.Repository.CreateFunction("Z_GET_COUNTER")

            Catch ex As RfcBaseException

                Console.WriteLine(vbNewLine + "This example cannot run without function modules Z_INCREMENT_COUNTER and Z_GET_COUNTER {0})", ex.Message)
                Return
            End Try

            Const loops As Integer = 5

            '' increment and get counter a number of times counter will always be 0
            Console.WriteLine(vbNewLine + "Without stateful call sequence:")
            For i As Integer = 0 To loops - 1

                incrementCounter.Invoke(destination)
                getCounter.Invoke(destination)
                Dim counter As Integer = getCounter.GetInt("GET_VALUE")
                Dim result As String = "WRONG"
                If counter = 0 Then
                    result = "correct"
                End If
                Console.WriteLine("Call {0} --> counter = {1} {2} value)", i + 1, counter, result)
            Next

            Console.WriteLine(vbNewLine + "With stateful call sequence:")
            RfcSessionManager.BeginContext(destination)
            '' increment and get counter a number of times counter value will grow by one with each iteration
            Try

                For i As Integer = 0 To loops - 1

                    incrementCounter.Invoke(destination)
                    getCounter.Invoke(destination)
                    Dim counter As Integer = getCounter.GetInt("GET_VALUE")
                    Dim result As String = "WRONG"
                    If counter = i + 1 Then
                        result = "correct"
                    End If
                    Console.WriteLine("Call {0} --> counter = {1} {2} value", i + 1, counter, result)
                Next

            Finally

                '' it is a good programming style to ensure EndContext is called no matter what happens after BeginContext
                RfcSessionManager.EndContext(destination)
            End Try
        End Sub

        ''
        ''This examples demonstrates stateful calls in a multi-threaded environment. In this example a number of threads
        ''execute stateful calls of the same function module on one and the same destination, each thread using a session
        ''of its own. For this scenario the default session provider is sufficient since it is designed to associate a
        ''session with each thread. As a matter of fact, the default session provider uses thread IDs as session IDs. Thus,
        ''this scenario perfectly suits the default session provider, and there is no reason to implement a session provider.
        ''
        Public Shared Sub ExampleMultiThreadedStatefulCallsWithDefaultSessionProvider()

            '' first ensure the required function modules are available
            Try

                Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
                destination.Repository.GetFunctionMetadata("Z_INCREMENT_COUNTER")
                destination.Repository.GetFunctionMetadata("Z_GET_COUNTER")
            Catch ex As RfcBaseException
                Console.WriteLine(vbNewLine + "This example cannot run without function modules Z_INCREMENT_COUNTER and Z_GET_COUNTER ({0})", ex.Message)
                Return
            End Try

            '' start a number of threads, each executing stateful calls to the same destination in a session of its own
            Const threads As Integer = 5
            pendingThreads = 0
            For i As Integer = 0 To threads - 1
                Interlocked.Increment(pendingThreads)
                Dim thread As New Thread(AddressOf RunIncrementCounterInSessionPerThread)
                thread.Start()
            Next
            '' wait here until all the started threads completed the calls and exit
            While pendingThreads > 0
                Thread.CurrentThread.Join(500)
            End While

            Console.WriteLine()
            Console.WriteLine("All threads terminated")
        End Sub


        ''This example demonstrates stateful calls in a multi-threaded environment. In this example different threads
        ''are to call a function module in the same session. Using the default session provider results in stateless
        ''calls since each thread is associated with a different session, and in particular the thread executing the
        ''BeginContext method is different from all threads executing Z_INCREMENT_COUNTER and Z_GET_COUNTER.

        ''A very simple custom session provider that manages a single given session ID allows to achieve the desired
        ''effect that different threads execute stateful calls within the same session. Note that the application must
        ''ensure that different threads associated with the same session must not execute in parallel, since this
        ''is very likely to cause issues by concurrently using the same connection for different requests.

        Public Shared Sub ExampleMultiThreadedStatefulCallsWithCustomSessionProvider()

            '' first ensure the required function modules are available
            Try
                Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
                destination.Repository.GetFunctionMetadata("Z_INCREMENT_COUNTER")
                destination.Repository.GetFunctionMetadata("Z_GET_COUNTER")
            Catch ex As RfcBaseException
                Console.WriteLine()
                Console.WriteLine("This example cannot run without function modules Z_INCREMENT_COUNTER and Z_GET_COUNTER (0})", ex.Message)
                Return
            End Try

            '' use default session provider, which will produce a list of call results consisting of 0s only -- none of the threads executing
            '' Z_INCREMENT_COUNTER nor Z_GET_COUNTER are tied to the session created in RunIncrementCounterInOneSession by a different thread
            '' those calls are effectively stateless
            RunIncrementCounterInOneSession()
            Console.WriteLine()
            Console.Write("Call Results for default session provider:")
            For Each i As Integer In callResults
                Console.Write("  {0}", i)
            Next
            Console.WriteLine()

            '' register custom session provider
            If exampleSessionProvider Is Nothing Then
                exampleSessionProvider = New ExampleSessionProvider()
            Else
                exampleSessionProvider.SetSession(exampleSessionProvider.CreateSession())
            End If
            RfcSessionManager.RegisterSessionProvider(exampleSessionProvider)

            '' use the custom session provider to repeat the above calls -- this time all threads will run in the same session, thus
            '' producing a list of call results consisting of 1, 2, ...
            RunIncrementCounterInOneSession()
            Console.Write("Call Results for custom session provider :")
            For Each i As Integer In callResults
                Console.Write(" {0}", i)
            Next
            Console.WriteLine()

            '' unregister custom session provider to revert to default session provider
            RfcSessionManager.UnregisterSessionProvider(exampleSessionProvider)
        End Sub


        ''This example demonstrates the use of custom destinations.

        ''A custom destination can be derived from a configured destination. It allows to modify certain configuration parameters
        ''without having to change the configuration file (XML). A custom destination is particulary useful in a scenario where
        ''any number of (different) users require access to possibly just one backend system. Since it is not feasible to create
        ''a destination for all possible users, simply configure one destination for each relevant backend system without providing
        ''user and password. Whenever a user request is received, derive a custom destination from such a "raw" destination and
        ''set user and password appropriately.

        Public Shared Sub ExampleCustomDestination()

            '' NCO_RAW is assumed to be the raw destination that does not supply user nor password
            Dim destination As RfcDestination = RfcDestinationManager.GetDestination("NCO_RAW")
            Console.WriteLine(vbNewLine + "Configured Destination: {0} [ {1} ]", destination.Name, destination.Parameters.ToString())
            Dim custDest As RfcCustomDestination = destination.CreateCustomDestination()
            Try
                custDest.Ping()
                Console.WriteLine("Error: {0} already has valid user and password!", destination.Name)
                Return
            Catch ex As RfcLogonException
                Console.WriteLine("Everything ok: unable to login before setting user and password")
            End Try

            Console.Write("User: ")
            custDest.User = Console.ReadLine()
            Console.Write("Password: ")
            custDest.Password = Console.ReadLine()
            Try
                custDest.Ping()
                Console.WriteLine("Ping successful after setting user and password")
            Catch ex As RfcBaseException
                Console.WriteLine("Ping fails after setting user and password: user/password for {0} invalid?", custDest.User)
            End Try
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

        Private Shared Sub ShowFunction(ByVal fct As IRfcFunction)

            Console.WriteLine(vbNewLine + "{0}", fct.Metadata.Name)
            For Each parameter As IRfcParameter In fct
                Console.WriteLine("  {0}: {1}", parameter.Metadata.Name, parameter.GetString())
            Next

        End Sub

        Private Shared Sub RunIncrementCounterInSessionPerThread()

            Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
            Dim incrementCounter As IRfcFunction = destination.Repository.CreateFunction("Z_INCREMENT_COUNTER")
            Dim getCounter As IRfcFunction = destination.Repository.CreateFunction("Z_GET_COUNTER")

            Const loops As Integer = 3

            RfcSessionManager.BeginContext(destination)
            '' increment and get counter a number of times counter value will grow by one with each iteration
            Try
                Dim counter As Integer = 0
                For i As Integer = 1 To loops
                    incrementCounter.Invoke(destination)
                    getCounter.Invoke(destination)
                    counter = getCounter.GetInt("GET_VALUE")
                    If counter <> i Then
                        SyncLock GetType(StepByStepClient)
                            Console.WriteLine(vbNewLine + "Thread {0} gets wrong value {1} (expected: {2})", Thread.CurrentThread.ManagedThreadId, counter, i)
                        End SyncLock
                    End If
                Next

                Dim isCorrect As String = "WRONG"
                If counter = loops Then
                    isCorrect = "correct"
                End If
                SyncLock GetType(StepByStepClient)
                    Console.WriteLine(vbNewLine + "Thread {0} terminates with counter = {1} {2} value)", Thread.CurrentThread.ManagedThreadId, counter, isCorrect)
                End SyncLock

            Finally

                ' It is a good programming style to ensure EndContext is called no matter what happens after BeginContext.
                ' Otherwise there would be a serious connection leak, resulting in the connection pool either opening more and
                ' more connections, or running out of connections when reaching MAX_POOL_SIZE. 
                RfcSessionManager.EndContext(destination)
            End Try
            Interlocked.Decrement(pendingThreads)
        End Sub


        Private Shared Sub RunIncrementCounterInOneSession()
            '' clear or create list of call results
            If callResults Is Nothing Then
                callResults = New List(Of Integer)
            Else

                callResults.Clear()
            End If

            '' a given number of threads will each invoke Z_INCREMENT_COUNTER once -- depending on the session provider
            '' the final values in callResults will vary
            Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
            RfcSessionManager.BeginContext(destination)
            Try
                For i As Integer = 1 To 5
                    Dim thread As New Thread(AddressOf InvokeIncrementCounter)
                    thread.Start()
                    '' wait here until all the started threads completed the calls and exit
                    While callResults.Count < i
                        thread.CurrentThread.Join(500)
                    End While
                Next
            Finally

                '' it is a good programming style to ensure EndContext is called no matter what happens after BeginContext
                RfcSessionManager.EndContext(destination)
            End Try

        End Sub

        Private Shared Sub InvokeIncrementCounter()

            Dim destination As RfcDestination = RfcDestinationManager.GetDestination(ABAP_APP_SERVER)
            destination.Repository.CreateFunction("Z_INCREMENT_COUNTER").Invoke(destination)
            SyncLock callResults

                Dim getCounter As IRfcFunction = destination.Repository.CreateFunction("Z_GET_COUNTER")
                getCounter.Invoke(destination)
                callResults.Add(getCounter.GetInt("GET_VALUE"))
            End SyncLock

        End Sub
    End Class


    ''This is a rudimentary session provider that is used in the example on several threads
    ''executing calls in the same session (ExampleMultiThreadedStatefulCallsWithCustomSessionProvider).

    ''When using a session provider like this the application needs to ensure that session IDs are set
    ''appropriately, and that no more than one thread can execute calls in a session at any given time.


    Public Class ExampleSessionProvider
        Implements ISessionProvider

        Private sessionCounter As Integer = 0
        Private sessionID As String

        Friend Sub New()
            Me.sessionID = CreateSession()
        End Sub

        Friend Sub SetSession(ByVal sessionID As String)
            Me.sessionID = sessionID
        End Sub

#Region "ISessionProvider Members"

        Public Function GetCurrentSession() As String Implements ISessionProvider.GetCurrentSession
            Return sessionID
        End Function

        Public Sub ContextStarted() Implements ISessionProvider.ContextStarted

        End Sub


        Public Sub ContextFinished() Implements ISessionProvider.ContextFinished

        End Sub


        Public Function CreateSession() As String Implements ISessionProvider.CreateSession
            Return "Session_" + Convert.ToString(Interlocked.Increment(sessionCounter))
        End Function

        Public Sub PassivateSession(ByVal sessionID As String) Implements ISessionProvider.PassivateSession

        End Sub

        Public Sub ActivateSession(ByVal sessionID As String) Implements ISessionProvider.ActivateSession

        End Sub


        Public Sub DestroySession(ByVal sessionID As String) Implements ISessionProvider.DestroySession
            If Not sessionID Is Nothing And sessionID.Equals(Me.sessionID) Then
                Me.sessionID = Nothing
            End If
        End Sub

        Public Function IsAlive(ByVal sessionID As String) As Boolean Implements ISessionProvider.IsAlive
            Return Not sessionID Is Nothing And sessionID.Equals(Me.sessionID)
        End Function

        Public Function ChangeEventsSupported() As Boolean Implements ISessionProvider.ChangeEventsSupported
            Return False
        End Function

        Public Event SessionChanged As RfcSessionManager.SessionChangeHandler Implements ISessionProvider.SessionChanged

#End Region
    End Class
End Namespace

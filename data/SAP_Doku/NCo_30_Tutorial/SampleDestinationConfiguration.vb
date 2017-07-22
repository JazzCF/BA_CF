Imports System
Imports System.Collections.Generic
Imports System.Text
Imports SAP.Middleware.Connector

Namespace SAP.Middleware.Connector.Examples
    'This example class demonstrates how to implement a Destination Configuration that resides in memory.

    'This example shows that it is rather simple to obtain destinations from an arbitrary storage. 
    'In productive environments, however, it is not recommended to store configuration data in code or only in memory.
    'Use a persistent data store for that purpose, e.g. a database or an LDAP directory. 

    Public Class InMemoryDestinationConfiguration
        Implements IDestinationConfiguration
        '' For the purpose of this example destinations are stored in a Dictionary<string,RfcConfigParameters>
        '' that maps a destination name to destination parameters.
        Private availableDestinations As Dictionary(Of String, RfcConfigParameters)

        Public Sub New()
            availableDestinations = New Dictionary(Of String, RfcConfigParameters)()
        End Sub



        'Method inherited from interface definition.

        'Gets the parameters for the destination specified through its name.

        'Note that this implementation returns null if the destination does not exist.
        'This is the preferred - since more efficient - way to handle the non-existence of
        'a destination. NCo3 will react appropriately, e.g. by throwing an exception if
        'necessary (if there is no sensible way to proceed without parameters, for instance).

        'It is, however, also legitimate to throw an exception of your choice, if the parameters
        'for a given destination cannot be supplied, which may incur a performance penalty as a
        'result of the .Net framework having to create an exception instance that is simply discarded.


        Public Function GetParameters(ByVal destinationName As String) As RfcConfigParameters _
            Implements IDestinationConfiguration.GetParameters
            Dim foundDestination As RfcConfigParameters = Nothing
            availableDestinations.TryGetValue(destinationName, foundDestination)
            Return foundDestination
        End Function


        ' Method inherited from interface definition.

        ' This implementation of a destination configuration supports events. Based on the return value of
        ' this method, NCo3 will (or will not) register an RfcDestinationManager.ConfigurationChangeHandler.
        ' In this case (return value is true) it will register such a handler.

        ' It is the responsibility of this class, however, to actually fire the ConfigurationChanged event.

        'Tip: if you know that our configuration never changes during the runtime of your program, it is
        'safe to return true here, even if you don't use any events. This will improve the performance,
        'because you avoid that NCo3 constantly re-reads the config parameters (which it would, if you
        'would return false here). As you know that the parameters will never change, these re-reads are
        'unnecessary.

        Public Function ChangeEventsSupported() As Boolean _
            Implements IDestinationConfiguration.ChangeEventsSupported

            Return True
        End Function


        ''Event inherited from interface definition.

        ''If change events are supported (which is the case since ChangeEventsSupported() returns true),
        ''the NCO3 runtime will register an RfcDestinationManager.ConfigurationChangeHandler to
        ''perform the necessary operations ensuing a modification or removal of a destination configuration.

        ''It is the responsibility of this class, however, to actually fire the ConfigurationChanged event.

        Public Event ConfigurationChanged As RfcDestinationManager.ConfigurationChangeHandler _
            Implements IDestinationConfiguration.ConfigurationChanged


        Public Sub AddOrEditDestination(ByVal parameters As RfcConfigParameters)
            Dim name As String = parameters(RfcConfigParameters.Name)

            If availableDestinations.ContainsKey(name) Then

                '' Fire a change event
                '' If Not ConfigurationChanged Is Nothing Then
                ''Always check for null on event handlers... If AddOrEditDestination() gets called before this
                ''instance of InMemoryDestinationConfiguration is registered with the RfcDestinationManager, we
                ''would get a NullReferenceException when trying to raise the event... Stupid concept.
                ''                Why(doesn) 't the .NET framework do this for me?
                Dim EventArgs As New RfcConfigurationEventArgs(RfcConfigParameters.EventType.CHANGED, parameters)
                Console.WriteLine("Firing change event {0} for destination {1}", EventArgs.ToString(), name)
                RaiseEvent ConfigurationChanged(name, EventArgs)
                '' End If
            End If

            '' Replace the current parameters of an existing destination or add a new one
            availableDestinations(name) = parameters
            Dim tmp As String = "Application server"
            Dim isLoadValancing As Boolean = parameters.TryGetValue(RfcConfigParameters.LogonGroup, tmp)
            If isLoadValancing Then
                tmp = "Load balancing"
            End If
            Console.WriteLine("{0} destination {1} added/changed", tmp, name)

        End Sub

        '' Removes the destination specified by its name
        Public Sub RemoveDestination(ByVal name As String)
            If availableDestinations.Remove(name) Then

                Console.WriteLine("Successfully removed destination {0}", name)
                ''If Not ConfigurationChanged Is Nothing Then  '' Always check for null
                Console.WriteLine("Firing deletion event for destination {0}", name)
                RaiseEvent ConfigurationChanged(name, New RfcConfigurationEventArgs(RfcConfigParameters.EventType.DELETED))
                ''  End If

            Else
                Console.WriteLine("The destination could not be removed since it does not exist")
            End If
        End Sub
    End Class


    ''This class, in combination with class InMemoryDestinationConfiguration, demonstrates how to register and use
    ''a destination configuration other than the default configuration derived from the application's configuration file.

    Public Class SampleDestinationConfiguration

        Private Shared inMemoryDestinationConfiguration As New InMemoryDestinationConfiguration()
        Public Shared Sub SetUp()
            '' register the in-memory destination configuration -- called before executing any of the examples
            RfcDestinationManager.RegisterDestinationConfiguration(InMemoryDestinationConfiguration)
        End Sub

        Public Shared Sub TearDown()
            '' unregister the in-memory destination configuration -- called after we are done working with the examples 
            RfcDestinationManager.UnregisterDestinationConfiguration(InMemoryDestinationConfiguration)
        End Sub


        ''This example gets a destination by name. The destination has to be added before Imports ExampleAddOrChangeDestination.

        Public Shared Sub ExampleGetDestination()
            Dim name As String = ReadName()
            Try
                Console.WriteLine(RfcDestinationManager.GetDestination(name).Parameters.ToString())
            Catch ex As RfcBaseException

                Console.WriteLine("{0} : {1}", ex.GetType().Name, ex.Message)
            End Try
        End Sub



        ''This example adds or changes a destination configuration.
        ''For the sake of simplicity only crucial parameters and the trace level can be entered.
        ''Others are set to certain values (like PoolSize=5 or Language="EN") or remain at their default values.

        Public Shared Sub ExampleAddOrChangeDestination()
            Dim parameters As New RfcConfigParameters()
            parameters(RfcConfigParameters.Language) = "EN"
            parameters(RfcConfigParameters.PeakConnectionsLimit) = "5"
            parameters(RfcConfigParameters.ConnectionIdleTimeout) = "600" '' 600 seconds, i.e. 10 minutes
            parameters(RfcConfigParameters.Name) = ReadName()
            Console.Write("User: ")
            parameters(RfcConfigParameters.User) = Console.ReadLine()
            Console.Write("Password: ")
            parameters(RfcConfigParameters.Password) = Console.ReadLine()
            Console.Write("Client: ")
            parameters(RfcConfigParameters.Client) = Console.ReadLine()
            Console.Write("AS/MS Host: ")
            Dim server As String = Console.ReadLine()
            Console.Write("System Nr/ID: ")
            Dim sys As String = Console.ReadLine()
            Dim tmp As Integer = 0
            If Integer.TryParse(sys, tmp) Then

                parameters(RfcConfigParameters.AppServerHost) = server
                parameters(RfcConfigParameters.SystemNumber) = sys

            Else

                parameters(RfcConfigParameters.MessageServerHost) = server
                parameters(RfcConfigParameters.SystemID) = sys
                Console.Write("Logon Group: ")
                parameters(RfcConfigParameters.LogonGroup) = Console.ReadLine()
            End If
            Console.Write("Trace: ")
            parameters(RfcConfigParameters.Trace) = Console.ReadLine()
            inMemoryDestinationConfiguration.AddOrEditDestination(parameters)
        End Sub

        ''
        ''This example removes a destination configuration.
        ''
        Public Shared Sub ExampleRemoveDestination()

            inMemoryDestinationConfiguration.RemoveDestination(ReadName())
        End Sub

        ''
        ''This example pings a destination.
        ''
        Public Shared Sub ExamplePingDestination()

            Try

                RfcDestinationManager.GetDestination(ReadName()).Ping()
                Console.WriteLine("Ping successful")

            Catch ex As RfcInvalidParameterException

                Console.WriteLine("{0} : {1}", ex.GetType().Name, ex.Message)

            Catch ex As RfcBaseException
                Console.WriteLine(ex.ToString())
            End Try
        End Sub

        Private Shared Function ReadName() As String

            Console.WriteLine()
            Console.Write("Name: ")
            Return Console.ReadLine()
        End Function
    End Class

End Namespace

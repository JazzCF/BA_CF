Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Threading
Imports System.Reflection
Imports SAP.Middleware.Connector

Namespace SAP.Middleware.Connector.Examples

    Public Module Tutorial

        Private Const EXAMPLE_METHOD_NAME_PREFIX As String = "Example"

        Sub Main(ByVal args() As String)

            Dim exampleClasses() As Type
            exampleClasses = New Type() {GetType(StepByStepClient), GetType(StepBystepServer), GetType(SampleDestinationConfiguration)}

            Dim keepGoing As Boolean = True

            Do
                Console.WriteLine(vbNewLine + "===== TUTORIAL EXAMPLES =====")
                For i As Integer = 0 To exampleClasses.Length - 1
                    Console.WriteLine("{0} {1}", i + 1, OutputName(exampleClasses(i).Name))
                Next
                Console.WriteLine("E Exit")
                Console.WriteLine("T Change Trace Directory (currently is {0})", RfcTrace.TraceDirectory)
                Console.Write(vbNewLine + "Your Choice: ")
                Dim input As String = Console.ReadLine()
                Dim inputRecognized As Boolean = True
                Dim n As Integer

                If input.Equals("E", StringComparison.OrdinalIgnoreCase) Then
                    keepGoing = False
                ElseIf input.Equals("T", StringComparison.OrdinalIgnoreCase) Then

                    Console.Write("New trace directory: ")
                    Dim traceDir As String = Console.ReadLine()
                    Try
                        RfcTrace.TraceDirectory = traceDir
                    Catch e As Exception
                        Console.WriteLine(e.ToString())
                    End Try

                ElseIf Integer.TryParse(input, n) And n >= 1 And n <= exampleClasses.Length Then

                    keepGoing = SubMenu(exampleClasses(n - 1))

                Else
                    inputRecognized = False
                End If


                If Not inputRecognized Then

                    Console.WriteLine(vbNewLine + "Enter 1 - {0} for an example, 'T' or 't' to change the current trace directory, 'E' or 'e' to exit", exampleClasses.Length.ToString())
                End If
            Loop While keepGoing

            Console.WriteLine(vbNewLine + "BYE")
        End Sub

        Private Function SubMenu(ByVal exampleClass As Type) As Boolean
            Dim methods() As MethodInfo = exampleClass.GetMethods()
            Dim examples As New List(Of MethodInfo)
            Dim setup As MethodInfo = Nothing
            Dim tearDown As MethodInfo = Nothing
            For Each method As MethodInfo In methods
                '' assume all parameterless, Sub, Sharedmethods starting with "Example" to be relevant (by convention)
                If method.IsStatic And method.GetParameters().Length = 0 Then   '' And method.ReturnType = GetType(Sub) 

                    If (method.Name.StartsWith(EXAMPLE_METHOD_NAME_PREFIX)) Then
                        examples.Add(method)
                    ElseIf (method.Name.Equals("SetUp")) Then
                        setup = method
                    ElseIf (method.Name.Equals("TearDown")) Then
                        tearDown = method
                    End If
                End If
            Next

            If Not Invoke(setup) Then
                Return True
            End If

            Dim input As String
            Dim keepGoing As Boolean = True
            Do
                Console.WriteLine(vbNewLine + "~~~ {0}  ~~~", OutputName(exampleClass.Name))
                For i As Integer = 0 To examples.Count - 1
                    Console.WriteLine("{0} {1}", i + 1, OutputName(examples(i).Name))
                Next
                Console.WriteLine("R Return to Main Menu")
                Console.WriteLine("E Exit")
                Console.Write(vbNewLine + "Your Choice: ")
                input = Console.ReadLine()
                Dim n As Integer
                Dim inputValid As Boolean = False
                If Integer.TryParse(input, n) Then
                    If n >= 1 And n <= examples.Count Then

                        inputValid = True
                        Invoke(examples(n - 1))
                    End If
                ElseIf "R".Equals(input, StringComparison.OrdinalIgnoreCase) Then
                    inputValid = True
                ElseIf "E".Equals(input, StringComparison.OrdinalIgnoreCase) Then
                    inputValid = True
                    keepGoing = False
                End If

                If Not inputValid Then
                    Console.WriteLine("\n--- Enter a number 1 - {0} or A or R ---", examples.Count.ToString())
                End If
            Loop While Not "R".Equals(input, StringComparison.OrdinalIgnoreCase) And keepGoing

            Invoke(tearDown)

            Return keepGoing
        End Function

        Private Function OutputName(ByVal internalName As String) As String

            Dim sb As New StringBuilder()
            Dim previous As Char = vbNullChar
            Dim start As Integer = 0
            If internalName.StartsWith(EXAMPLE_METHOD_NAME_PREFIX) Then
                start = EXAMPLE_METHOD_NAME_PREFIX.Length
            End If
            For i As Integer = start To internalName.Length - 1
                Dim c As Char = internalName(i)
                If c = "_" Then
                    sb.Append(" ")
                ElseIf Char.IsUpper(c) Then
                    If sb.Length > 0 And Not Char.IsUpper(previous) Then

                        sb.Append(" ")
                    End If
                    sb.Append(c)
                Else
                    sb.Append(c)
                End If
                previous = c
            Next
            Return sb.ToString()
        End Function

        Private Function Invoke(ByVal method As MethodInfo) As Boolean
            If method Is Nothing Then
                Return True
            End If
            Try
                method.Invoke(Nothing, Nothing)
                Return True
            Catch ex As Exception
                Console.WriteLine(">>>> ERROR: {0} threw {1} <<<<", method.Name, ex.GetType().Name)
                Console.WriteLine(ex.ToString())
                Return False
            End Try
            Return True
        End Function

    End Module
End Namespace
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.IO

Namespace SAP.Middleware.Connector.Examples
    Public Enum TidStatus As Byte
        Created = 1
        Executed
        Committed
        RolledBack
        Confirmed
    End Enum

    Class StoreEntry
        Friend tid As String
        Friend status As TidStatus
        Friend index As Integer
    End Class

    Public Class TidStore
        Const REMAINDER_SIZE As Integer = 80
        Const TID_SIZE As Integer = 24
        Const GUID_SIZE As Integer = 32
        Shared EMPTY_REMAINDER(REMAINDER_SIZE) As Char
        '' TID		24   GUID 32
        '' Status	 1
        '' Error	80

        ReadOnly entrySize, idSize As Integer
        ReadOnly empty_id() As Char
        Private fs As FileStream
        Private br As BinaryReader
        Private bw As BinaryWriter

        Private table As Dictionary(Of String, StoreEntry)
        Private freeEntries As New Stack(Of StoreEntry)()
        Private slots As Integer
        'Dim forward As Int64

        '' Opens an existing TidStore, or creates a fresh one, if the given file does not yet exist. 
        Public Sub New(ByVal fileName As String, ByVal bgRFC As Boolean)
            '' Dim arr(24) As Byte
            If bgRFC Then
                idSize = GUID_SIZE
            Else
                idSize = TID_SIZE
            End If
            entrySize = idSize + REMAINDER_SIZE

            If String.IsNullOrEmpty(fileName) Then
                fileName = "TIDStore.tid"
            End If
            Try
                fs = New FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)
                br = New BinaryReader(fs)
                bw = New BinaryWriter(fs)
                If fs.Length < entrySize Then  '''' a newly created empty store file
                    slots = 0
                    bw.Seek(0, SeekOrigin.Begin)
                    bw.Write(slots)
                    bw.Flush()

                Else
                    br.BaseStream.Seek(0, SeekOrigin.Begin)
                    slots = br.ReadInt32()
                End If
                table = New Dictionary(Of String, StoreEntry)(slots)
                For i As Integer = 0 To slots - 1
                    Dim entry As New StoreEntry()
                    entry.index = fs.Position
                    '' read tid
                    Dim tempId() As Char = br.ReadChars(idSize)
                    If tempId Is Nothing Or Not tempId.Length = idSize Then
                        Throw New IOException("Unable to read entry at position " + i)
                    End If
                    entry.tid = New String(tempId)
                    '' read status
                    entry.status = br.ReadByte()
                    ''skip REMINDER
                    br.ReadChars(REMAINDER_SIZE)

                    If String.IsNullOrEmpty(entry.tid) Then

                        freeEntries.Push(entry)
                    Else
                        table(entry.tid) = entry
                    End If
                Next
            Catch
                If Not fs Is Nothing Then
                    fs.Close()
                    br = Nothing
                    bw = Nothing
                End If
                Throw
            End Try

        End Sub

        Public Sub Close()
            SyncLock Me
                Try
                    If Not fs Is Nothing Then
                        fs.Close()
                    End If
                Catch
                    freeEntries.Clear()
                    table.Clear()
                End Try
            End SyncLock
        End Sub


        Public Function CreateEntry(ByVal tid As String) As TidStatus
            SyncLock Me
                Dim entry As StoreEntry = Nothing

                If tid Is Nothing Or Not tid.Length = idSize Then
                    Throw New ArgumentException("Invalid TID")
                End If

                table.TryGetValue(tid, entry)
                If Not entry Is Nothing Then
                    Return entry.status
                End If
                If freeEntries.Count > 0 Then
                    entry = freeEntries.Pop()
                Else
                    entry = New StoreEntry()
                    entry.index = fs.Length

                    bw.Seek(0, SeekOrigin.Begin)
                    bw.Write(++slots)
                End If
                entry.status = TidStatus.Created
                entry.tid = tid
                table.Add(tid, entry)

                bw.Seek(entry.index, SeekOrigin.Begin)
                bw.Write(tid.ToCharArray(), 0, idSize)
                bw.Write(TidStatus.Created)
                bw.Write(EMPTY_REMAINDER, 0, REMAINDER_SIZE)
                bw.Flush()
                Return TidStatus.Created
            End SyncLock
        End Function

        ''	The following should be self-explanatory. */
        Public Sub DeleteEntry(ByVal tid As String)
            SyncLock Me
                Dim entry As StoreEntry = Nothing
                If table.TryGetValue(tid, entry) Then
                    '' Our implementation keeps the ones that failed, so an admin can later view error messages.
                    If entry.status = TidStatus.RolledBack Then
                        Return
                    End If

                    table.Remove(tid)
                    bw.Seek(entry.index, SeekOrigin.Begin)
                    bw.Write(empty_id, 0, idSize)
                    bw.Flush()
                    freeEntries.Push(entry)
                End If

                freeEntries.Push(entry)
            End SyncLock
        End Sub

        Public Sub SetStatus(ByVal tid As String, ByVal tidStatus As TidStatus, ByVal errorMessage As String)
            SyncLock Me
                Dim entry As StoreEntry = Nothing

                If Not table.TryGetValue(tid, entry) Then
                    Throw New ArgumentException("Invalid TID")
                End If

                entry.status = tidStatus
                bw.Seek(entry.index + idSize, SeekOrigin.Begin)
                bw.Write(tidStatus)

                If Not errorMessage Is Nothing Then
                    Dim err() As Byte = Encoding.UTF8.GetBytes(errorMessage)
                    Dim result As Integer = err.Length
                    If result > REMAINDER_SIZE Then
                        result = REMAINDER_SIZE
                    End If
                    bw.Write(err, 0, result)
                End If
                bw.Flush()
            End SyncLock

        End Sub

        Public Function GetStatus(ByVal tid As String, ByRef errorMessage As String) As TidStatus
            SyncLock Me
                errorMessage = ""
                Dim entry As StoreEntry = Nothing

                If Not table.TryGetValue(tid, entry) Then
                    Throw New ArgumentException("Invalid TID")
                End If

                Dim tidStatus As TidStatus = entry.status
                If tidStatus = tidStatus.RolledBack Then
                    br.BaseStream.Seek(entry.index, SeekOrigin.Begin)
                    Dim err() As Byte = br.ReadBytes(REMAINDER_SIZE)
                    errorMessage = Encoding.UTF8.GetString(err)
                End If
                Return tidStatus
            End SyncLock
        End Function


        ''	Prints all details for a given entry to the console. */
        Public Sub PrintEntry(ByVal tid As String)
            SyncLock Me

                Dim errmsg As String = ""
                Dim tidStatus As TidStatus = GetStatus(tid, errmsg)
                Console.WriteLine("TID:    {0}" + vbNewLine, tid)
                Console.WriteLine("Status: {0}" + vbNewLine, tidStatus)

            End SyncLock
        End Sub


        ''	Prints a list of all existing entries to the console. */
        Public Sub PrintOverview()
            If table.Count = 0 Then
                Console.WriteLine("No tRFC LUWs received yet" + vbNewLine)
                Return
            End If
            SyncLock Me
                Dim format As String = vbNewLine + "{0, 10}    {1}    {2, -20}    {3}"
                Dim idtype As String = "TID"
                If idSize = GUID_SIZE Then
                    idtype = "GUID"
                End If
                Console.WriteLine(format + vbNewLine, "Index", idtype, "Status", "Error message")
                Dim errorMessage As String = ""
                br.BaseStream.Seek(4, SeekOrigin.Begin)
                For i As Integer = 0 To slots - 1
                    Dim tid As String = br.ReadChars(idSize).ToString()
                    If String.IsNullOrEmpty(tid) Then
                        br.BaseStream.Seek(1 + REMAINDER_SIZE, SeekOrigin.Current)
                        Continue For
                    End If
                    Dim status As TidStatus = br.ReadByte()
                    If status = TidStatus.RolledBack Then
                        Dim byteBuf() As Byte = br.ReadBytes(REMAINDER_SIZE)
                        errorMessage = Encoding.UTF8.GetString(byteBuf, 0, REMAINDER_SIZE)
                    End If
                    Console.WriteLine(format, i, tid, status, errorMessage)
                Next

            End SyncLock
        End Sub

        Public ReadOnly Property Size As Int32
            Get
                Return table.Count
            End Get
        End Property

    End Class
End Namespace


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SAP.Middleware.Connector.Examples {
	public enum TidStatus : byte
    {
		Created = 1,
		Executed,
		Committed,
		RolledBack,
		Confirmed
	};

	class StoreEntry{
		internal String tid;
		internal TidStatus status;
		internal int index;
	};

	public class TidStore {
		const int REMAINDER_SIZE = 80;
        const int TID_SIZE = 24;
		const int GUID_SIZE = 32;
        static char[] EMPTY_REMAINDER = new char[REMAINDER_SIZE];

		readonly int entrySize, idSize;
        readonly char[] empty_id;
        FileStream fs;
        BinaryReader br;
        BinaryWriter bw;
		Dictionary<String, StoreEntry> table;
		Stack<StoreEntry> freeEntries = new Stack<StoreEntry>();
		int slots;

		// Opens an existing TidStore, or creates a fresh one, if the given file does not yet exist. 
		public TidStore(String fileName, bool bgRFC) {
            idSize = bgRFC ? GUID_SIZE : TID_SIZE;
			entrySize = idSize + REMAINDER_SIZE;
            empty_id = new char[idSize];

			if (string.IsNullOrEmpty(fileName))
                fileName = "TIDStore.tid";
			try {
                fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                br = new BinaryReader(fs);
                bw = new BinaryWriter(fs);
                if (fs.Length < entrySize)  // A newly created empty store file
                {
                    slots = 0;
                    bw.Seek(0, SeekOrigin.Begin);
                    bw.Write(slots);
                    bw.Flush();
                }
                else
                {
                    br.BaseStream.Seek(0, SeekOrigin.Begin);
                    slots = br.ReadInt32();
                }
                table = new Dictionary<String, StoreEntry>();

				for (int i=0; i<slots; ++i) {
                    StoreEntry entry = new StoreEntry();
                    entry.index = (int)fs.Position;
                    // Read tid/uid
                    char[] tempId = br.ReadChars(idSize);
                    if (tempId == null || tempId.Length != idSize)
                        throw new IOException("Unable to read entry at position " + i);
                    // Read status
                    entry.status = (TidStatus)br.ReadByte();
                    // Skip REMAINDER
					br.BaseStream.Seek(REMAINDER_SIZE, SeekOrigin.Current);

					if (tempId[0] == 0) {
						freeEntries.Push(entry);
					}
					else {
						entry.tid = new String(tempId);
						table[entry.tid] = entry;
					}
				}
			}
			catch (Exception) {
				if (fs  != null)
                    fs.Close();
                br = null;
                bw = null;
				throw;
			}
		}

		public void Close() {
			lock (this) {
				try {
                    if(fs != null)            
					    fs.Close();
				}
				catch (Exception) { }
				freeEntries.Clear();
				table.Clear();
			}
		}

		public TidStatus CreateEntry(String tid) {
			lock (this) {
				if (tid == null || tid.Length != idSize)
                    throw new ArgumentException("Invalid TID");
                StoreEntry entry = null;
				table.TryGetValue(tid, out entry);
				if (entry != null)
                    return entry.status;

				if (freeEntries.Count > 0) {
					entry = freeEntries.Pop();
				}
				else {
					entry = new StoreEntry();
                    entry.index = (int)fs.Length;

					bw.Seek(0, SeekOrigin.Begin);
                    bw.Write(++slots);
				}
				entry.status = TidStatus.Created;
				entry.tid = tid;
				table.Add(tid, entry);

				bw.Seek(entry.index, SeekOrigin.Begin);
				bw.Write(tid.ToCharArray(), 0, idSize);
				bw.Write((byte)TidStatus.Created);
                bw.Write(EMPTY_REMAINDER, 0, REMAINDER_SIZE);
				bw.Flush();

				return TidStatus.Created;
			}
		}

		//	The following should be self-explanatory. 
		public void DeleteEntry(String tid) {
			lock (this) {
				StoreEntry entry = null;

				if (table.TryGetValue(tid, out entry)) {
					table.Remove(tid);
					bw.Seek(entry.index, SeekOrigin.Begin);
                    bw.Write(empty_id, 0, idSize);
					bw.Flush();
					freeEntries.Push(entry);
				}
			}
		}

		public void SetStatus(String tid, TidStatus tidStatus, String errorMessage) {
			lock (this) {
				StoreEntry entry = null;

				if (!table.TryGetValue(tid, out entry))
                    throw new ArgumentException("Invalid TID");

				entry.status = tidStatus;
				bw.Seek(entry.index + idSize, SeekOrigin.Begin);
				bw.Write((byte)tidStatus);

				if (errorMessage != null)
                {
                    byte[] err = Encoding.UTF8.GetBytes(errorMessage);
                    int result = err.Length < REMAINDER_SIZE ? err.Length : REMAINDER_SIZE;
                    bw.Write(err, 0, result);
					if (result < REMAINDER_SIZE) bw.Write((byte)0);
				}
				bw.Flush();
			}
		}

		public TidStatus GetStatus(String tid, out String errorMessage) {
            TidStatus tidStatus = TidStatus.Created;
			lock (this) {
				errorMessage = "";
				StoreEntry entry = null;

				if (!table.TryGetValue(tid, out entry))
                    throw new ArgumentException("Invalid TID");

				tidStatus = entry.status;

				if (tidStatus == TidStatus.RolledBack) {
                    br.BaseStream.Seek(entry.index + idSize + 1, SeekOrigin.Begin);
                    byte[] err = br.ReadBytes(REMAINDER_SIZE);
                    errorMessage = Encoding.UTF8.GetString(err);
				}
			}
            return tidStatus;
		}

		//	Prints all details for a given entry to the console. 
		public void PrintEntry(String tid) {
			lock (this) {
				String error = "";
				TidStatus tidStatus;

				tidStatus = GetStatus(tid, out error);
				Console.WriteLine("TID:    {0}\n", tid);
                Console.WriteLine("Status: {0}\n", tidStatus);
			}
		}

		//	Prints a list of all existing entries to the console. 
		public void PrintOverview() {
			if (table.Count == 0) {
				if (idSize == TID_SIZE) Console.WriteLine("No tRFC LUWs received yet\n");
				else Console.WriteLine("No bgRFC LUWs received yet\n");
				return;
			}

			lock (this) {
                string format = "{0,10}    {1,-24}    {2,-20}    {3}";
                if (idSize == GUID_SIZE)
					format = "{0,10}    {1,-32}    {2,-20}    {3}";
                Console.WriteLine("\n"+format, "Index", idSize == GUID_SIZE ? "GUID" : "TID", "Status", "Error message\n");

                string errorMessage = "";
                br.BaseStream.Seek(sizeof(int), SeekOrigin.Begin);
				for (int i=0; i<slots; ++i) {
					char[] temp = br.ReadChars(idSize);
                    if (temp[0] == 0)
                    {
                        br.BaseStream.Seek(1 + REMAINDER_SIZE, SeekOrigin.Current);
                        continue;
                    }
                    TidStatus status = (TidStatus)br.ReadByte();
                    if (status == TidStatus.RolledBack)
                    {
                        byte[] byteBuf = br.ReadBytes(REMAINDER_SIZE);
						int result = 0;
						while (result < byteBuf.Length && byteBuf[result] != 0) ++result;
						errorMessage = Encoding.UTF8.GetString(byteBuf, 0, result);
                    }
					Console.WriteLine(format, i, new String(temp), status, errorMessage);
				}
			}
		}

		public int Size {
            get
            {
                return table.Count;
            }
		}
	}
}

using System;
using NUnit.Framework;
using DBus;

namespace DBus.Tests
{
	[TestFixture]
	public class BusTests
	{
		/// <summary>
		/// Tests that re-opening a bus with the same address works (in other words that closing a connection works)
		/// </summary>
		[Test]
		public void ReopenedBusIsConnected()
		{
         /*   Console.ReadKey();
			//var address = Environment.GetEnvironmentVariable ("DBUS_SESSION_BUS_ADDRESS");
            //var bus = Bus.Session;
            Console.ReadKey();
			var bus = Bus.Open ("DBUS_SESSION_BUS_ADDRESS");
			Assert.IsTrue (bus.IsConnected);
			bus.Close ();
			//bus = Bus.Open (address);
			Assert.IsTrue (bus.IsConnected);
            */

            var address = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS");
        //    Console.WriteLine("adresse ist :{0}",address);
         //   var address = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS=autolaunch:scope");
            Console.WriteLine("adresse ist :{0}", address);
            var bus = Bus.Open (address);
            Console.WriteLine("bus ist :{0}", bus);
            Assert.IsTrue (bus.IsConnected);
            bus.Close ();
            bus = Bus.Open (address);
            Assert.IsTrue (bus.IsConnected);


		}

		[Test]
		public void GetIdFromBusTest ()
		{
			var sessionID = Bus.Session.GetId ();
			var systemID = Bus.System.GetId (); 
			Assert.IsNotNull (sessionID);
			Assert.IsNotNull (systemID);
			Assert.AreNotEqual (systemID, sessionID);
		}

		[Test]
		public void DefaultBusesHaveUniqueName ()
		{
			var name = Bus.Session.UniqueName;
			Assert.IsNotNull (name);
			Assert.IsNotEmpty (name);
			name = Bus.System.UniqueName;
			Assert.IsNotNull (name);
			Assert.IsNotEmpty (name);
		}
	}
}

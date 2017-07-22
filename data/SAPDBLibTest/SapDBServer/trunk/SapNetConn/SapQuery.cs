using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBus;
using SapNetConn;
using SAP.Middleware.Connector;

/*
 * In der Klasse werden die Abfragen an SAP mit Hilfe von RFC_READ_TABLE
 * definiert. Nutzung anderer Funktionsbausbausteine moeglich.
 */

namespace SapNetConn
{
    public class SapServerQuery : ifaceServer
    {
        public DestConfig m_destconfig;
        public RfcDestination m_rfcdestination;
        string destName = "Flash";

        public void setDestConfig(ref DestConfig conf)
        {
            m_destconfig = conf;
        }
        public void setRfcDestination(ref RfcDestination destination)
        {
            m_rfcdestination = destination;
        }

        /* Ping destination Flash
        * return string with result of ping
        */
        public string pingSapDest()
        {
            string s = m_destconfig.PingDestination(destName);
            return s;
        }

        /* get Materialnummer, Gesamzmanenge im FA und Gesamte Fail Menge
        * strAuNr is PA-Nr like 60098052
        * return string with result
        */
        public string getMatNrFromPa(string strAufNr)
        {
            // Metadaten beschaffen für RFC_READ_TABLE   - allgemeines RFC Baustein zum Lesen
            IRfcFunction rfcFunktion = m_rfcdestination.Repository.CreateFunction("RFC_READ_TABLE");
            rfcFunktion.SetValue("QUERY_TABLE", "AFKO"); //Tabelle, die gelesen wird
            rfcFunktion.SetValue("DELIMITER", "~");//Trennzeichen
            IRfcTable tableField = rfcFunktion.GetTable("FIELDS");// Definieren welche Spalten gelesen werden
            tableField.Append();
            tableField.SetValue("FIELDNAME", "STLBEZ"); //MaterialNr im FA
            tableField.Append();
            tableField.SetValue("FIELDNAME", "GAMNG"); //Gesamtmaenge im FA
            tableField.Append();
            tableField.SetValue("FIELDNAME", "GASMG"); // Gesamte Fail Maenge im FA
            IRfcTable tableOptions = rfcFunktion.GetTable("OPTIONS");
            tableOptions.Append();
            // Auftragsnummer Format anpassen
            if (strAufNr.Length < 12)
            {
                String strTemp = strAufNr.PadLeft(12, '0');
                strAufNr = strTemp;
            }
            strAufNr = "AUFNR = '" + strAufNr + "'";// String zusammenbauen zum 000060098052
            tableOptions.SetValue("TEXT", strAufNr);// Daten zum Auftragsnummer 60098052 (auf Format achten!)
            rfcFunktion.Invoke(m_rfcdestination);

            // Daten verarbeiten
            IRfcTable dataTable = rfcFunktion.GetTable("DATA");//Datenspalte holen 
            string strErg = "";

            // uber Zeilen gehen 
            foreach (var dataRow in dataTable)
            {
                string data = dataRow.GetValue("WA").ToString();
                //Console.WriteLine(data);
                strErg = data.ToString();
            }
            return strErg;
        }

        /* get Materialkurztext
        * strMatNr is SAp-Nr like 604934
        * return string with result
        */
        public string getMatKurzText(string strMatNr)
        {
            //Console.WriteLine("SapNr : {0}", strMatNr);
            // Metadaten beschaffen für RFC_READ_TABLE   - allgemeines RFC Baustein zum Lesen
            IRfcFunction rfcFunktion = m_rfcdestination.Repository.CreateFunction("RFC_READ_TABLE");
            rfcFunktion.SetValue("QUERY_TABLE", "MAKT"); //Tabelle, die gelesen wird
            rfcFunktion.SetValue("DELIMITER", "~");//Trennzeichen
            IRfcTable tableField = rfcFunktion.GetTable("FIELDS");// Definieren welche Spalten gelesen werden
            tableField.Append();
            tableField.SetValue("FIELDNAME", "MAKTX"); // Materialkurztext
            IRfcTable tableOptions = rfcFunktion.GetTable("OPTIONS");
            tableOptions.Append();
            string strMatNrToSap = "MATNR = " + "'" + strMatNr + "'" + " and SPRAS = 'D'";
            //Console.WriteLine("stringto SapNr {0}", strMatNrToSap);
            tableOptions.SetValue("TEXT", strMatNrToSap);
            rfcFunktion.Invoke(m_rfcdestination);
            // Daten verarbeiten
            IRfcTable dataTable = rfcFunktion.GetTable("DATA");//Datenspalte holen 
            string strErg = "";
            // uber Zeilen gehen 
            foreach (var dataRow in dataTable)
            {
                string data = dataRow.GetValue("WA").ToString();
                //Console.WriteLine(data);
                strErg = data.ToString();
            }
            //Console.WriteLine("string {0}", strErg.Length);
            return strErg;
        }

        /* get getValueFromTable funktion to read a entry from SAP with Options
        * qtTable is name of Table as string like "AFKO"
        * qtColumn are columns as string like "STLBEZ","GAMNG","GASMG" as array 
        * qtDelimiter is delimiter string like "#"
        * qtWhere is a string like "AUFNR = '000060098052'" as array
        * return string with result
        */
        public string getValueFromTable(string qtTable, string[] qtColumn, string qtDelimiter, string []qtWhere)
        {
            // Metadaten beschaffen für RFC_READ_TABLE   - allgemeines RFC Baustein zum Lesen
            IRfcFunction rfcFunktion = m_rfcdestination.Repository.CreateFunction("RFC_READ_TABLE");
            rfcFunktion.SetValue("QUERY_TABLE", qtTable); //Tabelle, die gelesen wird
            rfcFunktion.SetValue("DELIMITER", qtDelimiter);//Trennzeichen
            IRfcTable tableField = rfcFunktion.GetTable("FIELDS");// Definieren welche Spalten gelesen werden
                                                                  // set columns
            //Console.WriteLine("ausgabe column count = {0}", qtColumn.Count());

            for (int i=0;i<qtColumn.Count();i++)
            {
                tableField.Append();
                tableField.SetValue("FIELDNAME", qtColumn[i]); //Über alle Spalten
            }
            // set Where
            IRfcTable tableOptions = rfcFunktion.GetTable("OPTIONS");
            //Console.WriteLine("ausgabe where count = {0}", qtWhere.Count());
            for (int i = 0; i < qtWhere.Count(); i++)
            {
                tableOptions.Append();
                tableOptions.SetValue("TEXT", qtWhere[i]); // Format in Qt definiert
            }
            // invoke on destination
            rfcFunktion.Invoke(m_rfcdestination);
            // Daten verarbeiten
            IRfcTable dataTable = rfcFunktion.GetTable("DATA");//Datenspalte holen 
            string strErg = "";

            // for each row
            foreach (var dataRow in dataTable)
            {
                string data = dataRow.GetValue("WA").ToString();
                //Console.WriteLine(data);
                strErg = data.ToString();
            }
            return strErg;
        }


        public string getOrderTypeNr(string qtSapOrderNr)
        {
            // Metadaten beschaffen für RFC_READ_TABLE   - allgemeines RFC Baustein zum Lesen
            IRfcFunction rfcFunktion = m_rfcdestination.Repository.CreateFunction("RFC_READ_TABLE");
            rfcFunktion.SetValue("QUERY_TABLE", "AFKO"); //Tabelle, die gelesen wird
            rfcFunktion.SetValue("DELIMITER", "~");//Trennzeichen
            IRfcTable tableField = rfcFunktion.GetTable("FIELDS");// Definieren welche Spalten gelesen werden
            tableField.Append();
            tableField.SetValue("FIELDNAME", "STLBEZ"); //MaterialNr im FA
            IRfcTable tableOptions = rfcFunktion.GetTable("OPTIONS");
            tableOptions.Append();
            // Auftragsnummer Format anpassen
            if (qtSapOrderNr.Length < 12)
            {
                String strTemp = qtSapOrderNr.PadLeft(12, '0');
                qtSapOrderNr = strTemp;
            }
            qtSapOrderNr = "AUFNR = '" + qtSapOrderNr + "'";// String zusammenbauen zum 0000xxxxxxxx
            tableOptions.SetValue("TEXT", qtSapOrderNr);
            rfcFunktion.Invoke(m_rfcdestination);

            // Daten verarbeiten
            IRfcTable dataTable = rfcFunktion.GetTable("DATA");//Datenspalte holen 
            string strErg = "";

            // uber Zeilen gehen 
            foreach (var dataRow in dataTable)
            {
                string data = dataRow.GetValue("WA").ToString();
                strErg = data.ToString();
            }
            return strErg;
        }

        public string getOrderQty(string qtSapOrderNr)
        {
            if (qtSapOrderNr.Length < 12)
            {
                String strTemp = qtSapOrderNr.PadLeft(12, '0');
                qtSapOrderNr = strTemp;
            }
            qtSapOrderNr = "AUFNR = '" + qtSapOrderNr + "'";
            string[] stringArrayColumn = new string[] {"GAMNG"};
            string[] stringArrayWhere = new string[] { qtSapOrderNr };
            string orderQty = getValueFromTable("AFKO", stringArrayColumn, "~",stringArrayWhere);
            return orderQty;
        }

        public string getOrderFailQty(string qtSapOrderNr)
        {
            if (qtSapOrderNr.Length < 12)
            {
                String strTemp = qtSapOrderNr.PadLeft(12, '0');
                qtSapOrderNr = strTemp;
            }
            qtSapOrderNr = "AUFNR = '" + qtSapOrderNr + "'";
            string[] stringArrayColumn = new string[] { "GASMG" };
            string[] stringArrayWhere = new string[] { qtSapOrderNr };
            string orderQty = getValueFromTable("AFKO", stringArrayColumn, "~", stringArrayWhere);
            return orderQty;
        }

        public string getOrderType(string qtSapOrderNr)
        {
            if (qtSapOrderNr.Length < 12)
            {
                String strTemp = qtSapOrderNr.PadLeft(12, '0');
                qtSapOrderNr = strTemp;
            }
            qtSapOrderNr = "AUFNR = '" + qtSapOrderNr + "'";
            string[] stringArrayColumn = new string[] { "AUART" };
            string[] stringArrayWhere = new string[] { qtSapOrderNr };
            string orderQty = getValueFromTable("AUFK", stringArrayColumn, "~", stringArrayWhere);
            return orderQty;
        }

    }
}

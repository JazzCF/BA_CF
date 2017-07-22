using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAP.Middleware.Connector;

/*
 * In der Klasse werden die Abfragen an SAP mit Hilfe von RFC_READ_TABLE
 * definiert. Nutzung anderer Funktionsbausbausteine möglich.
 */

namespace SapNetConn
{
    public class SapQuery
    {
        public bool getMatNr(string strAufNr, RfcDestination destination, ref string strMatNr, ref string strGesMenge, ref string strGesFailMenge)
        {
            // Metadaten beschaffen für RFC_READ_TABLE   - allgemeines RFC Baustein zum Lesen
            IRfcFunction rfcFunktion = destination.Repository.CreateFunction("RFC_READ_TABLE");
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
            rfcFunktion.Invoke(destination);
            
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
            // string spalten
            string[] erg = strErg.Split('~');
            strMatNr = erg[0];
           // strMatNr = strMatNr.TrimStart('0');

            string start1 = erg[1];
            strGesMenge = start1.Substring(0, start1.IndexOf('.'));
            string start2 = erg[2];
            strGesFailMenge = start2.Substring(0, start2.IndexOf('.'));
            return true;
        }
        public string getMatKurzText(string strMatNr, RfcDestination destination)
        {
            // Metadaten beschaffen für RFC_READ_TABLE   - allgemeines RFC Baustein zum Lesen
            IRfcFunction rfcFunktion = destination.Repository.CreateFunction("RFC_READ_TABLE");
            rfcFunktion.SetValue("QUERY_TABLE", "MAKT"); //Tabelle, die gelesen wird
            rfcFunktion.SetValue("DELIMITER", "~");//Trennzeichen
            IRfcTable tableField = rfcFunktion.GetTable("FIELDS");// Definieren welche Spalten gelesen werden
            tableField.Append();
            tableField.SetValue("FIELDNAME", "MAKTX"); // Materialkurztext
            IRfcTable tableOptions = rfcFunktion.GetTable("OPTIONS");
            tableOptions.Append();
            string strMatNrToSap = "MATNR = "+"'"+strMatNr+"'"+" and SPRAS = 'D'";
            tableOptions.SetValue("TEXT", strMatNrToSap);
            rfcFunktion.Invoke(destination);
            // Daten verarbeiten
            IRfcTable dataTable = rfcFunktion.GetTable("DATA");//Datenspalte holen 
            string strErg = "";
            // uber Zeilen gehen 
            foreach (var dataRow in dataTable)
            {
                string data = dataRow.GetValue("WA").ToString();
               // Console.WriteLine(data);
                strErg = data.ToString();
            }
            return strErg;
        }
    }
}

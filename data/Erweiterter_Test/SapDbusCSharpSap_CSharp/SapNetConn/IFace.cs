using System;
using DBus;
using SapNetConn;
using SAP.Middleware.Connector;


[Interface("my.server.interface")]
public interface ifaceServer
{
    string getMatNrFromPa(string qtAuftrNr);
    string pingSapDest();
    string getMatKurzText(string qtSapNr);
    string getValueFromTable(string qtTable, string[]qtColumn, string qtDelimiter,string []qtWhere);
    string getOrderTypeNr(string qtSapOrderNr);
    string getOrderQty(string qtSapOrderNr);
    string getOrderFailQty(string qtSapOrderNr);
}


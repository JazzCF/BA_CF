#include "sapdblib.h"
#include <QtDBus/QDBusConnection>
#include <QDebug>
#include "sapdblib_interface.h"

SAPDBLib::SAPDBLib(const QString &qstrConnectionStr,const QString& qstrName, QObject *parent) : QObject(parent)
{
	//	m_sapDBLib = new SAPDBLib("tcp:host=aaa.bbb.ccc.189,port=12345","my.qtconnect.DBus");

	QDBusConnection myconnection = QDBusConnection::connectToBus(qstrConnectionStr,qstrName);
	m_busName = qstrName;

	if (myconnection.isConnected())
	{
		m_remoteApp = new QDBusInterface ("com.swissbit.sap","/com/swissbit/sap","my.server.interface", myconnection);
		if (m_remoteApp->isValid())
		{
			qDebug()<<"connected to D-Bus";
		}
		else
		{
			m_lastError = m_remoteApp->lastError();
			qWarning()<<"Server interface is not available. Error: "<<m_lastError;
		}
	}
	else
	{
		m_lastError = myconnection.lastError();
		qWarning()<<"connection with D-Bus failed"<<m_lastError;
	}
}

SAPDBLib::SAPDBLib( QObject *parent) : QObject(parent)
{
	// verbindung aufbauen
	//m_sapdblibcsharpconnector = new my::server::interface::MyServerInterfaceInterface("com.swissbit.sap","/com/swissbit/sap",
	//																				  QDBusConnection::sessionBus());

	//qDebug()<<m_sapdblibcsharpconnector->lastError();
}

/* ping SAP Destination
 *
 */
bool SAPDBLib::ping()
{
	QDBusReply<QString>reply=m_remoteApp->call("pingSapDest");
	if(reply.isValid())
	{
		qDebug()<<reply.value();
		return true;
	}
	else
	{
		qWarning()<<"ping koennte nicht ausgefuehrt werden!";
	}
	return false;
}

/* get Materialkurztext
 * qstrSapTypeNr is SAP Nr, qstrMkt is Materialkurztext
 * return true if successful
 */
bool SAPDBLib::getMKT(const QString &qstrSapTypeNr, QString &qstrMkt)
{
	if(qstrSapTypeNr.length()==6)
	{
		if(checkInputToDecimal(qstrSapTypeNr)==true)
		{
			// mit '0' auffuellen
			QString temp = qstrSapTypeNr.rightJustified(18,'0');
			// Funktion aufrufen und Ruckgabe auf String pruefen
			QDBusReply<QString>reply=m_remoteApp->call("getMatKurzText",temp);
			if(reply.isValid())
			{
				qstrMkt = reply.value();
				return true;
			}
			else
			{
				qWarning()<<"getMKT() koennte nicht ausgefuehrt werden";
			}
		}
		else
		{
			qWarning()<<"SAP-Nr falsch";
		}
	}
	else
	{
		qWarning()<<"SAP-Nr falsch";
	}
	return false;
}

/* get SAP Number from PA
 * qstrSapOrderNr is PA, qstrSapTypeNr is SAPNr
 * return true if successful
 */
bool SAPDBLib::getOrderTypeNr(const QString &qstrSapOrderNr, QString &qstrSapTypeNr)
{
	if (qstrSapOrderNr.length()==8)
	{
		if(checkInputToDecimal(qstrSapOrderNr)==true)
		{
			QDBusReply<QString>reply=m_remoteApp->call("getOrderTypeNr",qstrSapOrderNr);
			if(reply.isValid())
			{
				qstrSapTypeNr = reply.value();
				return true;
			}
			else
			{
				qWarning()<<"getOrderTypeNr() koennte nicht ausgefuehrt werden";
			}
		}
		else
		{
			qWarning()<<"bitte fuer AuftragsNummer Zahlen eingeben ";
		}
	}
	else
	{
		qWarning()<<"AuftragsNummer falsch :"<<qstrSapOrderNr;
	}
	return false;
}

/* get Order Quantity from PA
 * qstrSapOrderNr is PA, qstrOrderQty is Order Quantity
 * return true if successful
 */
bool SAPDBLib::getOrderQty(const QString &qstrSapOrderNr, QString &qstrOrderQty)
{
	if(qstrSapOrderNr.length()==8)
	{
		if(checkInputToDecimal(qstrSapOrderNr)==true)
		{
			QDBusReply<QString>reply=m_remoteApp->call("getOrderQty",qstrSapOrderNr);

			if(reply.isValid())
			{
				qstrOrderQty = reply.value();
				return true;
			}
			else
			{
				qWarning()<<"getOrderQty() koennte nicht ausgefuehrt werden";
			}
		}
		else
		{
			qWarning()<<"bitte fuer AuftragsNummer Zahlen eingeben";
		}
	}
	else
	{
		qWarning()<<"AuftragsNummer falsch :"<<qstrSapOrderNr;
	}
	return false;
}

/* get Value from Table
 * qstrTable is name of table, qstrDelimiter is delimiter as string,
 * qstrColumn are columns, qstrWhere are where clause
 * qstrResult is result as string
 * return true if successful
 */
bool SAPDBLib::getValueFromTable(const QString &qstrTable, QString &qstrDelimiter, QStringList &qstrColumn, QStringList &qstrWhere, QString &qstrResult)
{
	QDBusReply<QString>reply=m_remoteApp->call("getValueFromTable",qstrTable,qstrColumn,qstrDelimiter,qstrWhere);

	if(reply.isValid())
	{
		qstrResult = reply.value();
		return true;
	}
	else
	{
		qWarning()<<"getValueFromTable() koennte nicht ausgefuehrt werden";
	}
	return false;
}

/* get Order Fail Quantity from PA
 * qstrSapOrderNr is PA, qstrOrderQty is Order Fail Quantity
 * return true if successful
 */
bool SAPDBLib::getOrderFailQty(const QString &qstrSapOrderNr, QString &qstrOrderFailQty)
{
	if(qstrSapOrderNr.length()==8)
	{
	if(checkInputToDecimal(qstrSapOrderNr)==true)
	{
		QDBusReply<QString>reply=m_remoteApp->call("getOrderFailQty",qstrSapOrderNr);

		if(reply.isValid())
		{
			qstrOrderFailQty = reply.value();
			return true;
		}
		else
		{
			qWarning()<<"getValueFromTable() koennte nicht ausgefuehrt werden";
		}
	}
	else
	{
		qWarning()<<"bitte fuer AuftragsNummer Zahlen eingeben";
	}
	}
	else
	{
		qWarning()<<"AuftragsNummer falsch :"<<qstrSapOrderNr;
	}
	return false;
}

/* check the parameter on decimal
 * qstrToCheck parameter to check
 * return true if it can be convert to Integer
 */
bool SAPDBLib::checkInputToDecimal(QString qstrToCheck)
{
	bool ok;
	if(qstrToCheck.toInt(&ok,10))
	{
		return true;
	}
	else
	{
		qWarning()<<"Input can not be converted to decimal";
	}
	return false;
}

/* get Order Type by Order Nr
* return true if ok
*/
bool SAPDBLib::getOrderType(const QString &qstrSapOrderNr, QString &qstrOrderType)
{
	if(qstrSapOrderNr.length()==8)
	{
	if(checkInputToDecimal(qstrSapOrderNr)==true)
	{
		QDBusReply<QString>reply=m_remoteApp->call("getOrderType",qstrSapOrderNr);

		if(reply.isValid())
		{
			qstrOrderType = reply.value();
			qDebug()<<"OrderType :"<<qstrOrderType;
			return true;
		}
		else
		{
			qWarning()<<"getValueFromTable() koennte nicht ausgefuehrt werden";
		}
	}
	else
	{
		qWarning()<<"bitte fuer AuftragsNummer Zahlen eingeben";
	}
	}
	else
	{
		qWarning()<<"AuftragsNummer falsch :"<<qstrSapOrderNr;
	}
	return false;
}

/* isConnected check connection like isInterfaceConnected
*return true if ok
*/
bool SAPDBLib::isConnected()
{
	if (m_remoteApp->isValid())
	{
		return true;
	}
	return false;
}

/* isDBusConnected check connection to dbus
*return true if ok
*/
bool SAPDBLib::isDBusConnected()
{
	if (QDBusConnection(m_busName).isConnected())
	{
		return true;
	}
	return false;
}

/* isInterfaceConnected check connection to Interface
* if connection ok, dbus connection is ok too
*return true if ok
*/
bool SAPDBLib::isInterfaceConnected()
{
	if (m_remoteApp->isValid())
	{
		return true;
	}
	return false;
}

/* lastError
*
* return last error
*/
QDBusError SAPDBLib::lastError()
{
	return m_lastError;
}

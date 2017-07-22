#include "sapwindow.h"
#include "ui_sapwindow.h"
#include <QDebug>
#include <QtDBus/QDBusConnection>
#include "sap_adapt.h"
#include "sap_interface.h"

SapWindow::SapWindow(QWidget *parent) :
	QMainWindow(parent),
	ui(new Ui::SapWindow)
{
	ui->setupUi(this);
	connect(this->ui->pbRead, &QPushButton::clicked, this, &SapWindow::getMatNrMeng);
	connect(this->ui->pbPing, &QPushButton::clicked, this, &SapWindow::pingSapServer);
	connect(this->ui->pbReadMatKurzText, &QPushButton::clicked, this, &SapWindow::getMatKurzText);

	QDBusConnection myconnection = QDBusConnection::connectToBus("tcp:host=aaa.bbb.ccc.189,port=12345","my.qtconnect.DBus");
	if (myconnection.isConnected())
	{
		m_remoteApp = new QDBusInterface ("com.swissbit.sap","/com/swissbit/sap","my.server.interface",
								 myconnection);
		qDebug()<<"connected to D-Bus";
	}
	else
	{
		qDebug()<<"connection with D-Bus failed";
	}
	// Aufbau der ersten Verbindung.
	pingSapServer();
}

SapWindow::~SapWindow()
{
	delete ui;
}

/* Ping Sap Server
 * return true if successfull
*/
bool SapWindow::pingSapServer()
{
	QDBusReply<QString>reply=m_remoteApp->call("pingSapDest");
	if(reply.isValid())
	{
		qDebug()<<reply.value();
		return true;
	}
	else
	{
		qDebug()<<"ping könnte nicht ausgefuehrt werden!";
	}
	return false;
}

/* getMatNr
 * liefert Materialnummer, gesamte Auftragsmenge, gesamte Fail-Menge
 * return true if successfull
*/
bool SapWindow::getMatNrMeng()
{
	QString qstrAuftragsNr = QString(this->ui->leAuftragsNr->text());
	int iAuftragsNr = qstrAuftragsNr.toInt();
	if(!((qstrAuftragsNr.length()!=8)||(iAuftragsNr==0)))
	{
		// Funktion aufrufen und Ruckgabe auf String prüfen
		QDBusReply<QString>reply=m_remoteApp->call("getMatNrFromPa",qstrAuftragsNr);
		if(reply.isValid())
		{
			QString responseFromSharp = reply.value();
			// String vom Sharp am "~" trennen
			QStringList ergebnis = responseFromSharp.split("~");
			//erstes Element ins Integer-> fuehrende Nullen weg
			int erg0 = ergebnis[0].toInt();
			//zweites Element am "." trennen
			QStringList erg1 = ergebnis[1].split(".");
			//drittes Element am "." trennen
			QStringList erg2 = ergebnis[2].split(".");
			this->ui->tbMaterialNr->setText(QString::number(erg0));
			this->ui->tbGesAufMenge->setText(erg1[0]);
			this->ui->tbGesFailMenge->setText(erg2[0]);
			return true;
		}
		else
		{
			qDebug()<<"getMatNrMeng() könnte nicht ausgeführt werden";
		}
	}
	return false;
}

/* getMatKurzText ist SapNr
 * return true if successfull
*/
bool SapWindow::getMatKurzText()
{
	QString qstrSapNr = QString(this->ui->leSapNr->text());
	if(qstrSapNr.length()==6)
	{
		// mit '0' auffuellen
		QString temp = qstrSapNr.rightJustified(18,'0');
		qstrSapNr=temp;

		// Funktion aufrufen und Ruckgabe auf String prüfen
		QDBusReply<QString>reply=m_remoteApp->call("getMatKurzText",qstrSapNr);
		if(reply.isValid())
		{
			QString responseFromSharp = reply.value();
			this->ui->tbMaterialkurztext->setText(responseFromSharp);
			return true;
		}
		else
		{
			qDebug()<<"getMatKurzText() könnte nicht ausgeführt werden";
		}
	}
	return false;
}

/* getValueFromTable
 * return true if successfull
 */
bool SapWindow::getValueFromTable()
{
	QString qstrTable="";
	QString qstrDelimiter;
	QStringList qstrColumn;
	QStringList qstrWhere; // auch als QStringList moeglich

	// Example
	qstrTable="AFKO";
	qstrDelimiter="#";
	qstrColumn<<"STLBEZ"<<"GAMNG"<<"GASMG";
	qstrWhere<<"AUFNR = '000060098052'"; // auf Format achten

	QDBusReply<QString>reply=m_remoteApp->call("getValueFromTable",qstrTable,qstrColumn,qstrDelimiter,qstrWhere);
	if(reply.isValid())
	{
		qDebug()<<"valid"<<reply;
		return true;
	}
	else
	{
		qDebug()<<"getValueFromTable() könnte nicht ausgeführt werden";
	}
	return false;
}


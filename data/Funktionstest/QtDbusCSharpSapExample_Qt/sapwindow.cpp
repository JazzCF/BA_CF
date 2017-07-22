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
	connect(this->ui->pbRead, &QPushButton::clicked, this, &SapWindow::SendAutrgNrRecive);
}

SapWindow::~SapWindow()
{
	delete ui;
}

bool SapWindow::SendAutrgNrRecive()
{
	QString qstrAuftragsNr = QString(this->ui->leAuftragsNr->text());
	int iAuftragsNr = qstrAuftragsNr.toInt();
	if(!((qstrAuftragsNr.length()!=8)||(iAuftragsNr==0)))
	{
		// Verbindung mit dem Objet
		QDBusInterface remoteApp("org.dbussharp.test","/org/dbussharp/test","my.server.interface",
								 QDBusConnection::connectToBus("tcp:host=aaa.bbb.ccc.189,port=12345","my.connect.DBus"));

		// Funktion aufrufen und Ruckgabe auf String prüfen
		QDBusReply<QString>reply=remoteApp.call("execute");
		if(reply.isValid())
		{
			qDebug()<<reply.value();
			return true;
		}
		else
		{
			qDebug()<<"nicht erfolgreich ausgeführt";
		}
	}
	return false;
}


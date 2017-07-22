#include "sapdblibexample.h"
#include "ui_sapdblibtest.h"
#include "sapdblib.h"
#include "QDebug"
SAPDBLibTest::SAPDBLibTest(QWidget *parent) :
	QMainWindow(parent),
	ui(new Ui::SAPDBLibTest)
{
	ui->setupUi(this);

	connect(this->ui->pbPing, &QPushButton::clicked, this, &SAPDBLibTest::PingTest);
	connect(this->ui->pbReadMatKurzText, &QPushButton::clicked, this, &SAPDBLibTest::getMKTTest);
	connect(this->ui->pbSapFromPa, &QPushButton::clicked, this, &SAPDBLibTest::getOrderTypeNrTest);
	connect(this->ui->pbOrderType, &QPushButton::clicked, this, &SAPDBLibTest::getOrderType);
	connect (this->ui->pbQtyPass, &QPushButton::clicked, this, &SAPDBLibTest::getOrderQty);

	m_sapDB = new SAPDBLib("tcp:host=aaa.bbb.ccc.189,port=12345","my.qtconnect.DBus");
	if(m_sapDB->isDBusConnected())
	{
		if (m_sapDB->isConnected())
		{
			m_sapDB->ping();
		}
		else
		{
		this->ui->tbStatus->setStyleSheet("background-color: rgb(255, 161, 163);");
		qWarning()<<m_sapDB->lastError();
		this->ui->tbStatus->setText("DBus Interface is not available");
		}
	}
	else
	{
		this->ui->tbStatus->setStyleSheet("background-color: rgb(255, 161, 163);");
		qWarning()<<m_sapDB->lastError();
		this->ui->tbStatus->setText("DBus is not available");
	}

	m_productDB = FlashDB::ProductDB::getInstance();
	m_productDB->connect( QString( "xxx.yyy.zzz.190" ), QString( "Products" ),
					  QString( "testexampleonly" ), QString( "testexampleonly" ), 1433 );
	if(!m_productDB->isConnected())
	{
		qWarning()<<m_productDB->getLastError();
	}

	m_resultDB = FlashDB::ResultDB::getInstance();
	m_resultDB->connect( QString( "xxx.yyy.zzz.190" ), QString( "Results" ),
					 QString( "testexampleonly" ), QString( "testexampleonly" ), 1433 );
	if(!m_resultDB->isConnected())
	{
		qWarning()<<m_resultDB->getLastError();
	}
}

SAPDBLibTest::~SAPDBLibTest()
{
	m_resultDB->destroy();
	m_productDB->destroy();
	delete ui;
}

bool SAPDBLibTest::PingTest()
{
	resetGui();
	if(m_sapDB->ping())
	{
		this->ui->tbStatus->setText("ping erfolgreich");
		return true;
	}
	this->ui->tbStatus->setText("ping NICHT erfolgreich");
	qDebug()<<"LastError: "<<m_sapDB->lastError();
	return false;
}

bool SAPDBLibTest::getMKTTest()
{
	resetGui();
	QString saptypnr = this->ui->leSapNr->text();
	QString mktProd;
	QString mktSap;
	//this->ui->gbGetMKT->setStyleSheet("background-color: rgb(255, 255, 255);");

	if(m_sapDB->getMKT(saptypnr,mktSap))
	{
		this->ui->tbMaterialkurztext->setText(mktSap);
		qDebug()<<"mktSAP :"<<mktSap;

		if(m_productDB->getMKT(saptypnr,mktProd))
		{
			qDebug()<<"mktProd :"<<mktProd;
			this->ui->tbMaterialkurztextFlash->setText(mktProd);
			if(!(mktSap==mktProd))
			{
				//qDebug()<<"bin im stringvergleich drin";
				this->ui->gbGetMKT->setStyleSheet("background-color: rgb(255, 11, 23);");
			}
			return true;
		}
		else
		{
			qWarning()<<"Zu PA "<<saptypnr<<"kann in Flash-DB kein Eintrag gefunden werden";
			this->ui->tbMaterialkurztextFlash->setText("kein Eintrag in FlashDB");
			this->ui->gbGetMKT->setStyleSheet("background-color: rgb(255, 11, 23);");
		}
	}
	else
	{
		qWarning()<<"Zu PA "<<saptypnr<<"kann in SAP-DB kein Eintrag gefunden werden";
		this->ui->tbMaterialkurztext->setText("kein Eintrag in SAP");
		this->ui->gbGetMKT->setStyleSheet("background-color: rgb(255, 11, 23);");
	}
	return false;
}

bool SAPDBLibTest::getOrderTypeNrTest()
{
	resetGui();
	this->ui->tbMaterialNr->setStyleSheet("background-color: rgb(255, 255, 255);");
	this->ui->tbMaterialNrFlash->setStyleSheet("background-color: rgb(255, 255, 255);");
	QString orderNr = this->ui->leAuftragsNr->text();
	QString SapFromProd;
	QString SapFromSap;

	if(m_sapDB->getOrderTypeNr(orderNr,SapFromSap))
	{
		qDebug()<<"SapFromSap :"<<SapFromSap;
		int temp=SapFromSap.toInt();
		SapFromSap= QString::number(temp);
		this->ui->tbMaterialNr->setText(SapFromSap);

		if(m_productDB->getTypeByOrderNr(orderNr,SapFromProd))
		{
			qDebug()<<"SapFromProd :"<<SapFromProd;

			this->ui->tbMaterialNrFlash->setText(SapFromProd);
			if(!(SapFromSap==SapFromProd))
			{
				//qDebug()<<"bin im stringvergleich drin";
				this->ui->gbSapFromPa->setStyleSheet("background-color: rgb(255, 11, 23);");
			}
			return true;
		}
		else
		{
			this->ui->gbSapFromPa->setStyleSheet("background-color: rgb(255, 11, 23);");
			this->ui->tbMaterialNrFlash->setText("kein Eintrag in SAP");
			qWarning()<<"Zu PA "<<orderNr<<"kann in Flash-DB kein Eintrag gefunden werden";
		}
	}
	else
	{
		this->ui->gbSapFromPa->setStyleSheet("background-color: rgb(255, 11, 23);");
		this->ui->tbMaterialNr->setText("kein Eintrag in FlashDB");
		qWarning()<<"Zu PA "<<orderNr<<"kann in SAP-DB kein Eintrag gefunden werden";
	}
	return false;
}

bool SAPDBLibTest::getOrderQty()
{
	resetGui();
	QString orderNr = this->ui->leAuftragsNr->text();
	int QtyFromProd;
	QString QtyFromSap;

	if(m_sapDB->getOrderQty(orderNr,QtyFromSap))
	{
		//qDebug()<<"QtyFromSap :"<<QtyFromSap;
		//int temp=SapFromSap.toInt();
		//SapFromSap= QString::number(temp);
		this->ui->tbGesAufMenge->setText(QtyFromSap);
		int warn;
		int fail;
		if(m_resultDB->getCurrentPassFailWarning(orderNr.toInt(),10,QtyFromProd,fail,warn))
		{
			//qDebug()<<"QtyFromProd :"<<QtyFromProd;
			if(!(QtyFromSap==QString(QtyFromProd)))
			{
				this->ui->gbQtyPass->setStyleSheet("background-color: rgb(255, 11, 23);");

			}
			this->ui->tbGesAufMengeFlash->setText(QString::number(QtyFromProd));
			return true;
		}
		else
		{
			qWarning()<<"Zu PA "<<orderNr<<"kann in Flash-DB kein Eintrag gefunden werden";
		}
	}
	else
	{
		qWarning()<<"Zu PA "<<orderNr<<"kann in SAP-DB kein Eintrag gefunden werden";
	}
	return false;
}

bool SAPDBLibTest::getOrderType()
{
	resetGui();
	QString orderNr = this->ui->leAuftragsNr->text();
	QString OrderTypeFromSap;
	if(m_sapDB->getOrderType(orderNr,OrderTypeFromSap))
	{
		//qDebug()<<"OrderTypeFromSap :"<<OrderTypeFromSap;
		this->ui->tbOrderType->setText(OrderTypeFromSap);
	}
	else
	{
		qWarning()<<"Zu PA "<<orderNr<<"kann in SAP-DB kein Eintrag gefunden werden";
	}
	return false;
}

void SAPDBLibTest::resetGui()
{
	this->ui->tbGesAufMenge->setText("");
	this->ui->tbGesAufMengeFlash->setText("");
//	this->ui->tbGesFailMenge->setText("");
//	this->ui->tbGesFailMengeFlash->setText("");
	this->ui->tbMaterialkurztext->setText("");
	this->ui->tbMaterialkurztextFlash->setText("");
	this->ui->tbMaterialNr->setText("");
	this->ui->tbMaterialNrFlash->setText("");
	this->ui->tbOrderType->setText("");
	this->ui->tbStatus->setText("");
	this->ui->gbGetMKT->setStyleSheet("background-color: rgb(245, 245, 245);");
	this->ui->gbQtyPass->setStyleSheet("background-color: rgb(245, 245, 245);");
	this->ui->gbSapFromPa->setStyleSheet("background-color: rgb(245, 245, 245);");	
}



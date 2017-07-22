#include "sapdblibtests.h"

QTEST_MAIN(SAPDBLibTests)

SAPDBLibTests::SAPDBLibTests(QObject *parent) : QObject(parent)
{

}

// erste funktion die aufgerufen wird
void SAPDBLibTests::initTestCase()
{
	m_sapDBLib = new SAPDBLib("tcp:host=aaa.bbb.ccc.189,port=12345","my.qtconnect.DBus");
	qDebug()<<m_sapDBLib->lastError();
}

// letzte funktion die aufgerufen wird
void SAPDBLibTests::cleanupTestCase()
{
	delete (m_sapDBLib);
}

void SAPDBLibTests::testGetMKT()
{
	QString saptypnr = "605229";
	QString mkt;
	QVERIFY(m_sapDBLib->getMKT(saptypnr, mkt));
	QVERIFY(mkt == "SFCA120GH1AA2TO-I-HC-216-STD");
	QVERIFY(m_sapDBLib->getMKT("60md29",mkt)==false);
	QVERIFY(m_sapDBLib->getMKT("60522999",mkt)==false);
}

void SAPDBLibTests::testGetOrderTypeNr()
{
	QString qstrOrderNr = "60098052";
	QString qStrSapNr;
	QVERIFY(m_sapDBLib->getOrderTypeNr(qstrOrderNr,qStrSapNr));
	QVERIFY(qStrSapNr == "000000000000604934");// SAP Format
	QVERIFY(m_sapDBLib->getOrderTypeNr("6009805255",qStrSapNr)==false);
	QVERIFY(m_sapDBLib->getOrderTypeNr("6009805a",qStrSapNr)==false);
}

void SAPDBLibTests::testPing()
{
	QVERIFY(m_sapDBLib->ping());
}

void SAPDBLibTests::testGetValueFromTable()
{
	QString result;
	QString qstrTable="AFKO";
	QString qstrDelimiter="~";
	QStringList qstrColumn;
	QStringList qstrWhere;
	qstrColumn<<"STLBEZ"<<"GAMNG"<<"GASMG";
	qstrWhere<<"AUFNR = '000060098052'"; // auf Format achten
	m_sapDBLib->getValueFromTable(qstrTable,qstrDelimiter,qstrColumn,qstrWhere,result);
	QVERIFY(result =="000000000000604934~      46.000 ~       0.000");
}

void SAPDBLibTests::testGetOrderQty()
{
	QString qstrOrderNr = "60098052";
	QString result;
	QVERIFY(m_sapDBLib->getOrderQty(qstrOrderNr,result));
	//qDebug()<<"Ergebniss: "<<result;
	QVERIFY(result == "      46.000");// SAP Format
	QVERIFY(m_sapDBLib->getOrderQty("600980",result)==false);
	QVERIFY(m_sapDBLib->getOrderQty("6009805A",result)==false);
}

void SAPDBLibTests::testGetOrderFailQty()
{
	QString qstrOrderNr = "60091935";
	QString result;
	QVERIFY(m_sapDBLib->getOrderFailQty(qstrOrderNr,result));
	//qDebug()<<"Ergebniss: "<<result;
	QVERIFY(result == "      33.000");// SAP Format
	QVERIFY(m_sapDBLib->getOrderFailQty("6009193599",result)==false);
	QVERIFY(m_sapDBLib->getOrderFailQty("6009193A",result)==false);
}

void SAPDBLibTests::testGetOrderType()
{
	QString qstrOrderNr = "60091935";
	QString result;
	QVERIFY(m_sapDBLib->getOrderType(qstrOrderNr,result));
	qDebug()<<"Ergebniss: "<<result;
	QVERIFY(result == "ZD01");
	QVERIFY(m_sapDBLib->getOrderType("6009193599",result)==false);
	QVERIFY(m_sapDBLib->getOrderType("6009193A",result)==false);
}

void SAPDBLibTests::testIsDBusConnected()
{
	QVERIFY(m_sapDBLib->isDBusConnected()==true);
//	qDebug()<<m_sapDBLib->lastError();
}

void SAPDBLibTests::testIsConnected()
{
	QVERIFY(m_sapDBLib->isInterfaceConnected()==true);
//	qDebug()<<m_sapDBLib->lastError();
}


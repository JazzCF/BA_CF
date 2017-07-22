#ifndef SAPDBLIBTESTS_H
#define SAPDBLIBTESTS_H

#include <QObject>
#include <QTest>
#include "sapdblib.h"

class SAPDBLibTests : public QObject
{
	Q_OBJECT

private:
	SAPDBLib* m_sapDBLib;

public:
	explicit SAPDBLibTests(QObject *parent = 0);

signals:

private slots:
	void initTestCase();
	void cleanupTestCase();
	void testGetMKT();
	void testGetOrderTypeNr();
	void testPing();
	void testGetValueFromTable();
	void testGetOrderQty();
	void testGetOrderFailQty();
};

#endif // SAPDBLIBTESTS_H

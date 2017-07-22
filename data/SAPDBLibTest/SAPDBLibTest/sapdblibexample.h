#ifndef SAPDBLIBTEST_H
#define SAPDBLIBTEST_H

#include <QMainWindow>
#include "sapdblib.h"
#include "ProductDB.h"
#include "ResultDB.h"
namespace Ui {
	class SAPDBLibTest;
}

class SAPDBLibTest : public QMainWindow
{
	Q_OBJECT
	SAPDBLib* m_sapDB;
	FlashDB::ProductDB* m_productDB;
	FlashDB::ResultDB* m_resultDB;

public:
	explicit SAPDBLibTest(QWidget *parent = 0);
	~SAPDBLibTest();
public slots:
	bool PingTest();
	bool getMKTTest();
	bool getOrderTypeNrTest();
	bool getOrderQty();
	bool getOrderType();
	void resetGui();

private:
	Ui::SAPDBLibTest *ui;
};

#endif // SAPDBLIBTEST_H

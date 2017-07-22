#ifndef SAPDBLIB_H
#define SAPDBLIB_H

#include <QObject>
#include <QString>
#include <QtDBus/QDBusConnection>
#include "sapdblib_interface.h"

class SAPDBLib : public QObject
{
	Q_OBJECT
	QDBusInterface* m_remoteApp;


public:
	explicit SAPDBLib(const QString &qstrConnectionStr, const QString &qstrName, QObject *parent = 0);
	explicit SAPDBLib(QObject *parent = 0);
//	local::SAPDBLib::LocalSAPDBLibInterface* m_sapdblibcsharpconnector;
//	my::server::interface::MyServerInterfaceInterface* m_sapdblibcsharpconnector;

signals:

public slots:

	bool ping();
	bool getMKT(const QString &qstrSapTypeNr, QString& qstrMkt);
	bool getValueFromTable(const QString &qstrTable, QString &qstrDelimiter, QStringList &qstrColumn, QStringList &qstrWhere, QString &qstrResult);
	bool getOrderTypeNr(const QString &qstrSapOrderNr, QString &qstrSapTypeNr);
	bool getOrderQty(const QString &qstrSapOrderNr, QString &qstrOrderQty);
	bool getOrderFailQty(const QString &qstrSapOrderNr, QString &qstrOrderFailQty);
	bool checkInputToDecimal(QString qstrToCheck);
};

#endif // SAPDBLIB_H

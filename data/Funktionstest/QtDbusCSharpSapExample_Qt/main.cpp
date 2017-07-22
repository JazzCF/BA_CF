#include "sapwindow.h"
#include <QApplication>
#include "sap_adapt.h"
#include "sap_interface.h"
#include <QtDBus/QDBusConnection>

int main(int argc, char *argv[])
{
	QApplication a(argc, argv);
	SapWindow w;
	w.show();
	return a.exec();
}

#include "sapdblibexample.h"
#include <QApplication>
#include "sapdblib.h"
//#include "sapdblib_interface.h"
int main(int argc, char *argv[])
{
	QApplication a(argc, argv);
	SAPDBLibTest w;
	w.show();
	return a.exec();
}

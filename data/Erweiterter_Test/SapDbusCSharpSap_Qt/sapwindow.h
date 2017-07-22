#ifndef SAPWINDOW_H
#define SAPWINDOW_H

#include <QMainWindow>
#include <QtDBus/QDBusConnection>

namespace Ui {
	class SapWindow;
}

class SapWindow : public QMainWindow
{
	Q_OBJECT
	QDBusInterface* m_remoteApp;

public:
	explicit SapWindow(QWidget *parent = 0);
	~SapWindow();
public slots:
	bool getMatKurzText();
	bool getMatNrMeng();
	bool pingSapServer();
	bool getValueFromTable();
private:
	Ui::SapWindow *ui;	
};

#endif // SAPWINDOW_H

#ifndef SAPWINDOW_H
#define SAPWINDOW_H

#include <QMainWindow>

namespace Ui {
	class SapWindow;
}

class SapWindow : public QMainWindow
{
	Q_OBJECT

public:
	explicit SapWindow(QWidget *parent = 0);
	~SapWindow();
public slots:
	bool SendAutrgNrRecive();
private:
	Ui::SapWindow *ui;
};

#endif // SAPWINDOW_H

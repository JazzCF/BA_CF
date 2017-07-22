#-------------------------------------------------
#
# Project created by QtCreator 2015-11-30T13:57:31
#
#-------------------------------------------------

QT       += core gui dbus

greaterThan(QT_MAJOR_VERSION, 4): QT += widgets

TARGET = SapDbus
TEMPLATE = app


SOURCES += main.cpp\
        sapwindow.cpp

HEADERS  += sapwindow.h

FORMS    += sapwindow.ui

DISTFILES += \
    sapwindow.xml

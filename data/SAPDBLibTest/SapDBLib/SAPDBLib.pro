#-------------------------------------------------
#
# Project created by QtCreator 2015-12-17T14:44:09
#
#-------------------------------------------------


test {
    TEMPLATE = app
    QT += testlib
    SOURCES += tests/sapdblibtests.cpp
    HEADERS += tests/sapdblibtests.h
} else {
    TEMPLATE = lib
}

QT       += dbus
QT       -= gui

TARGET = SAPDBLib
DEFINES += SAPDBLIB_LIBRARY


#DBUS_ADAPTORS += sapdblib.xml
DBUS_INTERFACES += sapdblib.xml


SOURCES += \
    sapdblib.cpp

HEADERS += \
    sapdblib.h

unix {
    target.path = /usr/lib
    INSTALLS += target
}

DISTFILES += \
    SAPDBLib_static.pri

#-------------------------------------------------
#
# Project created by QtCreator 2015-12-28T13:36:46
#
#-------------------------------------------------

QT       += core gui

greaterThan(QT_MAJOR_VERSION, 4): QT += widgets

include(../SAPDBLib/SAPDBLib_static.pri)
include (../FlashDBLib/FlashDBLib_static.pri)

TARGET = SAPDBLibTest
TEMPLATE = app


SOURCES += main.cpp \
    sapdblibexample.cpp

HEADERS  += \
    sapdblib_interface.h \
    sapdblibexample.h

FORMS    += sapdblibtest.ui


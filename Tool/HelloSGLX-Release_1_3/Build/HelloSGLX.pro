
TEMPLATE = app
TARGET   = HelloSGLX

win32 {
    DESTDIR = C:/Users/labadmin/Desktop/SGLBUILD/FIXU/HelloSGLX/HelloSGLX-win
#    DESTDIR = C:/Users/labadmin/Desktop/SGLBUILD/FIXU/HelloSGLX/Debug
}

unix {
    DESTDIR = /home/billkarsh/Code/HelloSGLX/HelloSGLX-linux
}

QT += widgets

HEADERS +=              \
    CGBL.h              \
    Cmdline.h           \
    SglxApi.h           \
    SglxCppClient.h     \
    Tool.h              \
    Util.h

SOURCES +=              \
    main.cpp            \
    CGBL.cpp            \
    Cmdline.cpp         \
    SglxCppClient.cpp   \
    Tool.cpp            \
    Util.cpp            \
    Util_osdep.cpp

win32 {
    QMAKE_LIBDIR += $${_PRO_FILE_PWD_}
    LIBS    += -lWS2_32 -lUser32 -lwinmm -llibSglxApi
    DEFINES += _CRT_SECURE_NO_WARNINGS WIN32
}

QMAKE_TARGET_COMPANY = Bill Karsh
QMAKE_TARGET_PRODUCT = HelloSGLX
QMAKE_TARGET_DESCRIPTION = SpikeGLX Com Link
QMAKE_TARGET_COPYRIGHT = (c) 2023, Bill Karsh, All rights reserved
VERSION = 1.3



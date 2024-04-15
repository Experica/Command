#ifndef CGBL_H
#define CGBL_H

#include <QString>

/* ---------------------------------------------------------------- */
/* Types ---------------------------------------------------------- */
/* ---------------------------------------------------------------- */

class CGBL
{
public:
    const char  *host;
    QString     cmd,
                args;
    int         port;

public:
    CGBL() : host("localhost"), port(4142)  {}

    bool SetCmdLine( int argc, char* argv[] );

private:
    QString trim_adjust_slashes( const QString &dir );
};

/* --------------------------------------------------------------- */
/* Globals ------------------------------------------------------- */
/* --------------------------------------------------------------- */

extern CGBL GBL;

#endif  // CGBL_H



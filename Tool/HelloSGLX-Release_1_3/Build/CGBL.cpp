
#include "CGBL.h"
#include "Cmdline.h"
#include "Util.h"


/* --------------------------------------------------------------- */
/* Globals ------------------------------------------------------- */
/* --------------------------------------------------------------- */

CGBL    GBL;

/* --------------------------------------------------------------- */
/* PrintUsage  --------------------------------------------------- */
/* --------------------------------------------------------------- */

static void PrintUsage()
{
    Log() << "------------------------";
    Log() << "Usage:\n";
    Log() << ">HelloSGLX -cmd=command [ options ]\n";
    Log() << "Options:";
    Log() << "-host=127.0.0.1   ;ip4 SpikeGLX network address";
    Log() << "-port=4142        ;ip4 port";
    Log() << "-args=as_needed   ;optional command arguments";
    Log() << "------------------------";
}

/* ---------------------------------------------------------------- */
/* CGBL ----------------------------------------------------------- */
/* ---------------------------------------------------------------- */

/* --------------------------------------------------------------- */
/* Public -------------------------------------------------------- */
/* --------------------------------------------------------------- */

bool CGBL::SetCmdLine( int argc, char* argv[] )
{
// Parse args

    const char  *sarg = 0;

    for( int i = 1; i < argc; ++i ) {

        if( GetArgStr( host, "-host=", argv[i] ) )
            ;
        else if( GetArgStr( sarg, "-cmd=", argv[i] ) )
            cmd = sarg;
        else if( GetArgStr( sarg, "-args=", argv[i] ) )
            args = sarg;
        else if( GetArg( &port, "-port=%d", argv[i] ) )
            ;
        else {
            Log() <<
            QString("error: Did not understand option '%1'").arg( argv[i] );
            return false;
        }
    }

// Check args

    if( cmd.isEmpty() ) {
        PrintUsage();
        return false;
    }

    return true;
}

/* --------------------------------------------------------------- */
/* Private ------------------------------------------------------- */
/* --------------------------------------------------------------- */

QString CGBL::trim_adjust_slashes( const QString &dir )
{
    QString s = dir.trimmed();

    s.replace( "\\", "/" );
    return s.remove( QRegExp("/+$") );
}




#include "CGBL.h"
#include "Util.h"
#include "Tool.h"


int main( int argc, char *argv[] )
{
    setLogFileName( "HelloSGLX.log" );

    if( !GBL.SetCmdLine( argc, argv ) ) {
        Log();
        return 42;
    }

    Tool    tool;
    tool.entrypoint();

    Log();
    return 0;
}



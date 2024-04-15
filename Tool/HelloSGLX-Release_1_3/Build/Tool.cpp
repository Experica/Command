
#include "Tool.h"
#include "CGBL.h"
#include "Util.h"

#include "SglxCppClient.h"
using namespace std;


/* ---------------------------------------------------------------- */
/* Tool ----------------------------------------------------------- */
/* ---------------------------------------------------------------- */

Tool::Tool()
{
}


Tool::~Tool()
{
}


void Tool::entrypoint()
{
    if( GBL.cmd == "justConnect" )
        justConnect();
    else if( GBL.cmd == "enumDataDir" )
        enumDataDir();
    else if( GBL.cmd == "getDataDir" )
        getDataDir();
    else if( GBL.cmd == "getGeomMap" )
        getGeomMap();
    else if( GBL.cmd == "getImecChanGains" )
        getImecChanGains();
    else if( GBL.cmd == "getParams" )
        getParams();
    else if( GBL.cmd == "getParamsImecCommon" )
        getParamsImecCommon();
    else if( GBL.cmd == "getParamsImecProbe" )
        getParamsImecProbe();
    else if( GBL.cmd == "getParamsOneBox" )
        getParamsOneBox();
    else if( GBL.cmd == "getProbeList" )
        getProbeList();
    else if( GBL.cmd == "getRunName" )
        getRunName();
    else if( GBL.cmd == "getStreamAcqChans" )
        getStreamAcqChans();
    else if( GBL.cmd == "getStreamI16ToVolts" )
        getStreamI16ToVolts();
    else if( GBL.cmd == "getStreamMaxInt" )
        getStreamMaxInt();
    else if( GBL.cmd == "getStreamNP" )
        getStreamNP();
    else if( GBL.cmd == "getStreamSampleRate" )
        getStreamSampleRate();
    else if( GBL.cmd == "getStreamSaveChans" )
        getStreamSaveChans();
    else if( GBL.cmd == "getStreamSN" )
        getStreamSN();
    else if( GBL.cmd == "getStreamVoltageRange" )
        getStreamVoltageRange();
    else if( GBL.cmd == "getTime" )
        getTime();
    else if( GBL.cmd == "getVersion" )
        getVersion();
    else if( GBL.cmd == "isInitialized" )
        isInitialized();
    else if( GBL.cmd == "isRunning" )
        isRunning();
    else if( GBL.cmd == "isSaving" )
        isSaving();
    else if( GBL.cmd == "isUserOrder" )
        isUserOrder();
    else if( GBL.cmd == "opto_emit" )
        opto_emit();
    else if( GBL.cmd == "opto_getAttenuations" )
        opto_getAttenuations();
    else if( GBL.cmd == "setAnatomy_Pinpoint" )
        setAnatomy_Pinpoint();
    else if( GBL.cmd == "setAudioEnable" )
        setAudioEnable();
    else if( GBL.cmd == "setAudioParams" )
        setAudioParams();
    else if( GBL.cmd == "setDataDir" )
        setDataDir();
    else if( GBL.cmd == "setDigitalOut" )
        setDigitalOut();
    else if( GBL.cmd == "setMetadata" )
        setMetadata();
    else if( GBL.cmd == "setMultiDriveEnable" )
        setMultiDriveEnable();
    else if( GBL.cmd == "setNextFileName" )
        setNextFileName();
    else if( GBL.cmd == "setParams" )
        setParams();
    else if( GBL.cmd == "setParamsImecCommon" )
        setParamsImecCommon();
    else if( GBL.cmd == "setParamsImecProbe" )
        setParamsImecProbe();
    else if( GBL.cmd == "setParamsOneBox" )
        setParamsOneBox();
    else if( GBL.cmd == "setRecordingEnable" )
        setRecordingEnable();
    else if( GBL.cmd == "setRunName" )
        setRunName();
    else if( GBL.cmd == "setTriggerOffBeep" )
        setTriggerOffBeep();
    else if( GBL.cmd == "setTriggerOnBeep" )
        setTriggerOnBeep();
    else if( GBL.cmd == "startRun" )
        startRun();
    else if( GBL.cmd == "stopRun" )
        stopRun();
    else if( GBL.cmd == "triggerGT" )
        triggerGT();
    else
        Log() << QString("error: Unknown command [%1].").arg( GBL.cmd );
}


void Tool::justConnect()
{
    printf( "\nCalling connect...\n\n" );

    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {
        printf( "vers %s\n", sglx_getVersion( hSglx ) );
        sglx_close( hSglx );
    }
    else
        printf( "error [%s]\n", sglx_getError( hSglx ) );

    sglx_destroyHandle( hSglx );
}


void Tool::enumDataDir()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        cppClient_sglx_get_strs vs;

        if( sglx_enumDataDir( vs, hSglx, GBL.args.toInt() ) ) {
            for( int i = 0, n = vs.vstr.size(); i < n; ++i )
                Log() << vs.vstr[i].c_str();
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getDataDir()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        string str;

        if( sglx_getDataDir( str, hSglx, GBL.args.toInt() ) )
            Log() << str.c_str();
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getGeomMap()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        cppClient_sglx_get_strs vs;

        if( sglx_getGeomMap( vs, hSglx, GBL.args.toInt() ) ) {
            for( int i = 0, n = vs.vstr.size(); i < n; ++i )
                Log() << vs.vstr[i].c_str();
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getImecChanGains()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: getImecChanGains requires {ip, chan} parameters.";
            return;
        }

        double  ap, lf;

        if( sglx_getImecChanGains( ap, lf, hSglx, sl[0].toInt(), sl[1].toInt() ) ) {
            Log() << ap;
            Log() << lf;
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getParams()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        cppClient_sglx_get_strs vs;

        if( sglx_getParams( vs, hSglx ) ) {
            for( int i = 0, n = vs.vstr.size(); i < n; ++i )
                Log() << vs.vstr[i].c_str();
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getParamsImecCommon()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        cppClient_sglx_get_strs vs;

        if( sglx_getParamsImecCommon( vs, hSglx ) ) {
            for( int i = 0, n = vs.vstr.size(); i < n; ++i )
                Log() << vs.vstr[i].c_str();
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getParamsImecProbe()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        cppClient_sglx_get_strs vs;

        if( sglx_getParamsImecProbe( vs, hSglx, GBL.args.toInt() ) ) {
            for( int i = 0, n = vs.vstr.size(); i < n; ++i )
                Log() << vs.vstr[i].c_str();
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getParamsOneBox()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        cppClient_sglx_get_strs vs;

        if( sglx_getParamsOneBox( vs, hSglx, GBL.args.toInt() ) ) {
            for( int i = 0, n = vs.vstr.size(); i < n; ++i )
                Log() << vs.vstr[i].c_str();
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getProbeList()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        string str;

        if( sglx_getProbeList( str, hSglx ) )
            Log() << str.c_str();
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getRunName()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        string str;

        if( sglx_getRunName( str, hSglx ) )
            Log() << str.c_str();
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getStreamAcqChans()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: getStreamAcqChans requires {js, ip} parameters.";
            return;
        }

        cppClient_sglx_get_ints vi;

        if( sglx_getStreamAcqChans( vi, hSglx, sl[0].toInt(), sl[1].toInt() ) ) {
            for( int i = 0, n = vi.vint.size(); i < n; ++i )
                Log() << QString("%1").arg( vi.vint[i] );
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getStreamI16ToVolts()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 3 ) {
            Log() << "error: getStreamI16ToVolts requires {js, ip, chan} parameters.";
            return;
        }

        double  mult;

        if( sglx_getStreamI16ToVolts( mult, hSglx, sl[0].toInt(), sl[1].toInt(), sl[2].toInt() ) ) {
            Log() << mult;
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getStreamMaxInt()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: getStreamMaxInt requires {js, ip} parameters.";
            return;
        }

        int maxi;

        if( sglx_getStreamMaxInt( maxi, hSglx, sl[0].toInt(), sl[1].toInt() ) )
            Log() << maxi;
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getStreamNP()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        int np;

        if( sglx_getStreamNP( np, hSglx, GBL.args.toInt() ) )
            Log() << np;
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getStreamSampleRate()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: getStreamSampleRate requires {js, ip} parameters.";
            return;
        }

        Log() << sglx_getStreamSampleRate( hSglx, sl[0].toInt(), sl[1].toInt() );
        sglx_close( hSglx );
    }
    else
        printf( "error [%s]\n", sglx_getError( hSglx ) );

    sglx_destroyHandle( hSglx );
}


void Tool::getStreamSaveChans()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: getStreamSaveChans requires {js, ip} parameters.";
            return;
        }

        cppClient_sglx_get_ints vi;

        if( sglx_getStreamSaveChans( vi, hSglx, sl[0].toInt(), sl[1].toInt() ) ) {
            for( int i = 0, n = vi.vint.size(); i < n; ++i )
                Log() << QString("%1").arg( vi.vint[i] );
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getStreamSN()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: getStreamSN requires {js, ip} parameters.";
            return;
        }

        string  sn;
        int     slot;

        if( sglx_getStreamSN( slot, sn, hSglx, sl[0].toInt(), sl[1].toInt() ) ) {
            Log() << slot;
            Log() << sn.c_str();
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getStreamVoltageRange()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: getStreamVoltageRange requires {js, ip} parameters.";
            return;
        }

        double  lo, hi;

        if( sglx_getStreamVoltageRange( lo, hi, hSglx, sl[0].toInt(), sl[1].toInt() ) ) {
            Log() << lo;
            Log() << hi;
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::getTime()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {
        Log() << sglx_getTime( hSglx );
        sglx_close( hSglx );
    }
    else
        printf( "error [%s]\n", sglx_getError( hSglx ) );

    sglx_destroyHandle( hSglx );
}


void Tool::getVersion()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {
        Log() << sglx_getVersion( hSglx );
        sglx_close( hSglx );
    }
    else
        printf( "error [%s]\n", sglx_getError( hSglx ) );

    sglx_destroyHandle( hSglx );
}


void Tool::isInitialized()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        bool    ready;

        if( sglx_isInitialized( ready, hSglx ) )
            Log() << ready;
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::isRunning()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        bool    running;

        if( sglx_isRunning( running, hSglx ) )
            Log() << running;
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::isSaving()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        bool    saving;

        if( sglx_isSaving( saving, hSglx ) )
            Log() << saving;
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::isUserOrder()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: isUserOrder requires {js, ip} parameters.";
            return;
        }

        bool    usr;

        if( sglx_isUserOrder( usr, hSglx, sl[0].toInt(), sl[1].toInt() ) ) {
            Log() << usr;
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::opto_emit()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 3 ) {
            Log() << "error: opto_emit requires {ip, color, site} parameters.";
            return;
        }

        if( !sglx_opto_emit( hSglx, sl[0].toInt(), sl[1].toInt(), sl[2].toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::opto_getAttenuations()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( sl.size() != 2 ) {
            Log() << "error: opto_getAttenuations requires {ip, color} parameters.";
            return;
        }

        cppClient_sglx_get_dbls vd;

        if( sglx_opto_getAttenuations( vd, hSglx, sl[0].toInt(), sl[1].toInt() ) ) {
            for( int i = 0, n = vd.vdbl.size(); i < n; ++i )
                Log() << vd.vdbl[i];
        }
        else
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setAnatomy_Pinpoint()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        if( !sglx_setAnatomy_Pinpoint( hSglx, GBL.args.toStdString() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setAudioEnable()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        if( !sglx_setAudioEnable( hSglx, GBL.args.toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setAudioParams()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        cppClient_sglx_set_keyvals  kv;

        for( int i = 1, n = sl.size(); i < n; ++i ) {
            const string    &s = sl[i].toStdString();
            int             eq = s.find( '=' );
            kv.mstrstr[s.substr( 0, eq )] = s.substr( eq + 1 );
        }

        if( !sglx_setAudioParams( hSglx, sl[0].toStdString(), kv ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setDataDir()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( !sglx_setDataDir( hSglx, sl[0].toInt(), sl[1].toStdString() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setDigitalOut()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( !sglx_setDigitalOut( hSglx, sl[0].toInt(), sl[1].toStdString() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setMetadata()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        cppClient_sglx_set_keyvals  kv;

        for( int i = 0, n = sl.size(); i < n; ++i ) {
            const string    &s = sl[i].toStdString();
            int             eq = s.find( '=' );
            kv.mstrstr[s.substr( 0, eq )] = s.substr( eq + 1 );
        }

        if( !sglx_setMetadata( hSglx, kv ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setMultiDriveEnable()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        if( !sglx_setMultiDriveEnable( hSglx, GBL.args.toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setNextFileName()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        if( !sglx_setNextFileName( hSglx, GBL.args.toStdString() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setParams()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        cppClient_sglx_set_keyvals  kv;

        for( int i = 0, n = sl.size(); i < n; ++i ) {
            const string    &s = sl[i].toStdString();
            int             eq = s.find( '=' );
            kv.mstrstr[s.substr( 0, eq )] = s.substr( eq + 1 );
        }

        if( !sglx_setParams( hSglx, kv ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setParamsImecCommon()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        cppClient_sglx_set_keyvals  kv;

        for( int i = 0, n = sl.size(); i < n; ++i ) {
            const string    &s = sl[i].toStdString();
            int             eq = s.find( '=' );
            kv.mstrstr[s.substr( 0, eq )] = s.substr( eq + 1 );
        }

        if( !sglx_setParamsImecCommon( hSglx, kv ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setParamsImecProbe()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        cppClient_sglx_set_keyvals  kv;

        for( int i = 1, n = sl.size(); i < n; ++i ) {
            const string    &s = sl[i].toStdString();
            int             eq = s.find( '=' );
            kv.mstrstr[s.substr( 0, eq )] = s.substr( eq + 1 );
        }

        if( !sglx_setParamsImecProbe( hSglx, kv, sl[0].toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setParamsOneBox()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        cppClient_sglx_set_keyvals  kv;

        for( int i = 1, n = sl.size(); i < n; ++i ) {
            const string    &s = sl[i].toStdString();
            int             eq = s.find( '=' );
            kv.mstrstr[s.substr( 0, eq )] = s.substr( eq + 1 );
        }

        if( !sglx_setParamsOneBox( hSglx, kv, sl[0].toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setRecordingEnable()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        if( !sglx_setRecordingEnable( hSglx, GBL.args.toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setRunName()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        if( !sglx_setRunName( hSglx, GBL.args.toStdString() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setTriggerOffBeep()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( !sglx_setTriggerOffBeep( hSglx, sl[0].toInt(), sl[1].toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::setTriggerOnBeep()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( !sglx_setTriggerOnBeep( hSglx, sl[0].toInt(), sl[1].toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::startRun()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        string  run;

        if( !GBL.args.isEmpty() )
            run = GBL.args.toStdString();

        if( !sglx_startRun( hSglx, run ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::stopRun()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        if( !sglx_stopRun( hSglx ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}


void Tool::triggerGT()
{
    void    *hSglx = sglx_createHandle_std();

    if( sglx_connect( hSglx, GBL.host ) ) {

        const QStringList   sl = GBL.args.split(
                                    QRegExp("\\s+"),
                                    QString::SkipEmptyParts );

        if( !sglx_triggerGT( hSglx, sl[0].toInt(), sl[1].toInt() ) )
            goto error;

        sglx_close( hSglx );
    }
    else {
error:
        printf( "error [%s]\n", sglx_getError( hSglx ) );
    }

    sglx_destroyHandle( hSglx );
}



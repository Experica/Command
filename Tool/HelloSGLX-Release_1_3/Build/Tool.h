#ifndef TOOL_H
#define TOOL_H

/* ---------------------------------------------------------------- */
/* Types ---------------------------------------------------------- */
/* ---------------------------------------------------------------- */

class Tool
{
private:

public:
    Tool();
    virtual ~Tool();

    void entrypoint();

private:
    void justConnect();
    void enumDataDir();
    void getDataDir();
    void getGeomMap();
    void getImecChanGains();
    void getParams();
    void getParamsImecCommon();
    void getParamsImecProbe();
    void getParamsOneBox();
    void getProbeList();
    void getRunName();
    void getStreamAcqChans();
    void getStreamI16ToVolts();
    void getStreamMaxInt();
    void getStreamNP();
    void getStreamSampleRate();
    void getStreamSaveChans();
    void getStreamSN();
    void getStreamVoltageRange();
    void getTime();
    void getVersion();
    void isInitialized();
    void isRunning();
    void isSaving();
    void isUserOrder();
    void opto_emit();
    void opto_getAttenuations();
    void setAnatomy_Pinpoint();
    void setAudioEnable();
    void setAudioParams();
    void setDataDir();
    void setDigitalOut();
    void setMetadata();
    void setMultiDriveEnable();
    void setNextFileName();
    void setParams();
    void setParamsImecCommon();
    void setParamsImecProbe();
    void setParamsOneBox();
    void setRecordingEnable();
    void setRunName();
    void setTriggerOffBeep();
    void setTriggerOnBeep();
    void startRun();
    void stopRun();
    void triggerGT();
};

#endif  // TOOL_H



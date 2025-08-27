[["python:pkgdir:QuanLan"]]

module QuanLan{

    sequence<int> IntSeq;

    struct AcquisitionParams {
        IntSeq channels; // one based
        int sampleRate = 1000; // Hz
        int range = 188; // [188,375,563,750,1125,2250,4500]
    };

    struct DCStimulationParams {
        int channel = 0; // zero based
        float current = 1; // mA
        float duration = 1; // second
        float rampUp = 0; // second
        float rampDown = 0; // second
        bool update = true;
    };

    struct ACStimulationParams {
        int channel = 0;
        float current = 1;
        float duration = 1;
        float rampUp = 0;
        float rampDown = 0;
        float frequency = 50; // Hz
        int phase = 0; // [0, 360]
        bool update = true;
    };

    struct SquareWaveStimulationParams {
        int channel = 0;
        float current = 1;
        float duration = 1;
        float rampUp = 0;
        float rampDown = 0;
        float frequency = 50;
        float duty = 0.5; // [0, 1]
        bool update = true;
    };

    struct PulseStimulationParams {
        int channel = 0;
        float current = 1;
        float duration = 1;
        float rampUp = 0;
        float rampDown = 0;
        float frequency = 50;
        int pulseWidth = 100; // uSec
        float pulseWidthRatio = 1; // [0, 1]
        int pulseInterval = 0; // uSec
        float delayTime = 0; // second, not used by hardware yet
        bool update = true;
    };
     
    exception DeviceConnectionException {
        string message;
    };

    exception OperationException {
        string message;
    };

    interface QuanLanInterface {
        bool connectDevice(string deviceId, int timeout) throws DeviceConnectionException;
        bool isConnected() throws OperationException;
        bool setAcquisitionParameters(AcquisitionParams params) throws OperationException;
        bool startAcquisition() throws OperationException;
        bool stopAcquisition() throws OperationException;
        bool startImpedance() throws OperationException;
        bool stopImpedance() throws OperationException;

        // 四种刺激类型分别定义
        bool setDCStimulation(DCStimulationParams params) throws OperationException;
        bool setACStimulation(ACStimulationParams params) throws OperationException;
        bool setSquareWaveStimulation(SquareWaveStimulationParams params) throws OperationException;
        bool setPulseStimulation(PulseStimulationParams params) throws OperationException;

        // 刺激参数管理
        bool clearStimulationParameters() throws OperationException;
        bool startStimulation() throws OperationException;
        bool stopStimulation() throws OperationException;
    }
}
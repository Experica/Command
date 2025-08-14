import signal, sys, os, Ice, QuanLan
from loguru import logger
from qlsdk.rsc import *
    
class QuanLanServant(QuanLan.QuanLanInterface):
    def __init__(self, log_dir: str = None):
        """
        Init Servant
        
        Args:
            log_dir: default = "~/Desktop/logs"
        """
        # 设置日志
        if log_dir is None:
            log_dir = os.path.expanduser("~/Desktop/logs")
        
        if not os.path.exists(log_dir):
            os.makedirs(log_dir)
        
        log_file = os.path.join(log_dir, "quanlan_api_{time}.log")
        logger.add(log_file, rotation="50MB", level="INFO")

        # 初始化设备相关变量
        self.device_container = None
        self.device = None
        self.device_id = None
        self.is_connected = False
        
        # 数据队列
        self.signal_queue = None
        self.impedance_queue = None
        
        # 消费者进程
        self.signal_consumer = None
        self.impedance_consumer = None
        
        self.stimulation_paradigm = None
        logger.info("QuanLan ICE Servant 初始化完成")

    def connectDevice(self, deviceId, timeout, current=None):
        try:
            logger.info(f"连接设备 {deviceId}")
            if self.is_connected:
                if self.device_id==deviceId:
                    logger.info(f"Device: {deviceId} Already Connected")
                    return True
                else:
                    logger.info(f"Current Connected Device: {self.device_id} is different than requested device: {deviceId}.")
                    return False
            # 创建设备容器
            self.device_container = DeviceContainer()
            self.device = self.device_container.connect(deviceId, timeout=timeout)
            
            if self.device is None:
                logger.error(f"未找到设备: {deviceId}")
                return False
            
            self.device_id = deviceId
            self.is_connected = True
            
            logger.info(f"设备 {deviceId} 连接成功")
            return True
        
        except Exception as e:
            logger.error(f"连接设备失败: {str(e)}")
            raise QuanLan.DeviceConnectionException(str(e))

    def isConnected(self, current=None):
        return self.is_connected
    
    def setAcquisitionParameters(self, params, current=None):
        """
        设置数据采集参数
        
        Args:
            channels: 通道列表，例如 [1,2,3,4,5,6,7,8] 或 [18,19,20,21,22,23,24,25]
            sample_rate: 采样率（Hz），默认1000
            duration: 采集时长（秒），默认188
            
        Returns:
            bool: 设置是否成功
        """
        try:
            if not self.is_connected:
                logger.warning("设备未连接，无法设置采集参数")
                return False
            
            self.device.set_acq_param(params.channels, params.sample_rate, params.range)
            logger.info(f"采集参数设置成功: 通道{params.channels}, 采样率{params.sample_rate}Hz, 量程{params.range}s")
            return True
            
        except Exception as e:
            logger.error(f"设置采集参数失败: {str(e)}")
            return False
        
    def startAcquisition(self, current=None):
        """
        开始数据采集
        
        Returns:
            bool: 启动是否成功
        """
        try:
            if not self.is_connected:
                logger.warning("设备未连接，无法开始采集")
                return False
            
            self.device.start_acquisition()
            logger.info("数据采集已开始")
            return True
            
        except Exception as e:
            logger.error(f"开始数据采集失败: {str(e)}")
            return False
        
    def stopAcquisition(self, current=None):
        """
        停止数据采集
        
        Returns:
            bool: 停止是否成功
        """
        try:
            if not self.is_connected:
                return True
            
            self.device.stop_acquisition()
            logger.info("数据采集已停止")
            return True
            
        except Exception as e:
            logger.error(f"停止数据采集失败: {str(e)}")
            return False
        
    def startImpedance(self, current=None):
        """
        开始阻抗测量
        
        Returns:
            bool: 启动是否成功
        """
        try:
            if not self.is_connected:
                logger.warning("设备未连接，无法开始阻抗测量")
                return False
            
            self.device.start_impedance()
            logger.info("阻抗测量已开始")
            return True
            
        except Exception as e:
            logger.error(f"开始阻抗测量失败: {str(e)}")
            return False
        
    def stopImpedance(self, current=None):
        """
        停止阻抗测量
        
        Returns:
            bool: 停止是否成功
        """
        try:
            if not self.is_connected:
                return True
            
            self.device.stop_impedance()
            logger.info("阻抗测量已停止")
            return True
            
        except Exception as e:
            logger.error(f"停止阻抗测量失败: {str(e)}")
            return False
        
    def setDCStimulation(self, params, current=None):
        try:
            logger.info("Set DCStim")
            if self.stimulation_paradigm is None:
                self.stimulation_paradigm = StimulationParadigm()
            s = DCStimulation(params.channel,params.current,params.duration,params.rampUp,params.rampDown)
            self.stimulation_paradigm.add_channel(s,params.update)

            return True
        except Exception as e:
            logger.error(f"Set DCStim Failed: {str(e)}")
            raise QuanLan.OperationException(str(e))
        
    def setACStimulation(self, params, current=None):
        try:
            logger.info("Set ACStim")
            if self.stimulation_paradigm is None:
                self.stimulation_paradigm = StimulationParadigm()
            s = ACStimulation(params.channel,params.current,params.duration,params.rampUp,params.rampDown,params.frequency,params.phase)
            self.stimulation_paradigm.add_channel(s,params.update)

            return True
        except Exception as e:
            logger.error(f"Set ACStim Failed: {str(e)}")
            raise QuanLan.OperationException(str(e))
        
    def setSquareWaveStimulation(self, params, current=None):
        try:
            logger.info("Set SWStim")
            if self.stimulation_paradigm is None:
                self.stimulation_paradigm = StimulationParadigm()
            s = SquareWaveStimulation(params.channel,params.current,params.duration,params.rampUp,params.rampDown,params.frequency,params.duty)
            self.stimulation_paradigm.add_channel(s,params.update)

            return True
        except Exception as e:
            logger.error(f"Set SWStim Failed: {str(e)}")
            raise QuanLan.OperationException(str(e))
        
    def setPulseStimulation(self, params, current=None):
        try:
            logger.info("Set PStim")
            if self.stimulation_paradigm is None:
                self.stimulation_paradigm = StimulationParadigm()
            s = PulseStimulation(params.channel,params.current,params.duration,params.frequency,params.pulseWidth,params.pulseWidthRatio,params.pulseInterval,params.rampUp,params.rampDown,params.delayTime)
            self.stimulation_paradigm.add_channel(s,params.update)

            return True
        except Exception as e:
            logger.error(f"Set PStim Failed: {str(e)}")
            raise QuanLan.OperationException(str(e))
        
    def clearStimulationParameters(self, current=None):
        try:
            logger.info("ICE调用: 清除刺激参数")
            if self.stimulation_paradigm is not None:
                self.stimulation_paradigm.clear()
                self.stimulation_paradigm = None
            return True
        except Exception as e:
            logger.error(f"清除刺激参数失败: {str(e)}")
            raise QuanLan.OperationException(str(e))
        
    def startStimulation(self, current=None) -> bool:
        try:
            if not self.is_connected:
                logger.warning("设备未连接，无法开始刺激")
                return False
            
            self.device.set_stim_param(self.stimulation_paradigm)
            self.device.start_stimulation()
            logger.info("刺激已开始")
            return True
            
        except Exception as e:
            logger.error(f"开始刺激失败: {str(e)}")
            return False
        
    def stopStimulation(self, current=None) -> bool:
        try:
            if not self.is_connected:
                return True
            
            self.device.stop_stimulation()
            logger.info("刺激已停止")
            return True
            
        except Exception as e:
            logger.error(f"停止刺激失败: {str(e)}")
            return False


def run(communicator):
    #
    # Create an object adapter
    #
    adapter = communicator.createObjectAdapterWithEndpoints(
        "QuanLan", "default -h localhost -p 9999")
 
    #
    # Create the QuanLan Servant
    #
    servant = QuanLanServant()
    adapter.add(servant, Ice.stringToIdentity("QuanLan"))
 
    #
    # All objects are created, allow client requests now
    #
    adapter.activate()
 
    #
    # Wait until we are done
    #
    communicator.waitForShutdown()

#
# Ice.initialize returns an initialized Ice communicator,
# the communicator is destroyed once it goes out of scope.
#
with Ice.initialize(sys.argv) as communicator:
    #
    # Install a signal handler to shutdown the communicator on Ctrl-C
    #
    signal.signal(signal.SIGINT, lambda signum, frame: communicator.shutdown())
 
    run(communicator)
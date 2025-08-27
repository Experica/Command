import signal, sys, os
from loguru import logger
from qlsdk.rsc import *
    
dc = DeviceContainer()
deviceID = "390024340031"
device = dc.connect(deviceID, timeout=10)

if device is None:
    logger.error(f"未找到设备: {deviceID}")

sp = StimulationParadigm()

ch=1
current=1
duration=10
rampUp=0
rampDown=0
# s = DCStimulation(ch,current,duration,rampUp,rampDown)
# sp.add_channel(s,True)


frequency=100
# phase=0
# s = ACStimulation(ch,current,duration,rampUp,rampDown,frequency,phase)
# sp.add_channel(s,True)

# duty=0.5
# s = SquareWaveStimulation(ch,current,duration,rampUp,rampDown,frequency,duty)
# sp.add_channel(s,True)

pulseWidth=150
pulseWidthRatio=1
pulseInterval=0
delayTime=0
s = PulseStimulation(ch,current,duration,frequency,pulseWidth,pulseWidthRatio,pulseInterval,rampUp,rampDown,delayTime)
sp.add_channel(s,True)


device.set_stim_param(sp)

device.start_stimulation()
# device.stop_stimulation()
        




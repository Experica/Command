This folder contains **Plugins** that enable certain functions of **Experica**, install them as needed.

# Parallel Port

InpOutBinaries_1501: Windows kernel mode driver for parallel port.


SpikeGLXDotnet40Installer_web: Plugin for coordination with SpikeGLX data acqusition system.
			This will install the library and MATLAB runtime which is required to bridge Experica and SpikeGLX, then the "HHMI.dll" and "MWArray.dll" should be copied to plugins folder.


XippmexDotNet40Installer_web: Plugin for control Ripple data acqusition system.

# Measurement Computing

Measurement Computing devices are used through the Universal Library, please download and install the software, then copy appropriate files (MccDaq.dll) to **Assets/Plugins** folder.

# Python.Net

Python.Net do not work under .Net Framework/Mono, but work with Windows embeddable package. To add packages to embed python: 
 1. add `pip`: use `get-pip.py` script, and add `pip` to `PATH`
 2. enable `sys.path`: uncomment `#import site` in `pythonX._pth`

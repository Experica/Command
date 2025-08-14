This folder contains the files for QuanLan SDK Server that enable RPC call from another process such as Experica.Command.

# ICE interface defination

**QuanLan.ice** can be used by ICE Slice Compilers (slice2cs, slice2py, etc.) to generate language specific interfaces

# QuanLan SDK Server

 1. create `Conda` environment with `python=3.12` in `env` folder
 2. activate local `Conda` `env`
 3. `pip` install requirements.txt
 4. `slice2py` generate module in ./QuanLan
 5. run QuanLanICEServer.py

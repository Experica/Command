This folder contains the files for QuanLan SDK Server that enable RPC call from another process such as Experica.Command.

# ICE interface defination

**QuanLan.ice** can be used by ICE Slice Compilers (slice2cs, slice2py, etc.) to generate language specific interfaces

# QuanLan SDK Server

 1. create `Conda` environment with `python=3.12` in `env` subfolder:

    **cd "this folder/QuanLanServer"**
    
    **conda create --prefix ./env python=3.12**
 2. activate local `Conda` `env`: 

    **conda activate ./env**
 3. `pip` install requirements.txt: 

    **pip install -r requirements.txt**
 4. `slice2py` generate module in ./QuanLan:
    
    **slice2py ../QuanLan.ice**
 5. run QuanLanICEServer.py: 

    **python ./QuanLanICEServer.py**

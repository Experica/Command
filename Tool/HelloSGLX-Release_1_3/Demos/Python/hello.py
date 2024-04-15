import subprocess

HelloSGLX = r"C:/Users/labadmin/Desktop/HelloSGLX-win/HelloSGLX.exe"
host = "-host=127.0.0.1"
cmd = "-cmd=getImecChanGains"
args = "-args=0\n0"

process = subprocess.Popen([HelloSGLX, host, cmd, args], stdout=subprocess.PIPE)
stdout = process.communicate()[0]
outstr = stdout.decode().rstrip()
print(outstr)

# To go further and parse string data into numbers:
# parts = outstr.split()
# vals = [eval(i) for i in parts]
# print(vals)



/*
ControlManager.cs is part of the Experica.
Copyright (c) 2016 Li Alex Zhang and Contributors

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace Experica.Command
{
    [NetworkSettings(channel = 0, sendInterval = 0)]
    public class ControlManager : NetworkBehaviour
    {
        public UIController uicontroller;

        [Command]
        public void CmdControlSignal(byte[] signal)
        {
            using (var stream = new MemoryStream(signal))
            {
                var ctl = MsgPack.CtlSerializer.Unpack(stream);
                if (ctl != null) uicontroller.exmanager.el.OnReceiveControlSignal(ctl);
            }
        }

#if COMMAND
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            var b = uicontroller.netmanager.IsConnectionPeerType(conn, PeerType.Analysis);
            return b;
        }

        public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            var acs = uicontroller.netmanager.GetPeerTypeConnection(PeerType.Analysis);
            if (acs.Count > 0)
            {
                foreach (var c in acs)
                {
                    observers.Add(c);
                }
                return true;
            }
            return false;
        }
#endif

    }
}

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Collections.Generic;
using System.Linq;

namespace Experica.Command
{
    public class SyncFrameManager : MonoBehaviour
    {
        bool endingsyncframe;
        public NetManager netmanager;

        public void SyncFrame()
        {
            // send BeginSyncFrame Msg before lateupdate where syncvars being batched by UNET
            netmanager.BeginSyncFrame();
            // mark task in this lateupdate(set this script execution order later than UNET) to end SyncFrame Msg structure
            endingsyncframe = true;
        }

        void LateUpdate()
        {
            if (endingsyncframe)
            {
                netmanager.EndSyncFrame();
                endingsyncframe = false;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace VLab
{
    public class VLMsgType
    {
        public const short PeerInfo = MsgType.Highest + 1;

        public const short Highest = PeerInfo;

        internal static string[] msgLabels = new string[]
        {
            "PeerInfo"
        };

        public static string MsgTypeToString(short value)
        {
            if (value < PeerInfo || value > Highest)
            {
                return string.Empty;
            }
            string text = msgLabels[value - MsgType.Highest - 1];
            if (string.IsNullOrEmpty(text))
            {
                text = "[" + value + "]";
            }
            return text;
        }
    }

    public enum VLPeerType
    {
        VLab,
        VLabEnvironment,
        VLabAnalysis
    }
}

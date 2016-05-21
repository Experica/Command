// --------------------------------------------------------------
// VLNetwork.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace VLab
{
    public class VLMsgType
    {
        public const short PeerType = MsgType.Highest + 1;

        public const short AspectRatio = PeerType + 1;

        public const short Highest = AspectRatio;

        internal static string[] msgLabels = new string[]
        {
            "PeerType",
            "AspectRatio"
        };

        public static string MsgTypeToString(short value)
        {
            if (value < PeerType || value > Highest)
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

    public class FloatMessage: MessageBase
    {
        public float value;

        public FloatMessage()
        {
        }

        public FloatMessage(float value)
        {
            this.value = value;
        }
    }
}

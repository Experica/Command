// /*
// Net.cs is part of the Experica.
// Copyright (c) 2016 Li Alex Zhang and Contributors

// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// */
// using UnityEngine;
// using UnityEngine.Networking;
// using System.Collections;

// namespace Experica
// {
//     public class MsgType
//     {
//         public const short PeerType = UnityEngine.Networking.MsgType.Highest + 1;

//         public const short AspectRatio = PeerType + 1;

//         public const short BeginSyncFrame = AspectRatio + 1;

//         public const short EndSyncFrame = BeginSyncFrame + 1;

//         public const short CLUT = EndSyncFrame + 1;

//         public const short Highest = CLUT;

//         internal static string[] msgLabels = new string[]
//         {
//             "PeerType",
//             "AspectRatio",
//             "BeginSyncFrame",
//             "EndSyncFrame",
//             "CLUT"
//         };

//         public static string MsgTypeToString(short value)
//         {
//             if (value < PeerType || value > Highest)
//             {
//                 return string.Empty;
//             }
//             string text = msgLabels[value - UnityEngine.Networking.MsgType.Highest - 1];
//             if (string.IsNullOrEmpty(text))
//             {
//                 text = "[" + value + "]";
//             }
//             return text;
//         }
//     }

//     public enum PeerType
//     {
//         Command,
//         Environment,
//         Analysis
//     }

//     public class CLUTMessage : MessageBase
//     {
//         public byte[] clut;
//         public int size;
//     }

//     public class FloatMessage : MessageBase
//     {
//         public float value;
//     }
// }
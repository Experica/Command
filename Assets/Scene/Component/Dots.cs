/*
Dots.cs is part of the Experica.
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
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.VFX;

namespace Experica
{
    public class Dots : EnvNetVisual
    {
        [SyncVar(hook = "onrotation")]
        public Vector3 Rotation = Vector3.zero;
        [SyncVar(hook = "onrotationoffset")]
        public Vector3 RotationOffset = Vector3.zero;
        [SyncVar(hook = "ondir")]
        public float Dir = 0;
        [SyncVar(hook = "ondiroffset")]
        public float DirOffset = 0;
        [SyncVar(hook = "onspeed")]
        public float Speed = 1;
        [SyncVar(hook = "ondiameter")]
        public float Diameter = 10;
        [SyncVar(hook = "onsize")]
        public Vector3 Size = new Vector3(10, 10, 1);
        [SyncVar(hook = "onndots")]
        public uint NDots = 30;
        [SyncVar(hook = "ondotcolor")]
        public Color DotColor = Color.white;
        [SyncVar(hook = "ondotsize")]
        public Vector2 DotSize = new Vector2(1, 1);
        [SyncVar(hook = "oncoherence")]
        public float Coherence = 0;

        void onrotation(Vector3 r)
        {
            transform.localEulerAngles = r + RotationOffset;
            Rotation = r;
        }

        void onrotationoffset(Vector3 roffset)
        {
            transform.localEulerAngles = Rotation + roffset;
            RotationOffset = roffset;
        }

        void ondir(float d)
        {
            visualeffect.SetFloat("Dir", Mathf.Deg2Rad * (d + DirOffset));
            visualeffect.Reinit();
            Dir = d;
        }

        void ondiroffset(float doffset)
        {
            visualeffect.SetFloat("Dir", Mathf.Deg2Rad * (doffset + Dir));
            visualeffect.Reinit();
            DirOffset = doffset;
        }

        void onspeed(float s)
        {
            visualeffect.SetFloat("Speed", s);
            visualeffect.Reinit();
            Speed = s;
        }

        void onsize(Vector3 s)
        {
            visualeffect.SetVector3("Size", s);
            Size = s;
        }

        void ondiameter(float d)
        {
            onsize(new Vector3(d, d, Size.z));
            Diameter = d;
        }

        void onndots(uint n)
        {
            visualeffect.SetUInt("NDots", n);
            visualeffect.Reinit();
            NDots = n;
        }

        void ondotcolor(Color c)
        {
            visualeffect.SetVector4("DotColor", c);
            DotColor = c;
        }

        void ondotsize(Vector2 s)
        {
            visualeffect.SetVector2("DotSize", s);
            DotSize = s;
        }

        void oncoherence(float c)
        {
            visualeffect.SetFloat("Coherence", c);
            visualeffect.Reinit();
            Coherence = c;
        }

        protected override void OnVisible(bool v)
        {
            // reset dots when reappear
            if (v)
            {
                visualeffect.Reinit();
            }
            base.OnVisible(v);
        }
    }
}
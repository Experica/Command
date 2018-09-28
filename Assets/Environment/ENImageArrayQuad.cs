/*
ENImageArrayQuad.cs is part of the VLAB project.
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
using System.Collections.Generic;

namespace IExSys
{
    public class ENImageArrayQuad : ENQuad
    {
        [SyncVar(hook = "onstartindex")]
        public int StartIndex = 1;
        [SyncVar(hook = "onnumofimage")]
        public int NumOfImage = 10;
        [SyncVar(hook = "onimageset")]
        public string ImageSet = "ExampleImageSet";
        [SyncVar(hook = "onimage")]
        public int Image = 1;


        public override void OnOri(float o)
        {
            renderer.material.SetFloat("ori", o);
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(OriOffset + o);
            }
            Ori = o;
        }

        public override void OnOriOffset(float ooffset)
        {
            renderer.material.SetFloat("orioffset", ooffset);
            if (OriPositionOffset)
            {
                transform.localPosition = Position + PositionOffset.RotateZCCW(Ori + ooffset);
            }
            OriOffset = ooffset;
        }

        void onstartindex(int i)
        {
            OnStartIndex(i);
        }
        public virtual void OnStartIndex(int i)
        {
            StartIndex = i;
            OnImageSet(ImageSet);
        }

        void onnumofimage(int n)
        {
            OnNumOfImage(n);
        }
        public virtual void OnNumOfImage(int n)
        {
            NumOfImage = n;
            OnImageSet(ImageSet);
        }

        void onimage(int i)
        {
            OnImage(i);
        }
        public virtual void OnImage(int i)
        {
            renderer.material.SetInt("imgidx", i);
            Image = i;
        }

        void onimageset(string iset)
        {
            OnImageSet(iset);
        }
        public virtual void OnImageSet(string iset)
        {
            var imgs = iset.LoadImageSet(StartIndex, NumOfImage, false);
            if (imgs != null)
            {
                renderer.material.SetTexture("imgs", imgs);
                ImageSet = iset;
                OnImage(StartIndex);
            }
        }
    }
}
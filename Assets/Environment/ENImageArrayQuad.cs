using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace VLab
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
            var imgs = iset.LoadImageSet(StartIndex, NumOfImage);
            if (imgs != null)
            {
                renderer.material.SetTexture("imgs", imgs);
                ImageSet = iset;
                OnImage(StartIndex);
            }
        }
    }
}
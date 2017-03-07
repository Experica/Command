/*
ENImageQuad.cs is part of the VLAB project.
Copyright (c) 2017 Li Alex Zhang and Contributors

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

namespace VLab
{
    public class ENImageQuad : ENQuad
    {
        [SyncVar(hook ="onimage")]
        public string Image="0";
        [SyncVar(hook ="onimagedir")]
        public string ImageDir = "SampleImage";
        [SyncVar]
        public bool IsCacheImage = false;
        private Dictionary<string, Texture2D> imagecache=new Dictionary<string, Texture2D>();

        [ClientRpc]
        void RpcPreLoadImage(string[] iidx)
        {
            imagecache.Clear();
            Resources.UnloadUnusedAssets();
            foreach (var i in iidx)
            {
                imagecache[i]= Resources.Load<Texture2D>(ImageDir + "/" + i);
            }
        }

        void onimage(string i)
        {
            OnImage(i);
        }
        public virtual void OnImage(string i)
        {
            Image = i;
            if (imagecache.ContainsKey(i))
            {
                renderer.material.SetTexture("img", imagecache[i]);
            }
            else
            {
                Resources.UnloadUnusedAssets();
                var img = Resources.Load<Texture2D>(ImageDir + "/" + i);
                renderer.material.SetTexture("img", img);
                if (IsCacheImage)
                {
                    imagecache[i] = img;
                }
            }
        }

        void onimagedir(string idir)
        {
            OnImageDir(idir);
        }
        public virtual void OnImageDir(string idir)
        {
            ImageDir = idir;
            OnImage(Image);
        }
    }
}
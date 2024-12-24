/*
Cross.cs is part of the Experica.
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
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Experica.NetEnv
{
    public class Cross : NetEnvVisual
    {
        public NetworkVariable<Color> Color = new(UnityEngine.Color.green);
        public NetworkVariable<float> Width = new(0.1f);
        public NetworkVariable<Vector3> Size = new(Vector3.one);
        protected LineRenderer xlinerenderer, ylinerenderer;


        protected override void OnAwake()
        {
            xlinerenderer = Base.AddXLine(name: "XAxis", parent: transform);
            ylinerenderer = Base.AddYLine(name: "YAxis", parent: transform);
            OnColor(default, Color.Value);
            OnWidth(default, Width.Value);
            OnSize(default, Size.Value);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Color.OnValueChanged += OnColor;
            Width.OnValueChanged += OnWidth;
            Size.OnValueChanged += OnSize;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Color.OnValueChanged -= OnColor;
            Width.OnValueChanged -= OnWidth;
            Size.OnValueChanged -= OnSize;
        }

        protected override void OnVisible(bool p, bool c)
        {
            xlinerenderer.enabled = c;
            ylinerenderer.enabled = c;
        }

        protected virtual void OnColor(Color p, Color c)
        {
            xlinerenderer.startColor = c;
            xlinerenderer.endColor = c;
            ylinerenderer.startColor = c;
            ylinerenderer.endColor = c;
        }

        protected virtual void OnWidth(float p, float c)
        {
            xlinerenderer.widthMultiplier = c;
            ylinerenderer.widthMultiplier = c;
        }

        protected virtual void OnSize(Vector3 p, Vector3 c)
        {
            xlinerenderer.transform.localScale = new(c.x, 1, 1);
            ylinerenderer.transform.localScale = new(1, c.y, 1);
        }

    }
}

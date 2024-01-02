/*
ImageQuad.cs is part of the Experica.
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
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.VFX;

namespace Experica.NetEnv
{
    public class FixDot : NetworkBehaviour
    {
        public NetworkVariable<Vector3> FixDotPosition = new(Vector3.zero);
        public NetworkVariable<float> FixDotDiameter = new(1f);
        public NetworkVariable<bool> FixDotVisible = new(true);
        protected new Renderer renderer;

        void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake()
        {
            renderer = GetComponent<Renderer>();
        }

        void Start()
        {
            OnStart();
        }

        protected virtual void OnStart()
        {
        }


        public override void OnNetworkSpawn()
        {
            FixDotVisible.OnValueChanged += OnFixDotVisible;
            FixDotPosition.OnValueChanged += OnFixDotPosition;
            FixDotDiameter.OnValueChanged += OnFixDotDiameter;
        }

        public override void OnNetworkDespawn()
        {
            FixDotVisible.OnValueChanged -= OnFixDotVisible;
            FixDotPosition.OnValueChanged -= OnFixDotPosition;
            FixDotDiameter.OnValueChanged -= OnFixDotDiameter;
        }

        protected virtual void OnFixDotVisible(bool p, bool c)
        {
            renderer.enabled = c;
        }

        protected virtual void OnFixDotPosition(Vector3 p, Vector3 c)
        {
            transform.localPosition = c;
        }

        protected virtual void OnFixDotDiameter(float p, float c)
        {
            transform.localScale = new Vector3(c, c, c);
        }

    }
}
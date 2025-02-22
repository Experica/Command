﻿/*
NetEnvVisual.cs is part of the Experica.
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
using UnityEngine.VFX;
using Unity.Netcode;

namespace Experica.NetEnv
{
    /// <summary>
    /// Minimal base class for NetEnv Visual Object with only two NetworkVariable: Position and Visible
    /// </summary>
    public class NetEnvVisual : NetworkBehaviour
    {
        public NetworkVariable<Vector3> Position = new(Vector3.zero);
        public NetworkVariable<bool> Visible = new(true);
        protected new Renderer renderer;
        protected VisualEffect visualeffect;

        void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake()
        {
            renderer = GetComponent<Renderer>();
            visualeffect = GetComponent<VisualEffect>();
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
            Visible.OnValueChanged += OnVisible;
            Position.OnValueChanged += OnPosition;
        }

        public override void OnNetworkDespawn()
        {
            Visible.OnValueChanged -= OnVisible;
            Position.OnValueChanged -= OnPosition;
        }

        /// <summary>
        /// change visibility of the renderer
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        protected virtual void OnVisible(bool p, bool c)
        {
            renderer.enabled = c;
        }

        /// <summary>
        /// change local position
        /// </summary>
        /// <param name="p"></param>
        /// <param name="c"></param>
        protected virtual void OnPosition(Vector3 p, Vector3 c)
        {
            transform.localPosition = c;
        }

    }
}
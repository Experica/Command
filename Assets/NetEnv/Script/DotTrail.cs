﻿/*
DotTrail.cs is part of the Experica.
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

namespace Experica.NetEnv
{
    public class DotTrail : NetEnvVisual
    {
        public NetworkVariable<Vector3> Size = new(Vector3.one);
        public NetworkVariable<Color> Color = new(UnityEngine.Color.red);
        public NetworkVariable<Color> TrailEndColor = new(UnityEngine.Color.white);
        public NetworkVariable<float> TrailWidthScale = new(0.5f);
        public NetworkVariable<float> TrailTime = new(5f);
        public NetworkVariable<bool> TrailEmit = new(true);
        protected TrailRenderer trailrenderer;

        protected override void OnAwake()
        {
            base.OnAwake();
            trailrenderer = GetComponentInChildren<TrailRenderer>();
            OnColor(default, Color.Value);
            OnSize(default, Size.Value);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Size.OnValueChanged += OnSize;
            Color.OnValueChanged += OnColor;
            TrailEndColor.OnValueChanged += OnTrailEndColor;
            TrailTime.OnValueChanged += OnTrailTime;
            TrailWidthScale.OnValueChanged += OnTrailWidthScale;
            TrailEmit.OnValueChanged += OnTrailEmit;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Size.OnValueChanged -= OnSize;
            Color.OnValueChanged -= OnColor;
            TrailEndColor.OnValueChanged -= OnTrailEndColor;
            TrailTime.OnValueChanged -= OnTrailTime;
            TrailWidthScale.OnValueChanged -= OnTrailWidthScale;
            TrailEmit.OnValueChanged -= OnTrailEmit;
        }

        protected override void OnVisible(bool p, bool c)
        {
            renderer.enabled = c;
            trailrenderer.enabled = c;
        }

        protected virtual void OnSize(Vector3 p, Vector3 c)
        {
            transform.localScale = c;
            trailrenderer.widthMultiplier = c.x * TrailWidthScale.Value;
        }

        protected virtual void OnColor(Color p, Color c)
        {
            renderer.material.SetColor("_Color", c);
            trailrenderer.startColor = c;
        }

        protected virtual void OnTrailEndColor(Color p, Color c)
        {
            trailrenderer.endColor = c;
        }

        protected virtual void OnTrailTime(float p, float c)
        {
            trailrenderer.time = c;
        }

        protected virtual void OnTrailWidthScale(float p, float c)
        {
            trailrenderer.widthMultiplier = c * transform.localScale.x;
        }

        protected virtual void OnTrailEmit(bool p, bool c)
        {
            trailrenderer.emitting = c;
        }
    }
}
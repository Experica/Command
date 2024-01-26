/*
Grid.cs is part of the Experica.
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
    public class ScaleGrid : NetEnvVisual
    {
        public NetworkVariable<Color> AxisColor = new(new(0.1f, 0.1f, 0.1f));
        public NetworkVariable<float> AxisWidth = new(0.2f);
        public NetworkVariable<Color> TickColor = new(new(0.3f, 0.3f, 0.3f));
        public NetworkVariable<float> TickWidth = new(0.1f);
        public NetworkVariable<float> TickStep = new(5f);
        public NetworkVariable<Vector3> Size = new(Vector3.one);

        protected LineRenderer xaxis, yaxis;
        protected List<LineRenderer> xticks = new();
        protected List<LineRenderer> yticks = new();


        protected override void OnAwake()
        {
            xaxis = Experica.AddXLine(name: "XAxis", parent: transform);
            yaxis = Experica.AddYLine(name: "YAxis", parent: transform);
            // Axis on top of ticks
            xaxis.transform.localPosition = Vector3.back;
            yaxis.transform.localPosition = Vector3.back;
            OnAxisWidth(default, AxisWidth.Value);
            OnAxisColor(default, AxisColor.Value);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            AxisColor.OnValueChanged += OnAxisColor;
            AxisWidth.OnValueChanged += OnAxisWidth;
            TickWidth.OnValueChanged += OnTickWidth;
            TickColor.OnValueChanged += OnTickColor;
            TickStep.OnValueChanged += OnTickStep;
            Size.OnValueChanged += OnSize;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            AxisColor.OnValueChanged -= OnAxisColor;
            AxisWidth.OnValueChanged -= OnAxisWidth;
            TickWidth.OnValueChanged -= OnTickWidth;
            TickColor.OnValueChanged -= OnTickColor;
            TickStep.OnValueChanged -= OnTickStep;
            Size.OnValueChanged -= OnSize;
        }

        protected virtual void OnAxisColor(Color p, Color c)
        {
            xaxis.startColor = c;
            xaxis.endColor = c;
            yaxis.startColor = c;
            yaxis.endColor = c;
        }

        protected virtual void OnAxisWidth(float p, float c)
        {
            xaxis.widthMultiplier = c;
            yaxis.widthMultiplier = c;
        }

        protected virtual void OnTickColor(Color p, Color c)
        {
            foreach (var t in xticks) { t.startColor = c; t.endColor = c; }
            foreach (var t in yticks) { t.startColor = c; t.endColor = c; }
        }

        protected virtual void OnTickWidth(float p, float c)
        {
            foreach (var t in xticks) { t.widthMultiplier = c; }
            foreach (var t in yticks) { t.widthMultiplier = c; }
        }

        protected virtual void OnTickStep(float p, float c)
        {
            updatetick();
        }

        protected virtual void OnSize(Vector3 p, Vector3 c)
        {
            xaxis.transform.localScale = new(c.x, 1, 1);
            yaxis.transform.localScale = new(1, c.y, 1);
            updatetick();
        }

        protected override void OnVisible(bool p, bool c)
        {
            xaxis.enabled = c;
            yaxis.enabled = c;
            if (c) { updatetick(); }
            foreach (var t in xticks) { t.enabled = c; }
            foreach (var t in yticks) { t.enabled = c; }
        }

        float viewwidth, viewheight;
        protected override void OnPosition(Vector3 p, Vector3 c)
        {
            transform.localPosition = c;
            Size.Value = new(viewwidth + 2 * Mathf.Abs(c.x), viewheight + 2 * Mathf.Abs(c.y), 1);
        }

        public void UpdateView(INetEnvCamera camera)
        {
            viewwidth = camera.Width; viewheight = camera.Height;
            // fixed ratio of linewidth to camera view size, so that line can not be too thin to render as invisible
            AxisWidth.Value = viewheight * 0.005f;
            TickWidth.Value = viewheight * 0.0025f;
            Size.Value = new(viewwidth + 2 * Mathf.Abs(transform.localPosition.x), viewheight + 2 * Mathf.Abs(transform.localPosition.y), 1);
        }

        void updatetick()
        {
            if (!Visible.Value) { return; }
            var size = Size.Value;
            var tickstep = TickStep.Value;
            var tickwidth = TickWidth.Value;
            var tickcolor = TickColor.Value;

            var nt = size / 2f / tickstep;
            var nx = Mathf.FloorToInt(nt.x);
            var ny = Mathf.FloorToInt(nt.y);
            var xd = xticks.Count - 2 * nx;
            var yd = yticks.Count - 2 * ny;

            if (xd > 0)
            {
                for (var i = 0; i < xd; i++) { Destroy(xticks[i].gameObject); }
                xticks.RemoveRange(0, xd);
            }
            if (xd < 0)
            {
                for (var i = 0; i < -xd; i++) { xticks.Add(Experica.AddYLine(parent: transform)); }
            }
            for (var i = 0; i < nx; i++)
            {
                var tickvalue = (i + 1) * tickstep;
                var t = xticks[0 + 2 * i];
                t.transform.localPosition = new Vector3(tickvalue, 0, 0);
                t.gameObject.name = "XTick_" + tickvalue;
                t.transform.localScale = new Vector3(1, size.y, 1);
                t.widthMultiplier = tickwidth;
                t.startColor = tickcolor; t.endColor = tickcolor;

                t = xticks[1 + 2 * i];
                t.transform.localPosition = new Vector3(-tickvalue, 0, 0);
                t.gameObject.name = "XTick_" + (-tickvalue);
                t.transform.localScale = new Vector3(1, size.y, 1);
                t.widthMultiplier = tickwidth;
                t.startColor = tickcolor; t.endColor = tickcolor;
            }

            if (yd > 0)
            {
                for (var i = 0; i < yd; i++) { Destroy(yticks[i].gameObject); }
                yticks.RemoveRange(0, yd);
            }
            if (yd < 0)
            {
                for (var i = 0; i < -yd; i++) { yticks.Add(Experica.AddXLine(parent: transform)); }
            }
            for (var i = 0; i < ny; i++)
            {
                var tickvalue = (i + 1) * tickstep;
                var t = yticks[0 + 2 * i];
                t.transform.localPosition = new Vector3(0, tickvalue, 0);
                t.gameObject.name = "YTick_" + tickvalue;
                t.transform.localScale = new Vector3(size.x, 1, 1);
                t.widthMultiplier = tickwidth;
                t.startColor = tickcolor; t.endColor = tickcolor;

                t = yticks[1 + 2 * i];
                t.transform.localPosition = new Vector3(0, -tickvalue, 0);
                t.gameObject.name = "YTick_" + (-tickvalue);
                t.transform.localScale = new Vector3(size.x, 1, 1);
                t.widthMultiplier = tickwidth;
                t.startColor = tickcolor; t.endColor = tickcolor;
            }
        }

    }
}

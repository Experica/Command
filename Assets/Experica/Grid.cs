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
using UnityEngine;

namespace Experica
{
    public class Grid : MonoBehaviour
    {
        public GameObject xaxisprefab, yaxisprefab, xtickprefab, ytickprefab;
        public LineRenderer xaxis, yaxis;
        public List<LineRenderer> xticks = new List<LineRenderer>();
        public List<LineRenderer> yticks = new List<LineRenderer>();

        void Awake()
        {
            xaxisprefab = Resources.Load<GameObject>("XAxis");
            yaxisprefab = Resources.Load<GameObject>("YAxis");
            xtickprefab = Resources.Load<GameObject>("XTick");
            ytickprefab = Resources.Load<GameObject>("YTick");

            var gox = Instantiate(xaxisprefab, transform);
            gox.name = "XAxis";
            xaxis = gox.GetComponent<LineRenderer>();
            var goy = Instantiate(yaxisprefab, transform);
            goy.name = "YAxis";
            yaxis = goy.GetComponent<LineRenderer>();
        }

        private float axislinewidth = 0.008f;
        public float AxisLineWidth
        {
            get { return axislinewidth; }
            set { axislinewidth = value; }
        }

        public void UpdateAxisLineWidth(float viewporthalfheight)
        {
            var w = axislinewidth * viewporthalfheight;
            xaxis.widthMultiplier = w;
            yaxis.widthMultiplier = w;
        }

        private float ticklinewidth = 0.008f;
        public float TickLineWidth
        {
            get { return ticklinewidth; }
            set { ticklinewidth = value; }
        }

        public void UpdateTickLineWidth(float viewporthalfheight)
        {
            var w = ticklinewidth * viewporthalfheight;
            foreach (var t in xticks)
            {
                t.widthMultiplier = w;
            }
            foreach (var t in yticks)
            {
                t.widthMultiplier = w;
            }
        }


        public Vector3 Size
        {
            get { return new Vector3(xaxis.transform.localScale.x, yaxis.transform.localScale.y, 1); }
            set
            {
                xaxis.transform.localScale = new Vector3(value.x, 1, 1);
                yaxis.transform.localScale = new Vector3(1, value.y, 1);
            }
        }

        public Vector3 TickSize
        {
            get
            {
                if (xticks.Count > 0)
                {
                    return new Vector3(yticks[0].transform.localScale.x, xticks[0].transform.localScale.y, 1);
                }
                else
                {
                    return new Vector3(0, 0, 1);
                }
            }
            set
            {
                if (xticks.Count > 0)
                {
                    foreach (var t in xticks)
                    {
                        t.transform.localScale = new Vector3(1, value.y, 1);
                    }
                    foreach (var t in yticks)
                    {
                        t.transform.localScale = new Vector3(value.x, 1, 1);
                    }
                }
            }
        }

        public Vector3 Center
        {
            get { return transform.position; }
            set { transform.position = value; }
        }

        private float tickinterval = 5f;
        public float TickInterval
        {
            get { return tickinterval; }
            set
            {
                if (tickinterval != value)
                {
                    UpdateTick(value);
                }
            }
        }

        public void UpdateTick(float tickinterval)
        {
            foreach (var t in xticks)
            {
                Destroy(t.gameObject);
            }
            xticks.Clear();
            foreach (var t in yticks)
            {
                Destroy(t.gameObject);
            }
            yticks.Clear();

            for (var i = 0; i < Mathf.Floor(Size.x / tickinterval); i++)
            {
                var tickp = Instantiate(xtickprefab, transform);
                var tickvalue = (i + 1) * tickinterval;
                tickp.transform.localPosition = new Vector3(tickvalue, 0, 1);
                tickp.name = "XTick_" + tickvalue;
                xticks.Add(tickp.GetComponent<LineRenderer>());
                var tickn = Instantiate(xtickprefab, transform);
                tickn.transform.localPosition = new Vector3(-tickvalue, 0, 1);
                tickn.name = "XTick_" + (-tickvalue);
                xticks.Add(tickn.GetComponent<LineRenderer>());
            }
            for (var i = 0; i < Mathf.Floor(Size.y / tickinterval); i++)
            {
                var tickp = Instantiate(ytickprefab, transform);
                var tickvalue = (i + 1) * tickinterval;
                tickp.transform.localPosition = new Vector3(0, tickvalue, 1);
                tickp.name = "YTick_" + tickvalue;
                yticks.Add(tickp.GetComponent<LineRenderer>());
                var tickn = Instantiate(ytickprefab, transform);
                tickn.transform.localPosition = new Vector3(0, -tickvalue, 1);
                tickn.name = "YTick_" + (-tickvalue);
                yticks.Add(tickn.GetComponent<LineRenderer>());
            }
            this.tickinterval = tickinterval;
        }


    }
}

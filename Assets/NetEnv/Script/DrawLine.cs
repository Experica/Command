/*
DrawLine.cs is part of the Experica.
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
using UnityEngine.InputSystem;
using Unity.Netcode;
using Experica;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;


namespace Experica.NetEnv
{
    public class DrawLine : NetworkBehaviour, INetEnvPlayer
    {
        public NetworkVariable<Color> LineColor = new(Color.black);
        public NetworkVariable<float> LineWidth = new(0.25f);
        public NetworkVariable<bool> LineVisible = new(true);
        public NetworkVariable<bool> EnableDraw = new(false);

        public List<LineRenderer> Lines = new();
        public INetEnvCamera NetEnvCamera;
        public bool Submit;
        InputAction SubmitAction;
        LineRenderer currentline;

        public ulong ClientID { get; set; }

        void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += Touch_onFingerDown;
            Touch.onFingerMove += Touch_onFingerMove;
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                if (nm.IsClient)
                {
                    NetEnvCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<INetEnvCamera>();
                    Touch.onFingerUp += Touch_onFingerUp;
                }
                if (!nm.IsServer)
                {
                    SubmitAction = InputSystem.actions.FindActionMap("Logic").FindAction("Visible");
                }
            }
        }

        [Rpc(SendTo.Server)]
        void ReportCurrentLineRpc(Vector3[] pos)
        {
            currentline = Base.AddLine(null, parent: transform);
            currentline.startColor = LineColor.Value;
            currentline.endColor = LineColor.Value;
            currentline.widthMultiplier = LineWidth.Value;
            currentline.positionCount = pos.Length;
            currentline.SetPositions(pos);
            Lines.Add(currentline);
        }

        void Touch_onFingerUp(Finger finger)
        {
            var n = currentline.positionCount;
            var pos = new Vector3[n];
            currentline.GetPositions(pos);
            ReportCurrentLineRpc(pos);
        }

        void Touch_onFingerDown(Finger finger)
        {
            if (EnableDraw.Value)
            {
                currentline = Base.AddLine(null, parent: transform);
                currentline.startColor = LineColor.Value;
                currentline.endColor = LineColor.Value;
                currentline.widthMultiplier = LineWidth.Value;
                currentline.positionCount++;
                currentline.SetPosition(currentline.positionCount - 1, ScreenToViewportPoint(finger.screenPosition));
                Lines.Add(currentline);
            }
        }

        void Touch_onFingerMove(Finger finger)
        {
            if (EnableDraw.Value)
            {
                currentline.positionCount++;
                currentline.SetPosition(currentline.positionCount - 1, ScreenToViewportPoint(finger.screenPosition));
            }
        }

        Vector3 ScreenToViewportPoint(Vector3 screenPosition)
        {
            var vp = NetEnvCamera.Camera.ScreenToViewportPoint(screenPosition);
            vp.x = (vp.x - 0.5f) * NetEnvCamera.Width;
            vp.y = (vp.y - 0.5f) * NetEnvCamera.Height;
            vp.z = 0;
            return vp;
        }

        [Rpc(SendTo.Everyone)]
        public void ClearRpc()
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                Destroy(Lines[i].gameObject);
            }
            Lines.Clear();
            currentline = null;
        }

        public bool TryGetLine(out Vector3[] positions, int lineindex = 0)
        {
            positions = null;
            if (lineindex >= Lines.Count) { return false; }
            positions = new Vector3[Lines[lineindex].positionCount];
            Lines[lineindex].GetPositions(positions);
            return true;
        }

        public override void OnNetworkSpawn()
        {
            LineVisible.OnValueChanged += OnVisible;
            LineColor.OnValueChanged += OnLineColor;
            LineWidth.OnValueChanged += OnLineWidth;
        }

        public override void OnNetworkDespawn()
        {
            LineVisible.OnValueChanged -= OnVisible;
            LineColor.OnValueChanged -= OnLineColor;
            LineWidth.OnValueChanged -= OnLineWidth;
        }

        protected virtual void OnVisible(bool p, bool c)
        {
            foreach (var line in Lines) { line.enabled = c; }
        }

        protected virtual void OnLineColor(Color p, Color c)
        {
            if (currentline != null)
            {
                currentline.startColor = c;
                currentline.endColor = c;
            }
        }

        protected virtual void OnLineWidth(float p, float c)
        {
            if (currentline != null)
            {
                currentline.widthMultiplier = c;
            }
        }

        public void AskReportRpc()
        {
            throw new System.NotImplementedException();
        }

        public void ReportRpc(string name, float value)
        {
            throw new System.NotImplementedException();
        }

        [Rpc(SendTo.Server)]
        void ReportSubmitRpc()
        {
            Submit = true;
        }

        void Update()
        {
            if (EnableDraw.Value && SubmitAction != null && SubmitAction.WasPerformedThisFrame())
            {
                ReportSubmitRpc();
            }
        }
    }
}
/*
ExperimentSessionLogic.cs is part of the Experica.
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
using System;

namespace Experica.Command
{
    public class ExperimentSessionLogic : MonoBehaviour
    {
        public bool islogicactive = false;
        public ExperimentSession exsession;
        public ExperimentManager exmanager;
        public Action OnBeginStartExperimentSession, OnEndStartExperimentSession,
            OnBeginStopExperimentSession, OnEndStopExperimentSession;

        public ExperimentLogic EL { get { return exmanager.el; } }
        public int ExRepeat { get { return exmanager.ELRepeat; } }
        public double SinceExReady { get { return exmanager.SinceELReady; } }
        public double SinceExStop { get { return exmanager.SinceELStop; } }
        public EXPERIMENTSTATUS ExperimentStatus
        {
            get { return exmanager.ExperimentStatus; }
            set { exmanager.ExperimentStatus = value; }
        }

        public string ExperimentID
        {
            get { return exmanager.ELID; }
            set
            {
                exmanager.ChangeEx(value);
            }
        }

        public void StartExperiment()
        {
            exmanager.StartEx();
        }

        public void StartStopExperimentSession(bool isstart)
        {
            if (isstart)
            {
                OnBeginStartExperimentSession?.Invoke();
                exmanager.ELID = null;
                OnStartExperimentSession();
                exmanager.timer.Restart();
                OnEndStartExperimentSession?.Invoke();
                islogicactive = true;
            }
            else
            {
                OnBeginStopExperimentSession?.Invoke();
                islogicactive = false;
                OnStopExperimentSession();
                exmanager.timer.Stop();
                OnEndStopExperimentSession?.Invoke();
            }
        }

        protected virtual void OnStartExperimentSession()
        {
        }

        protected virtual void OnStopExperimentSession()
        {
        }

        void Awake()
        {
            OnAwake();
        }
        protected virtual void OnAwake()
        {
        }

        void Start()
        {
            OnStart();
        }
        protected virtual void OnStart()
        {
        }

        void Update()
        {
            OnUpdate();
            if (islogicactive)
            {
                Logic();
            }
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void Logic()
        {
        }
    }
}
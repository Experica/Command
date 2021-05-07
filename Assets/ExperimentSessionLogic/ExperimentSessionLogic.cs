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
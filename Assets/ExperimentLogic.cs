using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

public class Subject
{
    public string name;
    public string id;
    public string species;
    public string gender;
    public float age;
    public Vector3 size;
    public float weight;
}

public struct Control
{
    public int blockcount;
    public int trialcount;
    public int conditiontestcount;
}

public class Experiment
{
    public string name;
    public string id;
    public Subject subject=new Subject();
    public List<Dictionary<string, object>> condition;
    public double preIBI, blockdur, sufIBI, preITI, trialdur, sufITI, preICI, conditiontestdur, sufICI;
    public int nblock, ntrial, nconditiontest;
    public Control control;
    public Timer timer=new Timer();
}

public class Timer : Stopwatch
{
    public double ElapsedSeconds
    {
        get { return Elapsed.TotalSeconds; }
    }

    public void Rest(double restT)
    {
        if (!IsRunning)
        {
            Start();
        }
        double startT, endT;
        startT = Elapsed.TotalSeconds;
        endT = Elapsed.TotalSeconds;
        while ((endT - startT) < restT)
        {
            endT = Elapsed.TotalSeconds;
        }
    }

    public void ReStart()
    {
        Reset();
        Start();
    }
}

public class ExperimentLogic : MonoBehaviour
{
    public Experiment experiment = new Experiment();

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}

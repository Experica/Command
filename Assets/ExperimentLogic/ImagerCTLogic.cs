/*
ImagerCTLogic.cs is part of the Experica.
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
using System.Collections.Generic;
using Experica;
using System.IO;
using ColorSpace = Experica.ColorSpace;

/// <summary>
/// Episodic Condition Test(PreITI-{PreICI-Cond-SufICI}-SufITI) with Imager Data Acquisition System, and Predefined Colors
/// </summary>
public class ImagerCTLogic : ConditionTestLogic
{
    IRecorder markrecorder;

    protected override void OnStartExperiment()
    {
        recorder = Extension.GetImagerRecorder(Config.RecordHost1, Config.RecordHostPort1);
        StopEpochRecord();
        markrecorder = Extension.GetSpikeGLXRecorder(Config.RecordHost0, Config.RecordHostPort0);
        base.OnStartExperiment();
    }

    protected override void OnExperimentStopped()
    {
        StopEpochRecord();
        recorder = null;
        markrecorder = null;
        base.OnExperimentStopped();
    }

    protected void StartEpochRecord(int epoch = 0)
    {
        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
        {
            if (recorder != null)
            {
                // create data folder for Imager recording
                var datapath = ex.GetDataPath(addfiledir: true);
                var datadir = Path.GetDirectoryName(datapath);
                var dataname = Path.GetFileNameWithoutExtension(datapath);
                // save Imager frames in Epoch subfolder
                datadir = Path.Combine(datadir, $"Epoch{epoch}");
                Directory.CreateDirectory(datadir);
                recorder.RecordPath = Path.Combine(datadir, dataname);
                recorder.StartRecordAndAcquisite();
            }
        }
    }

    protected void StopEpochRecord()
    {
        if (recorder != null)
        {
            recorder.StopAcquisiteAndRecord();
        }
    }

    protected override void GenerateFinalCondition()
    {
        var colorspace = GetExParam<ColorSpace>("ColorSpace");
        var colorvar = GetExParam<string>("Color");
        var colorname = colorspace + "_" + colorvar;

        // get color
        List<Color> color = null;
        List<Color> wp = null;
        List<float> angle = null;
        var data = ex.Display_ID.GetColorData();
        if (data != null)
        {
            if (data.ContainsKey(colorname))
            {
                color = data[colorname].Convert<List<Color>>();

                var wpname = colorname + "_WP";
                if (data.ContainsKey(wpname))
                {
                    wp = data[wpname].Convert<List<Color>>();
                }
                var anglename = colorname + "_Angle";
                if (data.ContainsKey(anglename))
                {
                    angle = data[anglename].Convert<List<float>>();
                }
            }
            else
            {
                Debug.Log($"{colorname} is not found in colordata of {ex.Display_ID}.");
            }
        }

        if (color != null)
        {
            SetEnvActiveParam("MinColor", color[0]);
            SetEnvActiveParam("MaxColor", color[1]);
            if (wp != null)
            {
                SetEnvActiveParam("BGColor", wp[0]);
            }
        }

        base.GenerateFinalCondition();
    }

    protected override void StartExperimentTimeSync()
    {
        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
        {
            if (markrecorder != null)
            {
                markrecorder.RecordPath = ex.GetDataPath(addfiledir: true);
                timer.TimeoutMillisecond(Config.NotifyLatency);

                markrecorder.RecordStatus = RecordStatus.Recording;
                timer.TimeoutMillisecond(Config.NotifyLatency);
            }
        }
        base.StartExperimentTimeSync();
    }

    protected override void StopExperimentTimeSync()
    {
        if (markrecorder != null)
        {
            markrecorder.RecordStatus = RecordStatus.Stopped;
            timer.TimeoutMillisecond(Config.NotifyLatency);
        }
        base.StopExperimentTimeSync();
    }

    protected override void Logic()
    {
        switch (TrialState)
        {
            case TRIALSTATE.NONE:
                if (EnterTrialState(TRIALSTATE.PREITI) == EnterCode.NoNeed) { return; }
                SyncFrame();
                break;
            case TRIALSTATE.PREITI:
                if (PreITIHold >= ex.PreITI)
                {
                    EnterTrialState(TRIALSTATE.TRIAL, true);
                    SyncFrame();
                }
                break;
            case TRIALSTATE.TRIAL:
                switch (CondState)
                {
                    case CONDSTATE.NONE:
                        StartEpochRecord(condtestmanager.CondTestIndex);
                        EnterCondState(CONDSTATE.PREICI);
                        SyncFrame();
                        break;
                    case CONDSTATE.PREICI:
                        if (PreICIHold >= ex.PreICI)
                        {
                            EnterCondState(CONDSTATE.COND, true);
                            SetEnvActiveParam("Visible", true);
                            SyncFrame();
                        }
                        break;
                    case CONDSTATE.COND:
                        if (CondHold >= ex.CondDur)
                        {
                            EnterCondState(CONDSTATE.SUFICI, true);
                            SetEnvActiveParam("Visible", false);
                            SyncFrame();
                        }
                        break;
                    case CONDSTATE.SUFICI:
                        if (SufICIHold >= ex.SufICI)
                        {
                            StopEpochRecord();
                            EnterCondState(CONDSTATE.NONE);
                            EnterTrialState(TRIALSTATE.SUFITI, true);
                            SyncFrame();
                        }
                        break;
                }
                break;
            case TRIALSTATE.SUFITI:
                if (SufITIHold >= ex.SufITI)
                {
                    EnterTrialState(TRIALSTATE.NONE);
                }
                break;
        }
    }
}

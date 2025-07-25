﻿/*
ImagerEpoch.cs is part of the Experica.
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
using System.Linq;
using Experica;
using Experica.Command;
using System.IO;
using ColorSpace = Experica.NetEnv.ColorSpace;

/// <summary>
/// Episodic Condition Test(PreITI-{PreICI-Cond-SufICI}-SufITI) with Imager Data Acquisition System, and Predefined Colors
/// </summary>
public class ImagerEpoch : ConditionTestLogic
{
    IRecorder markrecorder; // for camera shutter signals of each frame
    bool online;
    string dataroot;
    string currentepoch;

    /// <summary>
    /// init Imager and mark recorders
    /// </summary>
    protected override void OnStartExperiment()
    {
        recorder = Base.QueryImagerRecorder(Config.RecordHost1, Config.RecordHostPort1);
        recorder?.StopAcquisiteAndRecord();
        //markrecorder = Extension.GetSpikeGLXRecorder(Config.RecordHost0, Config.RecordHostPort0);
        base.OnStartExperiment();
    }

    /// <summary>
    /// save experiment at the beginning for online analysis
    /// </summary>
    protected override void OnExperimentStarted()
    {
        online = GetExParam<bool>("OnLine");
        if (online)
        {
            // create data folder for Imager recording
            var datapath = ex.GetDataPath(addfiledir: true);
            dataroot = Path.GetDirectoryName(datapath);
            SaveData();
        }
    }

    /// <summary>
    /// release all recorders
    /// </summary>
    protected override void OnExperimentStopped()
    {
        recorder?.StopAcquisiteAndRecord();
        recorder = null;
        markrecorder = null;
        base.OnExperimentStopped();
    }

    /// <summary>
    /// prepare epoch file path for Imager and start recording
    /// </summary>
    /// <param name="epoch"></param>
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
                currentepoch = $"Epoch{epoch}";
                datadir = Path.Combine(datadir, currentepoch);
                Directory.CreateDirectory(datadir);
                recorder.RecordPath = Path.Combine(datadir, dataname);
                recorder.StartRecordAndAcquisite();
            }
        }
    }

    /// <summary>
    /// stop Imager recording
    /// </summary>
    /// <param name="saveepoch">save epoch's `CondTest` for online analysis</param>
    protected void StopEpochRecord(bool saveepoch = true)
    {
        recorder?.StopAcquisiteAndRecord();
        if (online && saveepoch)
        {
            var epochpath = Path.Combine(dataroot, $".{currentepoch}.{Config.SaveDataFormat.ToString().ToLower()}");
            var ct = condtestmgr.CurrentCondTest;
            epochpath.Save(ct, rmext: true);
        }
    }

    protected override void PrepareCondition()
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

        // get conditions from file
        base.PrepareCondition();

        if (color != null)
        {
            if (!ex.ID.EndsWith("Retinotopy"))
            {
                SetEnvActiveParam("MinColor", color[0]);
                SetEnvActiveParam("MaxColor", color[1]);
                if (wp != null)
                {
                    SetEnvActiveParam("BGColor", wp[0]);
                }
            }

            if (ex.ID == "ISIEpoch2Color" || ex.ID == "ISIEpochFlash2Color")
            {
                condmgr.PrepareCondition(new Dictionary<string, List<object>>() { ["Color"] = color.Select(i => (object)i).ToList() });
            }
        }
    }

    protected override void StartExperimentTimeSync()
    {
        if (ex.CondTestAtState != CONDTESTATSTATE.NONE)
        {
            if (markrecorder != null)
            {
                markrecorder.RecordPath = ex.GetDataPath(addfiledir: true);
                timer.WaitMillisecond(Config.NotifyLatency);

                markrecorder.RecordStatus = RecordStatus.Recording;
                timer.WaitMillisecond(Config.NotifyLatency);
            }
        }
        base.StartExperimentTimeSync();
    }

    protected override void StopExperimentTimeSync()
    {
        if (markrecorder != null)
        {
            markrecorder.RecordStatus = RecordStatus.Stopped;
            timer.WaitMillisecond(Config.NotifyLatency);
        }
        base.StopExperimentTimeSync();
    }

    protected override void OnCONDEntered()
    {
        if (ex.ID.EndsWith("Retinotopy"))
        {
            SetEnvActiveParam("Visible@Grating@Grating0", true);
            SetEnvActiveParam("Visible@Grating@Grating1", true);
        }
        else
        {
            base.OnCONDEntered();
        }
    }

    protected override void OnSUFICIEntered()
    {
        if (ex.ID.EndsWith("Retinotopy"))
        {
            SetEnvActiveParam("Visible@Grating@Grating0", false);
            SetEnvActiveParam("Visible@Grating@Grating1", false);
        }
        else
        {
            base.OnSUFICIEntered();
        }
    }

    protected override void Logic()
    {
        switch (TrialState)
        {
            case TRIALSTATE.NONE:
                if (EnterTrialState(TRIALSTATE.PREITI) == EnterStateCode.ExFinish) { return; }
                break;
            case TRIALSTATE.PREITI:
                if (PreITIHold >= ex.PreITI)
                {
                    EnterTrialState(TRIALSTATE.TRIAL, true);
                }
                break;
            case TRIALSTATE.TRIAL:
                switch (CondState)
                {
                    case CONDSTATE.NONE:
                        StartEpochRecord(condtestmgr.CondTestIndex);
                        EnterCondState(CONDSTATE.PREICI);
                        break;
                    case CONDSTATE.PREICI:
                        if (PreICIHold >= ex.PreICI)
                        {
                            EnterCondState(CONDSTATE.COND, true);
                            OnCONDEntered();
                        }
                        break;
                    case CONDSTATE.COND:
                        if (CondHold >= ex.CondDur)
                        {
                            EnterCondState(CONDSTATE.SUFICI, true);
                            OnSUFICIEntered();
                        }
                        break;
                    case CONDSTATE.SUFICI:
                        if (SufICIHold >= ex.SufICI)
                        {
                            StopEpochRecord();
                            EnterCondState(CONDSTATE.NONE);
                            EnterTrialState(TRIALSTATE.SUFITI, true);
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

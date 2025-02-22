/*
ColorEPhysLite.cs is part of the Experica.
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
using Experica;
using Experica.Command;

public class ColorEPhysLite : ColorEPhys
{
    protected override void Logic()
    {
        switch (ExperimentID)
        {
            case "ConditionTest":
                if (SinceExReady > exsession.ReadyWait)
                {
                    exmgr.el.Guide = exsession.IsGuideOn;
                    exmgr.appmgr.FullScreen = exsession.IsFullScreen;
                    exmgr.appmgr.FullViewport = exsession.IsFullViewport;
                    EL.SetExParam("NotifyExperimenter", exsession.NotifyExperimenter);
                    eye = EL.GetExParam<Eye>("Eye");
                    diameter = EL.GetEnvActiveParam<float>("Diameter");
                    position = EL.GetEnvActiveParam<Vector3>("Position");

                    ExperimentID = "Flash2Color";
                }
                break;
            case "Flash2Color":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            switch (ExRepeat)
                            {
                                case 0:
                                    exmgr.appmgr.FullViewportSize();
                                    EL.SetExParam("Eye", eye);
                                    EL.SetExParam("CondRepeat", 100);
                                    EL.SetExParam("PreICI", 0);
                                    EL.SetExParam("CondDur", 1500);
                                    EL.SetExParam("SufICI", 0);
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "X");
                                    break;
                                case 1:
                                    EL.SetExParam("Color", "Y");
                                    break;
                                case 2:
                                    EL.SetExParam("Color", "Z");
                                    break;

                            }
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            if (ExRepeat < 3)
                            {
                                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                            }
                            else
                            {
                                ExperimentID = "Color";
                            }
                        }
                        break;
                }
                break;
            case "Color":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            switch (ExRepeat)
                            {
                                case 0:
                                    EL.SetEnvActiveParam("Position", position);
                                    EL.SetEnvActiveParam("Diameter", diameter);
                                    EL.SetEnvActiveParam("MaskType", "Disk");
                                    EL.SetEnvActiveParam("MaskRadius", 0.5);
                                    EL.SetExParam("CondRepeat", 8);
                                    EL.SetExParam("PreICI", 250);
                                    EL.SetExParam("CondDur", 500);
                                    EL.SetExParam("SufICI", 250);
                                    EL.SetExParam("ColorSpace", "HSL");
                                    EL.SetExParam("Color", "HueYm");
                                    break;
                                case 1:
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "HueL0");
                                    break;
                            }
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            if (ExRepeat < 2)
                            {
                                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                            }
                            else
                            {
                                ExperimentID = "HartleySubspace";
                            }
                        }
                        break;
                }
                break;
            case "HartleySubspace":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            switch (ExRepeat)
                            {
                                case 0:
                                    EL.SetEnvActiveParam("Position", position);
                                    EL.SetEnvActiveParam("Diameter", diameter);
                                    EL.SetEnvActiveParam("GratingType", "Sinusoidal");
                                    EL.SetEnvActiveParam("MaskType", "Disk");
                                    EL.SetEnvActiveParam("MaskRadius", 0.5);
                                    EL.SetEnvActiveParam("TemporalFreq", 0);
                                    EL.SetExParam("CondRepeat", 5);
                                    EL.SetExParam("PreICI", 0);
                                    EL.SetExParam("SufICI", 0);
                                    EL.SetExParam("CondDur", 35.0.GetCondDur(Screen.currentResolution.refreshRate));
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "X");
                                    break;
                                case 1:
                                    EL.SetExParam("ColorSpace", "LMS");
                                    EL.SetExParam("Color", "Xmcc");
                                    break;
                                case 2:
                                    EL.SetExParam("Color", "Ymcc");
                                    break;
                                case 3:
                                    EL.SetExParam("Color", "Zmcc");
                                    break;
                            }
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            if (ExRepeat < 4)
                            {
                                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                            }
                            else
                            {
                                ExperimentID = "OriSF";
                            }
                        }
                        break;
                }
                break;
            case "OriSF":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            switch (ExRepeat)
                            {
                                case 0:
                                    EL.SetEnvActiveParam("Position", position);
                                    EL.SetEnvActiveParam("Diameter", diameter);
                                    EL.SetEnvActiveParam("GratingType", "Sinusoidal");
                                    EL.SetEnvActiveParam("MaskType", "Disk");
                                    EL.SetEnvActiveParam("MaskRadius", 0.5);
                                    EL.SetEnvActiveParam("TemporalFreq", 5);
                                    EL.SetExParam("CondRepeat", 6);
                                    EL.SetExParam("PreICI", 250);
                                    EL.SetExParam("CondDur", 1500);
                                    EL.SetExParam("SufICI", 250);
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "X");
                                    break;
                                case 1:
                                    EL.SetExParam("ColorSpace", "LMS");
                                    EL.SetExParam("Color", "Xmcc");
                                    break;
                                case 2:
                                    EL.SetExParam("Color", "Ymcc");
                                    break;
                                case 3:
                                    EL.SetExParam("Color", "Zmcc");
                                    break;
                            }
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            if (ExRepeat < 4)
                            {
                                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                            }
                            else
                            {
                                StartStopExperimentSession(false);
                                exmgr.appmgr.FullViewport = false;
                                exmgr.el.Guide = true;
                            }
                        }
                        break;
                }
                break;
        }
    }
}

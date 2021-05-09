/*
ColorEphys.cs is part of the Experica.
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
using Experica;
using Experica.Command;

public class ColorEphys : ExperimentSessionLogic
{
    float diameter = 3;
    string log = "";

    protected override void Logic()
    {
        switch (ExperimentID)
        {
            case null:
                ExperimentID = "ConditionTest";
                break;
            case "ConditionTest":
                if (SinceExReady > exsession.ReadyWait)
                {
                    exmanager.uicontroller.IsGuideOn = exsession.IsGuideOn;
                    exmanager.uicontroller.FullScreen = exsession.IsFullScreen;
                    exmanager.uicontroller.IsFullViewport = exsession.IsFullViewport;
                    EL.SetExParam("SendMail", exsession.SendMail);
                    diameter = EL.GetEnvActiveParam<float>("Diameter");
                    log = EL.GetEnvActiveParam<string>("Log");

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
                                    EL.SetExParam("Log", log);
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "X");
                                    exmanager.uicontroller.ViewportSize();
                                    var size = EL.GetEnvActiveParam<Vector3>("Size");
                                    var pos = EL.GetEnvActiveParam<Vector3>("Position");
                                    EL.SetEnvActiveParam("Diameter", Mathf.Max(size.x + Mathf.Abs(pos.x), size.y + Mathf.Abs(pos.y)));
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
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "HueL0");
                                    EL.SetEnvActiveParam("Diameter", diameter);
                                    break;
                                case 1:
                                    EL.SetExParam("ColorSpace", "HSL");
                                    EL.SetExParam("Color", "HueYm");
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
                                ExperimentID = "CycleColorPlane";
                            }
                        }
                        break;
                }
                break;
            case "CycleColorPlane":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            switch (ExRepeat)
                            {
                                case 0:
                                    EL.SetExParam("ModulateParam", "DKLIsoLum");
                                    EL.SetExParam("CycleDirection", 1f);
                                    EL.SetEnvActiveParam("Diameter", diameter);
                                    EL.SetEnvActiveParam("ModulateTemporalFreq", 0.25);
                                    EL.SetEnvActiveParam("GratingType", "Sinusoidal");
                                    EL.SetEnvActiveParam("SpatialPhase", 0.75);
                                    break;
                                case 1:
                                    EL.SetExParam("CycleDirection", -1f);
                                    break;
                                case 2:
                                    EL.SetExParam("ModulateParam", "DKLIsoLM");
                                    break;
                                case 3:
                                    EL.SetExParam("CycleDirection", 1f);
                                    break;
                                case 4:
                                    EL.SetExParam("ModulateParam", "DKLIsoSLM");
                                    break;
                                case 5:
                                    EL.SetExParam("CycleDirection", -1f);
                                    break;

                            }
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            if (ExRepeat < 6)
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
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "X");
                                    EL.SetEnvActiveParam("Diameter", diameter);
                                    EL.SetEnvActiveParam("PauseTime", true);
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
                                ExperimentID = "Image";
                            }
                        }
                        break;
                }
                break;
            case "Image":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            switch (ExRepeat)
                            {
                                case 0:
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "X");
                                    EL.SetEnvActiveParam("Diameter", diameter);
                                    EL.SetEnvActiveParam("ChannelModulate", "R");
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
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "X");
                                    EL.SetEnvActiveParam("Diameter", diameter);
                                    EL.SetEnvActiveParam("GratingType", "Sinusoidal");
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
                                case 4:
                                    EL.SetExParam("ColorSpace", "HSL");
                                    EL.SetExParam("Color", "RBYm");
                                    EL.SetEnvActiveParam("GratingType", "Square");
                                    break;

                            }
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            if (ExRepeat < 5)
                            {
                                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                            }
                            else
                            {
                                StartStopExperimentSession(false);
                                exmanager.uicontroller.IsFullViewport = false;
                                exmanager.uicontroller.IsGuideOn = true;
                                return;
                            }
                        }
                        break;
                }
                break;
        }
    }
}

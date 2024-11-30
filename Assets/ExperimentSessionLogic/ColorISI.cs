/*
ColorISI.cs is part of the Experica.
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
using Experica;
using Experica.Command;

public class ColorISI : ExperimentSessionLogic
{
    protected Eye eye = Eye.Right;

    protected override void OnExperimentSessionStarted()
    {
        ExperimentID = "ConditionTest";
    }

    protected override void OnExperimentSessionStopped()
    {
        ExperimentID = "ConditionTest";
    }

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

                    ExperimentID = "ISICycleOri";
                }
                break;
            case "ISICycleOri":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            switch (ExRepeat)
                            {
                                case 0:
                                    EL.SetExParam("Eye", eye);
                                    EL.SetExParam("ColorSpace", "DKL");
                                    EL.SetExParam("Color", "X");
                                    exmgr.appmgr.FullViewportSize();
                                    break;
                            }
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            if (ExRepeat < 1)
                            {
                                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                            }
                            else
                            {
                                ExperimentID = "ISIEpochOri8";
                            }
                        }
                        break;
                }
                break;
            case "ISIEpochOri8":
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
                                    exmgr.appmgr.FullViewportSize();
                                    break;

                            }
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            if (ExRepeat < 1)
                            {
                                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                            }
                            else
                            {
                                ExperimentID = "ISICycle2Color";
                            }
                        }
                        break;
                }
                break;
            case "ISICycle2Color":
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
                                    EL.SetExParam("ModulateParam", "ModulateTime");
                                    EL.SetExParam("CycleDirection", 1f);
                                    exmgr.appmgr.FullViewportSize();
                                    EL.SetEnvActiveParam("GratingType", "Sinusoidal");
                                    EL.SetEnvActiveParam("ModulateGratingType", "Sinusoidal");
                                    EL.SetEnvActiveParam("SpatialPhase", 0.75);
                                    break;
                                case 1:
                                    EL.SetExParam("ColorSpace", "LMS");
                                    EL.SetExParam("Color", "X");
                                    break;
                                case 2:
                                    EL.SetExParam("ColorSpace", "LMS");
                                    EL.SetExParam("Color", "Y");
                                    break;
                                case 3:
                                    EL.SetExParam("ColorSpace", "LMS");
                                    EL.SetExParam("Color", "Z");
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

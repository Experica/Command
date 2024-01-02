/*
BOShadowV1.cs is part of the Experica.
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

public class BOShadowV1 : ExperimentSessionLogic
{
    float diameter = 3;

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
                    exmanager.uicontroller.IsGuideOn = exsession.IsGuideOn;
                    exmanager.uicontroller.FullScreen = exsession.IsFullScreen;
                    exmanager.uicontroller.IsFullViewport = exsession.IsFullViewport;
                    EL.SetExParam("NotifyExperimenter", exsession.NotifyExperimenter);
                    diameter = EL.GetEnvActiveParam<float>("Diameter");

                    ExperimentID = "RFBar4Deg";
                }
                break;
            case "RFBar4Deg":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            ExperimentID = "OriSFSquareGrating";
                        }
                        break;
                }
                break;
            case "OriSFSquareGrating":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            EL.SetEnvActiveParam("Diameter", diameter);
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            ExperimentID = "BOImage";
                        }
                        break;
                }
                break;
            case "BOImage":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            exmanager.uicontroller.ViewportSize();
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            ExperimentID = "DynamicAdelsonFast";
                        }
                        break;
                }
                break;
            case "DynamicAdelsonFast":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            exmanager.uicontroller.ViewportSize();
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            ExperimentID = "ContralAdelson";
                        }
                        break;
                }
                break;
            case "ContralAdelson":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            exmanager.uicontroller.ViewportSize();
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            ExperimentID = "StaticAdelson";
                        }
                        break;
                }
                break;
            case "StaticAdelson":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            exmanager.uicontroller.ViewportSize();
                            StartExperiment();
                        }
                        break;
                    case EXPERIMENTSTATUS.STOPPED:
                        if (SinceExStop > exsession.StopWait)
                        {
                            StartStopExperimentSession(false);
                            exmanager.uicontroller.IsFullViewport = false;
                            exmanager.uicontroller.IsGuideOn = true;
                        }
                        break;
                }
                break;
        }
    }
}

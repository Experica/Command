using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Experica;
using Experica.Command;

public class BOShadowV1 : ExperimentSessionLogic
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

                    ExperimentID = "RFBar4Deg";
                }
                break;
            case "RFBar4Deg":
                switch (ExperimentStatus)
                {
                    case EXPERIMENTSTATUS.NONE:
                        if (SinceExReady > exsession.ReadyWait)
                        {
                            EL.SetExParam("Log", log);
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
                            EL.SetExParam("Log", log);
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
                            EL.SetExParam("Log", log);
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
                            EL.SetExParam("Log", log);
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
                            EL.SetExParam("Log", log);
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
                            EL.SetExParam("Log", log);
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
                            return;
                        }
                        break;
                }
                break;
        }
    }
}

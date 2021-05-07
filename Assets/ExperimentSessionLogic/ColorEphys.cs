using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Experica;
using Experica.Command;

public class ColorEphys : ExperimentSessionLogic
{
    float diameter = 3;

    protected override void Logic()
    {
        switch (ExperimentID)
        {
            case null:
                if(exsession.IsFullScreen && !Screen.fullScreen)
                {
                    exmanager.uicontroller.ToggleFullScreen();   
                }
                if (exsession.IsFullViewport && !exmanager.uicontroller.IsFullViewport)
                {
                    exmanager.uicontroller.ToggleFullViewport();
                }
                if (exsession.IsGuideOn && !exmanager.uicontroller.IsGuideOn)
                {
                    exmanager.uicontroller.ToggleGuide();
                }
                ExperimentID = "ConditionTest";
                break;
            case "ConditionTest":
                if (SinceExReady > exsession.ReadyWait)
                {
                    diameter = EL.GetEnvActiveParam<float>("Diameter");
                    exsession.Experimenter = EL.GetExParam<string>("Experimenter");
                    exsession.SendMail = EL.GetExParam<bool>("SendMail");
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
                                ExperimentID = "OriSF";
                            }
                        }
                        break;
                }
                break;
            //case "Color":
            //    switch (ExperimentStatus)
            //    {
            //        case EXPERIMENTSTATUS.NONE:
            //            if (SinceExReady > exsession.ReadyWait)
            //            {
            //                switch (ExRepeat)
            //                {
            //                    case 0:
            //                        EL.SetExParam("ColorSpace", "DKL");
            //                        EL.SetExParam("Color", "Hue_L0");
            //                        EL.SetEnvActiveParam("Diameter", diameter);
            //                        break;
            //                    case 1:
            //                        EL.SetExParam("ColorSpace", "HSL");
            //                        EL.SetExParam("Color", "Hue_Ym");
            //                        break;

            //                }
            //                StartExperiment();
            //            }
            //            break;
            //        case EXPERIMENTSTATUS.STOPPED:
            //            if (SinceExStop > exsession.StopWait)
            //            {
            //                if (ExRepeat < 2)
            //                {
            //                    ExperimentStatus = EXPERIMENTSTATUS.NONE;
            //                }
            //                else
            //                {
            //                    ExperimentID = "CycleColorPlane";
            //                }
            //            }
            //            break;
            //    }
            //    break;
            //case "CycleColorPlane":
            //    switch (ExperimentStatus)
            //    {
            //        case EXPERIMENTSTATUS.NONE:
            //            if (SinceExReady > exsession.ReadyWait)
            //            {
            //                switch (ExRepeat)
            //                {
            //                    case 0:
            //                        EL.SetExParam("ModulateParam", "DKLIsoLum");
            //                        EL.SetExParam("CycleDirection", 1f);
            //                        EL.SetEnvActiveParam("Diameter", diameter);
            //                        break;
            //                    case 1:
            //                        EL.SetExParam("CycleDirection", -1f);
            //                        break;
            //                    case 2:
            //                        EL.SetExParam("ModulateParam", "DKLIsoLM");
            //                        break;
            //                    case 3:
            //                        EL.SetExParam("CycleDirection", 1f);
            //                        break;
            //                    case 4:
            //                        EL.SetExParam("ModulateParam", "DKLIsoSLM");
            //                        break;
            //                    case 5:
            //                        EL.SetExParam("CycleDirection", -1f);
            //                        break;

            //                }
            //                StartExperiment();
            //            }
            //            break;
            //        case EXPERIMENTSTATUS.STOPPED:
            //            if (SinceExStop > exsession.StopWait)
            //            {
            //                if (ExRepeat < 6)
            //                {
            //                    ExperimentStatus = EXPERIMENTSTATUS.NONE;
            //                }
            //                else
            //                {
            //                    ExperimentID = "HartleySubspace";
            //                }
            //            }
            //            break;
            //    }
            //    break;
            //case "HartleySubspace":
            //    switch (ExperimentStatus)
            //    {
            //        case EXPERIMENTSTATUS.NONE:
            //            if (SinceExReady > exsession.ReadyWait)
            //            {
            //                switch (ExRepeat)
            //                {
            //                    case 0:
            //                        EL.SetExParam("ColorSpace", "DKL");
            //                        EL.SetExParam("Color", "X");
            //                        EL.SetEnvActiveParam("Diameter", diameter);
            //                        break;
            //                    case 1:
            //                        EL.SetExParam("ColorSpace", "LMS");
            //                        EL.SetExParam("Color", "Xmcc");
            //                        break;
            //                    case 2:
            //                        EL.SetExParam("Color", "Ymcc");
            //                        break;
            //                    case 3:
            //                        EL.SetExParam("Color", "Zmcc");
            //                        break;

            //                }
            //                StartExperiment();
            //            }
            //            break;
            //        case EXPERIMENTSTATUS.STOPPED:
            //            if (SinceExStop > exsession.StopWait)
            //            {
            //                if (ExRepeat < 4)
            //                {
            //                    ExperimentStatus = EXPERIMENTSTATUS.NONE;
            //                }
            //                else
            //                {
            //                    ExperimentID = "Image";
            //                }
            //            }
            //            break;
            //    }
            //    break;
            //case "Image":
            //    switch (ExperimentStatus)
            //    {
            //        case EXPERIMENTSTATUS.NONE:
            //            if (SinceExReady > exsession.ReadyWait)
            //            {
            //                switch (ExRepeat)
            //                {
            //                    case 0:
            //                        EL.SetExParam("ColorSpace", "DKL");
            //                        EL.SetExParam("Color", "X");
            //                        EL.SetEnvActiveParam("Diameter", diameter);
            //                        break;
            //                    case 1:
            //                        EL.SetExParam("ColorSpace", "LMS");
            //                        EL.SetExParam("Color", "Xmcc");
            //                        break;
            //                    case 2:
            //                        EL.SetExParam("Color", "Ymcc");
            //                        break;
            //                    case 3:
            //                        EL.SetExParam("Color", "Zmcc");
            //                        break;

            //                }
            //                StartExperiment();
            //            }
            //            break;
            //        case EXPERIMENTSTATUS.STOPPED:
            //            if (SinceExStop > exsession.StopWait)
            //            {
            //                if (ExRepeat < 4)
            //                {
            //                    ExperimentStatus = EXPERIMENTSTATUS.NONE;
            //                }
            //                else
            //                {
            //                    ExperimentID = "OriSF";
            //                }
            //            }
            //            break;
            //    }
            //    break;
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
                            if (ExRepeat < 1)
                            {
                                ExperimentStatus = EXPERIMENTSTATUS.NONE;
                            }
                            else
                            {
                                StartStopExperimentSession(false);
                                if (Screen.fullScreen)
                                {
                                    exmanager.uicontroller.ToggleFullScreen();
                                }
                                if (exmanager.uicontroller.IsFullViewport)
                                {
                                    exmanager.uicontroller.ToggleFullViewport();
                                }
                                if (!exmanager.uicontroller.IsGuideOn)
                                {
                                    exmanager.uicontroller.ToggleGuide();
                                }
                                return;
                            }
                        }
                        break;
                }
                break;
        }
    }
}

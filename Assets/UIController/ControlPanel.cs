// --------------------------------------------------------------
// ControlPanel.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VLab
{
    public class ControlPanel : MonoBehaviour
    {
        public VLUIController uimanager;
        public GameObject newexprefab;
        public Canvas canvas;
        public CanvasGroup canvasgroup;
        public Text startstoptext;

        GameObject newexpanel;

        public void NewEx()
        {
            newexpanel = Instantiate(newexprefab);
            newexpanel.name = "NewExperimentPanel";
            newexpanel.transform.SetParent(canvas.transform);
            ((RectTransform)newexpanel.transform).anchoredPosition = new Vector2();
            newexpanel.transform.localScale = new Vector3(1, 1, 1);

            newexpanel.GetComponent<NewExperimentPanel>().uimanager = uimanager;

            canvasgroup.interactable = false;
        }

        public void CancelNewEx()
        {
            Destroy(newexpanel);
            canvasgroup.interactable = true;
        }

        public void OnStartStopEx(bool ison)
        {
            if (ison)
            {
                uimanager.exmanager.el.StartExperiment();
                startstoptext.text = "Stop";
                uimanager.consolepanel.Log("Experiment Started.");
            }
            else
            {
                uimanager.exmanager.el.StopExperiment();
                startstoptext.text = "Start";
                uimanager.consolepanel.Log("Experiment Stoped.");
            }
        }

    }
}
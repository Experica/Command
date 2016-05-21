// --------------------------------------------------------------
// ControlPanel.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VLab
{
    public class ControlPanel : MonoBehaviour
    {
        public VLUIController uicontroller;
        public GameObject newexprefab;
        public Canvas canvas;
        public CanvasGroup panelcontentcanvasgroup, statusbarcanvasgroup;
        public Text startstoptext, pauseresumetext;
        public Toggle pauseresume;
        public Dropdown exdropdown;

        GameObject newexpanel;

        public void NewEx()
        {
            newexpanel = Instantiate(newexprefab);
            newexpanel.name = "NewExperimentPanel";
            newexpanel.transform.SetParent(canvas.transform);
            ((RectTransform)newexpanel.transform).anchoredPosition = new Vector2();
            newexpanel.transform.localScale = new Vector3(1, 1, 1);

            newexpanel.GetComponent<NewExperimentPanel>().uicontroller = uicontroller;

            panelcontentcanvasgroup.interactable = false;
            statusbarcanvasgroup.interactable = false;
        }

        public void CancelNewEx()
        {
            Destroy(newexpanel);
            panelcontentcanvasgroup.interactable = true;
            statusbarcanvasgroup.interactable = true;
        }

    }
}
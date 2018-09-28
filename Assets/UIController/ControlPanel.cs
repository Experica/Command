/*
ControlPanel.cs is part of the VLAB project.
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
using UnityEngine.UI;
using System.Collections;

namespace IExSys
{
    public class ControlPanel : MonoBehaviour
    {
        public VLUIController uicontroller;
        public GameObject newexpanelprefab;
        GameObject newexpanel;

        public Canvas canvas;
        public CanvasGroup panelcontentcanvasgroup, statusbarcanvasgroup;


        public void NewExPanel()
        {
            newexpanel = Instantiate(newexpanelprefab);
            newexpanel.name = "NewExperimentPanel";
            newexpanel.transform.SetParent(canvas.transform);
            ((RectTransform)newexpanel.transform).anchoredPosition = new Vector2();
            newexpanel.transform.localScale = new Vector3(1, 1, 1);
            newexpanel.GetComponent<NewExperimentPanel>().uicontroller = uicontroller;

            panelcontentcanvasgroup.interactable = false;
            statusbarcanvasgroup.interactable = false;
        }

        public void DeleteExPanel()
        {
            Destroy(newexpanel);
            panelcontentcanvasgroup.interactable = true;
            statusbarcanvasgroup.interactable = true;
        }

    }
}
// -----------------------------------------------------------------------------
// NewExParamPanel.cs is part of the VLAB project.
// Copyright (c) 2016  Li Alex Zhang  fff008@gmail.com
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace VLab
{
    public class NewExParamPanel : MonoBehaviour
    {
        public VLUIController uicontroller;
        public Text namecheck;
        public Button confirm, cancel;
        public InputField nameinput, valueinput;

        public void OnNewExParamName(string name)
        {
            if (Experiment.Properties.ContainsKey(name) || uicontroller.exmanager.el.ex.Param.ContainsKey(name))
            {
                namecheck.text = "Name Already Exists";
                confirm.interactable = false;
            }
            else
            {
                namecheck.text = "";
                confirm.interactable = true;
            }
        }

        public void Confirm()
        {
            var name = nameinput.text;
            var value = valueinput.text;
            uicontroller.expanel.NewExParam(name, value);
            uicontroller.expanel.DeleteExParamPanel();
        }

        public void Cancel()
        {
            uicontroller.expanel.DeleteExParamPanel();
        }
    }
}
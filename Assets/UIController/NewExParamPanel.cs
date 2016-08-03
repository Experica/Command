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
using System;

namespace VLab
{
    public class NewExParamPanel : MonoBehaviour
    {
        public VLUIController uicontroller;
        public Text namecheck,valuecheck;
        public Button confirm, cancel;
        public Dropdown typedropdown;
        public InputField nameinput, valueinput;

        string pname;Param param;bool isvalidname, isvalidvalue;

        void Start()
        {
            UpdateType();
        }

        public void UpdateType()
        {
            typedropdown.AddOptions(typeof(ParamType).GetValue());
        }

        public void OnNewExParamName(string name)
        {
            if (Experiment.Properties.ContainsKey(name) || uicontroller.exmanager.el.ex.Param.ContainsKey(name))
            {
                namecheck.text = "Name Already Exists";
                isvalidname = false;
            }
            else
            {
                namecheck.text = "";
                pname = nameinput.text;
                isvalidname = true;
            }
            if(isvalidname&&isvalidvalue)
            {
                confirm.interactable = true;
            }
            else
            {
                confirm.interactable = false;
            }
        }

        public void OnNewExParamValue(string name)
        {
            var value = valueinput.text;
            var type = typedropdown.captionText.text.Convert<ParamType>();
            try
            {
                param =new Param(type,  value.Convert(type));
                valuecheck.text = "";
                isvalidvalue = true;
            }
            catch (Exception ex)
            {
                valuecheck.text = "Invalid Value Format";
                isvalidvalue = false;
            }
            if (isvalidname && isvalidvalue)
            {
                confirm.interactable = true;
            }
            else
            {
                confirm.interactable = false;
            }
        }

        public void Confirm()
        {
            uicontroller.expanel.NewExParam(pname, param);
            uicontroller.expanel.DeleteExParamPanel();
        }

        public void Cancel()
        {
            uicontroller.expanel.DeleteExParamPanel();
        }
    }
}
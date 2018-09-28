/*
ConditionTestPanel.cs is part of the VLAB project.
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
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;

namespace IExSys
{
    public class ConditionTestPanel : MonoBehaviour
    {
        public VLUIController uicontroller;
        public GameObject ctcontent, ctheadcontent, blueheadertextprefab, redheadertextprefab,
            yellowheadertextprefab, greenheadertextprefab, textprefab;
        GridLayoutGroup grid; float ctcontentheight, textheight;
        Text cti, ci, cr, bi, br;

        void Start()
        {
            var yellowheadertext = Instantiate(yellowheadertextprefab);
            yellowheadertext.name = "CondTestIndex";
            yellowheadertext.GetComponentInChildren<Text>().text = "CondTestIndex";
            yellowheadertext.transform.SetParent(ctheadcontent.transform, false);

            var redheadertext = Instantiate(redheadertextprefab);
            redheadertext.name = "CondIndex";
            redheadertext.GetComponentInChildren<Text>().text = "CondIndex";
            redheadertext.transform.SetParent(ctheadcontent.transform, false);

            var buleheadertext = Instantiate(blueheadertextprefab);
            buleheadertext.name = "CondRepeat";
            buleheadertext.GetComponentInChildren<Text>().text = "CondRepeat";
            buleheadertext.transform.SetParent(ctheadcontent.transform, false);

            var greenheadertext = Instantiate(greenheadertextprefab);
            greenheadertext.name = "BlockIndex";
            greenheadertext.GetComponentInChildren<Text>().text = "BlockIndex";
            greenheadertext.transform.SetParent(ctheadcontent.transform, false);

            buleheadertext = Instantiate(blueheadertextprefab);
            buleheadertext.name = "BlockRepeat";
            buleheadertext.GetComponentInChildren<Text>().text = "BlockRepeat";
            buleheadertext.transform.SetParent(ctheadcontent.transform, false);

            grid = ctcontent.GetComponent<GridLayoutGroup>();
            grid.constraintCount = 5;
            ctcontentheight = (ctcontent.transform.parent as RectTransform).rect.height;

            cti = AddText("");
            ci = AddText("");
            cr = AddText("");
            bi = AddText("");
            br = AddText("");
            textheight = cti.fontSize + 3;
        }

        public void StartCondTest()
        {
            var ctm = uicontroller.exmanager.el.condtestmanager;
            var showlevel = uicontroller.exmanager.el.ex.CondTestShowLevel;
            switch (showlevel)
            {
                case CONDTESTSHOWLEVEL.FULL:
                    cti.text = cti.text + (ctm.CondTestIndex - 1).ToString() + "\n";
                    ci.text = ci.text + ctm.condtest[CONDTESTPARAM.CondIndex].Last().ToString() + "\n";
                    cr.text = cr.text + ctm.condtest[CONDTESTPARAM.CondRepeat].Last().ToString() + "\n";
                    if (uicontroller.exmanager.el.condmanager.nblock > 1)
                    {
                        bi.text = bi.text + ctm.condtest[CONDTESTPARAM.BlockIndex].Last().ToString() + "\n";
                        br.text = br.text + ctm.condtest[CONDTESTPARAM.BlockRepeat].Last().ToString() + "\n";
                    }

                    UpdateViewRect(ctm.CondTestIndex);
                    return;
                case CONDTESTSHOWLEVEL.SHORT:
                    cti.text = (ctm.CondTestIndex - 1).ToString();
                    ci.text = ctm.condtest[CONDTESTPARAM.CondIndex].Last().ToString();
                    cr.text = ctm.condtest[CONDTESTPARAM.CondRepeat].Last().ToString();
                    if (uicontroller.exmanager.el.condmanager.nblock > 1)
                    {
                        bi.text = ctm.condtest[CONDTESTPARAM.BlockIndex].Last().ToString();
                        br.text = ctm.condtest[CONDTESTPARAM.BlockRepeat].Last().ToString();
                    }
                    return;
            }
        }

        Text AddText(string value)
        {
            var textvalue = Instantiate(textprefab);
            var tt = textvalue.GetComponent<Text>();
            tt.text = value;
            textvalue.transform.SetParent(ctcontent.transform, false);
            return tt;
        }

        public void ClearCondTest()
        {
            cti.text = "";
            ci.text = "";
            cr.text = "";
            bi.text = "";
            br.text = "";
            var rt = (RectTransform)ctcontent.transform;
            grid.cellSize = ctheadcontent.GetComponent<GridLayoutGroup>().cellSize;
            rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * grid.constraintCount, grid.cellSize.y + grid.spacing.y);
            rt.anchoredPosition = new Vector2(0, 0);
        }

        public void UpdateViewRect(int ctn)
        {
            var cn = grid.constraintCount;
            var rn = 1;
            var rt = (RectTransform)ctcontent.transform;
            grid.cellSize = new Vector2(grid.cellSize.x, textheight * ctn);
            var dw = (grid.cellSize.x + grid.spacing.x) * cn;
            var dh = (grid.cellSize.y + grid.spacing.y) * rn;
            rt.sizeDelta = new Vector2(dw, dh);

            if (dh > ctcontentheight)
            {
                rt.anchoredPosition = new Vector2(0, dh - ctcontentheight);
            }
        }
    }
}
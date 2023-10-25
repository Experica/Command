/*
ConditionTestPanel.cs is part of the Experica.
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

namespace Experica.Command
{
    public class ConditionTestPanel : MonoBehaviour
    {
        public UIController uicontroller;
        public GameObject ctcontent, ctheadcontent, blueheadertextprefab, redheadertextprefab,
            yellowheadertextprefab, greenheadertextprefab, textprefab;
        GridLayoutGroup grid; float ctcontentheight, textheight;
        Text cti, ci, cr, bi, br;
        int condtestidx = -1;

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

        public void PushCondTest()
        {
            var ctm = uicontroller.exmanager.el.condtestmanager;
            if (ctm.CondTestIndex <= condtestidx)
            {
                return;
            }
            else
            {
                condtestidx = ctm.CondTestIndex;
            }
            var showlevel = uicontroller.exmanager.el.ex.CondTestShowLevel;
            var condindexstr = ""; var condrepeatstr = ""; var blockindexstr = ""; var blockrepeatstr = "";
            switch (showlevel)
            {
                case CONDTESTSHOWLEVEL.FULL:
                    cti.text = cti.text + condtestidx.ToString() + "\n";
                    if (ctm.condtest.ContainsKey(CONDTESTPARAM.CondIndex) && ctm.condtest[CONDTESTPARAM.CondIndex].Count > condtestidx)
                    {
                        condindexstr = ctm.condtest[CONDTESTPARAM.CondIndex][condtestidx].ToString();
                    }
                    ci.text = ci.text + condindexstr + "\n";
                    if (ctm.condtest.ContainsKey(CONDTESTPARAM.CondRepeat) && ctm.condtest[CONDTESTPARAM.CondRepeat].Count > condtestidx)
                    {
                        condrepeatstr = ctm.condtest[CONDTESTPARAM.CondRepeat][condtestidx].ToString();
                    }
                    cr.text = cr.text + condrepeatstr + "\n";
                    if (uicontroller.exmanager.el.condmanager.nblock > 1)
                    {
                        if (ctm.condtest.ContainsKey(CONDTESTPARAM.BlockIndex) && ctm.condtest[CONDTESTPARAM.BlockIndex].Count > condtestidx)
                        {
                            blockindexstr = ctm.condtest[CONDTESTPARAM.BlockIndex][condtestidx].ToString();
                        }
                        bi.text = bi.text + blockindexstr + "\n";
                        if (ctm.condtest.ContainsKey(CONDTESTPARAM.BlockRepeat) && ctm.condtest[CONDTESTPARAM.BlockRepeat].Count > condtestidx)
                        {
                            blockrepeatstr = ctm.condtest[CONDTESTPARAM.BlockRepeat][condtestidx].ToString();
                        }
                        br.text = br.text + blockrepeatstr + "\n";
                    }
                    UpdateViewRect(condtestidx + 1);
                    return;
                case CONDTESTSHOWLEVEL.SHORT:
                    cti.text = condtestidx.ToString();
                    if (ctm.condtest.ContainsKey(CONDTESTPARAM.CondIndex) && ctm.condtest[CONDTESTPARAM.CondIndex].Count > condtestidx)
                    {
                        condindexstr = ctm.condtest[CONDTESTPARAM.CondIndex][condtestidx].ToString();
                    }
                    ci.text = condindexstr;
                    if (ctm.condtest.ContainsKey(CONDTESTPARAM.CondRepeat) && ctm.condtest[CONDTESTPARAM.CondRepeat].Count > condtestidx)
                    {
                        condrepeatstr = ctm.condtest[CONDTESTPARAM.CondRepeat][condtestidx].ToString();
                    }
                    cr.text = condrepeatstr;
                    if (uicontroller.exmanager.el.condmanager.nblock > 1)
                    {
                        if (ctm.condtest.ContainsKey(CONDTESTPARAM.BlockIndex) && ctm.condtest[CONDTESTPARAM.BlockIndex].Count > condtestidx)
                        {
                            blockindexstr = ctm.condtest[CONDTESTPARAM.BlockIndex][condtestidx].ToString();
                        }
                        bi.text = blockindexstr;
                        if (ctm.condtest.ContainsKey(CONDTESTPARAM.BlockRepeat) && ctm.condtest[CONDTESTPARAM.BlockRepeat].Count > condtestidx)
                        {
                            blockrepeatstr = ctm.condtest[CONDTESTPARAM.BlockRepeat][condtestidx].ToString();
                        }
                        br.text = blockrepeatstr;
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
            condtestidx = -1;
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
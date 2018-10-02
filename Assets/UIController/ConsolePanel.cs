/*
ConsolePanel.cs is part of the Experica.
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
using System;
using System.Collections.Generic;

namespace Experica.Command
{
    public enum LogType
    {
        Log,
        Warning,
        Error
    }

    public class ConsolePanel : MonoBehaviour
    {
        public UIController uicontroller;
        public GameObject content, logprefab, warningprefab, errorprefab;

        int maxentry;
        int entrycount;

        public void Clear()
        {
            for (var i = 0; i < content.transform.childCount; i++)
            {
                Destroy(content.transform.GetChild(i).gameObject);
            }
            entrycount = 0;
            UpdateViewRect();
        }

        public void LogError(object msg, bool istimestamp = true)
        {
            Log(LogType.Error, msg, istimestamp);
        }

        public void LogWarn(object msg, bool istimestamp = true)
        {
            Log(LogType.Warning, msg, istimestamp);
        }

        public void Log(object msg, bool istimestamp = true)
        {
            Log(LogType.Log, msg, istimestamp);
        }

        public void Log(LogType logtype, object msg, bool istimestamp = true)
        {
            var v = msg.Convert<string>();
            if (istimestamp)
            {
                v = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + ":  " + v ;
            }
            
            var text = Instantiate(ChoosePrefab(logtype));
            text.GetComponent<Text>().text = v;
            text.transform.SetParent(content.transform,false);

            if (entrycount > maxentry)
            {
                Destroy(content.transform.GetChild(0).gameObject);
            }
            else
            {
                entrycount++;
                UpdateViewRect();
            }
        }

        public void UpdateViewRect()
        {
            var grid = content.GetComponent<GridLayoutGroup>();
            var rt = (RectTransform)content.transform;
            grid.cellSize = new Vector2(rt.rect.width, grid.cellSize.y);
            rt.sizeDelta = new Vector2(0, entrycount * (grid.cellSize.y + grid.spacing.y));
        }

        GameObject ChoosePrefab(LogType logtype)
        {
            switch(logtype)
            {
                case LogType.Warning:
                    return warningprefab;
                case LogType.Error:
                    return errorprefab;
                default:
                    return logprefab;
            }
        }

        void Awake()
        {
            maxentry = uicontroller.config.MaxLogEntry;
        }

    }
}
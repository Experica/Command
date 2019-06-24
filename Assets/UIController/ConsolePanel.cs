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

namespace Experica.Command
{

    public class ConsolePanel : MonoBehaviour
    {
        public UIController uicontroller;
        public GameObject content, logprefab, warningprefab, errorprefab ;

        public int maxentry;
        int entrycount;

        /// <summary>
        /// A log handler is added to the Unity application that calls ConsolePanel.Log() for every Debug message.
        /// The handler is added whent the consolepanel is createdt
        /// </summary>
        void Awake()
        {
            // logMessageReceived is an event that is called when messages are logged, which a handler is added to.
            Application.logMessageReceived += delegate(string logString, string stackTrace, LogType type)
            {
                Log(type, logString, true);
            };
        }

        /// <summary>
        /// Remove all the text prefab objects from the content box in the consolePanel. Basically,
        /// Get rid of all the text. This is called when the Clear button is pressed.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < content.transform.childCount; i++)
            {
                Destroy(content.transform.GetChild(i).gameObject);
            }
            entrycount = 0;
            UpdateViewRect();
        }

        /// <summary>
        /// Adds a text GameObject to the content GameObject in the Console Window.
        /// </summary>
        /// <param name="logtype">log, Warning, Error, Assert, or Exception</param>
        /// <param name="msg">Text to be displayed</param>
        /// <param name="istimestamp">True displays time, False doesn't</param>
        public void Log(LogType logtype, object msg, bool istimestamp = true)
        {
            var v = msg.Convert<string>();
            v = v.Replace('\n', ' ');               // With wrapping text and newline characters, text is small. Replace with just a space

            // Add time to text
            if (istimestamp)
            {
                v = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + ":  " + v;
            }

            // Add Text to GameObject
            var text = Instantiate(ChoosePrefab(logtype));
            text.GetComponent<Text>().text = v;
            text.transform.SetParent(content.transform, false);

            // Delete 
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
            switch (logtype)
            {
                case LogType.Warning:
                    return warningprefab;
                case LogType.Error:
                    return errorprefab;
                case LogType.Assert:
                    return warningprefab;
                case LogType.Exception:
                    return errorprefab;
                default:
                    return logprefab;
            }
        }

    }
}
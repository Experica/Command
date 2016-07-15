// --------------------------------------------------------------
// ConsolePanel.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace VLab
{
    public enum VLLogType
    {
        Log,
        Warning,
        Error
    }

    public class ConsolePanel : MonoBehaviour
    {
        public VLUIController uicontroller;
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
            Log(VLLogType.Error, msg, istimestamp);
        }

        public void LogWarn(object msg, bool istimestamp = true)
        {
            Log(VLLogType.Warning, msg, istimestamp);
        }

        public void Log(object msg, bool istimestamp = true)
        {
            Log(VLLogType.Log, msg, istimestamp);
        }

        public void Log(VLLogType logtype, object msg, bool istimestamp = true)
        {
            var v = msg.Convert<string>();
            if (istimestamp)
            {
                v = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + ":  " + v ;
            }
            
            var text = Instantiate(ChoosePrefab(logtype));
            text.GetComponent<Text>().text = v;
            text.transform.SetParent(content.transform);
            text.transform.localScale = new Vector3(1, 1, 1);

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

        GameObject ChoosePrefab(VLLogType logtype)
        {
            switch(logtype)
            {
                case VLLogType.Warning:
                    return warningprefab;
                case VLLogType.Error:
                    return errorprefab;
                default:
                    return logprefab;
            }
        }

        void Awake()
        {
            maxentry = (int)uicontroller.appmanager.config[VLCFG.MaxLogEntry];
        }

    }
}
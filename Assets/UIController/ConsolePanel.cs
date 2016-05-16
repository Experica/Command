using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace VLab
{
    public class ConsolePanel : MonoBehaviour
    {
        public VLUIController uimanager;
        public GameObject content, textprefab;
        public int maxentry = 99;
        int entrycount;

        public void Clear()
        {
            for (var i = 0; i < content.transform.childCount; i++)
            {
                Destroy(content.transform.GetChild(i).gameObject);
            }
            entrycount = 0;
        }

        public void Log(object msg, bool istimestamp = true)
        {
            var grid = content.GetComponent<GridLayoutGroup>();
            var rt = (RectTransform)content.transform;
            grid.cellSize = new Vector2(rt.rect.width, grid.cellSize.y);
            if (entrycount > maxentry)
            {
                Destroy(content.transform.GetChild(0).gameObject);
            }
            else
            {
                entrycount++;
            }
            var t = Instantiate(textprefab);
            var v = (string)VLConvert.Convert(msg, typeof(string));
            if (istimestamp)
            {
                v = DateTime.Now.ToString("MM/dd/yyyy HH:mm") + ":  " + v;
            }
            t.GetComponent<Text>().text = v;
            t.transform.SetParent(content.transform);
            t.transform.localScale = new Vector3(1, 1, 1);

            rt.sizeDelta = new Vector2(rt.sizeDelta.x, entrycount * (grid.cellSize.y + grid.spacing.y));
        }

    }
}
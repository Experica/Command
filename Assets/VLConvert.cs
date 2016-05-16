using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace VLab
{
    public static class VLConvert
    {
        public static object Convert(object value, Type T)
        {
            object v = 0;
            var vt = value.GetType();
            if (T == typeof(Vector3))
            {
                if (vt == typeof(Vector3))
                {
                    return value;
                }
                else if (vt == typeof(string))
                {
                    var t = value as string;
                    var vs = t.Substring(1, t.Length - 2).Split(',');
                    v = new Vector3(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]));
                }
                else
                {

                }
            }
            else if (T == typeof(Color))
            {
                if (vt == typeof(Color))
                {
                    return value;
                }
                if (vt == typeof(string))
                {
                    var t = value as string;
                    var vs = t.Substring(t.IndexOf('(') + 1, t.Length - 2).Split(',');
                    v = new Color(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]), float.Parse(vs[3]));
                }
                else
                {

                }
            }
            else if (T == typeof(string))
            {
                if (vt == typeof(Vector3))
                {
                    v = ((Vector3)value).ToString("G3");
                }
                else if (vt == typeof(Dictionary<string, object>))
                {
                    v = value.ToString();
                }
                else if (vt == typeof(List<string>))
                {
                    v = value.ToString();
                }
                else if (vt == typeof(Color))
                {
                    v = ((Color)value).ToString("G3").Substring(4);
                }
                else
                {
                    v = value.ToString();
                }
            }
            else if (T == typeof(Showroom.ItemID))
            {
                v = Enum.Parse(T, (string)value);
            }
            else
            {
                v = System.Convert.ChangeType(value, T);
            }
            return v;
        }
    }
}

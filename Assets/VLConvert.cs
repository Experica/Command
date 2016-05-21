// --------------------------------------------------------------
// VLConvert.cs is part of the VLAB project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-21-2016
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace VLab
{
    public class VLConvert
    {
        public static T Convert<T>(object value)
        {
            return (T)Convert(value, typeof(T));
        }

        public static object Convert(object value, Type T)
        {
            Type VT = value.GetType();
            if (T == typeof(Vector3))
            {
                if (VT == typeof(string))
                {
                    var t = (string)value;
                    var vs = t.Substring(1, t.Length - 2).Split(',');
                   return new Vector3(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]));
                }
                else
                {
                    return System.Convert.ChangeType(value, T);
                }
            }
            else if (T == typeof(Color))
            {
                if (VT == typeof(string))
                {
                    var t = (string)value;
                    var vs = t.Substring(t.IndexOf('(') + 1, t.Length - 2).Split(',');
                    return new Color(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]), float.Parse(vs[3]));
                }
                else
                {
                    return System.Convert.ChangeType(value, T);
                }
            }
            else if (T == typeof(string))
            {
                if (VT == typeof(Vector3))
                {
                    return ((Vector3)value).ToString("G3");
                }
                else if (VT == typeof(Color))
                {
                    return ((Color)value).ToString("G3").Substring(4);
                }
                else
                {
                    return value.ToString();
                }
            }
            else if (T.IsEnum)
            {
                if (Enum.IsDefined(T, value))
                {
                    if (VT == typeof(string))
                    {
                        return Enum.Parse(T, (string)value);
                    }
                    else
                    {
                        return value;
                    }
                }
                else
                {
                    return Activator.CreateInstance(T);
                }
            }
            else
            {
                return System.Convert.ChangeType(value, T);
            }
        }

    }

}

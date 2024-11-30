/*
ConditionTestManager.cs is part of the Experica.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using MathNet.Numerics.Random;
using MathNet.Numerics;

namespace Experica
{
    public class ConditionTestManager
    {
        public Dictionary<string, IList> CondTest { get; } = new();
        public int CondTestIndex { get; private set; } = -1;
        public Action OnNotifyUI, OnClear;

        public Func<CONDTESTPARAM, List<object>, bool> OnNotifyCondTest;
        public Func<double, bool> OnNotifyCondTestEnd;
        int notifiedidx = -1;
        public int NotifiedIndex { get { return notifiedidx; } }


        public void Clear()
        {
            CondTest.Clear();
            CondTestIndex = -1;
            notifiedidx = -1;
            OnClear?.Invoke();
        }

        public void NewCondTest() => CondTestIndex++;

        public void NewCondTest(double starttime, List<CONDTESTPARAM> notifyparam, int notifypercondtest = 0, bool pushall = false, bool notifyui = true)
        {
            PushCondTest(starttime, notifyparam, notifypercondtest, pushall, notifyui);
            CondTestIndex++;
        }

        public void PushCondTest(double pushtime, List<CONDTESTPARAM> notifyparam, int notifypercondtest = 0, bool pushall = false, bool notifyUI = true)
        {
            if (CondTestIndex < 0) { return; }
            if (notifyUI && OnNotifyUI != null) { OnNotifyUI(); }
            //if (notifypercondtest > 0 && OnNotifyCondTest != null && OnNotifyCondTestEnd != null)
            //{
            //    if (!pushall)
            //    {
            //        if (((CondTestIndex - notifiedidx) / notifypercondtest) >= 1)
            //        {
            //            if (NotifyCondTestAndEnd(notifiedidx + 1, notifyparam, pushtime))
            //            {
            //                notifiedidx = CondTestIndex;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        if (NotifyCondTestAndEnd(notifiedidx + 1, notifyparam, pushtime))
            //        {
            //            notifiedidx = CondTestIndex;
            //        }
            //    }
            //}
        }

        bool NotifyCondTest(int startidx, List<CONDTESTPARAM> notifyparam)
        {
            var hr = false;
            //if (startidx >= 0 && startidx <= CondTestIndex && OnNotifyCondTest != null)
            //{
            //    var t = new List<bool>();
            //    foreach (var p in notifyparam)
            //    {
            //        if (CondTest.ContainsKey(p))
            //        {
            //            var vs = CondTest[p];
            //            // notify condtest range should have rectangle shape
            //            for (var i = vs.Count; i <= CondTestIndex; i++)
            //            {
            //                vs.Add(null);
            //            }
            //            t.Add(OnNotifyCondTest(p, vs.GetRange(startidx, CondTestIndex - startidx + 1)));
            //        }
            //    }
            //    hr = t.Count == 0 ? false : t.All(i => i);
            //}
            return hr;
        }

        bool NotifyCondTestAndEnd(int startidx, List<CONDTESTPARAM> notifyparam, double notifytime)
        {
            return NotifyCondTest(startidx, notifyparam) && OnNotifyCondTestEnd != null && OnNotifyCondTestEnd(notifytime);
        }


        public Dictionary<string, object> this[int condtestindex] => CondTest.ToDictionary(kv => kv.Key, kv => kv.Value[condtestindex]);

        public Dictionary<string, object> CurrentCondTest => this[CondTestIndex];

        // because we need null to represent missing value, here we use object(boxing of value type) instead of Nullable<T> where T : stuct(double boxing)
        /// <summary>
        /// Add value to current condtest for a parameter, fill null for any previous condtest missing value of the parameter
        /// </summary>
        /// <param name="paramname"></param>
        /// <param name="paramvalue"></param>
        public void Add(string paramname, object paramvalue)
        {
            if (CondTestIndex < 0) { return; }
            if (CondTest.ContainsKey(paramname))
            {
                var vs = CondTest[paramname];
                for (var i = vs.Count; i < CondTestIndex; i++)
                {
                    vs.Add(null);
                }
                vs.Add(paramvalue);
            }
            else
            {
                var vs = new List<object>();
                for (var i = 0; i < CondTestIndex; i++)
                {
                    vs.Add(null);
                }
                vs.Add(paramvalue);
                CondTest[paramname] = vs;
            }
        }

        /// <summary>
        /// Add value to the list of current condtest for a parameter, fill null for any previous condtest missing list of the parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paramname"></param>
        /// <param name="listvalue"></param>
        public void AddInList<T>(string paramname, T listvalue)
        {
            if (CondTestIndex < 0) { return; }
            if (CondTest.ContainsKey(paramname))
            {
                var vs = CondTest[paramname];
                for (var i = vs.Count; i < CondTestIndex; i++)
                {
                    vs.Add(null);
                }
                if (vs.Count < (CondTestIndex + 1))
                {
                    vs.Add(new List<T>() { listvalue });
                }
                else
                {
                    var lvs = (List<T>)vs[CondTestIndex];
                    lvs.Add(listvalue);
                }
            }
            else
            {
                var vs = new List<List<T>>();
                for (var i = 0; i < CondTestIndex; i++)
                {
                    vs.Add(null);
                }
                vs.Add(new List<T>() { listvalue });
                CondTest[paramname] = vs;
            }
        }

        /// <summary>
        /// Add key:value pair to the list of current condtest for a parameter, fill null for any previous condtest missing list of the parameter
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="paramname"></param>
        /// <param name="listkey"></param>
        /// <param name="listvalue"></param>
        public void AddInList<TKey, TValue>(string paramname, TKey listkey, TValue listvalue)
        {
            if (CondTestIndex < 0) { return; }
            if (CondTest.ContainsKey(paramname))
            {
                var vs = CondTest[paramname];
                for (var i = vs.Count; i < CondTestIndex; i++)
                {
                    vs.Add(null);
                }
                if (vs.Count < (CondTestIndex + 1))
                {
                    vs.Add(new List<Dictionary<TKey, TValue>>() { new() { [listkey] = listvalue } });
                }
                else
                {
                    var lvs = (List<Dictionary<TKey, TValue>>)vs[CondTestIndex];
                    lvs.Add(new Dictionary<TKey, TValue>() { [listkey] = listvalue });
                }
            }
            else
            {
                var vs = new List<List<Dictionary<TKey, TValue>>>();
                for (var i = 0; i < CondTestIndex; i++)
                {
                    vs.Add(null);
                }
                vs.Add(new List<Dictionary<TKey, TValue>>() { new() { [listkey] = listvalue } });
                CondTest[paramname] = vs;
            }
        }

    }

}

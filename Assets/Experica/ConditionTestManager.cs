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
        public Dictionary<CONDTESTPARAM, IList> CondTest { get; } = new();
        public Func<CONDTESTPARAM, List<object>, bool> OnNotifyCondTest;
        public Func<double, bool> OnNotifyCondTestEnd;
        public Action PushUICondTest, OnClearCondTest;

        int notifiedidx = -1;
        public int NotifiedIndex { get { return notifiedidx; } }
        public int CondTestIndex { get; private set; } = -1;


        public void Clear()
        {
            CondTest.Clear();
            CondTestIndex = -1;
            notifiedidx = -1;
            OnClearCondTest?.Invoke();
        }

        public void NewCondTest(double starttime, List<CONDTESTPARAM> notifyparam, int notifypercondtest = 0, bool pushall = false, bool notifyui = true)
        {
            PushCondTest(starttime, notifyparam, notifypercondtest, pushall, notifyui);
            CondTestIndex++;
        }

        public void PushCondTest(double pushtime, List<CONDTESTPARAM> notifyparam, int notifypercondtest = 0, bool pushall = false, bool notifyui = true)
        {
            if (CondTestIndex >= 0)
            {
                if (notifyui && PushUICondTest != null) PushUICondTest();
                if (notifypercondtest > 0 && OnNotifyCondTest != null && OnNotifyCondTestEnd != null)
                {
                    if (!pushall)
                    {
                        if (((CondTestIndex - notifiedidx) / notifypercondtest) >= 1)
                        {
                            if (NotifyCondTestAndEnd(notifiedidx + 1, notifyparam, pushtime))
                            {
                                notifiedidx = CondTestIndex;
                            }
                        }
                    }
                    else
                    {
                        if (NotifyCondTestAndEnd(notifiedidx + 1, notifyparam, pushtime))
                        {
                            notifiedidx = CondTestIndex;
                        }
                    }
                }
            }
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

        public Dictionary<CONDTESTPARAM, object> this[int condtestindex]
        {
            get => CondTest.ToDictionary(kv => kv.Key, kv => kv.Value[condtestindex]);
        }

        public Dictionary<CONDTESTPARAM, object> CurrentCondTest => this[CondTestIndex];

        public void Add(CONDTESTPARAM paramname, object paramvalue)
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

        public void AddInList<T>(CONDTESTPARAM paramname, T listvalue)
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

        public void AddInList<TKey, TValue>(CONDTESTPARAM paramname, TKey listkey, TValue listvalue)
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
                    vs.Add(new List<Dictionary<TKey, TValue>>() { new Dictionary<TKey, TValue>() { [listkey] = listvalue } });
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
                vs.Add(new List<Dictionary<TKey, TValue>>() { new Dictionary<TKey, TValue>() { [listkey] = listvalue } });
                CondTest[paramname] = vs;
            }
        }

    }

}

/*
ConvertTest.cs is part of the Experica.
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
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Experica.Test
{
    public class ConvertTest
    {
        [Test]
        public void Convert()
        {
            var x = Vector4.one;
            var y = x.Convert<float[]>();
            Debug.Log($"{x.GetType()} To {y.GetType()}: {string.Join(" ", y.Select(i=>i.ToString()))}");   
        }

        class userclass
        {
            public string name { get; set; } = "userclass";
            public float height { get; set; } = 1.8f;
            public Vector3 position { get; set; } = Vector3.one;
        }

        [Test]
        public void ConvertString()
        {
            var str = "[2, 3, 4]";
            //var T = typeof(List<int>);
            var T = typeof(List<>);

            //var str = "[True, False]";
            //var T = typeof(List<bool>);

            //var str = "[0 1 1, 1 0 1]";
            //var T = typeof(List<Vector3>);

            //var str = "[[0 1 1, 1 0 1], [0 1 0, 0 0 1]]";
            //var T = typeof(List<List<Vector3>>);

            //var str = "[[0 1 1, 1 0 1], [0 1, 0 0]]";
            //var T = typeof(List<object>);

            //var str = "{name: userclass, height: 1.8, position: 1 1 1}";
            //var T = typeof(userclass);

            //var str = "{name: 2.2, value: 1.8}";
            ////var T = typeof(Dictionary<string, float>);
            //var T = typeof(IDictionary);

            var cv = str.Convert(T);
            Debug.Log($"Convert \"{str}\" to {T}: \n" +
                $"{cv.GetType()}-{((IList)cv)[0].GetType()}: {cv.Convert<string>()}");
        }

        [Test]
        public void ConvertList()
        {
            var a = typeof(List<>);
            var b = typeof(Dictionary<,>);
            //var list = new List<object>();
            //var T = typeof(string);

            //var list = new List<object>() { 1, 2, 3 };
            //var T = typeof(string);

            var list = new List<float>() { 3.2f, 1.1f, 2f };
            Debug.Log(list.GetType().GetGenericTypeDefinition());

            var dict = new Dictionary<string, float>() { ["first"] = 2.4f, ["last"]=3.5f};
            Debug.Log(dict.GetType().GetGenericTypeDefinition());
            //var T = typeof(Vector3);

            //var v = list.Convert(T);
            //Debug.Log($"Convert {list} to {T}: \n{v.Convert<string>()}");

            //v = "[1,2,3]";
            //var clist = v.Convert<List<object>>();
            //Debug.Log($"Convert Back: \n{clist.Convert<string>()}");
        }
    }
}

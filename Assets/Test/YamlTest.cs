/*
YamlTests.cs is part of the Experica.
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
using System;
using System.Collections;
using System.Collections.Generic;
using Experica.Command;

namespace Experica.Test
{
    public class YamlTest
    {
        string yaml = "Ori: [0, 45, 90, 135]\n" +
             "SpatialPhase: [0, 0.25, 0.5, 0.75]";
        static string datastring, exstring;


        class TestData
        {
            public string name { get; set; } = "TestData";
            public Vector2 vector2 { get; set; } = Vector2.left;
            public Vector3 vector3 { get; set; } = Vector3.left;
            public Vector4 vector4 { get; set; } = Vector4.one;
            public Color color { get; set; } = Color.black;
            public List<Color> colors { get; set; } = new List<Color>() { Color.red, Color.blue };
            public Dictionary<string, object> param { get; set; } = new Dictionary<string, object>()
            {
                ["Color"] = Color.black,
                ["Vector4"] = Vector4.one,
                ["Vector3"] = Vector3.right,
                ["Vector2"] = Vector2.left,
                ["Bool"] = true,
                ["UInt"] = 2,
                ["Int"] = 3,
                ["Float"] = 4.5f,
                ["Double"] = 3.142,
                ["Colors"] = new List<Color>() { Color.gray, Color.green }
            };
        }

        [Test]
        public void YamlWrite()
        {
            var data = new TestData();
            datastring = data.SerializeYaml();
            Debug.Log(datastring);
        }

        [Test]
        public void ExWrite()
        {
            var ex = new Experiment();
            //ex.InitializeDataSource();
            exstring = ex.SerializeYaml();
            Debug.Log(exstring);
        }

        [Test]
        public void YamlRead()
        {
            var data = datastring.DeserializeYaml<TestData>();
            Debug.Log("For params of `typeof(object)`");
            foreach (var p in data.param.Keys)
            { Debug.Log($"{p}({data.param[p].GetType()}): {data.param[p]}"); }

            //var cond = yaml.DeserializeYaml<Dictionary<string, List<object>>>();
        }

        [Test]
        public void ExRead()
        {
            var ex = exstring.DeserializeYaml<Experiment>();
            //ex.InitializeDataSource();
            Debug.Log(ex);
        }
    }
}

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

        [Test]
        public void YamlWrite()
        {
            var data = new Dictionary<string, object>
            {
                ["Color"] = Color.black,
                ["Vector4"] = Vector4.one,
                ["Vector3"] = Vector3.right,
                ["Vector2"] = Vector2.left,
                ["Bool"] = true,
                ["Int"] = 3,
                ["Float"] = 4.5f,
                ["Double"] = 3.142
            };
            datastring = data.SerializeYaml();
            Debug.Log(datastring);
        }

        [Test]
        public void ExWrite()
        {
            var ex = new Experiment();
            ex.InitializeDataSource();
            exstring = ex.SerializeYaml();
            Debug.Log(exstring);
        }

        [Test]
        public void YamlRead()
        {
            var data = datastring.DeserializeYaml<Dictionary<string, object>>();
            foreach (var p in data.Keys)
            { Debug.Log($"{p}({data[p].GetType()}): {data[p]}"); }

            //var cond = yaml.DeserializeYaml<Dictionary<string, List<object>>>();
        }

        [Test]
        public void ExRead()
        {
            var ex = exstring.DeserializeYaml<Experiment>();
            ex.InitializeDataSource();
            Debug.Log(ex);
        }

        // A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // yield to skip a frame
            yield return null;
        }
    }
}

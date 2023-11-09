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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Experica.Command;
using static UnityEngine.Analytics.IAnalytic;

namespace Experica.Test
{
    public class ConditionTest
    {
        static ConditionManager cm=new();
        static string condstring;
        static Dictionary<string, List<object>> cond;

        [Test]
        public void ReadCondition()
        {
            //var filepath = @"C:\Users\fff00\Command\Condition\Ori30DegStep.yaml";
            //var filepath = @"C:\Users\fff00\Command\Condition\OriSF.yaml";
            var filepath = @"C:\Users\fff00\Command\Condition\PositionOffset8Deg1DegStep.yaml";
            cond = cm.ReadConditionFile(filepath);
            condstring = cond.SerializeYaml();
            Debug.Log($"{cond.Values.First()[0].GetType()}\n\n{condstring}");
        }

        [Test]
        public void ProcessCond()
        {
            cond = cm.ProcessCondition(cond);
        }
    }
}

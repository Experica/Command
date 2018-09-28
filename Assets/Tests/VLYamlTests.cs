using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using IExSys;

public class VLYamlTests
{
    string yaml = "Ori: [0, 45, 90, 135]\n" +
         "SpatialPhase: [0, 0.25, 0.5, 0.75]";

    [Test]
    public void YamlReadWrite()
    {
        var cond = yaml.DeserializeYaml<Dictionary<string, List<object>>>();
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

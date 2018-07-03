using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using VLab;

public class VLConvertTests
{
    [Test]
    public void Convert()
    {
        object x = null;
        x.Convert(typeof(float));
        x.Convert<List<float>>();
        //x.Convert<float>();
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

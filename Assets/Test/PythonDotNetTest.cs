using NUnit.Framework;
using Python.Runtime;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace Experica.Test
{
    public class PythonDotNetTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            PythonDotNet.Initialize();
        }

        //[OneTimeTearDown]
        //public void OneTimeTearDown()
        //{
        //    PythonDotNet.Shutdown();
        //}

        [Test]
        public void PythonVersion()
        {
            using (Py.GIL())
            {
                using dynamic platformModule = Py.Import("platform");
                var version = (string)platformModule.python_version();
                Debug.Log($"Python: {version}");
            }
        }

        [Test]
        public void QuanLanDevice()
        {
            //var quanlan = new QuanLan();

            string device_id = "390024350033";
            

                using (Py.GIL())
                {
                //PythonEngine.Exec($"from qlsdk.rsc import *\r\ndevice_id = \"390024350033\"\r\ndc = DeviceContainer()\r\ndc.connect(device_id, timeout = 10)");
                //PythonEngine.Exec($"from qlsdk.rsc import *\r\ndevice_id = \"390024350033\"\r\ndc = DeviceContainer()");
                //PythonEngine.RunSimpleString($"from qlsdk.rsc import *\r\ndevice_id = \"390024350033\"\r\ndc = DeviceContainer()");

                //dynamic device =  PythonEngine.Eval($"from qlsdk.rsc import *\r\ndevice_id = \"390024350033\"\r\n" +
                //    $"dc = DeviceContainer()\r\ndc.connect(device_id, timeout = 30)");


                //PythonEngine.Exec($"dc.connect({device_id}, timeout = 10)");

                dynamic qlsdk = Py.Import("qlsdk.rsc");
                dynamic dc = qlsdk.DeviceContainer();
                dynamic device = dc.connect(device_id, timeout: 10);


                if (device == null) { Debug.LogError($"Can't find device: {device_id}"); }
            }
            
            
        }

        [Test]
        public void Numpy()
        {
            using (Py.GIL())
            {
                using dynamic np = Py.Import("numpy");
                Assert.AreEqual(1d, (double)np.cos(np.pi*2));

                using dynamic sin = np.sin;
                Assert.AreEqual(-0.958924274663, (double)sin(5), 0.000000000001);

                double cas = (double)(np.cos(5) + sin(5));
                Assert.AreEqual(-0.6752620892, cas, 0.0000000001);

                using dynamic a = np.array(new List<float> { 1f, 2f, 3f });
                Assert.AreEqual("float64", $"{a.dtype}");

                using dynamic b = np.array(new List<float> { 6f, 5f, 4f }, dtype: np.int32);
                Assert.AreEqual("int32", $"{b.dtype}");

                using dynamic d = a*b;
                Assert.AreEqual(6d, (double)d[0]);
                Assert.AreEqual(10d, (double)d[1]);
                Assert.AreEqual(12d, (double)d[2]);
            }
        }

        [Test]
        public void CallMyScriptsPasses()
        {
            using (Py.GIL())
            {
                using dynamic testModule1 = Py.Import("test_module1");
                var result1 = (int)testModule1.add(3, 8);
                Assert.AreEqual(11, result1);

                using dynamic testModule2 = Py.Import("test_package").GetAttr("test_module2");
                var result2 = (int)testModule2.mul(3, 8);
                Assert.AreEqual(24, result2);
            }
        }
    }
}
using Python.Runtime;
using System;
using UnityEngine;

namespace Experica
{
    public static class PythonDotNet
    {
        //private const string PythonFolder = "ql";
        private const string PythonFolder = "python-3.12.10-embed-amd64";
        private const string PythonDll = "python312.dll";
        private const string PythonZip = "python312.zip";
        private const string MyProject = "myproject";
        private const string TestProject = "test_project";
        //private const string rootpath = "C:\\Users\\zhang\\miniconda3\\envs";
        private const string rootpath = "Tool";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void PythonInitialize()
        {
            Application.quitting += PythonShutdown;
        }

        static void PythonShutdown()
        {
            Application.quitting -= PythonShutdown;
            Shutdown();
        }

        public static void Initialize()
        {
            if (PythonEngine.IsInitialized) { return; }

            var pythonHome = $"{rootpath}/{PythonFolder}";
            var myProject = $"{rootpath}/{MyProject}";
            var testProject = $"{rootpath}/{TestProject}";

            //var pythonHome = $"{Application.dataPath}/{PythonFolder}";
            //var myProject = $"{Application.dataPath}/{MyProject}";
            //var testProject = $"{Application.dataPath}/{TestProject}";
            var pythonPath = string.Join(";",
                $"{myProject}",
#if UNITY_EDITOR
                $"{testProject}",
#endif
                $"{pythonHome}/Lib/site-packages",
                $"{pythonHome}/{PythonZip}",
                $"{pythonHome}"
            );

            var scripts = $"{pythonHome}/Scripts";

            var path = Environment.GetEnvironmentVariable("PATH")?.TrimEnd(';');
            path = string.IsNullOrEmpty(path) ? $"{pythonHome};{scripts}" : $"{pythonHome};{scripts};{path}";
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", $"{pythonHome}/Lib", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", $"{pythonHome}/{PythonDll}", EnvironmentVariableTarget.Process);
#if UNITY_EDITOR
            Environment.SetEnvironmentVariable("PYTHONDONTWRITEBYTECODE", "1", EnvironmentVariableTarget.Process);
#endif

            PythonEngine.PythonHome = pythonHome;
            PythonEngine.PythonPath = pythonPath;
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
        }

        public static bool IsInitialized => PythonEngine.IsInitialized;

        public static void Shutdown()
        {
            if (PythonEngine.IsInitialized) {   PythonEngine.Shutdown(); }
        }
    }
}

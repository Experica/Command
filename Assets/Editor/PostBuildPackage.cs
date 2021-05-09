/*
PostBuildPackage.cs is part of the Experica.
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
namespace Experica.Editor
{
    public class PostBuildPackage : MonoBehaviour
    {
        static string builddir = "Build";
        static List<string> files = new List<string>()
        {
            "Command.exe",
            "CommandConfigManager.yaml",
            "LICENSE.md",
            "README.md",
            "UnityCrashHandler64.exe",
            "UnityPlayer.dll"
        };
        static List<string> dirs = new List<string>()
        {
            "Install",
            "ExperimentSession",
            "ExperimentLogic",
            "Experiment",
            "Environment",
            "Data",
            "Configuration",
            "Condition",
            "Command_Data",
            "MonoBleedingEdge"
        };

        [MenuItem("File/PostBuildPackage")]
        public static void Package()
        {
            builddir = Path.Combine("Build", $"Command_v{Application.version}");
            if (Directory.Exists(builddir))
            {
                Directory.Delete(builddir, true);
            }
            if (!Directory.Exists(builddir))
            {
                Directory.CreateDirectory(builddir);
            }
            foreach (var f in files)
            {
                File.Copy(f, Path.Combine(builddir, f));
            }
            foreach (var d in dirs)
            {
                d.CopyDirectory(Path.Combine(builddir, d), ".mp");
            }
            Debug.Log("Build Package Finished.");
        }
    }
}
#endif
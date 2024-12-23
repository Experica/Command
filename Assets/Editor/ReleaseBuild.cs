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
using System.Linq;

#if UNITY_EDITOR
namespace Experica.Editor
{
    public class ReleaseBuild : MonoBehaviour
    {
        static string builddir = Path.Combine(Experica.ProjectRootDir, "Build");
        static string releaserootdir = Path.Combine(Experica.ProjectRootDir, "Release");

        [MenuItem("File/Release Build")]
        public static void Release()
        {
            var builditems = new[] { "Command_Data", "D3D12", "MonoBleedingEdge", "Command.exe", "UnityCrashHandler64.exe", "UnityPlayer.dll" };
            var projectitems = new[] {  "ExperimentLogic", "Environment","Condition","Configuration", "Data","Tool","Install"
                , "CommandConfigManager.yaml", "LICENSE.md","README.md"};
            var allitems = builditems.Concat(projectitems).ToArray();
            var parentdir = Enumerable.Repeat(builddir, builditems.Length).Concat(Enumerable.Repeat(Experica.ProjectRootDir, projectitems.Length)).ToArray();

            var releasedir = Path.Combine(releaserootdir, $"Command_v{Application.version}");
            if (Directory.Exists(releasedir))
            {
                Directory.Delete(releasedir, true);
            }
            if (!Directory.Exists(releasedir))
            {
                Directory.CreateDirectory(releasedir);
            }

            for (var i = 0; i < allitems.Count(); i++)
            {
                var itempath = Path.Combine(parentdir[i], allitems[i]);
                var ispathfile = itempath.IsFileOrDir();
                if (!ispathfile.HasValue) { Debug.LogWarning($"Item: {itempath} Not Exist."); continue; }
                if (ispathfile.Value)
                {
                    File.Copy(itempath, Path.Combine(releasedir, allitems[i]));
                }
                else
                {
                    itempath.CopyDirectory(Path.Combine(releasedir, allitems[i]), ".mp");
                }
            }
            Debug.Log("Build Released.");
        }
    }
}
#endif
// --------------------------------------------------------------
// CompilerService.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;
using Microsoft.CSharp;
using Mono.CSharp;
using System.CodeDom.Compiler;
using System;
using System.Text;
using System.Reflection;

public class CompilerService
{
    static CSharpCodeProvider provider = new CSharpCodeProvider();
    static CompilerParameters param = new CompilerParameters();

    public static Assembly Compile(string sourcefile)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            param.ReferencedAssemblies.Add(assembly.Location);
        }
        param.GenerateExecutable = false;
        param.GenerateInMemory = true;

        var result = provider.CompileAssemblyFromFile(param, sourcefile);
        if (result.Errors.Count > 0)
        {
            var msg = new StringBuilder();
            foreach (CompilerError error in result.Errors)
            {
                msg.AppendFormat("Error ({0}): {1}\n",
                    error.ErrorNumber, error.ErrorText);
            }
            throw new Exception(msg.ToString());
        }
        return result.CompiledAssembly;
    }

}

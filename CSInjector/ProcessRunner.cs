// Author: Manuel Antony
// Reference: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?redirectedfrom=MSDN&view=netframework-4.8#System_Diagnostics_Process_StandardOutput

using System;
using System.Diagnostics;

namespace CshapInstrumenter
{
    class ProcessRunner
    {
        Process process;

        //
        // Summary:
        //         Takes any jar with arguments and will execute it. It will not pop new command shell for running jar.
        //
        // Returns:
        //         String: Standard output of the jar after execution
        public String RunJar(String jarPath, String argumentString)
        {
            process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = jarPath;
            process.StartInfo.Arguments = argumentString;
            process.Start();
            string output = process.StandardOutput.ReadLine();
            process.WaitForExit();
            return output;
        }

    }
}

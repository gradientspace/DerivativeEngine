using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph
{
    [NodeFunctionLibrary("Core.System")]
    public static class GSExecFunctions
    {

        [NodeFunction]
        public static void LaunchApplication(string Path, string Arguments = "", bool bHideProcessWindow = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = Path,
                Arguments = Arguments,
                UseShellExecute = false,               // always the case?
                CreateNoWindow = bHideProcessWindow,   // Hides the process window
                RedirectStandardOutput = true, RedirectStandardError = true
            };

            // want to throw exceptions because they will be caught as graph errors...
            Process? process = Process.Start(psi);
        }


        [NodeFunction]
        public static void RunShellCommand(string Path, 
            out int ExitCode, out string StdOut, out string StdErr,
            string Arguments = "", bool bHideProcessWindow = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = Path,
                Arguments = Arguments,
                UseShellExecute = false,               // always the case?
                CreateNoWindow = bHideProcessWindow,   // Hides the process window
                RedirectStandardOutput = bHideProcessWindow, RedirectStandardError = bHideProcessWindow
            };

            StdOut = StdErr = "";
            ExitCode = -1;

            // want to throw exceptions because they will be caught as graph errors...
            Process? process = Process.Start(psi);
            if (process != null) {
                process.WaitForExit();
                if (bHideProcessWindow) {
                    StdOut = process.StandardOutput.ReadToEnd();
                    StdErr = process.StandardError.ReadToEnd();
                }
                ExitCode = process.ExitCode;
                return;
            }

            throw new Exception("[RunShellCommand] failed to start Process");
        }



        [NodeFunction]
        public static void Sleep(int Milliseconds = 100) { 
            Thread.Sleep(Milliseconds);
        }


        [NodeFunction]
        public static void FindInSystemPaths(string ExecutableName, out string FullPath, out bool Found)
        {
            Found = false; FullPath = string.Empty;
            string AllPaths = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string[] Directories = AllPaths.Split(';');
            foreach (string dirPath in Directories) {
                if (!System.IO.Directory.Exists(dirPath)) continue;
                string[] found = System.IO.Directory.GetFiles(dirPath, ExecutableName, SearchOption.TopDirectoryOnly);
                if (found.Length > 0) {
                    FullPath = found[0];
                    Found = true;
                    return;
                }
            }
        }

    }



    [GraphNodeNamespace("Core.System")]
    [GraphNodeUIName("Make Arguments")]
    public class MakeArgumentsNode : VariableStringsInputNode
    {
        public static string OutputName { get { return "String"; } }

        public override string GetDefaultNodeName() { return "MakeArgs"; }
        protected override string ElementBaseName { get { return "Arg"; } }

        public MakeArgumentsNode()
        {
            Flags |= ENodeFlags.IsPure;
        }

        protected override void BuildStandardInputsOutputs()
        {
            AddOutput(OutputName, new StandardNodeOutput<string>());
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(OutputName);
            if (OutputIndex == -1)
                throw new Exception("MakeArgumentsNode: output not found");

            string[] AppendValues = ConstructStringArray(in DataIn);

            string result = string.Join(" ", AppendValues);
            RequestedDataOut.SetItemValue(OutputIndex, result);
        }
    }



}

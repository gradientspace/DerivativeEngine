// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System.IO;

namespace Gradientspace.NodeGraph
{
    [NodeFunctionLibrary("Core.Filesystem")]
    public static class GSFilesystemFunctions
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.directory?view=net-9.0
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.path?view=net-9.0
        // https://learn.microsoft.com/en-us/dotnet/api/system.io.file?view=net-9.0

        [NodeFunction]
        public static string GetCurrentDirectory() {
            return Directory.GetCurrentDirectory();
        }

        [NodeFunction]
        public static void SetCurrentDirectory(string Path)
        {
            if (Directory.Exists(Path) == false)
                GlobalGraphOutput.AppendError($"[Core.Filesystem.SetCurrentDirectory] Path {Path} does not exist");
            else
                Directory.SetCurrentDirectory(Path);
        }



        [NodeFunction]
        public static bool FileExists(string Path) {
            return File.Exists(Path);
        }
        [NodeFunction]
        public static bool DirectoryExists(string Path) {
            return Directory.Exists(Path);
        }
        [NodeFunction]
        public static bool PathExists(string Path) {
            return System.IO.Path.Exists(Path);
        }
        [NodeFunction(IsPure = true)]
        public static bool IsSamePath(string Path1, string Path2)
        {
            string a = System.IO.Path.GetFullPath(Path1);
            string b = System.IO.Path.GetFullPath(Path2);
            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }

        [NodeFunction(IsPure = true,ReturnName = "Path")]
        public static string CombinePath(IEnumerable<string> Strings)
        {
            return Path.Combine(Strings.ToArray());
        }

        [NodeFunction(IsPure = true)]
        public static void GetPathParts(string Path, out string Directory, out string Filename, out string FileBase, out string Extension)
        {
            Directory = System.IO.Path.GetDirectoryName(Path) ?? "";
            Filename = System.IO.Path.GetFileName(Path) ?? "";
            FileBase = System.IO.Path.GetFileNameWithoutExtension(Path) ?? "";
            Extension = System.IO.Path.GetExtension(Path) ?? "";
            if (Extension.StartsWith('.'))
                Extension = Extension.Remove(0, 1);
        }

        [NodeFunction(IsPure = true)]
        public static string DirSeparator(string Path)
        {
            return System.IO.Path.DirectorySeparatorChar.ToString();
        }

        [NodeFunction(IsPure = true, ReturnName = "NewPath")]
        public static string ChangeExtension(string Path, string NewExtension) {
            return System.IO.Path.ChangeExtension(Path, NewExtension) ?? Path;
        }

        [NodeFunction(IsPure = true, ReturnName = "FullPath")]
        public static string GetFullPath(string Path) {
            return System.IO.Path.GetFullPath(Path);
        }
        [NodeFunction(IsPure = true, ReturnName = "RelativePath")]
        public static string GetRelativePath(string RelativeTo, string Path) {
            return System.IO.Path.GetRelativePath(RelativeTo, Path);
        }

        [NodeFunction(ReturnName = "Filename")]
        public static string GetRandomFileName() {
            return System.IO.Path.GetRandomFileName();
        }
        [NodeFunction(ReturnName = "Path")]
        public static string CreateTempFile() {
            return System.IO.Path.GetTempFileName();
        }
        [NodeFunction(IsPure = true, ReturnName = "Path")]
        public static string GetTempPath() {
            return System.IO.Path.GetTempPath();
        }

        [NodeFunction]
        public static string[] ListDirectory(string Path, string Filter = "*", bool IncludeFiles = true, bool IncludeDirs = true, bool Recursive = false)
        {
            SearchOption searchOption = Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (IncludeDirs == false)
                return Directory.GetFiles(Path, Filter, searchOption);
            else if (IncludeFiles == false)
                return Directory.GetDirectories(Path, Filter, searchOption);
            List<string> all = Directory.GetFiles(Path, Filter, searchOption).ToList();
            foreach (string dir in Directory.EnumerateDirectories(Path, Filter, searchOption))
                all.Add(dir);
            return all.ToArray();
        }

        [NodeFunction(ReturnName = "FilePath")]
        public static string FindFilePath(string Filename, out bool Found, string PathHint = ".", bool SearchBelow = true, bool SearchAbove = false)
        {
            Found = false;
            string CurFolder = GetCurrentDirectory();
            if (PathHint != "." && Directory.Exists(PathHint))
                CurFolder = PathHint;

            bool bDone = false;
            while (!bDone) 
            {
                string[] found = Directory.GetFiles(CurFolder, Filename, (SearchBelow) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                if (found.Length > 0) {
                    Found = true;
                    return found[0];
                }

                if (SearchAbove) {
                    SearchBelow = false;
                    DirectoryInfo? parent = Directory.GetParent(CurFolder);
                    if (parent != null)
                        CurFolder = parent.FullName;
                    else
                        bDone = true;
                } else {
                    bDone = true;
                }
            }
            return "";
        }






        static bool filesystem_op(Action action, string Caller, bool bThrow = false)
        {
            try {
                action();
            } catch (Exception e) {
                GlobalGraphOutput.AppendError($"[Core.Filesystem.{Caller}] {e.Message}");
                if (bThrow)
                    throw;
                return false;
            }
            return true;
        }


        [NodeFunction]
        public static bool CreateDirectory(string Path) {
            return filesystem_op(() => { Directory.CreateDirectory(Path); }, "CreateDirectory");
        }
        [NodeFunction]
        public static bool DeleteDirectory(string Path, bool Recursive = true) {
            return filesystem_op(() => { Directory.Delete(Path, Recursive); }, "DeleteDirectory");
        }
        [NodeFunction]
        public static bool MoveDirectory(string FromPath, string ToPath) {
            return filesystem_op(() => { Directory.Move(FromPath, ToPath); }, "MoveDirectory");
        }


        [NodeFunction]
        public static bool CreateTextFile(string Path, string? InitialText = null) {
            if ( InitialText != null )
                return filesystem_op(() => { File.WriteAllText(Path, InitialText); }, "CreateTextFile");
            return filesystem_op(() => { File.Create(Path); }, "CreateTextFile");
        }
        [NodeFunction]
        public static bool CreateBinaryFile(string Path, byte[]? InitialBytes = null)
        {
            if (InitialBytes != null )
                return filesystem_op(() => { File.WriteAllBytes(Path, InitialBytes); }, "CreateBinaryFile");
            return filesystem_op(() => { File.Create(Path); }, "CreateBinaryFile");
        }
        [NodeFunction]
        public static bool DeleteFile(string Path) {
            return filesystem_op(() => { File.Delete(Path); }, "DeleteFile");
        }
        [NodeFunction]
        public static bool MoveFile(string FromPath, string ToPath, bool Overwrite = true) {
            return filesystem_op(() => { File.Move(FromPath, ToPath, Overwrite); }, "MoveFile");
        }
        [NodeFunction]
        public static bool CopyFile(string FromPath, string ToPath, bool Overwrite = true)
        {
            return filesystem_op(() => { File.Copy(FromPath, ToPath, Overwrite); }, "CopyFile");
        }




        [NodeFunction]
        public static string ReadAllText(string Path)
        {
            string result = "";
            filesystem_op(() => { result = File.ReadAllText(Path); }, "ReadAllText");
            return result;
        }
        [NodeFunction]
        public static bool WriteAllText(string Path, string Text)
        {
            return filesystem_op(() => { File.WriteAllText(Path, Text); }, "WriteAllText");
        }
        [NodeFunction]
        public static bool AppendText(string Path, string Text)
        {
            return filesystem_op(() => { File.AppendAllText(Path, Text); }, "AppendText");
        }



        [NodeFunction]
        public static string[] ReadAllLines(string Path) {
            string[]? result = null;
            if (filesystem_op(() => { result = File.ReadAllLines(Path); }, "ReadAllLines") == false)
                result = [];
            return result!;
        }
        [NodeFunction]
        public static bool WriteAllLines(string Path, IEnumerable<string> Lines)
        {
            return filesystem_op(() => { File.WriteAllLines(Path, Lines); }, "WriteAllLines");
        }
        [NodeFunction]
        public static bool AppendLines(string Path, IEnumerable<string> Lines)
        {
            return filesystem_op(() => { File.AppendAllLines(Path, Lines); }, "WriteAllLines");
        }


        [NodeFunction]
        public static byte[] ReadAllBytes(string Path)
        {
            byte[]? result = null;
            if (filesystem_op(() => { result = File.ReadAllBytes(Path); }, "ReadAllBytes") == false)
                result = [];
            return result!;
        }
        [NodeFunction]
        public static bool WriteAllBytes(string Path, byte[] Bytes)
        {
            return filesystem_op(() => { File.WriteAllBytes(Path, Bytes); }, "WriteAllBytes");
        }

    }



    [GraphNodeNamespace("Core.Filesystem")]
    [GraphNodeUIName("MakePath")]
    public class MakePathNode : VariableStringsInputNode
    {
        public static string SimplifyName { get { return "Simplify"; } }
        public static string ToFullName { get { return "Full Path"; } }
        public static string OutputName { get { return "Path"; } }

        public override string GetDefaultNodeName() { return "MakePath"; }
        protected override string ElementBaseName { get { return "Path"; } }

        public MakePathNode() {
            Flags |= ENodeFlags.IsPure;
        }

        protected override void BuildStandardInputsOutputs() {
            AddInput(SimplifyName, new StandardNodeInputWithConstant<bool>(false));
            AddInput(ToFullName, new StandardNodeInputWithConstant<bool>(false));
            AddOutput(OutputName, new StandardNodeOutput<string>());
        }

        public override void Evaluate(EvaluationContext EvalContext, ref readonly NamedDataMap DataIn, NamedDataMap RequestedDataOut)
        {
            int OutputIndex = RequestedDataOut.IndexOfItem(OutputName);
            if (OutputIndex == -1)
                throw new Exception("MakePathNode: output not found");

            string[] AppendValues = ConstructStringArray(in DataIn);
            string Path = System.IO.Path.Combine(AppendValues);

            if (DataIn.FindStructValueOrDefault<bool>(ToFullName, false, false)) 
            {
                Path = System.IO.Path.GetFullPath(Path);
            } 
            else if (DataIn.FindStructValueOrDefault<bool>(SimplifyName, false, false)) 
            {
                if (System.IO.Path.IsPathRooted(Path)) {
                    Path = System.IO.Path.GetFullPath(Path);
                } else {
                    string tempbase = Directory.GetCurrentDirectory();
                    Path = System.IO.Path.GetFullPath(Path);
                    Path = System.IO.Path.GetRelativePath(tempbase, Path);
                }
            }

            RequestedDataOut.SetItemValue(OutputIndex, Path);
        }
    }

}

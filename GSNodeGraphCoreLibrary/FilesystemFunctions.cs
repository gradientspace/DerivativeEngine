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

        //System.IO.Path functions


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





        [NodeFunction]
        public static string[] ListDirectory(string Path, string Filter = "*", bool IncludeFiles = true, bool IncludeDirs = true)
        {
            if (IncludeDirs == false)
                return Directory.GetFiles(Path, Filter);
            else if (IncludeFiles == false)
                return Directory.GetDirectories(Path, Filter);
            List<string> all = Directory.GetFiles(Path, Filter).ToList();
            foreach (string dir in Directory.EnumerateDirectories(Path, Filter))
                all.Add(dir);
            return all.ToArray();
        }

        [NodeFunction]
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

}

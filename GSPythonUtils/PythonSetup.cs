using System.Diagnostics;
using Python.Runtime;

namespace GSPython
{
	public static class PythonSetup
	{
		// useful documentation: https://github.com/pythonnet/pythonnet/wiki

		internal static bool bIsPythonInitialized = false;

		private static IntPtr BeginAllThreadsHandle = IntPtr.Zero;

		public struct PythonInstallation
		{
			public Version PythonVersion = new Version();       // this is dumb, only will work if versions are all #.#.#
			public string Path = "";
			public string PythonDLLPath = "";
			public PythonInstallation() { }
		}


		public static void InitializePython()
		{
			if (bIsPythonInitialized)
				return;

			// all the installed python versions we found
			List<PythonInstallation> PythonVersions = new List<PythonInstallation>();

			// try to detect a python installation in a folder and find the pythonXYZ.dll file
			var try_add_python_version = (string rootpath) => {
				try
				{
					string dirname = Path.GetFileName(rootpath);
					if (dirname.StartsWith("Python", StringComparison.InvariantCultureIgnoreCase))
					{

						// does this always work? assuming if we are in folder PythonXYZ then dll will be pythonXYZ.dll
						int VersionFromFolder = int.Parse(dirname.Substring(6));
						string dllpath = Path.Combine(rootpath, "python" + VersionFromFolder.ToString() + ".dll");
						if (File.Exists(dllpath) == false)
							return;

						// check if we already found this exact dll, ie were already called with this rootpath
						// via some other means (possible if we are iterating through different possible install locations)
						int ExistingIndex = PythonVersions.FindIndex((PythonInstallation p) => {
							return Path.GetFullPath(p.PythonDLLPath) == Path.GetFullPath(dllpath);
						});
						if (ExistingIndex != -1)
							return;

						// need python.exe to get version number
						string exePath = Path.Combine(rootpath, "python.exe");
						if (File.Exists(exePath) == false)
							return;

						// run python.exe --version to get version number
						Process process = new Process();
						process.StartInfo.FileName = exePath;
						process.StartInfo.Arguments = "--version";
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.CreateNoWindow = true;
						process.StartInfo.RedirectStandardOutput = true;
						process.Start();
						StreamReader reader = process.StandardOutput;
						string output = reader.ReadToEnd().Trim();
						process.WaitForExit();

						// assume printed output is in form "Python 13.3.1" etc
						output = output.Replace("Python ", "");
						System.Version.TryParse(output, out Version? FoundVersion);
						if (FoundVersion == null)
							return;

						PythonVersions.Add(new PythonInstallation() { PythonVersion = FoundVersion, Path = rootpath, PythonDLLPath = dllpath });
					}
				} catch (Exception) { }
			};

			// search in Users\<username>\AppData\Local\Programs\Python\Python###
			// this is the default installation folder for windows python installer if not installed for all users...
			string AppDataLocal =
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "..", "Local");
			string StandardPythonInstallDir = Path.Combine(AppDataLocal, "Programs\\Python");
			if (Directory.Exists(StandardPythonInstallDir))
			{
				string[] subdirs = Directory.GetDirectories(StandardPythonInstallDir);
				foreach (string subdir in subdirs)
					try_add_python_version(subdir);
			}

			// look in Program Files/Python###, this is default windows path if installed for all users
			string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			if (Directory.Exists(ProgramFiles))
			{
				string[] ProgramFilesPythonSubdirs = Directory.GetDirectories(ProgramFiles, "Python*");
				foreach (string subdir in ProgramFilesPythonSubdirs)
					try_add_python_version(subdir);
			}

			// todo look in C:\Python## ? does python still install there sometimes?

			// todo python may be in the system path...
			//string PathVariable = Environment.GetEnvironmentVariable("Path") ?? "";
			//string[] subpaths = PathVariable.Split(';');

			if (PythonVersions.Count == 0)
				throw new Exception("Could not find a suitable python dll!");

			// largest version number first
			PythonVersions.Sort((PythonInstallation a, PythonInstallation b) => { return a.PythonVersion.CompareTo(b.PythonVersion); });
			PythonVersions.Reverse();

			foreach (var version in PythonVersions)
				Debug.WriteLine($"[GSPython] Found Python {version.PythonVersion} installation at {version.Path}");

			Python.Runtime.Runtime.PythonDLL = PythonVersions[0].PythonDLLPath;

			Debug.WriteLine($"[GSPython] Trying to Initialize PythonEngine with DLL {PythonVersions[0].PythonDLLPath}");

			PythonEngine.Initialize();

			// ??? does BeginAllowThreads() block or not-block the GIL thing?
			// see https://github.com/pythonnet/pythonnet/wiki/Threading
			BeginAllThreadsHandle = PythonEngine.BeginAllowThreads();

			bIsPythonInitialized = true;
		}


		public static void PythonShutdown()
		{
			if (bIsPythonInitialized)
			{
				PythonEngine.EndAllowThreads(BeginAllThreadsHandle);

				// TODO
				// PythonEngine uses BinaryFormatter internally, which was deprecated in dotnet 8 and
				// removed in dotnet 9. Seems to only be used on Shutdown()? 
				// Waiting for library update to see if this gets resolved...
				// See issue here: https://github.com/pythonnet/pythonnet/issues/2282
				try
				{
					AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);	// doesn't work on dotnet 9
					PythonEngine.Shutdown();
					AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);
				} catch (Exception ex) {
					Debug.WriteLine("Exception thrown by PythonEngine.Shutdown(): " + ex.Message);
				}

				bIsPythonInitialized = false;
			}
		}
	}
}

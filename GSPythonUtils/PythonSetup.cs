// Copyright Gradientspace Corp. All Rights Reserved.
using System.Diagnostics;
using System.Runtime.InteropServices;
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

		public static bool IsPythonAvailable { get { return bIsPythonInitialized; } }

		public static bool InitializePython(List<string>? OutputMessages = null)
		{
			if (bIsPythonInitialized)
				return true;

			// all the installed python versions we found
			List<PythonInstallation> PythonVersions = new List<PythonInstallation>();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				FindPython_Win(PythonVersions);
				if (PythonVersions.Count == 0) {
                    OutputMessages?.Add("[GSPython] Could not find a suitable Python installation/DLL! Python will not be available.");
					return false;
				}				
			}


			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				FindPython_OSX(PythonVersions);
				if (PythonVersions.Count == 0) {
                    OutputMessages?.Add("[GSPython] Could not find a suitable Python Framework installation! Must install full OSX Framework from python.org/downloads.");
					return false;
				}
			}

			if (PythonVersions.Count == 0) {
				OutputMessages?.Add("[GSPython] Current platform does not support Python");
				return false;
			}

			// largest version number first
			PythonVersions.Sort((PythonInstallation a, PythonInstallation b) => { return a.PythonVersion.CompareTo(b.PythonVersion); });
			PythonVersions.Reverse();

			foreach (var version in PythonVersions)
                OutputMessages?.Add($"[GSPython] Found Python {version.PythonVersion} installation at {version.Path}");

			Python.Runtime.Runtime.PythonDLL = PythonVersions[0].PythonDLLPath;

            string initMessage = $"[GSPython] Trying to Initialize PythonEngine with DLL {PythonVersions[0].PythonDLLPath}...";
            try {
                PythonEngine.Initialize();

                // ??? does BeginAllowThreads() block or not-block the GIL thing?
                // see https://github.com/pythonnet/pythonnet/wiki/Threading
                BeginAllThreadsHandle = PythonEngine.BeginAllowThreads();
                OutputMessages?.Add(initMessage + "Ok!");

            } catch (Exception ex) {
                OutputMessages?.Add(initMessage);
                OutputMessages?.Add($"[GSPython] PythonEngine Initialization Failed : {ex.Message}");
                return false;
            }

			bIsPythonInitialized = true;
			return true;
		}


		internal static void FindPython_Win(List<PythonInstallation> PythonVersions)
		{
			// try to detect a python installation in a folder and find the pythonXYZ.dll file
			var try_add_python_version = (string rootpath) =>
			{
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
						int ExistingIndex = PythonVersions.FindIndex((PythonInstallation p) =>
						{
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
				}
				catch (Exception) { }
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
		}


		internal static void FindPython_OSX(List<PythonInstallation> PythonVersions)
		{
			// try to detect a python installation in a folder and find the pythonXYZ.dll file
			var try_add_python_version_osx = (string rootpath) =>
			{
				try
				{
					// TODO: assuming root python folder is just the version name
					string fullversion = Path.GetFileName(rootpath);
					//if (dirname.StartsWith("Python", StringComparison.InvariantCultureIgnoreCase))
					if (true)
					{
						string dylibpath = Path.Combine(rootpath, "lib", "libpython" + fullversion + ".dylib");
						if (File.Exists(dylibpath) == false)
							return;

						// check if we already found this exact dll, ie were already called with this rootpath
						// via some other means (possible if we are iterating through different possible install locations)
						int ExistingIndex = PythonVersions.FindIndex((PythonInstallation p) =>
						{
							return Path.GetFullPath(p.PythonDLLPath) == Path.GetFullPath(dylibpath);
						});
						if (ExistingIndex != -1)
							return;

						// need python.exe to get version number
						string exePath = Path.Combine(rootpath, "bin", "python3");
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

						PythonVersions.Add(new PythonInstallation() { PythonVersion = FoundVersion, Path = rootpath, PythonDLLPath = dylibpath });
					}
				}
				catch (Exception) { }
			};

			// yikes this is a horrible hack but I'm not sure how else to do it...
			for (int k = 50; k >= 1; --k)
			{
				string py_framework_path = $"/Library/Frameworks/Python.framework/Versions/3.{k}";
				if (Directory.Exists(py_framework_path))
					try_add_python_version_osx(py_framework_path);
			}
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
					AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);    // doesn't work on dotnet 9
					PythonEngine.Shutdown();
					AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Exception thrown by PythonEngine.Shutdown(): " + ex.Message);
				}

				bIsPythonInitialized = false;
			}
		}
	}
}

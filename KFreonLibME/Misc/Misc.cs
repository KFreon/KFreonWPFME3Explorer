using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KFreonLibGeneral.Debugging;
using KFreonLibME.Textures;
using SharpSvn;
using UsefulThings;

namespace KFreonLibME.Misc
{
    /// <summary>
    /// Provides miscellaneous methods for general tools. 
    /// </summary>
    public static class Methods
    {
        public static void ChangeListSelection(object element, bool Using)
        {
            foreach (var pccentry in ((AbstractTexInfo)element).PCCs)
                pccentry.Using = Using;
        }

        public static void UpgradeProperties()
        {
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Increments version of KFreonLibME assembly in code (No annoying extra project)
        /// Targets KFreonLibME\Properties\AssemblyInfo.cs
        /// </summary>
        public static void IncrementVersion()
        {
            // KFreon: Run off thread as SharpSVN can be slow sometimes. Also, no rush seeing as build won't actually be incremented until next run.
            Task.Run(() =>
                {
                    try
                    {
                        int rev = 0;

                        // KFreon: Get working copy revision.
                        using (SvnWorkingCopyClient client = new SvnWorkingCopyClient())
                        {
                            // KFreon: Get working copy revision.
                            SvnWorkingCopyVersion version;
                            client.GetVersion(@"D:\My Documents\ME3 WPF", out version);
                            rev = (int)version.End + 1;
                        }

                        // KFreon: Get current information.
                        Version current = Assembly.GetAssembly(typeof(DebugOutput)).GetName().Version;
                        int build = current.Revision + 1;

                        // KFreon: Reset build version if upgraded from svn.
                        if (current.Build != rev)
                            build = 1;

                        // KFreon: Build new AssemblyInfo strings
                        string newFileVersion = String.Format("[assembly: AssemblyFileVersion(\"{0}.{1}.{2}.{3}\")]", current.Major, current.Minor, rev, build);
                        string newVersion = newFileVersion.Replace("AssemblyFileVersion", "AssemblyVersion");

                        // KFreon: Read current AssemblyInfo.cs from disk.
                        string assemblyInfo = Path.Combine(General.GetExecutingLoc().GetDirParent(3), "KFreonLibGeneral", "Properties", "AssemblyInfo.cs");
                        List<string> lines = File.ReadAllLines(assemblyInfo).ToList(40);
                        if (lines.Last() == "")
                            lines.RemoveAt(lines.Count - 1);

                        // KFreon: Replace relevant lines with new ones.
                        lines[lines.Count - 1] = newFileVersion;
                        lines[lines.Count - 2] = newVersion;

                        // KFreon: Write back to AssemblyInfo.cs
                        File.WriteAllLines(assemblyInfo, lines);
                    }
                    catch (Exception e)
                    {
                        DebugOutput.PrintLn("Failed to increment version: ", "Misc", e);
                    }
                });   
        }


        public static int SetupThreadCount()
        {
            int currentThreads = GetNumThreads();
            if (currentThreads == 0)
                currentThreads = SetNumThreads(false);

            return currentThreads;
        }


        /// <summary>
        /// Sets number of threads in Project Settings.
        /// </summary>
        /// <param name="User">True = asks user for threads.</param>
        /// <param name="AutoSave">True = saves Settings automatically.</param>
        /// <returns>Number of threads set to.</returns>
        public static int SetNumThreads(bool User, bool AutoSave = true)
        {
            int threads = -1;
            try
            {
                if (User)
                    // KFreon: Get user input
                    while (true)
                        if (int.TryParse(Microsoft.VisualBasic.Interaction.InputBox("Set number of threads to use in multi-threaded programs: ", "Threads", "" + Environment.ProcessorCount), out threads))
                            break;
                        else
                            threads = Environment.ProcessorCount;

                // KFreon: Checks
                if (threads <= 0)
                    threads = Environment.ProcessorCount;


                Properties.Settings.Default.NumThreads = threads;

                // KFreon: Save Properties if requested.
                if (AutoSave)
                    SaveSettings();
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Failed to set numThreads: ", "Misc", e);
            }
            return threads;
        }


        /// <summary>
        /// Safely saves Project Settings
        /// </summary>
        private static void SaveSettings()
        {
            try
            {
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Failed to save settings: ", "Misc", e);
            }
        }


        /// <summary>
        /// Gets number of threads from Project Settings
        /// </summary>
        /// <returns></returns>
        public static int GetNumThreads()
        {
            return Properties.Settings.Default.NumThreads;
        }


        /// <summary>
        /// Runs commands in a shell. (WV's code I believe)
        /// </summary>
        /// <param name="cmd">Commands to run.</param>
        /// <param name="args">Arguments to give to commands.</param>
        public static void RunShell(string cmd, string args)
        {
            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo(cmd, args);
            procStartInfo.WorkingDirectory = Path.GetDirectoryName(cmd);
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
            proc.WaitForExit();
        }
    }
}

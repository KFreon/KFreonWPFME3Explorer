using KFreonLibGeneral.Debugging;
using KFreonLibME.Misc;
using KFreonLibME.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFreonLibME.Helpers
{
    /// <summary>
    /// Provides a bunch of helper methods for things.
    /// </summary>
    public static class Methods
    {
        public static void ShowPathInfo(MEViewModelBase vm, string name, int game)
        {
            /*PathInfoChanger info = new PathInfoChanger();
            info.Show();*/
            IndividualPathInfoChanger info = new IndividualPathInfoChanger(name, game);
            if (info.ShowDialog() == true)
                vm.RefreshDirecs();
        }

        /// <summary>
        /// Updates TOC's for a given game.
        /// </summary>
        /// <param name="MEExDirecs">Directory information of given game.</param>
        /// <param name="modifiedDLC">List of DLC to update.</param>
        public static void UpdateTOCs(MEDirectories.MEDirectories MEExDirecs, List<string> modifiedDLC = null, ConcurrentDictionary<string, long> BaseGameFileInfos = null)
        {
            UpdateTOCs(MEExDirecs.PathBIOGame, MEExDirecs.GameVersion, MEExDirecs.DLCPath, modifiedDLC, BaseGameFileInfos);
        }

        /// <summary>
        /// Updates Basegame and (ME3)DLC TOC's.
        /// </summary>
        /// <param name="pathBIOGame">Path to BIOGame folder.</param>
        /// <param name="GameVersion">Game version to update. Valid: 1-3.</param>
        /// <param name="DLCPath">Path to DLC folder.</param>
        /// <param name="modifiedDLC">Modified DLC names, if applicable.</param>
        public static void UpdateTOCs(string pathBIOGame, int GameVersion, string DLCPath, List<string> modifiedDLC = null, ConcurrentDictionary<string, long> BaseGameFileInfos = null)
        {
            DebugOutput.PrintLn("Updating Basegame TOC...");
            AutoTOC toc = new AutoTOC();

            // KFreon: Format filenames
            DebugOutput.PrintLn("Formatting filenames...");
            List<string> FileNames = toc.GetFiles(pathBIOGame + "\\");
            string basepath = Path.GetDirectoryName(pathBIOGame);
            
            // KFreon: Remove system specific pathing from filepaths
            DebugOutput.PrintLn("Bit of filenames to remove for TOC building: {0}.", basepath);
            for (int i = 0; i < FileNames.Count; i++)
                FileNames[i] = FileNames[i].Substring(basepath.Length + 1);

            // KFreon: Format basepath to remove the last bit.
            basepath += "\\";
            string tocfile = pathBIOGame + "\\PCConsoleTOC.bin";
            Debug.WriteLine("BasePath: " + basepath);
            Debug.WriteLine("Tocfile: " + tocfile);

            DebugOutput.PrintLn("Basepath formatted as {0}.  Creating BaseGame TOC at: {1}.", basepath, tocfile);

            if (BaseGameFileInfos == null)
                toc.CreateTOC(basepath, tocfile, FileNames.ToArray());
            else
                toc.CreateTOC(tocfile, BaseGameFileInfos);

            DebugOutput.PrintLn("Basegame updated.");


            // KFreon: Update DLC TOC's
            if (GameVersion == 3)
            {
                // KFreon: Setup pathing to each modified DLC
                List<string> files = null;
                DebugOutput.PrintLn("Updating DLC...");
                if (modifiedDLC == null)
                    files = new List<string>(Directory.EnumerateFiles(DLCPath, "Default.sfar", SearchOption.AllDirectories));
                else
                    files = modifiedDLC;

                // KFreon: Run DLC TOC Updater on each DLC
                foreach (string file in files)
                {
                    if (file != "")
                    {
                        DebugOutput.PrintLn("Updating DLC at: " + file);
                        DLCPackage dlc = new DLCPackage(file);
                        List<TOCBinFile.Entry> entries = dlc.UpdateTOCbin(true);
                    }
                }
                DebugOutput.PrintLn("DLC Updated.");
            }
        }
    }
}

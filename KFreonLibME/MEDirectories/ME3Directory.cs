using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KFreonLibME.MEDirectories
{
    public static class ME3Directory
    {
        public static bool DoesGameExist
        {
            get
            {
                return _gamePath == null ? false : Directory.Exists(_gamePath);
            }
        }

        static List<string> archives = null;
        public static List<string> Archives
        {
            get
            {
                if (archives == null)
                {
                    archives = MEDirectories.EnumerateGameFiles(3, BaseGameFiles, f => f.EndsWith(".tfc"));
                    if (archives.Count == 0)
                        archives = null;
                    else
                        archives.AddRange(MEDirectories.EnumerateGameFiles(3, DLCFiles, f => f.EndsWith(".tfc")));
                }

                return archives;
            }
        }

        private static List<String> baseGameFiles = null;
        public static List<string> BaseGameFiles
        {
            get
            {
                if (baseGameFiles == null && !String.IsNullOrEmpty(ME3Directory.cookedPath))
                    baseGameFiles = MEDirectories.EnumerateGameFiles(3, ME3Directory.cookedPath);

                return baseGameFiles;
            }
        }


        private static List<string> dlcFiles = null;
        public static List<string> DLCFiles
        {
            get
            {
                if (dlcFiles == null && !String.IsNullOrEmpty(ME3Directory.DLCPath))
                    dlcFiles = MEDirectories.EnumerateGameFiles(3, ME3Directory.DLCPath);

                return dlcFiles;
            }
        }

        private static string _gamePath = null;
        public static string gamePath
        {
            get 
            {
                if (_gamePath != null)
                {
                    if (!_gamePath.EndsWith("\\"))
                        _gamePath += '\\';
                }
                
                return _gamePath;
            }
            private set { _gamePath = value; }
        }
        public static string GamePath(string path = null)
        {
            if (path != null)
                _gamePath = path;

            return _gamePath;
        }

        static string exepath = null;
        public static string ExePath
        {
            get
            {
                if (exepath == null)
                {
                    string path = GamePath();
                    if (path != null && Directory.Exists(path))
                        exepath = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).First(t => t.ToUpperInvariant().Contains("MASSEFFECT3.EXE"));
                }

                return exepath;
            }
        }

        public static string tocFile { get { return (gamePath != null) ? Path.Combine(gamePath, @"BIOGame\PCConsoleTOC.bin") : null; } }
        public static string cookedPath { get { return (gamePath != null) ? Path.Combine(gamePath, @"BIOGame\CookedPCConsole\") : null; } }
        public static string DLCPath { get { return (gamePath != null) ? Path.Combine(gamePath , @"BIOGame\DLC\") : null; } }

        public static string BioWareDocPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\BioWare\Mass Effect 3\"; } }
        public static string GamerSettingsIniFile { get { return BioWareDocPath + @"BIOGame\Config\GamerSettings.ini"; } }

        public static string DLCFilePath(string DLCName)
        {
            string fullPath = DLCPath + DLCName + @"\CookedPCConsole\Default.sfar";
            if (File.Exists(fullPath))
                return fullPath;
            else
                throw new FileNotFoundException("Invalid DLC path " + fullPath);
        }

        static ME3Directory()
        {
            string hkey32 = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
            string hkey64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\";
            string subkey = @"BioWare\Mass Effect 3";
            string keyName;

            keyName = hkey32 + subkey;
            string test = (string)Microsoft.Win32.Registry.GetValue(keyName, "Install Dir", null);
            if (test != null)
            {
                gamePath = test;
                return;
            }


            keyName = hkey64 + subkey;
            gamePath = (string)Microsoft.Win32.Registry.GetValue(keyName, "Install Dir", null);
        }

        public static IEnumerable<string> Files
        {
            get
            {
                if (ME3Directory.BaseGameFiles != null)
                    return ME3Directory.BaseGameFiles.Concat(ME3Directory.DLCFiles);
                else
                    return null;
            }
        }
    }
}

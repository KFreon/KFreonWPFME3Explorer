using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KFreonLibME.MEDirectories
{
    public static class ME1Directory
    {
        public static bool DoesGameExist
        {
            get
            {
                return _gamePath == null ? false : Directory.Exists(_gamePath);
            }
        }

        private static List<String> baseGameFiles = null;
        public static List<string> BaseGameFiles { 
            get
            {
                // KFreon: Populate baseGameFiles if necessary and path is correct.
                if (baseGameFiles == null && !String.IsNullOrEmpty(ME1Directory.cookedPath))
                    baseGameFiles = MEDirectories.EnumerateGameFiles(1, ME1Directory.cookedPath);

                return baseGameFiles;
            }
        }

        private static List<string> dlcFiles = null;
        public static List<string> DLCFiles
        {
            get
            {
                if (dlcFiles == null && !String.IsNullOrEmpty(ME1Directory.DLCPath))
                    dlcFiles = MEDirectories.EnumerateGameFiles(1, ME1Directory.DLCPath);
                
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
                    if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
                        exepath = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).First(t => t.ToUpperInvariant().Contains("MASSEFFECT.EXE"));
                }

                return exepath;
            }
        }

        public static string cookedPath { get { return (gamePath != null) ? Path.Combine(gamePath, @"BioGame\CookedPC\") : null; } }
        public static string DLCPath { get { return (gamePath != null) ? Path.Combine(gamePath, @"DLC\") : null; } }

        public static string BioWareDocPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\BioWare\Mass Effect\"; } }
        public static string GamerSettingsIniFile { get { return BioWareDocPath + @"BIOGame\Config\GamerSettings.ini"; } }

        public static string DLCFilePath(string DLCName)
        {
            string fullPath = DLCPath + DLCName + @"\CookedPC\";
            if (File.Exists(fullPath))
                return fullPath;
            else
                throw new FileNotFoundException("Invalid DLC path " + fullPath);
        }

        static ME1Directory()
        {
            string hkey32 = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
            string hkey64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\";
            string subkey = @"BioWare\Mass Effect";
            string keyName;

            keyName = hkey32 + subkey;
            string test = (string)Microsoft.Win32.Registry.GetValue(keyName, "Path", null);
            if (test != null)
            {
                gamePath = test;
                return;
            }

            keyName = hkey64 + subkey;
            gamePath = (string)Microsoft.Win32.Registry.GetValue(keyName, "Path", null);
        }

        public static IEnumerable<string> Files
        {
            get
            {
                if (ME1Directory.BaseGameFiles != null)
                    return ME1Directory.BaseGameFiles.Concat(ME1Directory.DLCFiles);
                else
                    return null;
            }
        }
    }
}

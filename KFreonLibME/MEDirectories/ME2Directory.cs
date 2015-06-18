using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UsefulThings;

namespace KFreonLibME.MEDirectories
{
    public static class ME2Directory
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
                    archives = MEDirectories.EnumerateGameFiles(2, BaseGameFiles, f => f.EndsWith(".tfc"));
                    if (archives.Count == 0)
                    {
                        archives = null;
                        return null;
                    }
                    archives.AddRange(MEDirectories.EnumerateGameFiles(2, DLCFiles, f => f.EndsWith(".tfc")));
                }

                return archives;
            }
        }

        private static List<String> baseGameFiles = null;
        public static List<string> BaseGameFiles
        {
            get
            {
                if (baseGameFiles == null && !String.IsNullOrEmpty(ME2Directory.cookedPath))                        
                    baseGameFiles = MEDirectories.EnumerateGameFiles(2, ME2Directory.cookedPath);

                return baseGameFiles;
            }
        }


        private static List<string> dlcFiles = null;
        public static List<string> DLCFiles
        {
            get
            {
                if (dlcFiles == null && !String.IsNullOrEmpty(ME2Directory.DLCPath))
                    dlcFiles = MEDirectories.EnumerateGameFiles(2, ME2Directory.DLCPath);
                
                return dlcFiles;
            }
        }

        private static string _gamePath = null;
        public static string gamePath
        {
            get // if you are trying to use gamePath variable and it's null it asks to locate ME3 exe file
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
                        exepath = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).First(t => t.Contains("MASSEFFECT2.EXE", StringComparison.CurrentCultureIgnoreCase));
                }

                return exepath;
            }
        }

        public static string cookedPath { get { return (gamePath != null) ? Path.Combine(gamePath,  @"BioGame\CookedPC\") : null; } }
        public static string DLCPath { get { return (gamePath != null) ? Path.Combine(gamePath, @"BioGame\DLC\") : null; } }

        public static string BioWareDocPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\BioWare\Mass Effect 2\"; } }
        public static string GamerSettingsIniFile { get { return BioWareDocPath + @"BIOGame\Config\GamerSettings.ini"; } }

        public static string DLCFilePath(string DLCName)
        {
            string fullPath = DLCPath + DLCName + @"\CookedPC";
            if (File.Exists(fullPath))
                return fullPath;
            else
                throw new FileNotFoundException("Invalid DLC path " + fullPath);
        }

        static ME2Directory()
        {
            string hkey32 = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
            string hkey64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\";
            string subkey = @"BioWare\Mass Effect 2";
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
                if (ME2Directory.BaseGameFiles != null)
                    return ME2Directory.BaseGameFiles.Concat(ME2Directory.DLCFiles);
                else
                    return null;
            }
        }

    }
}

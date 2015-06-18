using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KFreonLibGeneral.Debugging;
using UsefulThings;
using UsefulThings.WinForms.UsefulForms;

namespace KFreonLibME.PCCObjects
{
    /// <summary>
    /// Provides miscellaneous methods related to PCC's.
    /// </summary>
    public static class Misc
    {
        // KFreon: List of valid game files .
        static Dictionary<string, List<string>> ValidFiles = new Dictionary<string, List<string>>();



        /// <summary>
        /// Decompresses a PCC using external tool.
        /// </summary>
        /// <param name="path">Path to PCC.</param>
        public static void PCCDecompress(string path)
        {
            string loc = Path.GetDirectoryName(Application.ExecutablePath);
            KFreonLibME.Misc.Methods.RunShell(loc + "\\exec\\Decompress.exe", "\"" + path + "\"");
        }


        /// <summary>
        /// Searches for PCC by name in a certain game specified by PathInclDLC. Can also use expID to narrow search if required. Return name of PCC, empty if not found.
        /// </summary>
        /// <param name="pccname">Name of PCC to search for.</param>
        /// <param name="PathInclDLC">Path encasing BIOGame and DLC. i.e. Parent folder containing folders BIOGame and DLC.</param>
        /// <param name="expID">ExpID of an object inside the desired PCC.</param>
        /// <param name="isTexture">True = objectName specifies a texture.</param>
        /// <param name="objectName">Type of object (Texture, binary)</param>
        public static List<string> SearchForPCC(string pccname, string PathInclDLC, int expID, string objectName, bool isTexture)
        {
            // KFreon: Lowercase PCC name
            string name = Path.GetFileName(pccname);
            List<string> searchResults = new List<string>();

            string key = PathInclDLC.ToLowerInvariant();

            // KFreon: Create lists of valid files if necessary
            lock (ValidFiles)
                if (!ValidFiles.ContainsKey(key))
                {
                    if (Directory.Exists(PathInclDLC))
                    {
                        List<string> temp = Directory.EnumerateFiles(PathInclDLC, "*", SearchOption.AllDirectories).Where(pcc => pcc.EndsWith(".pcc", StringComparison.CurrentCultureIgnoreCase) || pcc.EndsWith(".u", StringComparison.CurrentCultureIgnoreCase) || pcc.EndsWith(".sfm", StringComparison.CurrentCultureIgnoreCase) || pcc.EndsWith(".upk", StringComparison.CurrentCultureIgnoreCase)).ToList(2000);
                        ValidFiles.Add(key, temp);
                    }
                    else
                        return null;
                }

            // KFreon: Attempt to find PCC
            searchResults.AddRange(ValidFiles[key].Where(pcc => pcc.Contains(name, StringComparison.CurrentCultureIgnoreCase)));

            List<string> results = new List<string>();

            // KFreon: Do special stuff if multiple files found
            if (searchResults.Count > 1)
            {
                // KFreon: If expID given, use it to try to discern correct pcc
                if (expID != -1)
                {
                    List<string> temp = new List<string>();
                    foreach (string file in searchResults)
                    {
                        // KFreon: Only work on stuff if file is correct given the provided information
                        if (CheckSearchedTexture(file, expID, objectName))
                        {
                            temp.Add(file);

                            // KFreon: See if DLC is relevent
                            string dlcname = MEDirectories.MEDirectories.GetDLCNameFromPath(pccname);
                            if (dlcname != "")
                            {
                                temp.Clear();
                                temp.Add(file);
                                break;
                            }
                        }
                    }
                    if (temp.Count == 1)
                        results.Add(temp[0]);
                    else if (temp.Count > 1)
                    {
                        // KFreon: If still multiple files found, break things.
                        using (SelectionForm sf = new SelectionForm(temp, "LET ME KNOW ABOUT THIS PLEASE!!", "Oh dang. More work for me.", false))
                            sf.Show();

                        results.AddRange(temp);
                        DebugOutput.PrintLn("Multiple pccs found for: " + pccname);
                        foreach (string item in temp)
                            DebugOutput.PrintLn(item);
                        DebugOutput.PrintLn();
                    }
                }
            }
            else if (searchResults.Count == 1) // KFreon: Return correct texture based on whether or not it's a texture.
            {
                if (isTexture && CheckSearchedTexture(searchResults[0], expID, objectName) || !isTexture)
                    results.Add(searchResults[0]);
            }

            return results;
        }


        /// <summary>
        /// Checks a searched texture's details are correct, i.e. expected expID and object type.
        /// </summary>
        /// <param name="file">Searched texture filename.</param>
        /// <param name="expID">Found expID.</param>
        /// <param name="objectName">Type of object (texture, mesh)</param>
        /// <returns>True = details are correct. False otherwise.</returns>
        private static bool CheckSearchedTexture(string file, int expID, string objectName)
        {
            // KFreon: Get game version from name
            //List<string> parts = new List<string>(file.Split(' '));
            int ind = file.LastIndexOf("Effect") + 8;
            int whichgame = 0;
            if (!int.TryParse("" + file[ind], out whichgame))
                whichgame = 1;

            // KFreon: Test if this files' expID is the one we want
            AbstractPCCObject pcc = AbstractPCCObject.Create(file, whichgame, "");

            // KFreon: First check if there's enough expID's in current file, then if we're looking at a texture in current file
            if (pcc.Exports.Count >= expID && pcc.Exports[expID].ValidTextureClass())
            {
                bool nametest = (objectName == null ? true : pcc.Exports[expID].ObjectName.Contains(objectName, StringComparison.CurrentCultureIgnoreCase));
                return nametest;
            }
            return false;
        }


        /// <summary>
        /// Search for PCC's in all games. Returns number of game PCC's belongs to.
        /// </summary>
        /// <param name="pccs">PCC's to search through.</param>
        /// <param name="pathBIOs">List of BIOGame paths, can contain nulls for non-existent games. MUST have 3 elements.</param>
        /// <param name="expIDs">List of ExpID's matching the provided PCC's. MUST have the same number of elements as PCC's.</param>
        /// <param name="entries">List of PCC entries in a ModJob.</param>
        /// <param name="isTexture">Denotes whether to search for a texture or not.</param>
        /// <param name="objectName">Object type (texture, mesh)</param>
        /// <returns>Game version of found PCC.</returns>
        public static int SearchForPCC(List<string> pccs, List<string> pathBIOs, List<int> expIDs, string objectName, bool isTexture, List<PCCEntry> entries = null)
        {
            int game = -1;
            List<string> FoundGames = new List<string>();

            // KFreon: Search all 3 games
            for (int i = 0; i < 3; i++)
            {
                // KFreon: keep track of any multiple search results, index of this event, and the game targeted at the time
                List<string> multiples = new List<string>();
                List<int> MultiIndicies = new List<int>();   
                List<int> MultiGames = new List<int>();

                // KFreon: Skip if game not found
                if (pathBIOs[i] == "")
                    continue;

                // KFreon: Search current game for all given pccs
                List<int> games = new List<int>();
                int count = entries == null ? pccs.Count : entries.Count;
                for (int j = 0; j < count; j++)
                {
                    string pcc = entries == null ? pccs[j] : entries[j].File;
                    int expid = entries == null ? expIDs[j] : entries[j].ExpID;
                    List<string> results = SearchForPCC(pcc, (i == 0) ? pathBIOs[i].GetDirParent() : pathBIOs[i], expid, objectName, isTexture);

                    if (results.Count > 0)
                    {
                        multiples.AddRange(results);
                        MultiIndicies.Add(games.Count);
                        MultiGames.Add(i + 1);
                    }

                    if (results.Count == 1)  // KFreon: If pcc found
                        games.Add(i + 1);
                    else
                        games.Add(-1);
                }

                // KFreon: Deal with multiples
                if (multiples.Count > 0)
                {
                    // KFreon: See if sets of multiples are needed. i.e. a pair of files are both being modified (PROBABLY most common)
                    bool found = false;
                    for (int k = 0; k < multiples.Count; k++)
                    {
                        if (found)
                            break;

                        string pcc1 = multiples[k];
                        for (int j = k + 1; j < multiples.Count; j++)
                        {
                            string pcc2 = multiples[j];
                            if (pcc1 == pcc2)
                            {
                                found = true;
                                games[MultiIndicies[k]] = MultiGames[k];            
                            }
                        }
                    }

                    // KFreon: If multiples still unresolved
                    if (!found)
                    {
                        using (SelectionForm sf = new SelectionForm(multiples, "LET ME KNOW ABOUT THIS PLEASE!!", "Oh dang. More work for me.", false))
                            sf.Show();
                        // show selection
                        // TODO: KFREON add selection ability
                    }
                }


                // KFreon: Look at results and decide what to do
                bool correct = !games.Contains(-1);
                game = correct ? games.First(gam => gam != -1) : -1;

                if (!correct && game != -1)
                    FoundGames.Add(game.ToString() + "|" + games.Where(gm => gm != -1));
                else if (correct && game != -1)
                {
                    FoundGames.Clear();
                    break;
                }
            }

            // KFreon: Multiple game entries found.
            if (FoundGames.Count != 0)
            {
                int ind = -1;
                int max = -1;

                // KFreon: Find game with highest number of matches
                for (int i = 0; i < FoundGames.Count; i++)
                {
                    int num = int.Parse(FoundGames[0].Substring(FoundGames[0].LastIndexOf('|')));  // 0 should be i?

                    if (num > max)
                    {
                        max = num;
                        ind = i;
                    }
                }

                game = int.Parse(FoundGames[ind][0] + "");
            }
            return game;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.IO;
using KFreonLibGeneral.Debugging;
using UsefulThings;

namespace KFreonLibME.PCCObjects
{
    public class ME1PCCObject : AbstractPCCObject
    {
        public ME1PCCObject(string path)
            : base(path)
        {
            GameVersion = 1;
            LoadFromStream(tempStream);
            tempStream.Dispose();
        }

        public ME1PCCObject(string path, MemoryTributary stream)
            : base(path)
        {
            GameVersion = 1;
            LoadFromStream(stream);
        }

        public override void SaveToFile(string path)
        {
            DebugOutput.PrintLn("Writing pcc to: " + path + "\nRefreshing header to stream...");
            ListStream.Seek(0, SeekOrigin.Begin);
            ListStream.WriteBytes(header);
            DebugOutput.PrintLn("Opening filestream and writing to disk...");
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                ListStream.WriteTo(fs);
            }
        }
    }
}

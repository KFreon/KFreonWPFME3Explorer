using UsefulThings;

namespace KFreonLibME.PCCObjects
{
    public class ME2PCCObject : AbstractPCCObject
    {
        public ME2PCCObject(string path)
            : base(path)
        {
            GameVersion = 2;
            LoadFromStream(tempStream);
            tempStream.Dispose();
        }

        public ME2PCCObject(string path, MemoryTributary stream)
            : base(path)
        {
            GameVersion = 2;
            LoadFromStream(stream);
        }
    }
}

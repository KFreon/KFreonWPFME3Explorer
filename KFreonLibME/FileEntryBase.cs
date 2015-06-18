using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings;

namespace KFreonLibME
{
    public abstract class FileEntryBase : ISearchable
    {
        public List<FileEntry> Files { get; set; }
        public ICollectionView FilesView { get; set; }
        public virtual string ValidityString { get; set; }
        public string ThresholdString { get; set; }
        public FileSystemInfo Info { get; set; }
        public bool IsChecked { get; set; }
        public bool IsScanned { get; set; }

        long length = 0;
        public long Length
        {
            get
            {
                if (length == 0)
                {
                    if (Info.GetType() == typeof(FileInfo))
                        length = ((FileInfo)Info).Length;
                    else
                    {
                        var test = ((DirectoryInfo)Info).EnumerateFiles("*", SearchOption.AllDirectories);
                        foreach (var file in test)
                            length += file.Length;
                    }
                        
                }

                return length;
            }
        }

        public FileEntryBase(string path)
        {
            if (path.isFile())
                Info = new FileInfo(path);
            else
                Info = new DirectoryInfo(path);

            IsChecked = true;
            ThresholdString = "Unknown";

            IsSearchVisible = true;
        }

        public bool IsSearchVisible { get; set; }
    }
}

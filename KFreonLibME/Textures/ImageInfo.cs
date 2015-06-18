using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFreonLibME.Textures
{
    public class ImageInfo
    {
        #region Properties
        public AmaroK86.ImageFormat.ImageSize ImgSize { get; set; }

        public int Offset { get; set; }

        public int GameVersion { get; set; }

        PCCStorageType stor = PCCStorageType.empty;
        public PCCStorageType storageType
        {
            get
            {
                return stor;
            }
            set
            {
                if (GameVersion == 3 && (int)value == 3)
                    stor = PCCStorageType.arcCpr;
                else
                    stor = value;
            }
        }

        public int UncSize { get; set; }

        public int CprSize { get; set; }
        #endregion Properties

        public ImageInfo()
        {
        }

        public ImageInfo(AmaroK86.ImageFormat.ImageSize imgsize, int offset, int gameversion, PCCStorageType storage, int uncsize, int cprsize)
        {
            ImgSize = imgsize;
            Offset = offset;
            GameVersion = gameversion;
            storageType = storage;
            UncSize = uncsize;
            CprSize = cprsize;
        }     
    }
}

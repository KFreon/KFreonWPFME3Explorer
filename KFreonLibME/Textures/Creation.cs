using AmaroK86.ImageFormat;
using KFreonLibGeneral.Debugging;
using KFreonLibME.PCCObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFreonLibME.Textures
{
    /// <summary>
    /// Provides functions to create texture objects.
    /// </summary>
    public static class Creation
    {
        #region Object Creators
        /// <summary>
        /// Load an image into one of AK86's classes.
        /// </summary>
        /// <param name="im">AK86 image already, just return it unless null. Then load from fileToLoad.</param>
        /// <param name="fileToLoad">Path to file to be loaded. Irrelevent if im is provided.</param>
        /// <returns>AK86 Image file.</returns>
        public static ImageFile LoadAKImageFile(string fileToLoad)
        {
            ImageFile imgFile = null;

            if (!File.Exists(fileToLoad))
                throw new FileNotFoundException("invalid file to replace: " + fileToLoad);

            // check if replacing image is supported
            string fileFormat = Path.GetExtension(fileToLoad);
            switch (fileFormat)
            {
                case ".dds": imgFile = new DDS(fileToLoad, null); break;
                case ".tga": imgFile = new TGA(fileToLoad, null); break;
                default: throw new FileNotFoundException(fileFormat + " image extension not supported");
            }
            
            return imgFile;
        }


        /// <summary>
        /// Load an image into one of AK86's classes.
        /// </summary>
        /// <param name="im">AK86 image already, just return it unless null. Then load from fileToLoad.</param>
        /// <param name="fileToLoad">Path to file to be loaded. Irrelevent if im is provided.</param>
        /// <returns>AK86 Image file.</returns>
        public static ImageFile LoadAKImageFile(byte[] imgData)
        {
            ImageFile imgFile = null;

            try
            {
                imgFile = new DDS(null, imgData);
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Failed to load image as AK", "AKImage", e);
            }

            return imgFile;
        }
        #endregion
    }
}

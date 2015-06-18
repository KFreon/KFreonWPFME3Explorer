using AmaroK86.ImageFormat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.IO;
using System.Drawing;
using DDSPreview = KFreonLibME.Textures.SaltDDSPreview.DDSPreview;
using System.Drawing.Drawing2D;
using SaltTPF;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using KFreonLibME.PCCObjects;
using KFreonLibGeneral.Debugging;
using ResILWrapper;
using UsefulThings;

namespace KFreonLibME.Textures
{
    /// <summary>
    /// Provides methods related to textures.
    /// </summary>
    public static class Methods
    {
        #region HASHES
        /// <summary>
        /// Finds hash from texture name given list of PCC's and ExpID's.
        /// </summary>
        /// <param name="name">Name of texture.</param>
        /// <param name="Files">List of PCC's to search with.</param>
        /// <param name="IDs">List of ExpID's to search with.</param>
        /// <param name="TreeTexes">List of tree textures to search through.</param>
        /// <returns>Hash if found, else 0.</returns>
        public static uint FindHashByName(string name, List<string> Files, List<int> IDs, List<TreeTexInfo> TreeTexes)
        {
            foreach (TreeTexInfo tex in TreeTexes)
                if (name == tex.EntryName)
                    for (int i = 0; i < Files.Count; i++)
                        for (int j = 0; j < tex.PCCs.Count; j++)
                            //if (tex.PCCs[j].Contains(Files[i].Replace("\\\\", "\\")) && tex.ExpIDs[j] == IDs[i])
                                return tex.Hash;
            return 0;
        }


        /// <summary>
        /// Returns a uint of a hash in string format. 
        /// </summary>
        /// <param name="line">String containing hash in texmod log format of name|0xhash.</param>
        /// <returns>Hash as a uint.</returns>
        public static uint FormatTexmodHashAsUint(string line)
        {
            return uint.Parse(line.Split('|')[0].Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
        }


        /// <summary>
        /// Returns hash as a string in the 0xhash format.
        /// </summary>
        /// <param name="hash">Hash as a uint.</param>
        /// <returns>Hash as a string.</returns>
        public static string FormatTexmodHashAsString(uint hash)
        {
            return "0x" + System.Convert.ToString(hash, 16).PadLeft(8, '0').ToUpper();
        }
        #endregion

        public static TextureFormat ParseTextureFormat(string textureFormat)
        {
            TextureFormat format;
            if (textureFormat.Contains("PF_"))
                textureFormat = textureFormat.Remove(0, 3);


            if (!Enum.TryParse<TextureFormat>(textureFormat, true, out format))
            {
                if (textureFormat.Contains("NormalMap", StringComparison.CurrentCultureIgnoreCase) || textureFormat.Contains("ATI2", StringComparison.CurrentCultureIgnoreCase))
                    format = TextureFormat.ThreeDC;
                else 
                    format = TextureFormat.Unknown;
            }

            return format;
        }

        public static bool IsTextureFormatDDS(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.A8R8G8B8:
                case TextureFormat.DXT1:
                case TextureFormat.DXT2:
                case TextureFormat.DXT3:
                case TextureFormat.DXT4:
                case TextureFormat.DXT5:
                case TextureFormat.G8:
                case TextureFormat.ThreeDC:
                case TextureFormat.V8U8:
                case TextureFormat.ATI1:
                    return true;
                default:
                    return false;
            }
        }

        public static ResIL.Unmanaged.CompressedDataFormat ConvertTextureFormatToResIL(TextureFormat format, out ResIL.Unmanaged.ImageType type)
        {
            type = ResIL.Unmanaged.ImageType.Dds;
            ResIL.Unmanaged.CompressedDataFormat surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.None;

            switch (format)
            {
                case TextureFormat.A8R8G8B8:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.None;
                    break;
                case TextureFormat.ThreeDC:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.ThreeDC;
                    break;
                case TextureFormat.BMP:
                    type = ResIL.Unmanaged.ImageType.Bmp;
                    break;
                case TextureFormat.DXT1:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.DXT1;
                    break;
                case TextureFormat.DXT2:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.DXT2;
                    break;
                case TextureFormat.DXT3:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.DXT3;
                    break;
                case TextureFormat.DXT4:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.DXT4;
                    break;
                case TextureFormat.DXT5:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.DXT5;
                    break;
                case TextureFormat.G8:  // TODO More here?
                    break;
                case TextureFormat.GIF:
                    type = ResIL.Unmanaged.ImageType.Gif;
                    break;
                case TextureFormat.JPG:
                    type = ResIL.Unmanaged.ImageType.Jpg;
                    break;
                case TextureFormat.ATI1:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.ATI1N;
                    break;
                case TextureFormat.PNG:
                    type = ResIL.Unmanaged.ImageType.Png;
                    break;
                /*case TextureFormat.ThreeDc:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.ThreeDC;
                    break;*/
                case TextureFormat.TIFF:
                    type = ResIL.Unmanaged.ImageType.Tiff;
                    break;
                case TextureFormat.V8U8:
                    surfaceFormat = ResIL.Unmanaged.CompressedDataFormat.V8U8;
                    break;
            }

            return surfaceFormat;
        }

        public static TextureFormat ConvertResILToTextureFormat(ResIL.Unmanaged.ImageType type, ResIL.Unmanaged.CompressedDataFormat surfaceFormat)
        {
            TextureFormat format = TextureFormat.Unknown;
            switch (type)
            {
                case ResIL.Unmanaged.ImageType.Dds:
                    switch (surfaceFormat)
                    {
                        case ResIL.Unmanaged.CompressedDataFormat.ATI1N:
                            format = TextureFormat.ATI1;
                            break;
                        case ResIL.Unmanaged.CompressedDataFormat.ThreeDC:
                            format = TextureFormat.ThreeDC;
                            break;
                        case ResIL.Unmanaged.CompressedDataFormat.DXT1:
                            format = TextureFormat.DXT1;
                            break;
                        case ResIL.Unmanaged.CompressedDataFormat.DXT2:
                            format = TextureFormat.DXT2;
                            break;
                        case ResIL.Unmanaged.CompressedDataFormat.DXT3:
                            format = TextureFormat.DXT3;
                            break;
                        case ResIL.Unmanaged.CompressedDataFormat.DXT4:
                            format = TextureFormat.DXT4;
                            break;
                        case ResIL.Unmanaged.CompressedDataFormat.DXT5:
                            format = TextureFormat.DXT5;
                            break;
                        case ResIL.Unmanaged.CompressedDataFormat.None:
                            format = TextureFormat.A8R8G8B8;
                            break;
                        case ResIL.Unmanaged.CompressedDataFormat.V8U8:
                            format = TextureFormat.V8U8;
                            break;
                    }
                    break;
                case ResIL.Unmanaged.ImageType.Bmp:
                    format = TextureFormat.BMP;
                    break;
                case ResIL.Unmanaged.ImageType.Gif:
                    format = TextureFormat.GIF;
                    break;
                case ResIL.Unmanaged.ImageType.Jpg:
                    format = TextureFormat.JPEG;
                    break;
                case ResIL.Unmanaged.ImageType.Png:
                    format = TextureFormat.PNG;
                    break;
            }

            return format;
        }
    }
}

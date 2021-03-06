﻿//This class was generated by ME3Explorer
//Author: Warranty Voider
//URL: http://sourceforge.net/projects/me3explorer/
//URL: http://me3explorer.freeforums.org/
//URL: http://www.facebook.com/pages/Creating-new-end-for-Mass-Effect-3/145902408865659
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ME3Explorer.Unreal;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using BitConverter = KFreonLibGeneral.Misc.BitConverter;

namespace ME3Explorer.Unreal.Classes
{
    public class BioAnimSetData
    {
        #region Unreal Props

        //Bool Properties

        public bool bAnimRotationOnly = false;
        //Array Properties

        public List<string> TrackBoneNames;
        public List<string> UseTranslationBoneNames;

        #endregion

        public int MyIndex;
        public PCCObject pcc;
        public byte[] data;
        public List<PropertyReader.Property> Props;

        public BioAnimSetData(PCCObject Pcc, int Index)
        {
            pcc = Pcc;
            MyIndex = Index;
            if (pcc.isExport(Index))
                data = pcc.Exports[Index].Data;
            Props = PropertyReader.getPropList(pcc, data);
            BitConverter.IsLittleEndian = true;
            TrackBoneNames = new List<string>();
            UseTranslationBoneNames = new List<string>();
            foreach (PropertyReader.Property p in Props)
                switch (pcc.getNameEntry(p.Name))
                {

                    case "bAnimRotationOnly":
                        if (p.raw[p.raw.Length - 1] == 1)
                            bAnimRotationOnly = true;
                        break;
                    case "TrackBoneNames":
                        ReadTBN(p.raw);
                        break;
                    case "UseTranslationBoneNames":
                        ReadUTBN(p.raw);
                        break;
                }
        }

        public void ReadTBN(byte[] raw)
        {
            int count = GetArrayCount(raw);
            byte[] buff = GetArrayContent(raw);
            for (int i = 0; i < count; i++)
                TrackBoneNames.Add(pcc.getNameEntry(BitConverter.ToInt32(buff, i * 8)));
        }

        public void ReadUTBN(byte[] raw)
        {
            int count = GetArrayCount(raw);
            byte[] buff = GetArrayContent(raw);
            for (int i = 0; i < count; i++)
                UseTranslationBoneNames.Add(pcc.getNameEntry(BitConverter.ToInt32(buff, i * 8)));
        }

        public int GetArrayCount(byte[] raw)
        {
            return BitConverter.ToInt32(raw, 24);
        }

        public byte[] GetArrayContent(byte[] raw)
        {
            byte[] buff = new byte[raw.Length - 28];
            for (int i = 0; i < raw.Length - 28; i++)
                buff[i] = raw[i + 28];
            return buff;
        }


        public TreeNode ToTree()
        {
            TreeNode res = new TreeNode(pcc.Exports[MyIndex].ObjectName + "(#" + MyIndex + ")");
            res.Nodes.Add("bAnimRotationOnly : " + bAnimRotationOnly);
            res.Nodes.Add(TBNToTree());
            res.Nodes.Add(UTBNToTree());
            return res;
        }

        public TreeNode TBNToTree()
        {
            TreeNode res = new TreeNode("TrackBoneNames");
            int count = 0;
            foreach (string s in TrackBoneNames)
                res.Nodes.Add((count++) + " : " + s);
            return res;
        }

        public TreeNode UTBNToTree()
        {
            TreeNode res = new TreeNode("UseTranslationBoneNames");
            int count = 0;
            foreach (string s in UseTranslationBoneNames)
                res.Nodes.Add((count++) + " : " + s);
            return res;
        }

    }
}
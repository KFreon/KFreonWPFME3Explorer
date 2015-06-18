using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ME3Explorer.Unreal;
using ME2Explorer;
using System.IO;
using AmaroK86.ImageFormat;
using KFreonLibME.MEDirectories;
using KFreonLibME.Textures;
using KFreonLibME.PCCObjects;
using KFreonLibME;

namespace ME3Explorer.Texture_Tool
{
    public partial class TextureTool : Form
    {
        List<METexture2D> textures;
        METexture2D tex2D;
        string pathCooked;
        KFreonLibME.PCCObjects.ME3PCCObject pcc;
        int numFiles;
        string exec;

        public TextureTool()
        {
            InitializeComponent();
            exec = Path.GetDirectoryName(Application.ExecutablePath) + "\\exec\\";
            LoadMe();
        }

        private void LoadMe()
        {
            pathCooked = ME3Directory.cookedPath;
            textures = new List<METexture2D>();
            tex2D = new METexture2D();
            tex2D.PCCs.Add(new PCCEntry(pathCooked + "BioD_Nor_100Cabin.pcc", 2763));
            tex2D.pccExpIdx = 2763;
            textures.Add(tex2D);
        }

        private void PropertiesPopup()
        {
            AmaroK86.ImageFormat.ImageSize imgSize = tex2D.imgList.Max(image => image.ImgSize);

            string LOD;
            if (String.IsNullOrEmpty(tex2D.LODGroup))
            {
                LOD = "No LODGroup (Uses World)";
            }
            else
                LOD = tex2D.LODGroup;
            string arc;
            if (String.IsNullOrEmpty(tex2D.arcName))
            {
                arc = "\nPCC Stored";
            }
            else
                arc = "\nTexture Cache File: " + tex2D.arcName + ".tfc";

            string mesg = "Information about: " + tex2D.texName;
            mesg += "\nFormat: " + tex2D.texFormat;
            mesg += "\nWidth: " + imgSize.width + ", Height: " + imgSize.height;
            mesg += "\nLODGroup: " + LOD;
            mesg += arc;
            mesg += "\nOriginal Location: " + tex2D.PCCs[0].File;
            for (int i = 1; i < tex2D.PCCs.Count; i++)
                mesg += "\nAlso found in: " + tex2D.PCCs[i].File;

            MessageBox.Show(mesg);
        }

        private void SetStatus(string val)
        {
            toolStripStatusLabel1.Text = val;
            Application.DoEvents();
        }

        public void ExecuteCommandSync(object command)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                Console.WriteLine(result);
            }
            catch
            {
                // Log the exception
            }
        }

        private void loadTex(int texIndex)
        {
            pcc = new KFreonLibME.PCCObjects.ME3PCCObject(textures[texIndex].PCCs[0].File);
            tex2D = new METexture2D(pcc, textures[texIndex].pccExpIdx, Path.GetDirectoryName(pathCooked));
            tex2D.pccExpIdx = textures[texIndex].pccExpIdx;
            tex2D.PCCs = textures[texIndex].PCCs;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            loadTex(0);
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "DDS Files|*.dds";
            open.Title = "Please select the file to mod with";
            if (open.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            try
            {
                byte[] imgData = UsefulThings.General.GetExternalData(open.FileName);
                if (imgData != null)
                {
                    tex2D.OneImageToRuleThemAll(pathCooked, imgData);
                    SaveChanges();
                }

            }
            catch (Exception exc)
            {
                MessageBox.Show("An error occurred: \n" + exc);
            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            loadTex(0);
            //PreviewPopup();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            loadTex(0);
            PropertiesPopup();
        }

        private void SaveChanges()
        {
            numFiles = tex2D.PCCs.Count + 1;
            bgw1.RunWorkerAsync();
            pb1.Maximum = numFiles + 1;
        }

        private void bgw1_DoWork(object sender, DoWorkEventArgs e)
        {
            bgw1.ReportProgress(0);

            pcc = new ME3PCCObject(tex2D.PCCs[0].File);
            AbstractExportEntry expEntry = pcc.Exports[tex2D.pccExpIdx];
            expEntry.Data = tex2D.ToArray(pcc,  expEntry.DataOffset);
            pcc.Exports[tex2D.pccExpIdx] = expEntry;
            pcc.SaveToFile(tex2D.PCCs[0].File);

            
            tocUpdate();
        }

        private void bgw1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pb1.Value = e.ProgressPercentage;
            if (e.ProgressPercentage < numFiles)
                SetStatus(e.ProgressPercentage + " / " + numFiles + " saved...");
            else
                SetStatus("Updating PCConsoleTOC.bin...");
        }

        private void bgw1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pb1.Value = pb1.Maximum;
            SetStatus("Finished");
        }

        private void tocUpdate()
        {
            TOCUpdater.TOCUpdater toc = new TOCUpdater.TOCUpdater();
            toc.EasyUpdate();
        }
    }
}

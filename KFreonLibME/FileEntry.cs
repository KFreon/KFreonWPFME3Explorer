using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFreonLibME
{
    public class FileEntry : FileEntryBase
    {
        public bool? ValidDate { get; set; }
        public override string ValidityString
        {
            get
            {
                string retval = null;
                if (ValidDate == null)
                    retval = "DLC extraction dates unknown.";
                else
                {
                    if (ValidDate == true)
                        retval = "Unmodified";
                    else if (ValidDate == false)
                        retval = "MODIFIED!";
                    retval += IsScanned ? " since last scanned on: " : " on disk.";
                }
                
                return retval;
            }
        }

        public FileEntry(string filename, int ValidYear, int ValidMonth) : base(filename)
        {
            ValidDate = ValidYear >= Info.LastWriteTime.Year && ValidMonth >= Info.LastWriteTime.Month;

            Setup(new DateTime(ValidYear, ValidMonth, 1), (ValidYear != -1 && ValidMonth != -1));
        }

        public FileEntry(string filename, DateTime dlcExtractedDate) : base(filename)
        {
            ValidDate = dlcExtractedDate.CompareTo(Info.LastWriteTime) != -1;
            Setup(dlcExtractedDate, ValidDate == true);
        }

        public FileEntry(string filename, DateTime dlcExtractedDate, bool isScanned)
            : this(filename, dlcExtractedDate)
        {
            IsScanned = isScanned;
        }

        public FileEntry(string filename, int ValidYear, int ValidMonth, bool isScanned)
            : this(filename, ValidYear, ValidMonth)
        {
            IsScanned = isScanned;
        }


        private void Setup(DateTime date, bool dlcValid) 
        {
            ThresholdString = date.ToShortDateString();
        }
    }
}

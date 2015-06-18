using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KFreonLibGeneral.Debugging
{
    public class Error
    {
        public Exception exception { get; set; }
        public string ToolName { get; set; }
        public string Additional { get; set; }
        
        public Error(string additional, string toolname, Exception e)
        {
            Additional = additional;
            ToolName = toolname;
            exception = e;
        }
    }



    /// <summary>
    /// Provides a threadsafe (hopefully) set of debug logging output.
    /// </summary>
    public static class DebugOutput
    {
        public static ObservableCollection<Error> AllErrors { get; set; }

        static RichTextBox rtb;
        static StreamWriter debugFileWriter = null;
        static readonly object _sync = new object();
        static StringBuilder waiting = null;
        static DateTime LastPrint;
        static string DebugFilePath = null;

        private static Timer UpdateTimer;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 277;
        private const int SB_PAGEBOTTOM = 7;


        /// <summary>
        /// Scrolls to bottom of textbox, regardless of focus.
        /// </summary>
        /// <param name="MyRichTextBox">Textbox to scroll.</param>
        public static void ScrollToBottom(RichTextBox MyRichTextBox)
        {
            SendMessage(MyRichTextBox.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
        }


        /// <summary>
        /// Starts debugger if not already started. Prints basic info if required.
        /// </summary>
        /// <param name="toolName">Name of tool where debugging is to be started from.</param>
        public static void StartDebugger(string toolName)
        {
            if (AllErrors == null)
                AllErrors = new ObservableCollection<Error>();

            string appender = "";
            if (rtb == null)
            {
                UpdateTimer = new Timer();

                // KFreon: Deal with file in use
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        DebugFilePath = Path.GetDirectoryName(Application.ExecutablePath) + "\\DebugOutput" + appender + ".txt";
                        debugFileWriter = new StreamWriter(DebugFilePath);
                        break;
                    }
                    catch
                    {
                        var t = i;
                        appender = t.ToString();
                    }
                }

                if (debugFileWriter == null)
                    MessageBox.Show("Failed to open any debug output files. Disk cached debugging disabled for this session.");

                // KFreon: Thread debugger
                Task.Factory.StartNew(() =>
                {
                    DebugWindow debugger = new DebugWindow();
                    debugger.WindowState = FormWindowState.Minimized;
                    debugger.ShowDialog();
                }, TaskCreationOptions.LongRunning);

                waiting = new StringBuilder();

                // KFreon: Print basic info
                System.Threading.Thread.Sleep(200);
                PrintLn("-----New Execution of " + toolName + "-----");
                PrintLn(".........Environment Information.........");
                PrintLn("Build Version: " + KFreonLibGeneral.Misc.Methods.GetBuildVersion());
                PrintLn("OS Version: " + Environment.OSVersion);
                PrintLn("Architecture: " + (Environment.Is64BitOperatingSystem ? "x64 (64 bit)" : "x86 (32 bit)"));
                PrintLn("Using debug file: " + DebugFilePath);
                PrintLn("Using ResIL dll at: " + Path.Combine(UsefulThings.General.GetExecutingLoc(), "ResIL.dll"));
                PrintLn(".........................................");
                PrintLn();
            }
            else
                PrintLn("-----New Execution of " + toolName + "-----");
        }


        /// <summary>
        /// Sets textbox to output to.
        /// </summary>
        /// <param name="box">Textbox to output debug info to.</param>
        public static void SetBox(RichTextBox box)
        {
            try
            {
                LastPrint = DateTime.Now;
                UpdateTimer.Interval = 500;
                UpdateTimer.Tick += UpdateTimer_Tick;
                UpdateTimer.Enabled = true;
                rtb = box;
            }
            catch { }
        }


        /// <summary>
        /// Checks whether the specified textbox can be written to atm.
        /// </summary>
        /// <returns>True if it can be written to.</returns>
        private static bool CheckRTB()
        {
            return rtb != null && rtb.Parent != null && rtb.Parent.Created == true;
        }


        static void UpdateTimer_Tick(object sender, EventArgs e)
        {
            lock (_sync)
            {
                if (CheckRTB())
                {

                    if (waiting.Length != 0)
                    {
                        string temp = waiting.ToString();
                        waiting.Clear();
                        rtb.BeginInvoke(new Action(() =>
                        {
                            rtb.AppendText(temp);
                            ScrollToBottom(rtb);
                        }));

                        try
                        {
                            debugFileWriter.WriteLine(temp);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine();
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Prints a blank line to the textbox.
        /// </summary>
        public static void PrintLn()
        {
            PrintLn("");
        }

        /// <summary>
        /// Prints text on a new line.
        /// </summary>
        /// <param name="s">String to write to textbox.</param>
        public static void PrintLn(string s, string toolDisplayName, Exception e, params object[] bits)
        {
            if (waiting == null)
                return;

            lock (_sync)
            {
                string DTs = DateTime.Now.ToLongTimeString() + ":  " + (bits != null && bits.Length > 0 ? String.Format(s, bits) : s);

                // KFreon: Add error to list and format output accordingly
                if(e != null)
                {
                    AllErrors.Add(new Error(DTs, toolDisplayName, e));
                    waiting.AppendLine(DTs + e.Message);
                    waiting.AppendLine("-----------------------------------");
                    waiting.AppendLine(e.ToString());
                    waiting.AppendLine("-----------------------------------");
                }
                else
                {
                    // KFreon: Just print string
                    waiting.AppendLine(DTs);
                }
            }
        }


        public static void PrintLn(string s, params object[] bits)
        {
            PrintLn(s, "", null, bits);
        }

        /// <summary>
        /// Prints text to textbox.
        /// </summary>
        /// <param name="s">Text to print to textbox.</param>
        /// <param name="update">Updates textbox now if true.</param>
        public static void Print(string s)
        {
            lock (_sync)
            {
                waiting.Append(DateTime.Now.ToLongTimeString() + ":  " + s);
            }
        }


        /// <summary>
        /// Clears textbox.
        /// </summary>
        /// <param name="update">Updates textbox now if true.</param>
        public static void Clear(bool update = true)
        {
            lock (_sync)
            {
                if (CheckRTB())
                {
                    try
                    {
                        rtb.BeginInvoke(new Action(() => rtb.Clear()));
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
        }

        public static void NullifyRTB()
        {
            lock (_sync)
            {
                UpdateTimer.Stop();
                rtb = null;
                debugFileWriter.Dispose();
            }
        }


        /// <summary>
        /// KFreon: Provides similar functionality to Debug.WriteIf(). Prints text if condition is satisfied.
        /// </summary>
        /// <param name="condition">Condition. If true, message is printed to window.</param>
        /// <param name="message">Message to print.</param>
        /// <param name="update">Immediately update window (layover from PrintLn method).</param>
        public static void PrintLnIf(bool condition, string message)
        {
            if (condition)
                PrintLn(message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace SCCMaker
{
    public partial class Form1 : Form
    {

        public static int[] oddParityTranslationMatrix = { 0x80, 0x01, 0x02, 0x83, 0x04, 0x85, 0x86, 0x07, 0x08, 0x89, 0x8a, 0x0b, 0x8c, 0x0d, 0x0e, 0x8f,
                                                    0x10, 0x91, 0x92, 0x13, 0x94, 0x15, 0x16, 0x97, 0x98, 0x19, 0x1a, 0x9b, 0x1c, 0x9d, 0x9e, 0x1f,
                                                    0x20, 0xa1, 0xa2, 0x23, 0xa4, 0x25, 0x26, 0xa7, 0xa8, 0x29, 0x2a, 0xab, 0x2c, 0xad, 0xae, 0x2f,
                                                    0xb0, 0x31, 0x32, 0xb3, 0x34, 0xb5, 0xb6, 0x37, 0x38, 0xb9, 0xba, 0x3b, 0xbc, 0x3d, 0x3e, 0xbf,
                                                    0x40, 0xc1, 0xc2, 0x43, 0xc4, 0x45, 0x46, 0xc7, 0xc8, 0x49, 0x4a, 0xcb, 0x4c, 0xcd, 0xce, 0x4f,
                                                    0xd0, 0x51, 0x52, 0xd3, 0x54, 0xd5, 0xd6, 0x57, 0x58, 0xd9, 0xda, 0x5b, 0xdc, 0x5d, 0x5e, 0xdf,
                                                    0xe0, 0x61, 0x62, 0xe3, 0x64, 0xe5, 0xe6, 0x67, 0x68, 0xe9, 0xea, 0x6b, 0xec, 0x6d, 0x6e, 0xef,
                                                    0x70, 0xf1, 0xf2, 0x73, 0xf4, 0x75, 0x76, 0xf7, 0xf8, 0x79, 0x7a, 0xfb, 0x7c, 0xfd, 0xfe, 0x7f };


        public Font normalFont = new Font("Consolas", 12);
        public Font italicFont = new Font("Consolas", 12, FontStyle.Italic);
        public Font underlinedFont = new Font("Consolas", 12, FontStyle.Underline);
        public Font italicAndUnderlinedFont = new Font("Consolas", 12, FontStyle.Strikeout);

        List<Caption> captionList;

        public static string zeroTime = "00:00:00:00";

        ComponentResourceManager resources = new ComponentResourceManager(typeof(Form1));
        public int paintProblem = 0;
        public string paintProblemStr = "";
        private bool firstSave;
        bool startBoxGotFocus = false;
        bool endBoxGotFocus = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (Control c in Controls)
            {
                if (c.Font.Equals(SystemFonts.DefaultFont))
                    c.Font = SystemFonts.MessageBoxFont;
            }

            StreamReader fs = new StreamReader(Application.StartupPath + @"\CharacterCodes.txt");
            string[] input = fs.ReadToEnd().Split('\n');
            foreach (string charcode in input)
            {
                if (charcode.StartsWith("//")) //Skip the comment lines
                    continue;
                string _charcode = charcode.Replace("\r", "");
                string[] s = _charcode.Split('`');
                //System.Diagnostics.Debug.WriteLine(string.Join(",", s));
                if (s.Length == 3)
                {
                    Caption.charCodes.Add(s[0][0], s[1]);
                    Caption.specialCharRefs.Add(s[2], s[0][0]);
                }
                else if (s.Length == 2)
                {
                    Caption.charCodes.Add(s[0][0], s[1]);
                }
                else
                {
                    MessageBox.Show("Error reading character codes. Error: " + string.Join(",", s));
                }
            }
            //Fallback character, solid bar
            Caption.charCodes.Add('\0', "7f");
            Caption.specialCharRefs.Add("undefined", '\0');
            //Used for padding bytes to conform to byte -> word
            Caption.charCodes.Add('\b', "80");
            Caption.specialCharRefs.Add("wait", '\b');

            fs = new StreamReader(Application.StartupPath + @"\ControlCodes.txt");
            input = fs.ReadToEnd().Split('\n');
            foreach (string contcode in input)
            {
                if (contcode.StartsWith("//")) //Skip the comment lines
                    continue;
                string _contcode = contcode.Replace("\r", "");
                string[] s = _contcode.Split('\t');
                //System.Diagnostics.Debug.WriteLine(string.Join(",", s));
                if (s.Length == 3)
                {
                    int first = Convert.ToInt32(s[1], 16);
                    int second = (first << 8) | Convert.ToInt32(s[2], 16);
                    System.Diagnostics.Debug.WriteLine(string.Join(",", s) + " -> " + second.ToString("x"));
                    Caption.commandCodes.Add(s[0], second);
                }
                else if (s.Length == 2)
                {
                    System.Diagnostics.Debug.WriteLine(string.Join(",", s) + " -> " + Convert.ToInt32(s[1], 16).ToString("x"));
                    Caption.commandCodes.Add(s[0], Convert.ToInt32(s[1], 16));
                }
                else
                {
                    MessageBox.Show("Error reading control codes. Error: " + string.Join(",", s));
                }
            }

            pictureBox1.Refresh();
            captionTypeSelector.SelectedIndex = 0;

            statusBar.Text = "Ready.";
        }

        public byte getByteFromBools(bool[] arr)
        {
            byte val = 0;
            foreach (bool b in arr)
            {
                val <<= 1;
                if (b) val |= 1;
            }
            return val;
        }

        private void paintPictureBox(object sender, PaintEventArgs e)
        {
            if (richTextBox1.Text.Length < 1)
            {
                StringFormat align = new StringFormat();
                align.Alignment = StringAlignment.Center;
                align.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString("(The fully formatted caption\nwill be displayed here!)",
                    italicFont, Brushes.White, pictureBox1.DisplayRectangle, align);
                return;
            }
            if (paintProblem != 0)
            {
                StringFormat align = new StringFormat();
                align.Alignment = StringAlignment.Center;
                align.LineAlignment = StringAlignment.Center;
                switch (paintProblem)
                {
                    case 1: // No ending braces
                        e.Graphics.DrawString("The last command has\nno ending braces.\n\nThese things. --> >",
                            normalFont, Brushes.Red, pictureBox1.DisplayRectangle, align);
                        break;
                    case 2:
                        e.Graphics.DrawString("Unknown command:\n<" + paintProblemStr + ">",
                            normalFont, Brushes.Red, pictureBox1.DisplayRectangle, align);
                        break;
                    case 3:
                        e.Graphics.DrawString("Unknown colour name:\n" + paintProblemStr,
                            normalFont, Brushes.Red, pictureBox1.DisplayRectangle, align);
                        break;
                    case 4:
                        e.Graphics.DrawString("The function specified has an\nincorrect number of arguments.\n\nFunction specified:\n" + paintProblemStr,
                            normalFont, Brushes.Red, pictureBox1.DisplayRectangle, align);
                        break;
                    case 5:
                        e.Graphics.DrawString("Unknown character specified.\n\"" + paintProblemStr + "\"",
                            normalFont, Brushes.Red, pictureBox1.DisplayRectangle, align);
                        break;
                    default:
                        e.Graphics.DrawString("An unknown error occurred while rendering the caption.",
                            normalFont, Brushes.Red, pictureBox1.DisplayRectangle, align);
                        break;
                }
                paintProblem = 0;
                return;
            }
            string caption = richTextBox1.Text;
            int height = (int)e.Graphics.MeasureString("Ag", normalFont).Height;
            int width = (int)e.Graphics.MeasureString("O", normalFont).Width - 5; //O is typically the widest character in a font
            int curRow = (int)(rowSelector.Value - 1) * height;
            int curCol = 0;
            int alignment = 0;
            int curLine = 0;
            Brush curColour = Brushes.White;
            Font curFont = normalFont;
            string curStr = "";
            int i = 0;
            while (i < caption.Length)
            {
                if (caption[i] == '<')
                {
                    int j = 1;
                    if (i + j >= caption.Length)
                    {
                        forcePaintStop(1, "This should only pop up if \"<\" is the only character in the string.");
                        return;
                    }
                    string command = "";
                    while (caption[i + j] != '>')
                    {
                        if (i + j >= caption.Length - 1)
                        {
                            forcePaintStop(1, null);
                            return;
                        }
                        command += caption[i + j++];
                    }
                    //Draw the string before we start parsing the command
                    if (alignment == 1)
                        curCol = ((32 - removeControlCodes(caption.Split('\n')[curLine]).Length) / 2) * width;
                    else if (alignment == 2)
                        curCol = ((32 - removeControlCodes(caption.Split('\n')[curLine]).Length)) * width;
                    foreach (char c in curStr)
                    {
                        if (!Caption.charCodes.ContainsKey(c))
                        {
                            e.Graphics.DrawString("■", curFont, curColour, new PointF(curCol, curRow));
                            curCol += width;
                        }
                        else
                        {
                            e.Graphics.DrawString(c.ToString(), curFont, curColour, new PointF(curCol, curRow));
                            curCol += width;
                        }
                    }
                    alignment = 0;

                    string preString = curStr;
                    curStr = "";
                    i += j;

                    //Start parsing the command
                    switch (command.Split(' ')[0].ToLowerInvariant())
                    {
                        case "colour":
                        case "color":
                            if (command.Split(' ').Length == 1)
                            {
                                forcePaintStop(4, command);
                                return;
                            }
                            switch (command.Split(' ')[1].ToLowerInvariant())
                            {
                                case "white": curColour = Brushes.White; break;
                                case "green": curColour = Brushes.Green; break;
                                case "blue": curColour = Brushes.Blue; break;
                                case "cyan": curColour = Brushes.Cyan; break;
                                case "red": curColour = Brushes.Red; break;
                                case "yellow": curColour = Brushes.Yellow; break;
                                case "magenta": curColour = Brushes.Magenta; break;
                                default: forcePaintStop(3, command.Split(' ')[1]); break;
                            }
                            if (!preString.EndsWith(" ")) { curStr += ' '; }
                            break;
                        case "centre":
                        case "center":
                            alignment = 1;
                            break;
                        case "right":
                            alignment = 2;
                            break;
                        case "char":
                            if (command.Split(' ').Length == 1)
                            {
                                forcePaintStop(4, command);
                                return;
                            }
                            string cCode = command.Split(' ')[1];
                            if (!Caption.specialCharRefs.ContainsKey(cCode))
                            {
                                forcePaintStop(5, cCode);
                                return;
                            }
                            else
                                curStr += Caption.specialCharRefs[cCode];
                            break;
                        case "i":
                            if (curFont.Equals(underlinedFont))
                                curFont = italicAndUnderlinedFont;
                            else
                                curFont = italicFont;
                            if(!preString.EndsWith(" ")) { curStr += ' '; }
                            break;
                        case "u":
                            if (curFont.Equals(italicFont))
                                curFont = italicAndUnderlinedFont;
                            else
                                curFont = underlinedFont;
                            if (!preString.EndsWith(" ")) { curStr += ' '; }
                            break;
                        case "/i":
                            if (curFont.Equals(italicAndUnderlinedFont))
                                curFont = underlinedFont;
                            else
                                curFont = normalFont;
                            if (!preString.EndsWith(" ")) { curStr += ' '; }
                            break;
                        case "/u":
                            if (curFont.Equals(italicAndUnderlinedFont))
                                curFont = italicFont;
                            else
                                curFont = normalFont;
                            if (!preString.EndsWith(" ")) { curStr += ' '; }
                            break;
                        default:
                            forcePaintStop(2, command);
                            return;
                    }
                }
                else if (caption[i] == '\n')
                {
                    if (alignment == 1)
                        curCol = ((32 - removeControlCodes(caption.Split('\n')[curLine]).Length) / 2) * width;
                    else if (alignment == 2)
                        curCol = ((32 - removeControlCodes(caption.Split('\n')[curLine]).Length)) * width;
                    foreach (char c in curStr)
                    {
                        if (!Caption.charCodes.ContainsKey(c))
                        {
                            e.Graphics.DrawString("■", curFont, curColour, new PointF(curCol, curRow));
                            curCol += width;
                        }
                        else
                        {
                            e.Graphics.DrawString(c.ToString(), curFont, curColour, new PointF(curCol, curRow));
                            curCol += width;
                        }
                    }
                    alignment = 0;
                    curCol = 0;
                    curRow += curStr.Split('\n').Length * height;
                    curStr = "";
                    curLine++;
                }
                else
                {
                    curStr += caption[i];
                }
                i++;
            }
            if (alignment == 1)
                curCol = ((32 - removeControlCodes(caption.Split('\n')[curLine]).Length) / 2) * width;
            else if (alignment == 2)
                curCol = ((32 - removeControlCodes(caption.Split('\n')[curLine]).Length)) * width;
            foreach (char c in curStr)
            {
                if (!Caption.charCodes.ContainsKey(c))
                {
                    e.Graphics.DrawString("■", curFont, curColour, new PointF(curCol, curRow));
                    curCol += width;
                }
                else
                {
                    e.Graphics.DrawString(c.ToString(), curFont, curColour, new PointF(curCol, curRow));
                    curCol += width;
                }
            }

            /*using (normalFont)
            {
                e.Graphics.DrawString("12345678901234567890123456789012\nRow 2\nRow 3\nRow 4\nRow 5\nRow 6\nRow 7\nRow 8\nRow 9\nRow 10\nRow 11\nRow 12\nRow 13\nRow 14\nRow 15",
                    normalFont, Brushes.White, new PointF(2, 2));
            } // */
        }

        public string removeControlCodes(string line)
        {
            string s = "";
            string cmd = "";
            bool isInBraces = false;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '<')
                {
                    isInBraces = true;
                    cmd = "";
                    continue;
                }
                else if (line[i] == '>' && isInBraces)
                {
                    isInBraces = false;
                    if (cmd.StartsWith("char"))
                        s += "?";
                }
                else if (!isInBraces)
                    s += line[i];
                else if (isInBraces)
                    cmd += line[i];
            }
            return s;
        }

        public void forcePaintStop(int error, string errorStr)
        {
            paintProblem = error;
            paintProblemStr = errorStr;
            pictureBox1.Invalidate();
        }

        public int getCentredPos(string line)
        {
            return 32 - line.Length / 2;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int sel = richTextBox1.SelectionStart;
            richTextBox1.Text = richTextBox1.Text.Insert(sel, "♪");
            richTextBox1.SelectionStart = sel + 1;
            richTextBox1.Select();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!isTimecode(startTimeBox.Text))
                pictureBox2.Image = SystemIcons.Error.ToBitmap();
            else
                pictureBox2.Image = null;
        }

        public bool isTimecode(string s)
        {
            s = s.Replace(';', ':');
            string[] _s = s.Split(':');
            if (_s.Length == 4)
                if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9:]*$"))
                    if (_s[0].Length > 0) //Hours
                        if (Convert.ToInt32(_s[0]) >= 0)
                            if (_s[1].Length == 2) //Minutes
                                if (Convert.ToInt32(_s[1]) >= 0 && Convert.ToInt32(_s[1]) < 60)
                                    if (_s[2].Length == 2) //Seconds
                                        if (Convert.ToInt32(_s[2]) >= 0 && Convert.ToInt32(_s[2]) < 60)
                                            if (_s[3].Length == 2) //Frame #
                                                if (Convert.ToInt32(_s[3]) >= 0 && Convert.ToInt32(_s[3]) < fpsSelector.Value)
                                                    return true;
            return false;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (!isTimecode(endTimeBox.Text))
                pictureBox3.Image = SystemIcons.Error.ToBitmap();
            else
                pictureBox3.Image = null;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(captionTypeSelector.SelectedIndex);
            if (captionTypeSelector.SelectedIndex == 0 || captionTypeSelector.SelectedIndex == 4)
            {
                int row = (int)rowSelector.Value;
                if (richTextBox1.Lines.Length + row > 16)
                {
                    rowSelector.Value = 16 - richTextBox1.Lines.Length;
                }
                rowSelector.Maximum = 16 - richTextBox1.Lines.Length;
            }
            else rowSelector.Maximum = 15;


            if(captionList != null)
                label11.Text = "Total captions: " + captionList.Count;
            else label11.Text = "Total captions: Ø";
            pictureBox1.Refresh();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            pictureBox1.Refresh();
        }

        Caption createClearCaption(string timecode)
        {
            Caption c = new Caption("{clear}");
            c.StartTime = timecode;
            return c;
        }

        private string generateCaptionArguments(int type, decimal rowNum, bool isFirst)
        {
            string[] types = { "Popon", "Rollup2", "Rollup3", "Rollup4", "Painton" };
            return types[type] + "," + Convert.ToInt32(rowNum) + "," + Convert.ToInt32(isFirst);
        }

        private void captionTypeSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            richTextBox1_TextChanged(null, null);
        }

        private void pasteToBox(object sender, EventArgs e)
        {
            Button b = sender as Button;
            if (b.Name == pasteStartBtn.Name)
            {
                if (Clipboard.ContainsText())
                    startTimeBox.Text = Clipboard.GetText();
                else
                {
                    statusBar.Text = "Item in clipboard cannot be pasted.";
                    startTimeBox.Text = "00:00:00:00";
                    System.Media.SystemSounds.Beep.Play();
                }
            }
            else if (b.Name == pasteEndBtn.Name)
            {
                if (Clipboard.ContainsText())
                    endTimeBox.Text = Clipboard.GetText();
                else
                {
                    statusBar.Text = "Item in clipboard cannot be pasted.";
                    endTimeBox.Text = "00:00:00:00";
                    System.Media.SystemSounds.Beep.Play();
                }
            }
            else MessageBox.Show("This shouldn't happen. What the heck are you doing?");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "All compatible files|*.txt;*.sccip|Transcripts|*.txt|SCC in progress file|*.sccip";
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            if (!e.Cancel)
            {
                captionList = null; //Clear any previous data we may have had
                if (openFileDialog1.FileName.EndsWith(".txt"))
                {
                    try
                    {
                        StreamReader r = new StreamReader(openFileDialog1.FileName);
                        string[] transcriptFile = Regex.Split(r.ReadToEnd(), @"(?:\r\n){2,}");
                        r.Close();
                        numericUpDown1.Maximum = transcriptFile.Length;
                        captionList = new List<Caption>(transcriptFile.Length);
                        captionList.Clear();
                        for (int h = 0; h < transcriptFile.Length; h++)
                        {
                            string display = "";
                            string s = transcriptFile[h];
                            string arg = "";
                            bool isInBraces = false;
                            for (int i = 0; i < s.Length; i++)
                            {
                                if (s[i] == '{') { isInBraces = true; continue; }
                                if (s[i] == '}')
                                {
                                    isInBraces = false;
                                    if (s.Substring(i + 1).StartsWith("\r\n"))
                                        i += "\r\n".Length;
                                    continue;
                                }
                                if (!isInBraces)
                                    display += s[i];
                                else
                                    arg += s[i];
                            }
                            Caption c = new Caption(display.Replace("\r", ""));
                            if (!arg.Equals(""))
                            {
                                string[] args = arg.Replace(" ", "").ToLowerInvariant().Split(',');
                                switch (args[0])
                                {
                                    case "tl":
                                        arg = "1";
                                        break;
                                    case "tc":
                                        arg = "1";
                                        c.DisplayStr = "<centre>" + c.DisplayStr.Replace("\n", "\n<centre>");
                                        break;
                                    case "tr":
                                        arg = "1";
                                        c.DisplayStr = "<right>" + c.DisplayStr.Replace("\n", "\n<right>");
                                        break;
                                    case "bl":
                                        arg = (16 - c.DisplayStr.Split('\n').Length).ToString();
                                        break;
                                    case "bc":
                                        arg = (16 - c.DisplayStr.Split('\n').Length).ToString();
                                        c.DisplayStr = "<centre>" + c.DisplayStr.Replace("\n", "\n<centre>");
                                        break;
                                    case "br":
                                        arg = (16 - c.DisplayStr.Split('\n').Length).ToString();
                                        c.DisplayStr = "<right>" + c.DisplayStr.Replace("\n", "\n<right>");
                                        break;
                                    case "s2":
                                        if (args.Length > 1)
                                            arg = generateCaptionArguments(1, Convert.ToInt32(args[1].Replace("row", "")), h == 0);
                                        else arg = generateCaptionArguments(1, 15, h == 0);
                                        break;
                                    case "s3":
                                        if (args.Length > 1)
                                            arg = generateCaptionArguments(2, Convert.ToInt32(args[1].Replace("row", "")), h == 0);
                                        else arg = generateCaptionArguments(2, 15, h == 0);
                                        break;
                                    case "s4":
                                        if (args.Length > 1)
                                            arg = generateCaptionArguments(3, Convert.ToInt32(args[1].Replace("row", "")), h == 0);
                                        else arg = generateCaptionArguments(3, 15, h == 0);
                                        break;
                                    case "paint":
                                        arg = generateCaptionArguments(4, 16 - c.DisplayStr.Split('\n').Length, h == 0);
                                        c.DisplayStr = "<centre>" + c.DisplayStr.Replace("\n", "\n<centre>");
                                        break;
                                    default:
                                        arg = "???";
                                        break;
                                }
                                if (args[0].StartsWith("row") && arg.Equals("???"))
                                    arg = generateCaptionArguments(0, Convert.ToInt32(args[0].Replace("row", "")) > 15 ? 15 : Convert.ToInt32(args[0].Replace("row", "")), h == 0);

                                if (args.Length > 1 && !args[0].Equals("s2") && !args[0].Equals("s3") && !args[0].Equals("s4") && !args[0].Equals("paint") && !arg.Equals("???"))
                                {
                                    switch (args[1])
                                    {
                                        case "pop":
                                        case "flash":
                                            arg = generateCaptionArguments(0, Convert.ToInt32(arg), h == 0);
                                            break;
                                        case "paint":
                                            arg = generateCaptionArguments(4, Convert.ToInt32(arg), h == 0);
                                            break;
                                        default:
                                            arg = generateCaptionArguments(0, Convert.ToInt32(arg), h == 0);
                                            break;
                                    }
                                }
                                else if (!args[0].Equals("s2") && !args[0].Equals("s3") && !args[0].Equals("s4") && !args[0].Equals("paint") && !arg.Equals("???"))
                                    arg = generateCaptionArguments(0, Convert.ToInt32(arg), h == 0);
                                
                                if (args.Length > 1 && !arg.Equals("???") && (args[0].Equals("s2") || args[0].Equals("s3") || args[0].Equals("s4") || args[0].Equals("paint")))
                                {
                                    if (args[1].StartsWith("row"))
                                        args[1] = args[1].Replace("row", "");
                                    string type = arg.Split(',')[0];
                                    string first = arg.Split(',')[2];
                                    arg = type + "," + args[1] + "," + first;
                                }

                                if (arg.Equals("???")) //"Catch all" type thing
                                    arg = generateCaptionArguments(0, 1, h == 0);
                                c.Arguments = arg;
                            }
                            else //Argument is empty
                            {
                                c.Arguments = generateCaptionArguments(0, 16 - c.DisplayStr.Split('\n').Length, h == 0);
                                c.DisplayStr = "<centre>" + c.DisplayStr.Replace("\n", "\n<centre>");
                            }

                            c.StartTime = zeroTime;
                            c.EndTime = zeroTime;
                            captionList.Add(c);
                            System.Diagnostics.Debug.WriteLine(Environment.NewLine + captionList[h].DisplayStr);
                            System.Diagnostics.Debug.WriteLine(captionList[h].ToString(fpsSelector.Value));

                        }

                        saveFileDialog.FileName = "";
                        firstSave = true;
                    }
                    catch (IOException)
                    {
                        statusBar.Text = "An I/O exception occurred. The selected file may be in use by another program.";
                    }
                }
                else if (openFileDialog1.FileName.EndsWith(".sccip"))
                {
                    BinaryReader r = new BinaryReader(File.Open(openFileDialog1.FileName, FileMode.Open), Encoding.UTF8);
                    int len = r.ReadInt32();
                    captionList = new List<Caption>(len);
                    captionList.Clear();
                    for(int i = 0; i < len; i++)
                    {
                        Caption c = new Caption("");
                        c.StartTime = r.ReadString();
                        c.EndTime = r.ReadString();
                        c.Arguments = r.ReadString();
                        c.DisplayStr = r.ReadString();
                        captionList.Add(c);

                        System.Diagnostics.Debug.WriteLine(Environment.NewLine + captionList[i].DisplayStr);
                        System.Diagnostics.Debug.WriteLine(captionList[i].ToString(fpsSelector.Value));
                    }
                    r.Close();
                    saveFileDialog.FileName = openFileDialog1.FileName;
                    firstSave = false;
                }

                numericUpDown1.Value = 1;
                numericUpDown1.Minimum = 1;
                numericUpDown1.Maximum = captionList.Count;
                numericUpDown1_ValueChanged(numericUpDown1, new EventArgs());
                statusBar.Text = string.Format("File loaded. {0} caption{1} loaded.", captionList.Count, captionList.Count > 1 ? "s" : "");
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (captionList != null)
            {
                label11.Text = "Total captions: " + captionList.Count;
                int newVal = (int)(sender as NumericUpDown).Value - 1;
                richTextBox1.Text = captionList[newVal].DisplayStr;
                startTimeBox.Text = captionList[newVal].StartTime;
                endTimeBox.Text = captionList[newVal].EndTime;
                string[] args = captionList[newVal].Arguments.Split(',');
                switch (args[0])
                {
                    case "Rollup2": captionTypeSelector.SelectedIndex = 1; break;
                    case "Rollup3": captionTypeSelector.SelectedIndex = 2; break;
                    case "Rollup4": captionTypeSelector.SelectedIndex = 3; break;
                    case "Painton": captionTypeSelector.SelectedIndex = 4; break;
                    case "Popon": default: captionTypeSelector.SelectedIndex = 0; break;
                }
                rowSelector.Value = Convert.ToInt32(args[1]);

                //Format captions for previous and next labels
                if (newVal + 1 == 1 && captionList.Count > 1) //Is first index
                {
                    prevCaptionLabel.Text = "---------------------";
                    prevStartLabel.Text = "--:--:--:--";
                    prevEndLabel.Text = "--:--:--:--";
                    nextCaptionLabel.Text = captionList[newVal + 1].DisplayStr;
                    nextStartLabel.Text = captionList[newVal + 1].StartTime;
                    nextEndLabel.Text = captionList[newVal + 1].EndTime;
                }
                else if (newVal + 1 == 1) //Is first and only index
                {
                    prevCaptionLabel.Text = "---------------------";
                    prevStartLabel.Text = "--:--:--:--";
                    prevEndLabel.Text = "--:--:--:--";
                    nextCaptionLabel.Text = "---------------------";
                }
                else if (newVal + 1 == captionList.Count) //Is last index
                {
                    prevCaptionLabel.Text = captionList[newVal - 1].DisplayStr;
                    prevStartLabel.Text = captionList[newVal - 1].StartTime;
                    prevEndLabel.Text = captionList[newVal - 1].EndTime;
                    nextCaptionLabel.Text = "---------------------";
                    nextStartLabel.Text = "--:--:--:--";
                    nextEndLabel.Text = "--:--:--:--";
                }
                else //Every other case
                {
                    prevCaptionLabel.Text = captionList[newVal - 1].DisplayStr;
                    prevStartLabel.Text = captionList[newVal - 1].StartTime;
                    prevEndLabel.Text = captionList[newVal - 1].EndTime;
                    nextCaptionLabel.Text = captionList[newVal + 1].DisplayStr;
                    nextStartLabel.Text = captionList[newVal + 1].StartTime;
                    nextEndLabel.Text = captionList[newVal + 1].EndTime;
                }
            }
            else
            {
                statusBar.Text = "Please either load a transcipt or press the \"Save && Add New\" button first.";
                System.Media.SystemSounds.Beep.Play();
            }
        }

        private void saveSCCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveAsFileDialog.FileName = "";
            saveAsFileDialog.Filter = "SCC caption file|*.scc";
            saveAsFileDialog.ShowDialog();
        }

        private void saveAsFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            if (!e.Cancel)
            {
                if (captionList != null)
                {
                    if (captionList.Count > 0)
                    {
                        SaveFileDialog s = sender as SaveFileDialog;

                        List<Caption> listToWrite = new List<Caption>();
                        for (int i = 0; i < captionList.Count; i++)
                        {
                            if (i == 0)
                            {
                                listToWrite.Add(captionList[i].Clone());
                                continue;
                            }
                            if (!captionList[i].StartTime.Equals(captionList[i - 1].EndTime))
                                listToWrite.Add(createClearCaption(captionList[i - 1].EndTime));
                            listToWrite.Add(captionList[i].Clone());
                        }
                        listToWrite.Add(createClearCaption(captionList[captionList.Count - 1].EndTime));

                        using (StreamWriter vttWriter = new StreamWriter(s.FileName))
                        {
                            vttWriter.Write("Scenarist_SCC V1.0\r\n\r\n");
                            foreach (Caption c in listToWrite)
                            {
                                vttWriter.Write(c.ToString(fpsSelector.Value));
                                vttWriter.Write("\r\n\r\n");
                            }
                        }
                        statusBar.Text = "Export to SCC successful.";
                    } else statusBar.Text = "Export failed. No captions TO export (size = 0).";
                } else statusBar.Text = "Export failed. No captions TO export.";
            } else statusBar.Text = "Export cancelled by user.";
        }

        private bool updateCurrentCaptionInList()
        {
            if (captionList != null)
                if (captionList.Count > 0)
                    if (isTimecode(startTimeBox.Text) && isTimecode(endTimeBox.Text))
                    {
                        int curIndex = (int)numericUpDown1.Value - 1;
                        captionList[curIndex].StartTime = startTimeBox.Text;
                        captionList[curIndex].EndTime = endTimeBox.Text;
                        captionList[curIndex].Arguments = generateCaptionArguments(captionTypeSelector.SelectedIndex, rowSelector.Value, curIndex == 0);
                        captionList[curIndex].DisplayStr = richTextBox1.Text.Replace("\r\n", "\n");
                        return true;
                    }
            return false;
        }

        private void btn_saveAndContinue_Click(object sender, EventArgs e)
        {
            /*Caption c = new Caption(richTextBox1.Text);
            c.StartTime = startTimeBox.Text;
            c.EndTime = endTimeBox.Text;
            c.Arguments = generateCaptionArguments(captionTypeSelector.SelectedIndex, rowSelector.Value, true);
            System.Diagnostics.Debug.WriteLine(c.ToString(fpsSelector.Value)); // */

            if (captionList != null)
            {
                if (captionList.Count > 0)
                {
                    if (updateCurrentCaptionInList())
                    {
                        int curIndex = (int)numericUpDown1.Value - 1;
                        if (numericUpDown1.Value >= captionList.Count)
                        {
                            numericUpDown1_ValueChanged(numericUpDown1, null);
                            statusBar.Text = "Recorded. End of transcript.";
                            System.Media.SystemSounds.Beep.Play();
                        }
                        else
                        {
                            if (checkBox1.Checked)
                            {
                                captionList[curIndex + 1].StartTime = captionList[curIndex].EndTime;
                                string[] time = captionList[curIndex].EndTime.Split(':');
                                captionList[curIndex + 1].EndTime = Caption.getTimeCode(Convert.ToDecimal(time[0]), Convert.ToDecimal(time[1]), Convert.ToDecimal(time[2]) + 1, Convert.ToDecimal(time[3]));
                            }
                            numericUpDown1.Value++;
                            numericUpDown1_ValueChanged(numericUpDown1, null);
                            statusBar.Text = "Recorded. Next caption ready.";
                        }
                    }
                    else
                    {
                        statusBar.Text = "Record failed. At least one of the timecodes are not formatted correctly. Try again.";
                        System.Media.SystemSounds.Beep.Play();
                    }
                }
                else statusBar.Text = "Record failed. You must first load a caption file, transcript, or click \"Record && Add New\".";
            }
            else statusBar.Text = "Record failed. You must first load a caption file, transcript, or click \"Record && Add New\".";
        }

        private void btn_saveAndAddNew_Click(object sender, EventArgs e)
        {
            if (captionList != null)
            {
                if (captionList.Count > 0)
                {
                    if (updateCurrentCaptionInList())
                    {
                        //Begin creating new caption
                        int curIndex = (int)numericUpDown1.Value - 1;
                        Caption c = new Caption("");
                        if (checkBox1.Checked)
                        {
                            c.StartTime = captionList[curIndex].EndTime;
                            string[] time = captionList[curIndex].EndTime.Split(':');
                            c.EndTime = Caption.getTimeCode(Convert.ToDecimal(time[0]), Convert.ToDecimal(time[1]), Convert.ToDecimal(time[2]) + 1, Convert.ToDecimal(time[3]));
                        }
                        else
                        {
                            c.StartTime = captionList[curIndex].StartTime;
                            c.EndTime = captionList[curIndex].EndTime;
                        }
                        if (rowSelector.Value + captionList[curIndex].DisplayStr.Split('\n').Length >= 15)
                            c.Arguments = generateCaptionArguments(captionTypeSelector.SelectedIndex, 15, false);
                        else c.Arguments = generateCaptionArguments(captionTypeSelector.SelectedIndex, rowSelector.Value, false);

                        captionList.Insert(curIndex + 1, c);
                        numericUpDown1.Maximum++;
                        numericUpDown1.Value++;
                        statusBar.Text = "Recorded. Inserted new caption at current position.";
                    }
                    else
                    {
                        statusBar.Text = "Record failed. At least one of the timecodes are not formatted correctly. Try again.";
                        System.Media.SystemSounds.Beep.Play();
                    }
                }
                else //Although this should be impossible, it couldn't hurt.
                {
                    if (isTimecode(startTimeBox.Text) && isTimecode(endTimeBox.Text))
                    {
                        //Just to let us know this shouldn't happen.
                        System.Diagnostics.Debug.WriteLine("Warning! Caption list is not null, but still empty!");

                        //Create list, first entry, and save it
                        captionList = new List<Caption>();
                        Caption c = new Caption(richTextBox1.Text.Replace("\r\n", "\n"));
                        c.StartTime = startTimeBox.Text; c.EndTime = endTimeBox.Text;
                        c.Arguments = generateCaptionArguments(captionTypeSelector.SelectedIndex, rowSelector.Value, true);
                        captionList.Add(c);

                        //Now start new entry (2nd)
                        Caption d = new Caption("");
                        if (checkBox1.Checked)
                        {
                            d.StartTime = c.EndTime;
                            string[] time = c.EndTime.Split(':');
                            d.EndTime = Caption.getTimeCode(Convert.ToDecimal(time[0]), Convert.ToDecimal(time[1]), Convert.ToDecimal(time[2]) + 1, Convert.ToDecimal(time[3]));
                        }
                        else
                        {
                            d.StartTime = c.StartTime;
                            d.EndTime = c.EndTime;
                        }
                        d.Arguments = generateCaptionArguments(captionTypeSelector.SelectedIndex, rowSelector.Value, true);
                        captionList.Add(d);

                        numericUpDown1.Minimum = 1;
                        numericUpDown1.Maximum = 2;
                        numericUpDown1.Value = 2;
                        statusBar.Text = "(WARNING VERSION) Created new transcript and recorded given info. New caption ready.";
                    }
                    else statusBar.Text = "(WARNING VERSION) Create and record failed. Improper timecode(s).";
                }
            }
            else
            {
                if (isTimecode(startTimeBox.Text) && isTimecode(endTimeBox.Text))
                {
                    //Create list, first entry, and save it
                    captionList = new List<Caption>();
                    Caption c = new Caption(richTextBox1.Text.Replace("\r\n", "\n"));
                    c.StartTime = startTimeBox.Text; c.EndTime = endTimeBox.Text;
                    c.Arguments = generateCaptionArguments(captionTypeSelector.SelectedIndex, rowSelector.Value, true);
                    captionList.Add(c);

                    //Now start new entry (2nd)
                    Caption d = new Caption("");
                    if (checkBox1.Checked)
                    {
                        d.StartTime = c.EndTime;
                        string[] time = c.EndTime.Split(':');
                        d.EndTime = Caption.getTimeCode(Convert.ToDecimal(time[0]), Convert.ToDecimal(time[1]), Convert.ToDecimal(time[2]) + 1, Convert.ToDecimal(time[3]));
                    }
                    else
                    {
                        d.StartTime = c.StartTime;
                        d.EndTime = c.EndTime;
                    }
                    d.Arguments = generateCaptionArguments(captionTypeSelector.SelectedIndex, rowSelector.Value, true);
                    captionList.Add(d);

                    numericUpDown1.Minimum = 1;
                    numericUpDown1.Maximum = 2;
                    numericUpDown1.Value = 2;
                    statusBar.Text = "Created new transcript and recorded given info. New caption ready.";
                }
                else statusBar.Text = "Create and record failed. At least one of the timecodes are not formatted correctly.";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string[] lines = richTextBox1.Lines;
            for (int i = 0; i < lines.Length; i++ )
            {
                string s = lines[i];
                lines[i] = s.Replace("<right>", "").Replace("<center>", "").Replace("<centre>", "");
            }
            richTextBox1.Lines = lines;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string[] lines = richTextBox1.Lines;
            for (int i = 0; i < lines.Length; i++)
            {
                string s = lines[i];
                if (!s.Contains("<center>") && !s.Contains("<centre>"))
                    lines[i] = "<centre>" + s.Replace("<right>", "");
            }
            richTextBox1.Lines = lines;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string[] lines = richTextBox1.Lines;
            for (int i = 0; i < lines.Length; i++)
            {
                string s = lines[i];
                if (!s.Contains("<right>"))
                    lines[i] = "<right>" + s.Replace("<centre>", "").Replace("<center>", "");
            }
            richTextBox1.Lines = lines;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (captionList == null)
            {
                if (isTimecode(startTimeBox.Text) && isTimecode(endTimeBox.Text))
                {
                    //Create list, first entry, and save it
                    numericUpDown1.Minimum = 1;
                    numericUpDown1.Maximum = 1;
                    numericUpDown1.Value = 1;
                    captionList = new List<Caption>();
                    captionList.Add(new Caption(""));
                    updateCurrentCaptionInList();
                    statusBar.Text = "Created a new transcript, and... ";
                }
            }
            else
            {
                if (isTimecode(startTimeBox.Text) && isTimecode(endTimeBox.Text))
                {
                    updateCurrentCaptionInList();
                    statusBar.Text = "Updated this index, and... ";
                }
            }
            System.Diagnostics.Debug.WriteLine(captionList[(int)numericUpDown1.Value - 1].ToString(fpsSelector.Value));
            statusBar.Text += "Printed to console.";
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (updateCurrentCaptionInList())
            {
                if (firstSave)
                {
                    firstSave = false;
                    saveFileDialog.FileName = "";
                    saveFileDialog.ShowDialog();
                }
                else
                {
                    saveFileDialog_FileOk(saveFileDialog, new CancelEventArgs(false));
                }
            } else MessageBox.Show("This caption must first be properly formatted before saving.\n\n(Error: Improper timecodes.)");
        }

        private void saveFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            if (!e.Cancel)
            {
                if (captionList != null)
                {
                    if (captionList.Count > 0)
                    {
                        SaveFileDialog s = sender as SaveFileDialog;
                        
                        using (BinaryWriter w = new BinaryWriter(s.OpenFile(), Encoding.UTF8))
                        {
                            w.Write(captionList.Count);
                            for (int i = 0; i < captionList.Count; i++)
                            {
                                Caption c = captionList[i];
                                w.Write(c.StartTime);
                                w.Write(c.EndTime);
                                w.Write(c.Arguments);
                                w.Write(c.DisplayStr);
                            }
                        }
                        statusBar.Text = "Save successful.";
                    } else statusBar.Text = "Save failed. No captions TO save (size = 0).";
                } else statusBar.Text = "Save failed. No captions TO save.";
            } else statusBar.Text = "Save cancelled by user.";
        }

        private void deleteThisBtn_Click(object sender, EventArgs e)
        {
            if (captionList != null)
            {
                if (captionList.Count > 1)
                {
                    if (numericUpDown1.Value == 1)
                        captionList.RemoveAt(0);
                    else captionList.RemoveAt((int)--numericUpDown1.Value);
                    numericUpDown1.Maximum--;
                    numericUpDown1_ValueChanged(numericUpDown1, null);
                } else statusBar.Text = "Remove failed. Removing would create an empty transcript.";
            } else statusBar.Text = "Remove failed. No active transcipt.";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (captionList != null)
                if (captionList.Count > 0)
                    if (numericUpDown1.Value < numericUpDown1.Maximum)
                        if (updateCurrentCaptionInList())
                        {
                            numericUpDown1.Value++;
                            numericUpDown1_ValueChanged(numericUpDown1, null);
                        }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (captionList != null)
                if (captionList.Count > 0)
                    if (numericUpDown1.Value > numericUpDown1.Minimum)
                        if (updateCurrentCaptionInList())
                        {
                            numericUpDown1.Value--;
                            numericUpDown1_ValueChanged(numericUpDown1, null);
                        }
        }

        private void onStartBoxFocus(object sender, EventArgs e)
        {
            if (startBoxGotFocus)
            {
                startTimeBox.SelectAll();
                startBoxGotFocus = false;
            }
        }

        private void onEndBoxFocus(object sender, EventArgs e)
        {
            if (endBoxGotFocus)
            {
                endTimeBox.SelectAll();
                endBoxGotFocus = false;
            }
        }

        private void giveStartBoxFocus(object sender, EventArgs e)
        {
            startBoxGotFocus = true;
        }

        private void giveEndBoxFocus(object sender, EventArgs e)
        {
            endBoxGotFocus = true;
        }
    }
}

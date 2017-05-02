using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace SCCMaker
{
    static class SingleCaptionBuilder
    {
        static StringBuilder sb;
        static StringBuilder encodedStr;
        static string prevByte;
        static readonly string charactersThatNeedSpaces = @"‘’“”ÁÀÂÄÃÅåäãÇÉÈÊËëÎÍÌÏïìÓÒÕÔÖØòõöøßÚÙÛÜüù¡*–©·«»{}\^_¦~¥¤|┍┐└┘";

        /// <summary>
        /// Resets various objects that may have leftover data from
        ///  previous calls. Should be called at the start of every method.
        /// </summary>
        private static void resetClassGlobals()
        {
            sb = new StringBuilder();
            encodedStr = new StringBuilder();
            prevByte = null;
        }

        private static string getParityBytes(int val)
        {
            if (val > 0xff) //2 byte code
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Form1.oddParityTranslationMatrix[((val & 0xff00) >> 8)].ToString("x2"));
                sb.Append(Form1.oddParityTranslationMatrix[(val & 0xff)].ToString("x2"));
                return sb.ToString();
            }
            else return Form1.oddParityTranslationMatrix[val].ToString("x2");
        }

        private static string getCommandParityCode(string commandName)
        {
            return getParityBytes(Caption.commandCodes[commandName]);
        }

        private static string removeControlCodes(string line)
        {
            string s = "";
            bool isInBraces = false;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '<')
                    isInBraces = true;
                else if (line[i] == '>' && isInBraces)
                    isInBraces = false;
                else if (!isInBraces)
                    s += line[i];
            }
            return s;
        }

        private static int countCommandsThatRequireSpaces(string line)
        {
            List<int> positions = new List<int>();
            int pos = 0;
            while ((pos < line.Length) && (pos = line.IndexOf("<i>", pos)) != -1)
            {
                positions.Add(pos);
                pos += "<i>".Length;
            }

            pos = 0;
            while ((pos < line.Length) && (pos = line.IndexOf("</i>", pos)) != -1)
            {
                positions.Add(pos);
                pos += "</i>".Length;
            }

            pos = 0;
            while ((pos < line.Length) && (pos = line.IndexOf("<char ", pos)) != -1)
            {
                positions.Add(pos);
                pos += "<char ".Length;
            }

            return positions.Count;

            /*Console.WriteLine("{0} occurrences", positions.Count);
            foreach (var p in positions)
            {
                Console.WriteLine(p);
            }*/
        }

        /// <summary>
        /// Generates an SCC compliant string based on the Caption object
        ///  given without a timecode.
        /// </summary>
        /// <param name="c">The Caption object to encode.</param>
        /// <returns>The formatted SCC string.</returns>
        public static string encode(Caption c)
        {
            resetClassGlobals();
            sb.Append(buildPreamble(c));
            sb.Append(" ");
            buildAndAppendStringContent(c);
            sb.Append(buildEndingBits(c));
            if (sb.ToString().EndsWith(" "))
                return sb.ToString().Substring(0, sb.ToString().LastIndexOf(' '));
            else return sb.ToString();
        }

        private static string buildPreamble(Caption c)
        {
            StringBuilder preamble = new StringBuilder();
            string[] args = c.Arguments.Split(',');
            preamble.Append(getParityBytes(Caption.commandCodes[args[0]]));
            if (args[0].Equals("Popon"))
            {
                preamble.Append(" ");
                preamble.Append(getCommandParityCode("ClearBuffer"));
            }
            return preamble.ToString();
        }

        private static void buildAndAppendStringContent(Caption c)
        {
            string[] args = c.Arguments.Split(',');
            string[] lines = c.DisplayStr.Split('\n');
            int startRow = Convert.ToInt32(args[1]);
            for (int i = 0; i < lines.Length; i++)
            {
                encodedStr = new StringBuilder();
                prevByte = null;
                int spaceOffset = 0, startCol = 0;
                if (lines[i].StartsWith(" "))
                {
                    int l = 0;
                    while (lines[i][l].Equals(' '))
                    {
                        spaceOffset++;
                        l++;
                    }
                }
                bool isItalics = false;
                bool isUnderlined = false;
                string curColour = "white";
                int j = spaceOffset;
                while (j < lines[i].Length)
                {
                    if (lines[i][j].Equals('<'))
                    {
                        if (lines[i].Substring(j + 1).Length > 4) //Just to be safe
                        {
                            if (!lines[i].Substring(j + 1, 4).ToLowerInvariant().Equals("char"))
                            {
                                if (encodedStr.ToString().EndsWith("20 "))
                                {
                                    encodedStr.Remove(encodedStr.Length - 3, 3);
                                    encodedStr.Append("80 ");
                                }
                                if (pendingChars())
                                {
                                    if (!prevByte.Equals("20"))
                                    {
                                        encodedStr.Append(prevByte);
                                        encodedStr.Append("80 ");
                                        prevByte = null;
                                    }
                                    else
                                    {
                                        prevByte = null;
                                    }
                                }
                            }
                        }
                        int k = 1;
                        string command = "";
                        while (!lines[i][j + k].Equals('>'))
                            command += lines[i][j + k++];
                        string[] cmd = command.Split(' ');
                        switch (cmd[0].ToLowerInvariant())
                        {
                            case "colour":
                            case "color":
                                if (pendingChars())
                                    appendNextChar('\b', isItalics);
                                if (command.Split(' ').Length == 1)
                                    throw new ArgumentException("Colour: Missing colour name.");
                                switch (command.Split(' ')[1].ToLowerInvariant())
                                {
                                    case "white":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("White_"));
                                        else encodedStr.Append(getCommandParityCode("White"));
                                        break;
                                    case "green":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Green_"));
                                        else encodedStr.Append(getCommandParityCode("Green"));
                                        break;
                                    case "blue":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Blue_"));
                                        else encodedStr.Append(getCommandParityCode("Blue"));
                                        break;
                                    case "cyan":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Cyan_"));
                                        else encodedStr.Append(getCommandParityCode("Cyan"));
                                        break;
                                    case "red":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Red_"));
                                        else encodedStr.Append(getCommandParityCode("Red"));
                                        break;
                                    case "yellow":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Yellow_"));
                                        else encodedStr.Append(getCommandParityCode("Yellow"));
                                        break;
                                    case "magenta":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Magenta_"));
                                        else encodedStr.Append(getCommandParityCode("Magenta"));
                                        break;
                                    default: throw new ArgumentException("Colour: Invalid colour name, \"" + command.Split(' ')[1].ToLowerInvariant() + "\"");
                                }
                                encodedStr.Append(" ");
                                curColour = command.Split(' ')[1].ToLowerInvariant();
                                break;
                            case "centre":
                            case "center":
                                startCol = (32 - removeControlCodes(lines[i]).Length - countCommandsThatRequireSpaces(lines[i])) / 2;
                                break;
                            case "right":
                                startCol = 32 - removeControlCodes(lines[i]).Length - countCommandsThatRequireSpaces(lines[i]);
                                break;
                            case "char":
                                string cCode;
                                if (command.Split(' ').Length == 1)
                                    cCode = "undefined";
                                else cCode = command.Split(' ')[1];
                                appendNextChar(getCharFromRefName(cCode), isItalics);
                                break;
                            case "i":
                                if (j == 0)
                                    encodedStr.Append("2080 ");
                                if (pendingChars())
                                    appendNextChar('\b', isItalics);
                                if (isUnderlined)
                                    encodedStr.Append(getCommandParityCode("Italics_"));
                                else
                                    encodedStr.Append(getCommandParityCode("Italics"));
                                isItalics = true;
                                encodedStr.Append(" ");
                                break;
                            case "u":
                                if (pendingChars())
                                    appendNextChar('\b', isItalics);
                                isUnderlined = true;
                                switch (curColour)
                                {
                                    case "white":
                                        encodedStr.Append(getCommandParityCode("White_"));
                                        break;
                                    case "green":
                                        encodedStr.Append(getCommandParityCode("Green_"));
                                        break;
                                    case "blue":
                                        encodedStr.Append(getCommandParityCode("Blue_"));
                                        break;
                                    case "cyan":
                                        encodedStr.Append(getCommandParityCode("Cyan_"));
                                        break;
                                    case "red":
                                        encodedStr.Append(getCommandParityCode("Red_"));
                                        break;
                                    case "yellow":
                                        encodedStr.Append(getCommandParityCode("Yellow_"));
                                        break;
                                    case "magenta":
                                        encodedStr.Append(getCommandParityCode("Magenta_"));
                                        break;
                                    default: throw new ArgumentException("Underline: Colour mismatch.");
                                }
                                encodedStr.Append(" ");
                                break;
                            case "/i":
                                if (pendingChars())
                                    appendNextChar('\b', isItalics);
                                isItalics = false;
                                switch (curColour)
                                {
                                    case "white":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("White_"));
                                        else encodedStr.Append(getCommandParityCode("White"));
                                        break;
                                    case "green":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Green_"));
                                        else encodedStr.Append(getCommandParityCode("Green"));
                                        break;
                                    case "blue":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Blue_"));
                                        else encodedStr.Append(getCommandParityCode("Blue"));
                                        break;
                                    case "cyan":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Cyan_"));
                                        else encodedStr.Append(getCommandParityCode("Cyan"));
                                        break;
                                    case "red":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Red_"));
                                        else encodedStr.Append(getCommandParityCode("Red"));
                                        break;
                                    case "yellow":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Yellow_"));
                                        else encodedStr.Append(getCommandParityCode("Yellow"));
                                        break;
                                    case "magenta":
                                        if (isUnderlined)
                                            encodedStr.Append(getCommandParityCode("Magenta_"));
                                        else encodedStr.Append(getCommandParityCode("Magenta"));
                                        break;
                                    default: throw new ArgumentException("End italics: Colour mismatch.");
                                }
                                encodedStr.Append(" ");
                                break;
                            case "/u":
                                if (pendingChars())
                                    appendNextChar('\b', isItalics);
                                isUnderlined = false;
                                switch (curColour)
                                {
                                    case "white":
                                        encodedStr.Append(getCommandParityCode("White"));
                                        break;
                                    case "green":
                                        encodedStr.Append(getCommandParityCode("Green"));
                                        break;
                                    case "blue":
                                        encodedStr.Append(getCommandParityCode("Blue"));
                                        break;
                                    case "cyan":
                                        encodedStr.Append(getCommandParityCode("Cyan"));
                                        break;
                                    case "red":
                                        encodedStr.Append(getCommandParityCode("Red"));
                                        break;
                                    case "yellow":
                                        encodedStr.Append(getCommandParityCode("Yellow"));
                                        break;
                                    case "magenta":
                                        encodedStr.Append(getCommandParityCode("Magenta"));
                                        break;
                                    default: throw new ArgumentException("End underline: Colour mismatch.");
                                }
                                encodedStr.Append(" ");
                                break;
                            default:
                                MessageBox.Show("Unknown command: " + String.Join(",", cmd));
                                break;
                        }
                        k++;
                        //Pushes past any spaces that may follow after a command to prevent extra unnecessary spaces, like "</i>".
                        //Add exceptions as needed.
                        if (!cmd[0].ToLowerInvariant().Equals("char"))
                            while (lines[i].Length > j + k && lines[i][j + k].Equals(' '))
                                k++;

                        j += k - 1;
                    }
                    else
                    {
                        appendNextChar(lines[i][j], isItalics);
                    }
                    j++;
                }
                if (startCol < 0)
                    startCol = 0;
                int a = startCol + spaceOffset;
                int curRow = startRow + i;
                if (args[0].StartsWith("Rollup"))
                {
                    sb.Append(getCommandParityCode("CarriageReturn"));
                    sb.Append(" ");
                    curRow = startRow; //Since we're inserting a return, we can still use this row.
                }

                sb.Append(getCommandParityCode("Row" + (curRow))); //Row & column sector
                if (curRow > 11)
                {
                    if (curRow % 2 == 1)
                        sb.Append(getCommandParityCode("EvenTabs" + (a / 4)));
                    else
                        sb.Append(getCommandParityCode("OddTabs" + (a / 4)));
                }
                else
                {
                    if (curRow % 2 == 1)
                        sb.Append(getCommandParityCode("OddTabs" + (a / 4)));
                    else
                        sb.Append(getCommandParityCode("EvenTabs" + (a / 4)));
                }
                sb.Append(" ");
                if (a % 4 > 0) //Tabs
                {
                    sb.Append(getCommandParityCode("TabOver" + (a % 4)));
                    sb.Append(" ");
                }

                string e = encodedStr.ToString();
                if (prevByte != null)
                {
                    sb.Append(e + prevByte + "80 ");
                    prevByte = null;
                }
                else sb.Append(e);
            }
        }

        private static string buildEndingBits(Caption c)
        {
            StringBuilder ending = new StringBuilder();
            string[] args = c.Arguments.Split(',');
            if (args[0].Equals("Popon"))
            {
                if (args[2].Equals("1"))
                {
                    ending.Append(getCommandParityCode("ClearScreen"));
                    ending.Append(" ");
                }
                ending.Append(getCommandParityCode("DisplayCaption"));
            }
            return ending.ToString();
        }

        private static bool pendingChars()
        {
            return prevByte != null;
        }

        private static void appendNextChar(char ch, bool isItalics)
        {
            if (!pendingChars())
            {
                if (getCharCode(ch).Length > 2) //Char is xxxx
                {
                    if (charactersThatNeedSpaces.IndexOf(ch) != -1)
                        encodedStr.Append("2080 ");
                    encodedStr.Append(getCharCode(ch));
                    encodedStr.Append(" ");
                }
                else //Char is xx, set to prevByte
                    prevByte = getCharCode(ch);
            }
            else
            {
                if (getCharCode(ch).Length > 2) //Char is xxxx, but prevbyte was set
                {
                    encodedStr.Append(prevByte);
                    if (charactersThatNeedSpaces.IndexOf(ch) != -1)
                    {
                        if ((encodedStr.ToString().Contains(getCommandParityCode("Italics"))
                            || encodedStr.ToString().Contains(getCommandParityCode("Italics_")))
                            && !isItalics)
                            encodedStr.Append("80 ");
                        else encodedStr.Append("20 ");
                    }
                    else encodedStr.Append("80 ");
                    encodedStr.Append(getCharCode(ch));
                    encodedStr.Append(" ");
                    prevByte = null;
                }
                else
                {
                    encodedStr.Append(prevByte);
                    encodedStr.Append(getCharCode(ch));
                    encodedStr.Append(" ");
                    prevByte = null;
                }
            }
        }

        private static char getCharFromRefName(string cCode)
        {
            if (Caption.specialCharRefs.ContainsKey(cCode))
                return Caption.specialCharRefs[cCode];
            else Console.WriteLine("Undefined character reference: {0}", cCode);
            return Caption.specialCharRefs["undefined"];
        }

        private static string getCharCode(char c)
        {
            if (Caption.charCodes.ContainsKey(c))
                return Caption.charCodes[c];
            else Console.WriteLine("Undefined character: {0} ({0:D})", c);
            return Caption.charCodes['\0'];
        }
    }
}

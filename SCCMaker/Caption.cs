using System;
using System.Collections.Generic;
using System.Text;

namespace SCCMaker
{
    class Caption
	{
		public static Dictionary<char, int> charCodes = new Dictionary<char, int>();
		public static Dictionary<string, char> specialCharRefs = new Dictionary<string, char>();
		public static Dictionary<string, int> commandCodes = new Dictionary<string, int>();

		public Timestamp StartTime { get; set; }
		public Timestamp EndTime { get; set; }
		public string Arguments { get; set; }
		public string DisplayStr { get; set; }

		public Caption(string dispStr)
		{
			DisplayStr = dispStr;
		}

		/// <summary>
		/// Clones this caption object.
		/// </summary>
		/// <returns>A new Caption instance with the same settings as this Caption object.</returns>
		public Caption Clone()
		{
			Caption c = new Caption(this.DisplayStr);
			c.StartTime = this.StartTime;
			c.EndTime = this.EndTime;
			c.Arguments = this.Arguments;
			return c;
		}

		public int getEncodedStringWordLength()
		{
			return SingleCaptionBuilder.encode(this).Split(' ').Length;
		}

		/// <summary>
		/// Returns this caption in its encoded form.
		/// </summary>
		/// <returns>Returns this caption in its encoded form.</returns>
		public override string ToString()
		{
            if (DisplayStr.Equals("{clear}"))
            {
                return MakeFinalizedTimestamp(this, true).ToString() + "\t" + getProperParityBytes(commandCodes["ClearScreen"]);
            }

            return MakeFinalizedTimestamp(this, true) + "\t" + SingleCaptionBuilder.encode(this);
        }

        public static Timestamp MakeFinalizedTimestamp(Caption c, bool isStart)
        {
            if (c.Arguments != null)
            {
                if (c.Arguments.Contains("Popon"))
                {
                    if (isStart)
                    {
                        Timestamp t = c.StartTime.getAsFinalized();
                        int fillTime = c.getEncodedStringWordLength();
                        t.Frame -= fillTime - 1;
                        t.IsFinalizedTimestamp = true;
                        return t;
                    }
                }
            }
            return isStart ? c.StartTime.getAsFinalized() : c.EndTime.getAsFinalized();
        }

		public string getProperParityBytes(int val)
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

        public static Caption createClearCaption(string timecode)
        {
            Caption c = new Caption("{clear}");
            c.StartTime = Timestamp.parse(timecode);
            return c;
        }
    }
}

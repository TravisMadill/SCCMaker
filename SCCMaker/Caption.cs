using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace SCCMaker
{
	class Caption
	{
        public static Dictionary<char, string> charCodes = new Dictionary<char, string>();
        public static Dictionary<string, char> specialCharRefs = new Dictionary<string, char>();
        public static Dictionary<string, int> commandCodes = new Dictionary<string, int>();
        protected string start, end;

        public string StartTime { get { return start; } set { start = value.Replace(";", ":"); } }
        public string EndTime { get { return end; } set { end = value.Replace(";", ":"); } }
        public string Arguments { get; set; }
		public string DisplayStr { get; set; }

		public Caption(string dispStr)
		{
			DisplayStr = dispStr;
		}

		/// <summary>
		/// Creates a timecode string, with the final number being the frame number.
		/// </summary>
		/// <param name="hours">The hour mark.</param>
		/// <param name="minutes">The minute mark.</param>
		/// <param name="seconds">The second mark.</param>
		/// <param name="frames">The frame mark.</param>
		/// <returns>A timecode string, with the final number being the frame number.</returns>
		public static string getTimeCode(decimal hours, decimal minutes, decimal seconds, decimal frames)
		{
			string s = hours < 10 ? "0" + Convert.ToInt32(hours).ToString() : Convert.ToInt32(hours).ToString();
			s += ":";
			s += minutes < 10 ? "0" + Convert.ToInt32(minutes).ToString() : Convert.ToInt32(minutes).ToString();
			s += ":";
			s += seconds < 10 ? "0" + Convert.ToInt32(seconds).ToString() : Convert.ToInt32(seconds).ToString();
			s += ":";
			s += frames < 10 ? "0" + Convert.ToInt32(frames).ToString() : Convert.ToInt32(frames).ToString();

			return s;
		}

		/// <summary>
		/// Creates a timecode string, with the final number being in milliseconds.
		/// </summary>
		/// <param name="hours">The hour mark.</param>
		/// <param name="minutes">The minute mark.</param>
		/// <param name="seconds">The second mark.</param>
		/// <param name="frames">The frame mark.</param>
		/// <param name="fps">The video's FPS.</param>
		/// <returns>A timecode string, with the final number being in milliseconds.</returns>
		public static string getTimeCode(decimal hours, decimal minutes, decimal seconds, decimal frames, decimal fps)
		{
			string s = hours < 10 ? "0" + Convert.ToInt32(hours).ToString() : Convert.ToInt32(hours).ToString();
			s += ":";
			s += minutes < 10 ? "0" + Convert.ToInt32(minutes).ToString() : Convert.ToInt32(minutes).ToString();
			s += ":";
			s += seconds < 10 ? "0" + Convert.ToInt32(seconds).ToString() : Convert.ToInt32(seconds).ToString();
			s += ";";
			int millis = Convert.ToInt32(frames / fps * 29);
			s += millis < 10 ? "0" + Convert.ToInt32(millis).ToString() : Convert.ToInt32(millis).ToString();

			return s;
		}

		public static string getTimeCodeFromFramesToMillis(string timecode, decimal videoFPS)
		{
			string[] a = timecode.Split(':');
			return getTimeCode(Convert.ToDecimal(a[0]), Convert.ToDecimal(a[1]), Convert.ToDecimal(a[2]), Convert.ToDecimal(a[3]), videoFPS);
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

        /// <summary>
        /// Returns this caption in its encoded form.
        /// Uses default value of 59.97fps to print proper timecode.
        /// </summary>
        /// <returns>Returns this caption in its encoded form. (Uses 59.97fps as marker for timecode)</returns>
        public override string ToString()
        {
            return ToString(59.97m);
        }

		/// <summary>
		/// Returns this caption in its encoded form.
        /// Requires video framerate to convert the timecode properly.
		/// </summary>
		/// <returns>Returns this caption in its encoded form.</returns>
		public string ToString(decimal frameRate)
		{
            if (DisplayStr.Equals("{clear}"))
            {
                return getTimeCodeFromFramesToMillis(StartTime, frameRate) + "\t" + getProperParityBytes(commandCodes["ClearScreen"]);
            }

            return getTimeCodeFromFramesToMillis(StartTime, frameRate) + "\t" + SingleCaptionBuilder.encode(this);
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
	}
}

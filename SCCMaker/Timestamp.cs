using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCCMaker
{
    public class Timestamp
    {
        private int minute;
        private int second;
        private int frame;
        public int Hour { get; set; }
        public int Minute
        {
            get { return minute; }
            set
            {
                while(value >= 60)
                {
                    Hour++;
                    value -= 60;
                }
                while(value < 0)
                {
                    Hour--;
                    value += 60;
                }
                minute = value;
            }
        }
        public int Second
        {
            get { return second; }
            set
            {
                while (value >= 60)
                {
                    Minute++;
                    value -= 60;
                }
                while (value < 0)
                {
                    Minute--;
                    value += 60;
                }
                second = value;
            }
        }
        public int Frame
        {
            get { return frame; }
            set
            {
                if (IsFinalizedTimestamp)
                {
                    while (value >= 30)
                    {
                        Second++;
                        value -= 30;
                    }
                    while (value < 0)
                    {
                        Second--;
                        value += 30;
                    }
                }
                else
                {
                    while (value >= FrameRate)
                    {
                        Second++;
                        value -= (int)Math.Ceiling(FrameRate);
                    }
                    while (value < 0)
                    {
                        Second--;
                        value += (int)Math.Ceiling(FrameRate);
                    }
                }
                frame = value;
            }
        }
        public bool IsFinalizedTimestamp { get; set; }
        public static decimal FrameRate { get; set; } = 59.94m;
        public static bool DropFrame { get; set; } = false;
        public static Timestamp Zero { get => zero; set => zero = value; }

        private static Timestamp zero = new Timestamp()
        {
            Hour = 0,
            Minute = 0,
            Second = 0,
            Frame = 0
        };

        public Timestamp() { }

        public Timestamp Clone()
        {
            Timestamp t = new Timestamp();
            t.Hour = this.Hour;
            t.Minute = this.Minute;
            t.Second = this.Second;
            t.Frame = this.Frame;
            t.IsFinalizedTimestamp = this.IsFinalizedTimestamp;
            return t;
        }

        public override string ToString()
        {
            return $"{Hour:00}:{Minute:00}:{Second:00}{(DropFrame ? ";" : ":")}{Frame:00}";
        }

        /// <summary>
        /// Parses an input string and returns a Timestamp object.
        /// Format for a Timestamp string is:
        /// Without drop frame: "hh:mm:ss:ff"
        /// With drop frame: "hh:mm:ss;ff"
        /// </summary>
        /// <param name="s">The string representation of the Timestamp.</param>
        /// <returns>The Timestamp equivalent of the given string, default non-finalized.</returns>
        public static Timestamp parse(string s)
        {
            Timestamp t = new Timestamp();
            string[] data = s.Split(':');
            if (!(data.Length == 3 || data.Length == 4))
                throw new FormatException($"Incorrect string format: \"{s}\"");
            t.Hour = Convert.ToInt32(data[0]);
            t.Minute = Convert.ToInt32(data[1]);
            if (data[2].Contains(';'))
            {
                string[] saf = data[2].Split(';');
                t.Second = Convert.ToInt32(saf[0]);
                t.Frame = Convert.ToInt32(saf[1]);
            }
            else
            {
                t.Second = Convert.ToInt32(data[2]);
                t.Frame = Convert.ToInt32(data[3]);
            }
            return t;
        }

        public bool Equals(Timestamp other)
        {
            if (this.Frame == other.Frame)
                if (this.Second == other.Second)
                    if (this.Minute == other.Minute)
                        if (this.Hour == other.Hour)
                            return true;
            return false;
        }

        public Timestamp getAsFinalized()
        {
            Timestamp t = new Timestamp();
            t.Hour = Hour;
            t.Minute = Minute;
            t.Second = Second;
            t.Frame = (int)(Frame / FrameRate);
            t.IsFinalizedTimestamp = true;
            return t;
        }
    }
}

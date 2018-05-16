using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DQ.Core 
{
    public class DqNumber : IComparable<DqNumber>
    {
        public static DqNumber TryParse(string s)
        {
            var m = Regex.Match(s, @"(\d+)(?:\.(\d+))?");
            if (!m.Success) return null;

            var ex = new DqNumber();
            var l1 = int.Parse(m.Groups[1].Value);
            ex.Levels.Add(l1);
            if (m.Groups[2].Success)
            {
                var l2 = int.Parse(m.Groups[2].Value);
                ex.Levels.Add(l2);
            }

            return ex;
        }

        public List<int> Levels { get; } = new List<int>();

        public int CompareTo(DqNumber other)
        {
            for (var i = 0; i < Math.Min(Levels.Count, other.Levels.Count); i++)
            {
                var result = Comparer<int>.Default.Compare(Levels[i], other.Levels[i]);
                if (result != 0) return result;
            }

            return Comparer<int>.Default.Compare(Levels.Count, other.Levels.Count);
        }
    }
}
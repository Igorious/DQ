using System.Linq;

namespace DQ.Core
{
    public static class StringExtensions
    {
        public static string Reduce(this string s, int lenght)
        {
            if (s == null) return "{null}";
            s = s.Trim();
            if (s.Length <= lenght) return s;

            var leftLength = (lenght - 1) / 2;
            var rightLength = (lenght - 1) - leftLength;

            return string.Format("{0}…{1}", s.Substring(0, leftLength), s.Substring(s.Length - rightLength));
        }

        public static string TrimPunctuation(this string value)
        {
            var removeFromStart = 0;
            foreach (var c in value) 
            {
                if (char.IsPunctuation(c) || char.IsWhiteSpace(c))
                {
                    removeFromStart++;
                }
                else
                {
                    break;
                }
            }

            var removeFromEnd = 0;
            foreach (var c in value.Reverse()) 
            {
                if (char.IsPunctuation(c) || char.IsWhiteSpace(c))
                {
                    removeFromEnd++;
                }
                else
                {
                    break;
                }
            }

            if (removeFromStart == 0 && removeFromEnd == 0)
            {
                return value;
            }

            if (removeFromStart == value.Length && removeFromEnd == value.Length)
            {
                return "";
            }

            return value.Substring(removeFromStart, value.Length - removeFromEnd - removeFromStart);
        }
    }
}

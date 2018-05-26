using System.Collections.Generic;

namespace DQ.Core.Styling 
{
    public sealed class DqNumbering
    {
        public int Id { get; set; } = -1;
        public List<DqNumberingLevel> Levels { get; } = new List<DqNumberingLevel>();

        public DqNumbering Clone(int id)
        {
            var copy = (DqNumbering) MemberwiseClone();
            copy.Id = id;
            return copy;
        }
    }
}
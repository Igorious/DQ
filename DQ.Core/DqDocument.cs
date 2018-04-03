using System.Collections.Generic;

namespace DQ.Core 
{
    public sealed class DqDocument
    {
        public List<DqSection> Sections { get; } = new List<DqSection>();
        public List<DqParagraph> Paragraphs { get; } = new List<DqParagraph>();
        public List<DqStyle> Styles { get; } = new List<DqStyle>();
        public List<DqNumbering> Numbering { get; } = new List<DqNumbering>();
    }
}
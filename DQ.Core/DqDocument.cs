using System.Collections.Generic;
using DQ.Core.Styling;

namespace DQ.Core 
{
    public sealed class DqDocument
    {
        public List<DqSection> Sections { get; } = new List<DqSection>();
        public List<DqParagraph> Paragraphs { get; } = new List<DqParagraph>();
        public DqStyleTable StyleTable { get; set; }
        public DqNumberingTable NumberingTable { get; set; }

        public DqStructure Structure { get; set; }
    }
}
using System.Collections.Generic;

namespace DQ.Core 
{
    public class DqPart
    {
        public PartType Type { get; set; }
        public DqParagraph Start { get; set; }
        public List<DqParagraph> Paragraphs { get; } = new List<DqParagraph>();
    }
}
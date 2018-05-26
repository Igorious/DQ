using System.Collections.Generic;

namespace DQ.Core 
{
    public sealed class DqPart
    {
        public MainPartType Type { get; set; }
        public List<DqParagraph> Paragraphs { get; } = new List<DqParagraph>();
    }
}
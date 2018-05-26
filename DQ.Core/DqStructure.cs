using System.Collections.Generic;

namespace DQ.Core 
{
    public sealed class DqStructure
    {
        public DqPart Title { get; set; }
        public List<DqPart> Abstracts { get; } = new List<DqPart>();
        public DqPart Toc { get; set; }
        public DqPart Introduction { get; set; }
        public DqPart MainPart { get; set; }
        public DqPart Conclusion { get; set; }
        public DqPart Bibliography { get; set; }
        public DqPart Appendixes { get; set; }
    }
}
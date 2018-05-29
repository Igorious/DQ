using System.Collections.Generic;
using System.Linq;

namespace DQ.Core 
{
    public sealed class DqStructure
    {
        public DqPart Title { get; set; }
        public List<DqPart> Abstracts { get; } = new List<DqPart>();
        public DqPart Toc { get; set; }
        public DqPart Introduction { get; set; }
        public DqMainPart MainPart { get; set; }
        public DqPart Conclusion { get; set; }
        public DqPart Bibliography { get; set; }
        public DqPart Appendixes { get; set; }

        public IReadOnlyCollection<DqPart> GetAllParts()
        {
            return Iterate().Where(p => p != null).ToList();

            IEnumerable<DqPart> Iterate()
            {
                yield return Title;
                foreach (var @abstract in Abstracts)
                {
                    yield return @abstract;
                }

                yield return Toc;
                yield return Introduction;
                if (MainPart != null)
                {
                    foreach (var child in MainPart.Children)
                    {
                        yield return child;
                    }
                }
                yield return Conclusion;
                yield return Bibliography;
                yield return Appendixes;
            }
        }
    }
}
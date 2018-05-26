using System.Linq;
using System.Text.RegularExpressions;

namespace DQ.Core 
{
    public sealed class DqSourceReferenceParser
    {
        public void Parse(DqParagraph paragraph, Node root)
        {
            var m = Regex.Matches(paragraph.Text, @"[^]]\[(\d+)(?:,\s*c(?:тр)\.\d+)?\]", RegexOptions.IgnoreCase);
            if (m.Count != 0)
            {
                paragraph.Meta.Structure.AddRange(m.Cast<Match>().Select(mm => mm.Groups[1].Value).Select(t => new DqNumberedElement(paragraph, DqStructureElementType.SourceReference) { Number = t }));
            }

            var sourceNode = root.Children.FirstOrDefault(c => c.Type == MainPartType.Bibliography);
            if (sourceNode == null || !sourceNode.ContentParagraphs.Contains(paragraph)) return;

            if (paragraph.Index > sourceNode.HeaderParagraph.Index)
            {
                var m1 = Regex.Match(paragraph.Text, @"^(\d+)", RegexOptions.IgnoreCase);
                if (m1.Success)
                {
                    paragraph.Meta.Structure.Add(new DqSource(paragraph, DqStructureElementType.SourceDeclaration) { Number = m1.Groups[1].Value });
                }
            }
        }
    }
}
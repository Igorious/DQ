using System.Linq;
using System.Text.RegularExpressions;

namespace DQ.Core 
{
    public sealed class DqSourceReferenceParser
    {
        public void Parse(DqParagraph paragraph, DqDocument dqDocument)
        {
            var m = Regex.Matches(paragraph.Text, @"[^]]\[(\d+)(?:,\s*c(?:тр)\.\d+)?\]", RegexOptions.IgnoreCase);
            if (m.Count != 0)
            {
                paragraph.Meta.Structure.AddRange(m.Cast<Match>().Select(mm => mm.Groups[1].Value).Select(t => new DqNumberedElement(paragraph, DqStructureElementType.SourceReference) { Number = t }));
            }

            var bibliographyPart = dqDocument.Structure.Bibliography;
            if (bibliographyPart == null || !bibliographyPart.Paragraphs.Contains(paragraph)) return;

            var match = Regex.Match(paragraph.Text, @"^\s*(\d+)", RegexOptions.IgnoreCase);
            if (!match.Success) return;
            paragraph.Meta.Structure.Add(new DqSource(paragraph, DqStructureElementType.SourceDeclaration) { Number = match.Groups[1].Value });
        }
    }
}
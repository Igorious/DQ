using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DQ.Core 
{
    public sealed class DqFigureReferenceParser
    {
        public void Parse(DqParagraph paragraph)
        {
            var m = Regex.Matches(paragraph.Text, @"(?:рис(?:\.|унок|унк[аеу]|унком)|мал(?:\.|юнак|юнк[аеу]|юнкам))\s+(\d+(\.\d+)*)", RegexOptions.IgnoreCase);
            if (m.Count == 0) return;

            var trimmedText = paragraph.Text.TrimStart();
            if (m.Count == 1
                && (m[0].Value.StartsWith("рисунок", StringComparison.InvariantCultureIgnoreCase) 
                    || m[0].Value.StartsWith("малюнак", StringComparison.InvariantCultureIgnoreCase))
                && trimmedText.StartsWith(m[0].Value))
            {
                paragraph.Meta.Structure.Add(new DqNumberedElement(paragraph, DqStructureElementType.FigureDeclaration) { Number = m[0].Groups[1].Value });
            }
            else
            {
                paragraph.Meta.Structure.Add(new DqNumberedElement(paragraph, DqStructureElementType.FigureReference) { Number =  m[0].Groups[1].Value });
            }

            paragraph.Meta.Structure.AddRange(m.Cast<Match>().Skip(1).Select(mm => mm.Groups[1].Value).Select(t => new DqSource(paragraph, DqStructureElementType.FigureReference) { Number = t }));
        }
    }
}
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DQ.Core 
{
    public sealed class DqTableReferenceParser
    {
        public void Parse(DqParagraph paragraph)
        {
            var m = Regex.Matches(paragraph.Text, @"(?:табл(?:\.|иц[аые]|ице[ею]|іц[аы]|іца[йю]))\s+(\d+(\.\d+)*)", RegexOptions.IgnoreCase);
            if (m.Count == 0) return;

            var trimmedText = paragraph.Text.TrimStart();
            if (m.Count == 1
                && (m[0].Value.StartsWith("таблица", StringComparison.InvariantCultureIgnoreCase) 
                    || m[0].Value.StartsWith("табліца", StringComparison.InvariantCultureIgnoreCase))
                && trimmedText.StartsWith(m[0].Value)
                && (trimmedText.Substring(m[0].Value.Length).TrimStart().Length == 0 || !char.IsLower(trimmedText.Substring(m[0].Value.Length).TrimStart().First())))
            {
                paragraph.Meta.Structure.Add(new DqNumberedElement(paragraph, DqStructureElementType.TableDeclaration) { Number = m[0].Groups[1].Value });
            }
            else
            {
                paragraph.Meta.Structure.Add(new DqNumberedElement(paragraph, DqStructureElementType.TableReference) { Number =  m[0].Groups[1].Value });
            }

            paragraph.Meta.Structure.AddRange(m.Cast<Match>().Skip(1).Select(mm => mm.Groups[1].Value).Select(t => new DqNumberedElement(paragraph, DqStructureElementType.TableReference) { Number = t }));
        }
    }
}
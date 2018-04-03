using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DQ.Core
{
    public sealed class DqReferenceParser
    {
        public void ParseReferences(DqDocument document)
        {
            foreach (var paragraph in document.Paragraphs)
            {
                ParseFigureReferences(paragraph);
                ParseTableReferences(paragraph);
                ParseSourceReferences(paragraph);
            }

            var allFigureDeclarations = document.Paragraphs.SelectMany(p => p.Meta.FigureDeclarations).ToList();
            var allFigureReferences = document.Paragraphs.SelectMany(p => p.Meta.FigureReferences).ToList();

            foreach (var figureReference in allFigureReferences)
            {
                figureReference.IsMissing = allFigureDeclarations.All(fd => fd.Number != figureReference.Number);
            }

            foreach (var figureDeclaration in allFigureDeclarations)
            {
                figureDeclaration.IsMissing = allFigureReferences.All(fr => fr.Number != figureDeclaration.Number);
            }

            var allTableDeclarations = document.Paragraphs.SelectMany(p => p.Meta.TableDeclarations).ToList();
            var allTableReferences = document.Paragraphs.SelectMany(p => p.Meta.TableReferences).ToList();

            foreach (var tableReference in allTableReferences)
            {
                tableReference.IsMissing = allTableDeclarations.All(fd => fd.Number != tableReference.Number);
            }

            foreach (var tableDeclaration in allTableDeclarations)
            {
                tableDeclaration.IsMissing = allTableReferences.All(fr => fr.Number != tableDeclaration.Number);
            }
        }

        public void ParseFigureReferences(DqParagraph paragraph)
        {
            var m = Regex.Matches(paragraph.Text, @"(?:рис(?:\.|унок|унк[аеу]|унком)|мал(?:\.|юнак|юнк[аеу]|юнкам))\s+(\d+(\.\d+)*)", RegexOptions.IgnoreCase);
            if (m.Count == 0) return;

            var trimmedText = paragraph.Text.TrimStart();
            if (m.Count == 1
                && (m[0].Value.StartsWith("рисунок", StringComparison.InvariantCultureIgnoreCase) 
                || m[0].Value.StartsWith("малюнак", StringComparison.InvariantCultureIgnoreCase))
                && trimmedText.StartsWith(m[0].Value))
            {
                paragraph.Meta.FigureDeclarations.Add(new DqReference { Number = m[0].Groups[1].Value });
            }
            else
            {
                paragraph.Meta.FigureReferences.Add(new DqReference { Number =  m[0].Groups[1].Value });
            }

            paragraph.Meta.FigureReferences.AddRange(m.Cast<Match>().Skip(1).Select(mm => mm.Groups[1].Value).Select(t => new DqReference { Number = t }));
        }

        public void ParseTableReferences(DqParagraph paragraph)
        {
            var m = Regex.Matches(paragraph.Text, @"(?:табл(?:\.|иц[аые]|ице[ею]|іц[аы]|іца[йю]))\s+(\d+(\.\d+)*)", RegexOptions.IgnoreCase);
            if (m.Count == 0) return;

            var trimmedText = paragraph.Text.TrimStart();
            if (m.Count == 1
                && (m[0].Value.StartsWith("таблица", StringComparison.InvariantCultureIgnoreCase) 
                    || m[0].Value.StartsWith("табліца", StringComparison.InvariantCultureIgnoreCase))
                && trimmedText.StartsWith(m[0].Value))
            {
                paragraph.Meta.TableDeclarations.Add(new DqReference { Number = m[0].Groups[1].Value });
            }
            else
            {
                paragraph.Meta.TableReferences.Add(new DqReference { Number =  m[0].Groups[1].Value });
            }

            paragraph.Meta.TableReferences.AddRange(m.Cast<Match>().Skip(1).Select(mm => mm.Groups[1].Value).Select(t => new DqReference { Number = t }));
        }

        public void ParseSourceReferences(DqParagraph paragraph)
        {
            var m = Regex.Matches(paragraph.Text, @"[^]]\[(\d+)\]", RegexOptions.IgnoreCase);
            if (m.Count == 0) return;

            paragraph.Meta.SourceReferences.AddRange(m.Cast<Match>().Select(mm => mm.Groups[1].Value).Select(t => new DqReference { Number = t }));
        }
    }
}

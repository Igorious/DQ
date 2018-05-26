using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DQ.Core 
{
    internal sealed class HeaderParser
    {      
        public IList<Token> GetHeaders(DqDocument document)
        {
            var dqParagraphs = document.Report.MainPart.Paragraphs
                .Skip(1)
                .Where(IsNumberedParagraph)
                .ToList();

            var list = new List<Token>();
            var partNames = new List<Token>();
            foreach (var dqParagraph in dqParagraphs)
            {
                if (!list.Any())
                {
                    list.Add(CreateToken(dqParagraph));
                    continue;
                }

                if (dqParagraph.Index - list.Last().Paragraph.Index == 1 && !IsChapter(dqParagraph))
                {
                    list.Add(new Token(dqParagraph, GetLevel(dqParagraph)));
                }
                else
                {
                    var isGreater = true;
                    for (var i = 1; i < list.Count; ++i)
                    {
                        if (list[i - 1].Level >= list[i].Level)
                        {
                            isGreater = false;
                        }
                    }

                    if (isGreater)
                    {
                        partNames.AddRange(list);
                    }

                    list.Clear();
                    list.Add(new Token(dqParagraph, GetLevel(dqParagraph)));
                }
            }

            partNames.InsertRange(0, GetHeadersBeforeMainParts(document));
            partNames.AddRange(GetHeadersAfterMainParts(document));

            foreach (var token in partNames)
            {
                token.Paragraph.Meta.IsHeader = true;
            }

            return partNames;
        }

        private Token CreateToken(DqParagraph dqParagraph) => new Token(dqParagraph, GetLevel(dqParagraph));

        private IEnumerable<Token> GetHeadersBeforeMainParts(DqDocument dqDocument)
        {
            var report = dqDocument.Report;
            return report.Abstracts.Concat(new[]
                {
                    report.Toc,
                    report.Introduction,
                })
                .Select(p => p?.Paragraphs.FirstOrDefault())
                .Where(p => p != null)
                .Select(CreateToken);
        }

        private IEnumerable<Token> GetHeadersAfterMainParts(DqDocument dqDocument)
        {
            var report = dqDocument.Report;
            return new[]
                {
                    report.Conclusion,
                    report.Bibliography,
                    report.Appendixes,
                }
                .Select(p => p?.Paragraphs.FirstOrDefault())
                .Where(p => p != null)
                .Select(CreateToken);
        }

        private bool IsNumberedParagraph(DqParagraph paragraph) =>
            IsChapter(paragraph)
            || Regex.IsMatch(paragraph.Text, @"^\d+(\.\d+)*\s*[\D\S]", RegexOptions.IgnoreCase);

        private int GetLevel(DqParagraph paragraph)
        {
            if (paragraph.Style.OutlineLevel > 0)
            {
                return paragraph.Style.OutlineLevel;
            }

            if (IsChapter(paragraph)) return 0;

            var numberPart = Regex.Match(paragraph.Text, @"^\s*\d+(\.\d+)*\s*[\D\S]", RegexOptions.IgnoreCase).Value;
            var m = Regex.Matches(numberPart, @"\b(\d+)\b", RegexOptions.IgnoreCase);
            return m.Count == 0
                ? 0
                : m.Count - 1;
        }

        private bool IsChapter(DqParagraph paragraph) => 
            Regex.IsMatch(paragraph.Text, @"^\s*(Глава|Раздел|Часть|Падзел)\s*\d+(\s*[\D\S])?", RegexOptions.IgnoreCase);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace DQ.Core 
{
    internal sealed class DqPartParser
    {
        private static readonly Dictionary<PartType, IReadOnlyList<string>> TitlesByPartType = new Dictionary<PartType, IReadOnlyList<string>>
        {
            { PartType.Toc, new[] { "содержание", "змест" } },
            { PartType.Abstract, new[] { "реферат", "рэферат", "abstract" } },
            { PartType.Introduction, new[] { "введение", "ўводзіны" } },
            { PartType.Conclusion, new[] { "заключение", "вынікі" } },
            { PartType.Bibliography, new[] { "список использованных источников", "спіс выкарыстаных крыніц", "список использованной литературы" } },
            { PartType.Annex, new[] { "приложения" } },
        };

        public DqStructure PrimaryParse(DqDocument document)
        {
            var partTypeByTitle = TitlesByPartType
                .SelectMany(kv => kv.Value.Select(v => (kv.Key, v)))
                .ToDictionary(kv => kv.v, kv => kv.Key);

            var partStart = 0;

            var dqParts = new List<DqPart>();
            while (true)    
            {
                var nextPartStart = TryFindByTitle(document, partTypeByTitle.Keys, partStart);
                if (nextPartStart == null) break;

                var start = document.Paragraphs[nextPartStart.Value];
                dqParts.Add(new DqPart
                {
                    Type = partTypeByTitle[start.GetPureText().ToLower()],
                    Start = start,
                });

                partStart = nextPartStart.Value;
            }

            for (var i = 0; i < dqParts.Count - 1; ++i)
            {
                var dqPart = dqParts[i];
                dqPart.Paragraphs.AddRange(document.Paragraphs.GetRange(dqPart.Start.Index, dqParts[i + 1].Start.Index - dqPart.Start.Index));
            }

            if (!dqParts.Any())
            {
                var mainPart = new DqMainPart();
                mainPart.Paragraphs.AddRange(document.Paragraphs);
                mainPart.Start = document.Paragraphs.First();
                return new DqStructure { MainPart = mainPart };              
            }

            var lastPart = dqParts.Last();
            lastPart.Paragraphs.AddRange(document.Paragraphs.GetRange(lastPart.Start.Index, document.Paragraphs.Count - lastPart.Start.Index));

            var report = new DqStructure();
            report.Title = CopyContent(new DqPart
            {
                Type = PartType.Title,
                Start = document.Paragraphs.First()
            }, document, 0, dqParts.First().Start.Index);

            foreach (var dqPart in dqParts)
            {
                switch (dqPart.Type)
                {
                    case PartType.Abstract:
                        report.Abstracts.Add(dqPart);
                        break;

                    case PartType.Toc:
                        report.Toc = dqPart;
                        break;

                    case PartType.Introduction:
                        report.Introduction = dqPart;
                        break;

                    case PartType.Conclusion:
                        report.Conclusion = dqPart;
                        break;

                    case PartType.Bibliography:
                        report.Bibliography = dqPart;
                        break;

                    case PartType.Annex:
                        report.Appendixes = dqPart;
                        break;
                }
            }

            return report;
        }   

        public void SecondaryParse(DqStructure dqStructure)
        {
            if (dqStructure.Introduction == null) return;

            var firstHeader = dqStructure.Introduction.Paragraphs
                .Select((p, i) => (p, i))
                .Skip(1)
                .FirstOrDefault(x => x.p.Meta.IsHeader);
            if (firstHeader.p == null) return;

            var mainPartRange = dqStructure.Introduction.Paragraphs.GetRange(firstHeader.i, dqStructure.Introduction.Paragraphs.Count - firstHeader.i);
            dqStructure.Introduction.Paragraphs.RemoveRange(firstHeader.i, dqStructure.Introduction.Paragraphs.Count - firstHeader.i);

            dqStructure.MainPart = new DqMainPart();
            dqStructure.MainPart.Paragraphs.AddRange(mainPartRange);
            dqStructure.MainPart.Start = mainPartRange.First();
        }

        private DqPart CopyContent(DqPart dqPart, DqDocument dqDocument, int partStart, int? partEnd)
        {
            dqPart.Paragraphs.AddRange(dqDocument.Paragraphs.GetRange(partStart, partEnd.Value - partStart));
            return dqPart;
        }

        private int? TryFindByTitle(DqDocument document, IReadOnlyCollection<string> titles, int startIndex)
        {
            for (var i = startIndex + 1; i < document.Paragraphs.Count; ++i)
            {
                var paragraph = document.Paragraphs[i];
                var title = titles.FirstOrDefault(t => string.Equals(paragraph.GetPureText(), t, StringComparison.OrdinalIgnoreCase));
                if (title != null)
                {
                    return i;
                }
            }

            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace DQ.Core 
{
    internal sealed class DqPartParser
    {
        private static readonly Dictionary<MainPartType, IReadOnlyList<string>> TitlesByPartType = new Dictionary<MainPartType, IReadOnlyList<string>>
        {
            { MainPartType.Toc, new[] { "содержание", "змест" } },
            { MainPartType.Abstract, new[] { "реферат", "рэферат", "abstract" } },
            { MainPartType.Introduction, new[] { "введение", "ўводзіны" } },
            { MainPartType.Conclusion, new[] { "заключение", "вынікі" } },
            { MainPartType.Bibliography, new[] { "список использованных источников", "спіс выкарыстаных крыніц" } },
            { MainPartType.Annex, new[] { "приложения" } },
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

            var lastPart = dqParts.Last();
            lastPart.Paragraphs.AddRange(document.Paragraphs.GetRange(lastPart.Start.Index, document.Paragraphs.Count - lastPart.Start.Index));

            var report = new DqStructure();
            report.Title = CopyContent(new DqPart { Type = MainPartType.Title }, document, 0, dqParts.First().Start.Index);

            foreach (var dqPart in dqParts)
            {
                switch (dqPart.Type)
                {
                    case MainPartType.Abstract:
                        report.Abstracts.Add(dqPart);
                        break;

                    case MainPartType.Toc:
                        report.Toc = dqPart;
                        break;

                    case MainPartType.Introduction:
                        report.Introduction = dqPart;
                        break;

                    case MainPartType.Conclusion:
                        report.Conclusion = dqPart;
                        break;

                    case MainPartType.Bibliography:
                        report.Bibliography = dqPart;
                        break;

                    case MainPartType.Annex:
                        report.Appendixes = dqPart;
                        break;
                }
            }

            return report;
        }   

        public void SecondaryParse(DqStructure dqStructure)
        {
            var firstHeader = dqStructure.MainPart.Paragraphs
                .Select((p, i) => (p, i))
                .FirstOrDefault(x => x.p.Meta.IsHeader);
            if (firstHeader.p == null) return;

            var introductionRange = dqStructure.MainPart.Paragraphs.GetRange(0, firstHeader.i);
            dqStructure.MainPart.Paragraphs.RemoveRange(0, firstHeader.i);

            dqStructure.Introduction = new DqPart { Type = MainPartType.Introduction };
            dqStructure.Introduction.Paragraphs.AddRange(introductionRange);
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
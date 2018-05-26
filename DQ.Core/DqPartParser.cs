using System;
using System.Collections.Generic;
using System.Linq;

namespace DQ.Core 
{
    internal sealed class DqPartParser
    {
        public DqReport PrimaryParse(DqDocument document)
        {
            var report = new DqReport();
            var partStart = 0;

            var abstractTitles = new List<string> { "реферат", "рэферат", "abstract" };

            var abstractStart = TryFindByTitle(document, abstractTitles, partStart);
            if (abstractStart != null)
            {
                report.Title = CopyContent(new DqPart { Type = MainPartType.Title }, document, partStart, abstractStart);
                partStart = abstractStart.Value;
            }

            while (true)
            {
                abstractStart = TryFindByTitle(document, abstractTitles, partStart);
                if (abstractStart == null) break;

                report.Abstracts.Add(CopyContent(new DqPart { Type = MainPartType.Abstract }, document, partStart, abstractStart));
                partStart = abstractStart.Value;
            }

            //var tocStart = TryFindByTitle(document, new[] { "содержание", "змест" }, partStart);
            //if (tocStart != null)
            //{
            //    report.Abstracts.Add(CopyContent(new DqPart { Type = MainPartType.Abstract }, document, partStart, tocStart));
            //    partStart = tocStart.Value;
            //}

            var introductionStart = TryFindByTitle(document, new[] { "введение", "ўводзіны" }, partStart);
            if (introductionStart != null)
            {
                report.Abstracts.Add(CopyContent(new DqPart { Type = MainPartType.Abstract }, document, partStart, introductionStart));
                partStart = introductionStart.Value;
            }

            var conclusionStart = TryFindByTitle(document, new[] { "заключение", "вынікі" }, partStart);
            if (conclusionStart != null)
            {
                report.MainPart = CopyContent(new DqPart { Type = MainPartType.Chapter }, document, partStart, conclusionStart);
                partStart = conclusionStart.Value;
            }

            var bibliographyStart = TryFindByTitle(document, new[] { "список использованных источников", "спіс выкарыстаных крыніц" }, partStart);
            if (bibliographyStart != null)
            {
                report.Conclusion = CopyContent(new DqPart { Type = MainPartType.Conclusion }, document, partStart, bibliographyStart);
                partStart = bibliographyStart.Value;
            }

            var annexStart = TryFindByTitle(document, new[] { "приложения" }, partStart);
            if (annexStart != null)
            {
                report.Bibliography = CopyContent(new DqPart { Type = MainPartType.Bibliography }, document, partStart, annexStart);
                report.Appendixes = CopyContent(new DqPart { Type = MainPartType.Annex }, document, annexStart.Value, document.Paragraphs.Count);
            }
            else
            {
                report.Bibliography = CopyContent(new DqPart { Type = MainPartType.Bibliography }, document, partStart, document.Paragraphs.Count);
            }

            return report;
        }

        public void SecondaryParse(DqReport dqReport)
        {
            var firstHeader = dqReport.MainPart.Paragraphs
                .Select((p, i) => (p, i))
                .FirstOrDefault(x => x.p.Meta.IsHeader);
            if (firstHeader.p == null) return;

            var introductionRange = dqReport.MainPart.Paragraphs.GetRange(0, firstHeader.i);
            dqReport.MainPart.Paragraphs.RemoveRange(0, firstHeader.i);

            dqReport.Introduction = new DqPart { Type = MainPartType.Introduction };
            dqReport.Introduction.Paragraphs.AddRange(introductionRange);
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
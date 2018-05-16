using System.Collections.Generic;
using System.Linq;

namespace DQ.Core 
{
    public sealed class NumberingService
    {
        public void RestoreNumbering(DqDocument document)
        {
            var numberedParagraphs = document.Paragraphs.Where(IsNumbered).ToList();

            var countersById = numberedParagraphs
                .Select(p => p.Style.Numbering?.Id)
                .Distinct()
                .ToDictionary(k => k, k => Enumerable.Repeat(element: 0, count: 9).ToList());

            foreach (var paragraph in numberedParagraphs)
            {
                if (paragraph.Style.OutlineLevel < paragraph.Style.Numbering?.Levels.Count)
                {
                    UpdateCounters(paragraph, countersById[paragraph.Style.Numbering.Id]);

                    paragraph.Number = CalculateNumbers(paragraph, countersById[paragraph.Style.Numbering.Id]);
                    paragraph.Text = paragraph.Number + " " + paragraph.Text;
                }  
            }    
        }

        private static void UpdateCounters(DqParagraph paragraph, IList<int> counters)
        {
            for (var i = paragraph.Style.OutlineLevel + 1; i < 9; ++i)
            {
                counters[i] = 0;
            }
            counters[paragraph.Style.OutlineLevel]++;
        }

        private bool IsNumbered(DqParagraph paragraph) => paragraph.Style.Numbering != null;

        private string CalculateNumbers(DqParagraph paragraph, IReadOnlyList<int> counters)
        {
            var level = paragraph.Style.Numbering.Levels[paragraph.Style.OutlineLevel];
            var numberText = level.Text;
            for (var i = 0; i <= paragraph.Style.OutlineLevel; i++)
            {
                numberText = numberText.Replace("%" + (i + 1), counters[i].ToString());
            }
            return numberText;
        }
    }
}
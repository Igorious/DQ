using System.Linq;

namespace DQ.Core
{
    public class ParagraphAnalyzer
    {
        public void Analyze(DqDocument document)
        {
            document.Paragraphs.ForEach(Analyze);
        }

        private void Analyze(DqParagraph paragraph)
        {
            if (string.IsNullOrWhiteSpace(paragraph.Text.Replace("{IMG}", "").Replace("{TBL}", "")))
            {
                return;
            }

            if (paragraph.Style.GetFontSize() != 14)
            {
                if (paragraph.Style.GetFontSize() < 14 && (paragraph.Meta.FigureDeclarations.Any() || paragraph.Meta.TableDeclarations.Any()))
                {
                    ;
                }
                else if (paragraph.Meta.IsHeader && paragraph.Style.GetFontSize() > 14)
                {
                   
                }
                else
                {
                    paragraph.Meta.Errors.Add(new DqError($"Неверный размер шрифта ({paragraph.Style.GetFontSize()})"));
                }
            }

            var expectedFontName = "Times New Roman";
            if (paragraph.Style.GetFontName() != expectedFontName)
            {
                paragraph.Meta.Errors.Add(new DqError($"Неверный шрифт ({paragraph.Style.GetFontName()}, ожидается {expectedFontName})"));
            }

            if (!(paragraph.Style.GetIndent() != 0 || paragraph.Meta.IsHeader || paragraph.Meta.FigureDeclarations.Any() || paragraph.Meta.TableDeclarations.Any())
                || (paragraph.Style.GetIndent() != 0 && paragraph.Style.GetAligment() == Aligment.Center))
            {
                paragraph.Meta.Errors.Add(new DqError($"Неверный абзацный отступ ({paragraph.Style.GetIndent()})"));
            }

            if (!(paragraph.Style.GetSpacingBetweenLines() == 1.15m || paragraph.Meta.IsHeader))
            {
                paragraph.Meta.Errors.Add(new DqError($"Неверный междустрочный интервал ({paragraph.Style.GetSpacingBetweenLines()})"));
            }
        }
    }
}

using System.Linq;
using DQ.Core.Styling;

namespace DQ.Core
{
    public class ParagraphAnalyzer
    {
        public void Analyze(DqDocument document, Node root)
        {
            var startIndex = root.Children.First().HeaderParagraph.Index;
            document.Paragraphs.Skip(startIndex).ToList().ForEach(paragraph => Analyze(paragraph, document, root));
        }

        private void Analyze(DqParagraph paragraph, DqDocument document, Node root)
        {
            if (string.IsNullOrWhiteSpace(paragraph.Text.Replace("{IMG}", "").Replace("{TBL}", "").Replace("{PageBreak}", "")))
            {
                return;
            }

            if (paragraph.Meta.Node?.Level == 0 && root.Children.FirstOrDefault(c => c.Type == MainPartType.Bibliography)?.ContentParagraphs.Skip(1).Contains(paragraph) == false)
            {
                if (!paragraph.Text.StartsWith("{PageBreak}") && !document.Paragraphs[paragraph.Index].Text.EndsWith("{PageBreak}"))
                {
                    paragraph.Meta.Errors.Add(new DqError("Перед разделом отсутсвует разрыв страницы."));
                }
            }

            var tocNode = root.Children.FirstOrDefault(c => c.Type == MainPartType.Toc);
            if (tocNode != null && tocNode.ContentParagraphs.Contains(paragraph)) return;

            if (paragraph.Meta.FigureDeclarations.Any())
            {
                if (paragraph.Style.FontSize > 14)
                {
                    paragraph.Meta.Errors.Add(new DqError($"Неверный размер шрифта ({paragraph.Style.FontSize} пт). Подписи риунков должны использовать шрифт не больше основного (14 пт)."));
                }
            }
            else if (paragraph.Meta.TableDeclarations.Any())
            {
                if (paragraph.Style.FontSize > 14)
                {
                    paragraph.Meta.Errors.Add(new DqError($"Неверный размер шрифта ({paragraph.Style.FontSize} пт). Заголовки таблиц должны использовать шрифт не больше основного (14 пт)."));
                }
            }
            else if (paragraph.Meta.IsHeader)
            {
                if (paragraph.Style.FontSize < 14)
                {
                    paragraph.Meta.Errors.Add(new DqError($"Неверный размер шрифта ({paragraph.Style.FontSize} пт). Заголовки должны использовать шрифт не меньше основного (14 пт)."));
                }
            }

            var expectedFontName = "Times New Roman";
            if (paragraph.Style.FontName != expectedFontName)
            {
                paragraph.Meta.Errors.Add(new DqError($"Неверный шрифт ({paragraph.Style.FontName}). Иcпользуйте {expectedFontName}."));
            }

            var centeredParts = new MainPartType?[] { MainPartType.Bibliography, MainPartType.Abstract, MainPartType.Introduction, MainPartType.Conclusion, MainPartType.Toc };
            if (paragraph.Meta.IsHeader && centeredParts.Contains(paragraph.Meta.Node?.Type))
            {
                if (paragraph.Style.Aligment != DqAligment.Center)
                {
                    paragraph.Meta.Errors.Add(new DqError($"Заголовок раздела должен быть выровнен по центру."));
                }
                else if (paragraph.Style.Indent != 0)
                {
                    paragraph.Meta.Errors.Add(new DqError($"При выравнивании по центру должен отсутвовать абзацный отступ."));
                }
            }
            else if (paragraph.Meta.FigureDeclarations.Any())
            {
                if (paragraph.Style.Aligment != DqAligment.Center)
                {
                    paragraph.Meta.Errors.Add(new DqError($"Подпись рисунка должна быть выровнена по центру."));
                }
                else if (paragraph.Style.Indent != 0)
                {
                    paragraph.Meta.Errors.Add(new DqError($"При выравнивании по центру должен отсутвовать абзацный отступ."));
                }
            }
            else if (paragraph.Meta.TableDeclarations.Any())
            {
                if (paragraph.Style.Aligment != DqAligment.Left)
                {
                    paragraph.Meta.Errors.Add(new DqError($"Заголовок таблицы должен быть выровнен по левому краю."));
                }
                else if (paragraph.Style.Indent != 0)
                {
                    paragraph.Meta.Errors.Add(new DqError($"Заголовок таблицы помещают без абзацного отступа."));
                }
            }
            else if (!paragraph.Meta.IsHeader)
            {   
                if (paragraph.Style.Indent == 0)
                {
                    paragraph.Meta.Errors.Add(new DqError($"Отсутствует абзацный отступ."));
                }
            }

            if (paragraph.Style.SpacingBetweenLines != 1.5m && !paragraph.Meta.IsHeader)
            {
                paragraph.Meta.Errors.Add(new DqError($"Неверный междустрочный интервал ({paragraph.Style.SpacingBetweenLines}). Ожидается полуторный интервал."));
            }
        }
    }
}

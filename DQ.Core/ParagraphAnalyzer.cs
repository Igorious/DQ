using System;
using System.Linq;
using System.Text.RegularExpressions;
using DQ.Core.Styling;
using DQ.Properties;

namespace DQ.Core
{
    public class ParagraphAnalyzer
    {
        public void Analyze(DqDocument document)
        {
            var parts = new[]
            {
                document.Structure.Toc,
                document.Structure.Introduction,
                document.Structure.MainPart,
                document.Structure.Conclusion,
                document.Structure.Bibliography,
                document.Structure.Appendixes,
            }.Concat(document.Structure.Abstracts).Where(p => p != null);

            foreach (var dqPart in parts)
            {
                if (dqPart is DqMainPart dqMainPart)
                {
                    AnalyzeMainPart(dqMainPart, document);
                }
                else
                {
                    AnalyzeNonMainPart(dqPart, document);
                }
                FindSpaces(dqPart);
            }
        }

        private void FindSpaces(DqPart dqPart)
        {
            foreach (var dqParagraph in dqPart.Paragraphs)
            {
                if (dqParagraph.Text.StartsWith("  "))
                {
                    dqParagraph.Meta.Errors.Add(new DqWarning("Абзац начинается с нескольких пробелов."));
                }
            }
        }

        private void AnalyzeNonMainPart(DqPart dqPart, DqDocument dqDocument)
        {
            var centeredParts = new[] { PartType.Bibliography, PartType.Abstract, PartType.Introduction, PartType.Conclusion, PartType.Toc };
            if (!centeredParts.Contains(dqPart.Type)) return;

            var dqParagraph = dqPart.Start;
            if (!dqParagraph.Text.StartsWith("{PageBreak}") && !dqDocument.Paragraphs[dqParagraph.Index - 1].Text.EndsWith("{PageBreak}"))
            {
                dqParagraph.Meta.Errors.Add(new DqError("Перед разделом отсутсвует разрыв страницы."));
            }

            if (dqParagraph.Style.Aligment != DqAligment.Center)
            {
                dqParagraph.Meta.Errors.Add(new DqAlignmentError($"Заголовки разделов должны быть выровнены по центру."));
            }
            else if (dqParagraph.Style.Indent != 0)
            {
                dqParagraph.Meta.Errors.Add(new DqAlignmentError($"При выравнивании по центру должен отсутвовать абзацный отступ."));
            }

            if (string.Equals(dqParagraph.GetPureText(), "список использованной литературы", StringComparison.OrdinalIgnoreCase))
            {
                dqParagraph.Meta.Errors.Add(new DqError($"Нормативное название части — «Список использованных источников»."));
            }
        }

        private void AnalyzeMainPart(DqMainPart dqMainPart, DqDocument dqDocument)
        {
            foreach (var dqParagraph in dqMainPart.Paragraphs)
            {
                AnalyzeMainPartParagraph(dqParagraph, dqDocument);
            }

            foreach (var chapter in dqMainPart.Children)
            {
                var dqParagraph = chapter.Start;
                if (!dqParagraph.Text.StartsWith("{PageBreak}") && !dqDocument.Paragraphs[dqParagraph.Index - 1].Text.EndsWith("{PageBreak}"))
                {
                    dqParagraph.Meta.Errors.Add(new DqError("Перед разделом отсутсвует разрыв страницы."));
                }
            }
        }

        private void AnalyzeMainPartParagraph(DqParagraph dqParagraph, DqDocument dqDocument)
        {
            if (string.IsNullOrWhiteSpace(dqParagraph.Text.Replace("{IMG}", "").Replace("{TBL}", "").Replace("{PageBreak}", "")))
            {
                return;
            }
       
            if (dqParagraph.Meta.FigureDeclarations.Any())
            {
                if (dqParagraph.Style.FontSize > Settings.Default.ExpectedFontSize)
                {
                    dqParagraph.Meta.Errors.Add(new DqFontSizeError($"Неверный размер шрифта ({dqParagraph.Style.FontSize} пт). Подписи риcунков должны использовать шрифт не больше основного ({Settings.Default.ExpectedFontSize} пт)."));
                }
            }
            else if (dqParagraph.Meta.TableDeclarations.Any())
            {
                if (dqParagraph.Style.FontSize > Settings.Default.ExpectedFontSize)
                {
                    dqParagraph.Meta.Errors.Add(new DqFontSizeError($"Неверный размер шрифта ({dqParagraph.Style.FontSize} пт). Заголовки таблиц должны использовать шрифт не больше основного ({Settings.Default.ExpectedFontSize} пт)."));
                }
            }
            else if (dqParagraph.Meta.IsHeader)
            {
                if (dqParagraph.Style.FontSize < Settings.Default.ExpectedFontSize)
                {
                    dqParagraph.Meta.Errors.Add(new DqFontSizeError($"Неверный размер шрифта ({dqParagraph.Style.FontSize} пт). Заголовки должны использовать шрифт не меньше основного ({Settings.Default.ExpectedFontSize} пт)."));
                }

                if (dqParagraph.GetPureText().Trim().EndsWith("."))
                {
                    dqParagraph.Meta.Errors.Add(new DqError($"В конце заголовков точка не ставится."));
                }

                var number = Regex.Match(dqParagraph.GetPureText().TrimStart(), @"^((?:\d+\.)*\d+?)");
                if (number.Success && number.Value.EndsWith("."))
                {
                    dqParagraph.Meta.Errors.Add(new DqError($"В конце номера (под)раздела точка не ставится."));
                }
            }

            if (dqParagraph.Style.FontName != Settings.Default.ExpectedFontName)
            {
                dqParagraph.Meta.Errors.Add(new DqFontError($"Неверный шрифт ({dqParagraph.Style.FontName}). Иcпользуйте {Settings.Default.ExpectedFontName}."));
            }

            if (dqParagraph.Meta.IsHeader)
            {
                if (dqParagraph.Style.Aligment == DqAligment.Center || dqParagraph.Style.Aligment == DqAligment.Right)
                {
                    dqParagraph.Meta.Errors.Add(new DqAlignmentError($"Заголовки главной части должны быть выровнены по левому краю."));
                }
                else if (dqParagraph.Style.Indent == 0)
                {
                    dqParagraph.Meta.Errors.Add(new DqAlignmentError($"Отсутвует абзацный отступ."));
                }
            }
            else if (dqParagraph.Meta.FigureDeclarations.Any())
            {
                if (dqParagraph.Style.Aligment != DqAligment.Center)
                {
                    dqParagraph.Meta.Errors.Add(new DqAlignmentError($"Подпись рисунка должна быть выровнена по центру."));
                }
                else if (dqParagraph.Style.Indent != 0)
                {
                    dqParagraph.Meta.Errors.Add(new DqAlignmentError($"При выравнивании по центру должен отсутвовать абзацный отступ."));
                }
            }
            else if (dqParagraph.Meta.TableDeclarations.Any())
            {
                if (dqParagraph.Style.Aligment == DqAligment.Center || dqParagraph.Style.Aligment == DqAligment.Right)
                {
                    dqParagraph.Meta.Errors.Add(new DqAlignmentError($"Заголовок таблицы должен быть выровнен по левому краю."));
                }
                else if (dqParagraph.Style.Indent != 0)
                {
                    dqParagraph.Meta.Errors.Add(new DqAlignmentError($"Заголовок таблицы помещают без абзацного отступа."));
                }
            }
            else if (!dqParagraph.Meta.IsHeader)
            {   
                if (dqParagraph.Style.Indent == 0)
                {
                    dqParagraph.Meta.Errors.Add(new DqAlignmentError($"Отсутствует абзацный отступ."));
                }
            }

            if (dqParagraph.Style.SpacingBetweenLines != Settings.Default.ExpectedSpacingBetweenLines && !dqParagraph.Meta.IsHeader && !dqParagraph.Meta.FigureDeclarations.Any() && !dqParagraph.Meta.TableDeclarations.Any())
            {
                dqParagraph.Meta.Errors.Add(new DqError($"Неверный междустрочный интервал ({dqParagraph.Style.SpacingBetweenLines}). Ожидается {Settings.Default.ExpectedSpacingBetweenLines}-ый интервал."));
            }
        }
    }
}

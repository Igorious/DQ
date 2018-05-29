using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JetBrains.Annotations;

namespace DQ.Core.Styling 
{
    public sealed class DqStyleParser
    {
        private readonly DqNumberingParser _dqNumberingParser = new DqNumberingParser();

        public DqStyleTable ParseStyleTable(WordprocessingDocument wDocument)
        {
            var dqFontScheme = GetFontScheme(wDocument);
            var dqNumberingTable = _dqNumberingParser.ParseNumberingTable(wDocument);

            var wStyles = wDocument.MainDocumentPart.StyleDefinitionsPart.Styles;
            return new DqStyleTable(GetStyles(wStyles, dqNumberingTable, dqFontScheme), GetDefaultFont(wStyles, dqFontScheme));
        }

        public DqStyle GetParagraphStyle(Paragraph paragraph, DqStyleTable styleTable, DqFontScheme fontScheme, DqNumberingTable dqNumberingTable)
        {
            var value = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            var basicStyle = styleTable.Paragraph[value] ?? styleTable.Paragraph.Default;

            var pPr = paragraph.ParagraphProperties;
            if (pPr == null) return basicStyle;

            var localStyle = new DqStyle { BaseStyle = basicStyle };

            if (pPr.SpacingBetweenLines != null)
            {
                localStyle.Current.SpacingBetweenLines = GetSpacingBetweenLines(pPr.SpacingBetweenLines.Line);
            }

            if (pPr.Indentation?.FirstLine != null)
            {
                localStyle.Current.Indent = Convertion.TwipToCm(int.Parse(pPr.Indentation.FirstLine.Value));
            }

            if (pPr.Justification != null)
            {
                localStyle.Current.Aligment = Convert(pPr.Justification.Val);
            }
            
            if (pPr.GetFirstChild<NumberingProperties>() != null)
            {
                var id = pPr.GetFirstChild<NumberingProperties>().NumberingId;
                var numbering = dqNumberingTable[id.Val];
                localStyle.Current.Numbering = numbering;

                var level = pPr.GetFirstChild<NumberingProperties>().NumberingLevelReference;
                if (level != null && level.Val < numbering.Levels.Count)
                {
                    localStyle.Current.Indent = numbering.Levels[level.Val].Indent ?? localStyle.Indent;
                }               
            }

            var rPr = (OpenXmlElement) pPr.ParagraphMarkRunProperties;

            if (rPr != null)
            {
                if (rPr.GetFirstChild<RunFonts>() != null)
                {
                    localStyle.Current.FontName = GetFontName(rPr.GetFirstChild<RunFonts>(), fontScheme);
                }

                if (rPr.GetFirstChild<FontSize>() != null)
                {
                    localStyle.Current.FontSize = ConvertFontSize(rPr.GetFirstChild<FontSize>());
                }

                if (rPr.GetFirstChild<Bold>() != null)
                {
                    localStyle.Current.IsBold = ConvertBold(rPr.GetFirstChild<Bold>());
                }
            }
     
            var runs = paragraph.Descendants<Run>().ToList();
            if (runs.Count == 1)
            {
                rPr = runs.Single().RunProperties;
                if (rPr != null)
                {
                    if (rPr.GetFirstChild<RunFonts>() != null)
                    {
                        localStyle.Current.FontName = GetFontName(rPr.GetFirstChild<RunFonts>(), fontScheme);
                    }

                    if (rPr.GetFirstChild<FontSize>() != null)
                    {
                        localStyle.Current.FontSize = ConvertFontSize(rPr.GetFirstChild<FontSize>());
                    }

                    if (rPr.GetFirstChild<Bold>() != null)
                    {
                        localStyle.Current.IsBold = ConvertBold(rPr.GetFirstChild<Bold>());
                    }
                }
            }

            return localStyle;
        }

        public DqFontScheme GetFontScheme(WordprocessingDocument wDocument) => 
            new DqFontScheme(wDocument.MainDocumentPart.ThemePart.Theme.ThemeElements.FontScheme);

        private DqStyleBasis GetDefaultFont(Styles wStyles, DqFontScheme dqFontScheme) => new DqStyleBasis
        {
            IsDefault = false,
            IsBold = false,
            FontSize = ConvertFontSize(wStyles.DocDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.FontSize),
            FontName = GetFontName(wStyles.DocDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.RunFonts, dqFontScheme),
            OutlineLevel = 0,
            Aligment = DqAligment.Left,
            OtherIndent = 0,
            Indent = 0,
            InlineLevel = 0,
            SpacingBetweenLines = 1,
            Numbering = null,
        };

        private IReadOnlyCollection<DqStyle> GetStyles(Styles wStyles, DqNumberingTable dqNumberingsTable, DqFontScheme dqFontScheme) => 
            wStyles.Descendants<Style>().Select(wStyle => ConvertStyle(wStyle, dqNumberingsTable, dqFontScheme)).ToList();

        private bool ConvertBold(Bold bold) => bold.Val?.Value ?? true;

        private DqStyle ConvertStyle(Style style, DqNumberingTable dqNumberingsTable, DqFontScheme fontScheme) =>
            new DqStyle
            {
                ID = style.StyleId,
                Type = style.Type == StyleValues.Paragraph ? DqStyleType.Paragraph : DqStyleType.Other,
                BaseStyleID = style.BasedOn?.Val.Value,
                Current = new DqStyleBasis
                {
                    IsBold = style.StyleRunProperties?.Bold is Bold bold
                        ? ConvertBold(bold)
                        : (bool?)null,
                    FontSize = ConvertFontSize(style.StyleRunProperties?.FontSize),
                    FontName = GetFontName(style, fontScheme),
                    IsDefault = style.Default?.Value,
                    OutlineLevel = style.StyleParagraphProperties?.GetFirstChild<OutlineLevel>()?.Val?.Value,
                    InlineLevel = style.StyleParagraphProperties?.NumberingProperties?.NumberingLevelReference?.Val?.Value,
                    Numbering = style.StyleParagraphProperties?.NumberingProperties?.NumberingId?.Val?.Value is int numID
                        ? dqNumberingsTable[numID]
                        : null,
                    Indent = style.StyleParagraphProperties?.Indentation?.FirstLine != null
                        ? Convertion.TwipToCm(int.Parse(style.StyleParagraphProperties.Indentation.FirstLine))
                        : (decimal?)null,
                    OtherIndent = style.StyleParagraphProperties?.Indentation?.Left != null
                        ? Convertion.TwipToCm(int.Parse(style.StyleParagraphProperties.Indentation.Left))
                        : (decimal?) null,
                    SpacingBetweenLines = GetSpacingBetweenLines(style),
                    Aligment = GetJustification(style),
                }
            };
  
        private static decimal? ConvertFontSize([CanBeNull] FontSize wFontSize) =>
            wFontSize != null
                ? int.Parse(wFontSize.Val) / 2m
                : (decimal?) null;

        private decimal? GetSpacingBetweenLines(Style wStyle)
        {
            var spacingBetweenLines = wStyle.StyleParagraphProperties?.SpacingBetweenLines?.Line;
            return GetSpacingBetweenLines(spacingBetweenLines);
        }
       
        private static decimal? GetSpacingBetweenLines(StringValue spacingBetweenLines)
        {
            if (spacingBetweenLines == null) return null;
            var singleSpacing = 240m;
            return decimal.Round(int.Parse(spacingBetweenLines) / singleSpacing, decimals: 2);
        }

        private DqAligment? GetJustification(Style style)
        {
            var wJustification = style.StyleParagraphProperties?.Justification?.Val.Value;
            return wJustification != null? Convert(wJustification) : null;
        }

        private static DqAligment? Convert(JustificationValues? wJustification)
        {
            switch (wJustification)
            {
                case JustificationValues.Left:
                    return DqAligment.Left;

                case JustificationValues.Center:
                    return DqAligment.Center;

                case JustificationValues.Right:
                    return DqAligment.Right;

                case JustificationValues.Both:
                    return DqAligment.Justify;

                default:
                    return null;
            }
        }

        [CanBeNull]
        private string GetFontName(Style style, DqFontScheme fontScheme)
        {
            return GetFontName(style.StyleRunProperties?.RunFonts, fontScheme);
        }

        [CanBeNull]
        private static string GetFontName([CanBeNull] RunFonts runFonts, DqFontScheme fontScheme)
        {
            var asciiTheme = runFonts?.ComplexScriptTheme?.Value;

            if (asciiTheme == ThemeFontValues.MajorAscii || asciiTheme == ThemeFontValues.MajorHighAnsi || asciiTheme == ThemeFontValues.MajorBidi)
            {
                return fontScheme.MajorFont.LatinFont.Typeface;
            }

            if (asciiTheme == ThemeFontValues.MinorAscii || asciiTheme == ThemeFontValues.MinorHighAnsi || asciiTheme == ThemeFontValues.MinorBidi)
            {
                return fontScheme.MinorFont.LatinFont.Typeface;
            }

            return runFonts?.ComplexScript;
        }
    }
}
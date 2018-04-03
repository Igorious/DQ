using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JetBrains.Annotations;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace DQ.Core 
{
    public class DocxParser
    {
        public static decimal TwipToCm(int twips)
        {
            const decimal cmPerTwip = 0.0017638889m;
            return decimal.Round(twips * cmPerTwip, 1);
        }

        public static decimal TwipToCm(uint twips) => TwipToCm((int)twips);

        public DqDocument Parse(string path)
        {
            using (var doc = WordprocessingDocument.Open(path, isEditable: false))
            {                
                var dqDocument = new DqDocument();

                var fontScheme = doc.MainDocumentPart.ThemePart.Theme.ThemeElements.FontScheme;

                dqDocument.Numbering.AddRange(GetNumbering(doc));
                dqDocument.Styles.AddRange(GetStyles(doc, dqDocument.Numbering, fontScheme));

                var body = doc.MainDocumentPart.Document.Body;
                dqDocument.Sections.AddRange(GetSections(body));

                List<DqParagraph> paragraphs = new List<DqParagraph>();
                foreach (var element in body.Elements())
                {
                    if (element is Paragraph p)
                    {
                        paragraphs.Add(Convert(p, dqDocument.Styles, fontScheme));
                    }
                    else if (element is DocumentFormat.OpenXml.Wordprocessing.Table)
                    {
                        paragraphs.Add(new DqParagraph("{TBL}", dqDocument.Styles.First(s => s.IsDefault)));
                    }
                }

                dqDocument.Paragraphs.AddRange(paragraphs.Where(p => !string.IsNullOrWhiteSpace(p.Text)));

                for (int i = 0; i < dqDocument.Paragraphs.Count; i++)
                {
                    dqDocument.Paragraphs[i].Index = i;
                }

                return dqDocument;
            }
        }

        private IReadOnlyCollection<DqStyle> GetStyles(WordprocessingDocument document, IReadOnlyCollection<DqNumbering> numberings, FontScheme fontScheme)
        {
            var styles = document.MainDocumentPart.StyleDefinitionsPart.Styles;
            var dqStyles = styles.Descendants<Style>().Select(s => Convert(s, numberings, fontScheme)).ToDictionary(s => s.ID);
            var defaultStyle = new DqStyle("default", null, false, false, Convert(styles.DocDefaults.RunPropertiesDefault.RunPropertiesBaseStyle.FontSize), null, 9, null, null);
            foreach (var dqStyle in dqStyles.Values)
            {
                dqStyle.BaseStyle = dqStyle.BaseStyleID != null
                    ? dqStyles[dqStyle.BaseStyleID]
                    : defaultStyle;
            }
            return dqStyles.Values;
        }

        private static decimal? Convert([CanBeNull] FontSize fontSize) =>
            fontSize != null
                ? int.Parse(fontSize.Val) / 2m
                : (decimal?) null;

        private DqStyle Convert(Style style, IReadOnlyCollection<DqNumbering> numberings, FontScheme fontScheme) =>
            new DqStyle(
                id: style.StyleId,
                baseStyleID: style.BasedOn?.Val.Value,
                isBold: style.StyleRunProperties?.Bold is Bold bold
                    ? bold.Val?.Value ?? true
                    : (bool?)null,
                fontSize: Convert(style.StyleRunProperties?.FontSize),
                fontName: GetFontName(style, fontScheme),
                isDefault: style.Default?.Value ?? false,
                outlineLevel: style.StyleParagraphProperties?.GetFirstChild<OutlineLevel>()?.Val?.Value ?? 9,
                inlineLevel: style.StyleParagraphProperties?.NumberingProperties?.NumberingLevelReference?.Val?.Value,
                numbering: style.StyleParagraphProperties?.NumberingProperties?.NumberingId?.Val != null
                    ? numberings.FirstOrDefault(n => n.Id == style.StyleParagraphProperties.NumberingProperties.NumberingId.Val)
                    : null)
            {
                Indent = style.StyleParagraphProperties?.Indentation?.FirstLine != null
                    ? TwipToCm(int.Parse(style.StyleParagraphProperties.Indentation.FirstLine))
                    : (decimal?)null,
                OtherIndent =  style.StyleParagraphProperties?.Indentation?.Left != null
                    ? TwipToCm(int.Parse(style.StyleParagraphProperties.Indentation.Left))
                    : (decimal?) null,
                SpacingBetweenLines = GetSpacingBetweenLines(style),
                Aligment = GetJustification(style),
            };

        private decimal? GetSpacingBetweenLines(Style style)
        {
            var spacingBetweenLines = style.StyleParagraphProperties?.SpacingBetweenLines?.Line;
            if (spacingBetweenLines == null) return null;

            var singleSpacing = 240m;
            return decimal.Round(int.Parse(spacingBetweenLines) / singleSpacing, decimals: 2);
        }

        private Aligment? GetJustification(Style style)
        {
            var j = style.StyleParagraphProperties?.Justification?.Val.Value;
            if (j == null) return null;

            switch (j)
            {
                case JustificationValues.Left:
                    return Aligment.Left;

                case JustificationValues.Center:
                    return Aligment.Center;

                case JustificationValues.Right:
                    return Aligment.Right;

                case JustificationValues.Both:
                    return Aligment.Justify;
                 
                default:
                    return null;
            }
        }

        [CanBeNull]
        private string GetFontName(Style style, FontScheme fontScheme)
        {
            return GetFontName(style.StyleRunProperties?.RunFonts, fontScheme);
        }

        [CanBeNull]
        private static string GetFontName([CanBeNull] RunFonts runFonts, FontScheme fontScheme)
        {
            var asciiTheme = runFonts?.AsciiTheme?.Value;

            if (asciiTheme == ThemeFontValues.MajorAscii || asciiTheme == ThemeFontValues.MajorHighAnsi)
            {
                return fontScheme.MajorFont.LatinFont.Typeface;
            }

            if (asciiTheme == ThemeFontValues.MinorAscii || asciiTheme == ThemeFontValues.MinorHighAnsi)
            {
                return fontScheme.MinorFont.LatinFont.Typeface;
            }

            return runFonts?.Ascii;
        }

        private IReadOnlyCollection<DqNumbering> GetNumbering(WordprocessingDocument document)
        {
            var numbering = document.MainDocumentPart.NumberingDefinitionsPart.Numbering;
            var abstractNumById = numbering.Elements<AbstractNum>().ToDictionary(an => an.AbstractNumberId.Value, Convert);
            return numbering.Elements<NumberingInstance>().Select(ni => abstractNumById[ni.AbstractNumId.Val.Value].Clone(ni.NumberID.Value)).ToList();
        }

        private DqNumbering Convert(AbstractNum abstractNum)
        {
            var dqNumbering = new DqNumbering();
            dqNumbering.Levels.AddRange(abstractNum.Descendants<Level>().Select(Convert));
            return dqNumbering;
        }

        private DqNumberingLevel Convert(Level level) => new DqNumberingLevel(level.LevelText.Val);

        private IReadOnlyCollection<DqSection> GetSections(Body body) => 
            body.Descendants<SectionProperties>().Select(Convert).ToList();

        private DqSection Convert(SectionProperties sectionProperties) =>
            new DqSection(
                pageSize: Convert(sectionProperties.GetFirstChild<PageSize>()),
                pageMargin: Convert(sectionProperties.GetFirstChild<PageMargin>()),
                footers: Convert(sectionProperties.Descendants<FooterReference>().ToList(), sectionProperties));

        private DqFooters Convert(IReadOnlyCollection<FooterReference> footerReferences, SectionProperties sectionProperties) =>
            new DqFooters(
                @default: Convert(footerReferences, HeaderFooterValues.Default),
                first: sectionProperties.GetFirstChild<TitlePage>() != null
                    ? Convert(footerReferences, HeaderFooterValues.First)
                    : Convert(footerReferences, HeaderFooterValues.Default),
                even: sectionProperties.GetFirstChild<EvenAndOddHeaders>() != null
                    ? Convert(footerReferences, HeaderFooterValues.Even)
                    : Convert(footerReferences, HeaderFooterValues.Default));

        private DqFooter Convert(IReadOnlyCollection<FooterReference> footerReferences, HeaderFooterValues headerFooter) =>
            Convert(footerReferences.FirstOrDefault(fr => fr.Type == headerFooter));

        private DqFooter Convert(FooterReference footerReference) =>
            footerReference != null
                ? new DqFooter { HasPageNumber = HasPageNumber(footerReference) }
                : null;

        private bool HasPageNumber(FooterReference footerReference)
        {
            var document = footerReference.Ancestors<Document>().Single();             
            var footerPart = (FooterPart) document.MainDocumentPart.GetPartById(footerReference.Id);
            var fieldCodes = footerPart.Footer.Descendants<FieldCode>();
            return fieldCodes.Any(fc => fc.Text.Contains("PAGE"));
        }

        private DqPageSize Convert(PageSize pageSize) =>
            pageSize != null
                ? new DqPageSize(
                    height: TwipToCm(pageSize.Height),
                    width: TwipToCm(pageSize.Width))
                : null;

        private DqPageMargin Convert(PageMargin pageMargin) =>
            pageMargin != null
                ? new DqPageMargin(
                    top: TwipToCm(pageMargin.Top),
                    left: TwipToCm(pageMargin.Left),
                    right: TwipToCm(pageMargin.Right),
                    bottom: TwipToCm(pageMargin.Bottom))
                : null;

        private DqParagraph Convert(Paragraph paragraph, IReadOnlyCollection<DqStyle> styles, FontScheme fontScheme) =>
            new DqParagraph(
                text: string.Join("", paragraph.Elements<Run>().Select(r => GetText(r, styles, fontScheme))),
                style: GetStyle(paragraph, styles, fontScheme));

        private static DqStyle GetStyle(Paragraph paragraph, IReadOnlyCollection<DqStyle> styles, FontScheme fontScheme)
        {
            var basicStyle = styles.FirstOrDefault(s => s.ID == paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value)
                             ?? styles.First(s => s.IsDefault);

            if (paragraph.ParagraphProperties?.ParagraphMarkRunProperties != null)
            {
                basicStyle = basicStyle.Clone();

                var rPr = paragraph.ParagraphProperties.ParagraphMarkRunProperties;
                if (rPr.GetFirstChild<RunFonts>() != null)
                {
                    basicStyle.FontName = GetFontName(rPr.GetFirstChild<RunFonts>(), fontScheme);
                }

                if (rPr.GetFirstChild<FontSize>() != null)
                {
                    basicStyle.FontSize = Convert(rPr.GetFirstChild<FontSize>());
                }
            }

            return basicStyle;
        }

        private string GetText(Run run, IReadOnlyCollection<DqStyle> styles, FontScheme fontScheme)
        {
            var buffer = new StringBuilder();

            foreach (var element in run.Elements())
            {
                switch (element)
                {
                    case AlternateContent alternateContent:
                        if (alternateContent.Descendants<GraphicFrameLocks>().Any())
                        {
                            buffer.Append("{IMG}");
                            break;
                        }
                        var textBoxes = alternateContent.Elements<AlternateContentFallback>().SelectMany(f => f.Descendants<TextBoxContent>());
                        var paragraphs = textBoxes.SelectMany(textBox => textBox.Elements<Paragraph>()).ToList();
                        foreach (var paragraph in paragraphs.Select(p => Convert(p, styles, fontScheme)))
                        {
                            buffer.AppendLine(paragraph.Text);
                        }
                        break;

                    case Text text:
                        buffer.Append(text.Text);
                        break;

                    case Drawing drawing:
                        if (drawing.Descendants<DocumentFormat.OpenXml.Drawing.Pictures.Picture>().Any())
                        {
                            buffer.Append("{IMG}");
                        }
                        break;
                }
            }

            return buffer.ToString();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DQ.Core.Styling;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Path = System.IO.Path;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace DQ.Core 
{
    public static class Convertion
    {
        public static decimal TwipToCm(int twips)
        {
            const decimal cmPerTwip = 0.0017638889m;
            return decimal.Round(twips * cmPerTwip, 1);
        }

        public static decimal TwipToCm(uint twips) => TwipToCm((int) twips);
    }

    public class DocxParser
    {
        private readonly DqNumberingParser _dqNumberingParser = new DqNumberingParser();
        private readonly DqStyleParser _dqStyleParser = new DqStyleParser();

        public DqDocument Parse(string path)
        {
            var tempPath = Path.GetTempFileName();
            File.Copy(path, tempPath, overwrite: true);

            try
            {
                using (var doc = WordprocessingDocument.Open(tempPath, isEditable: false))
                {                
                    var dqDocument = new DqDocument();

                    var fontScheme = _dqStyleParser.GetFontScheme(doc);
                    dqDocument.NumberingTable = _dqNumberingParser.ParseNumberingTable(doc);
                    dqDocument.StyleTable = _dqStyleParser.ParseStyleTable(doc);

                    var body = doc.MainDocumentPart.Document.Body;
                    dqDocument.Sections.AddRange(GetSections(body));

                    List<DqParagraph> paragraphs = new List<DqParagraph>();
                    foreach (var element in body.Elements())
                    {
                        if (element is Paragraph p)
                        {
                            paragraphs.Add(Convert(p, dqDocument.StyleTable, fontScheme, dqDocument.NumberingTable));
                        }
                        else if (element is DocumentFormat.OpenXml.Wordprocessing.Table)
                        {
                            paragraphs.Add(new DqParagraph("{TBL}", dqDocument.StyleTable.Paragraph.Default));
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
            finally
            {
                File.Delete(tempPath);
            }
        }

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
                    height: Convertion.TwipToCm(pageSize.Height),
                    width: Convertion.TwipToCm(pageSize.Width))
                : null;

        private DqPageMargin Convert(PageMargin pageMargin) =>
            pageMargin != null
                ? new DqPageMargin(
                    top: Convertion.TwipToCm(pageMargin.Top),
                    left: Convertion.TwipToCm(pageMargin.Left),
                    right: Convertion.TwipToCm(pageMargin.Right),
                    bottom: Convertion.TwipToCm(pageMargin.Bottom))
                : null;

        private DqParagraph Convert(Paragraph paragraph, DqStyleTable dqStyleTable, DqFontScheme dqFontScheme, DqNumberingTable dqNumberingTable) =>
            new DqParagraph(
                text: GetText(paragraph, dqStyleTable, dqFontScheme, dqNumberingTable),
                style: _dqStyleParser.GetParagraphStyle(paragraph, dqStyleTable, dqFontScheme, dqNumberingTable));

        private string GetText(Paragraph paragraph, DqStyleTable dqStyleTable, DqFontScheme fontScheme, DqNumberingTable dqNumberingTable)
        {
            var buffer = new StringBuilder();
            foreach (var child in paragraph.Elements())
            {
                switch (child)
                {
                    case Run run:
                        buffer.Append(GetText(run, dqStyleTable, fontScheme, dqNumberingTable));
                        break;

                    case DocumentFormat.OpenXml.Wordprocessing.Hyperlink hyperlink:
                        foreach (var run in hyperlink.Elements<Run>())
                        {
                            buffer.Append(GetText(run, dqStyleTable, fontScheme, dqNumberingTable));
                        }                    
                        break;
                }
            }

            return buffer.ToString();
        }

        private string GetText(Run run, DqStyleTable dqStyleTable, DqFontScheme fontScheme, DqNumberingTable dqNumberingTable)
        {
            var buffer = new StringBuilder();

            foreach (var element in run.Elements())
            {
                switch (element)
                {
                    case DocumentFormat.OpenXml.Wordprocessing.Break @break when @break.Type?.Value == BreakValues.Page:
                        buffer.Append("{PageBreak}");
                        break;

                    case AlternateContent alternateContent:
                        if (alternateContent.Descendants<GraphicFrameLocks>().Any())
                        {
                            buffer.Append("{IMG}");
                            break;
                        }
                        var textBoxes = alternateContent.Elements<AlternateContentFallback>().SelectMany(f => f.Descendants<TextBoxContent>());
                        var paragraphs = textBoxes.SelectMany(textBox => textBox.Elements<Paragraph>()).ToList();
                        foreach (var paragraph in paragraphs.Select(p => Convert(p, dqStyleTable, fontScheme, dqNumberingTable)))
                        {
                            buffer.AppendLine(paragraph.Text);
                        }
                        break;

                    case Text text:
                        buffer.Append(text.Text);
                        break;

                    case TabChar tab:
                        buffer.Append('\t');
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
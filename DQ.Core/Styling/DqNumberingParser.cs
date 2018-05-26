using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DQ.Core.Styling 
{
    public sealed class DqNumberingParser
    {
        public DqNumberingTable ParseNumberingTable(WordprocessingDocument wDocument)
        {
            var numbering = wDocument.MainDocumentPart.NumberingDefinitionsPart?.Numbering;
            if (numbering == null) return new DqNumberingTable();

            var abstractNumById = numbering.Elements<AbstractNum>().ToDictionary(an => an.AbstractNumberId.Value, ConvertAbstractNum);
            var dqNumberings = new[] { new DqNumbering { Id = 0 } }
                .Concat(numbering.Elements<NumberingInstance>().Select(ni => abstractNumById[ni.AbstractNumId.Val].Clone(ni.NumberID)));
            return new DqNumberingTable(dqNumberings);
        }

        private DqNumbering ConvertAbstractNum(AbstractNum abstractNum)
        {
            var dqNumbering = new DqNumbering();
            dqNumbering.Levels.AddRange(abstractNum.Descendants<Level>().Select(ConvertLevel));
            return dqNumbering;
        }

        private DqNumberingLevel ConvertLevel(Level level) => 
            new DqNumberingLevel(level.LevelText.Val)
            {
                Indent = level.GetFirstChild<ParagraphProperties>()?.Indentation?.Left?.Value is string leftIndent
                    ? Convertion.TwipToCm(int.Parse(leftIndent))
                    : (decimal?) null
            };
    }
}
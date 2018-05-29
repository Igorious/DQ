using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DQ.Core.Styling;
using JetBrains.Annotations;

namespace DQ.Core
{
    public abstract class DqStructureElement { }

    public sealed class DqSource : DqNumberedElement
    {
        public DqSource(DqParagraph paragraph, DqStructureElementType type) : base(paragraph, type) { }

        public SourceType SourceType { get; set; } 
        public string Formatted { get; set; }
    }

    [DebuggerDisplay("[{Type.ToString(),nq}] [{Number,nq}] {Paragraph.Text,nq}")]
    public class DqNumberedElement : DqStructureElement
    {
        public DqNumberedElement(DqParagraph paragraph, DqStructureElementType type)
        {
            Paragraph = paragraph;
            Type = type;
        }

        public DqParagraph Paragraph { get; }
        public DqStructureElementType Type { get; }

        [CanBeNull]
        public string Number { get; set; }

        public bool IsMissing { get; set; }
    }

    public enum DqStructureElementType
    {
        FigureReference,
        FigureDeclaration,
        Figure,

        TableReference,
        TableDeclaration,
        Table,

        SourceReference,
        SourceDeclaration,
    }

    public class DqParagraphMeta
    {
        public IEnumerable<DqNumberedElement> FigureReferences => Structure.OfType<DqNumberedElement>().Where(s => s.Type == DqStructureElementType.FigureReference);
        public IEnumerable<DqNumberedElement> FigureDeclarations => Structure.OfType<DqNumberedElement>().Where(s => s.Type == DqStructureElementType.FigureDeclaration);

        public IEnumerable<DqNumberedElement> TableReferences => Structure.OfType<DqNumberedElement>().Where(s => s.Type == DqStructureElementType.TableReference);
        public IEnumerable<DqNumberedElement> TableDeclarations => Structure.OfType<DqNumberedElement>().Where(s => s.Type == DqStructureElementType.TableDeclaration);

        public IEnumerable<DqNumberedElement> SourceReferences => Structure.OfType<DqNumberedElement>().Where(s => s.Type == DqStructureElementType.SourceReference);
        public IEnumerable<DqNumberedElement> SourceDeclarations => Structure.OfType<DqNumberedElement>().Where(s => s.Type == DqStructureElementType.SourceDeclaration);


        public bool IsHeader { get; set; }

        public DqPart Part { get; set; }

        public List<DqStructureElement> Structure { get; } = new List<DqStructureElement>();

        public List<DqError> Errors { get; } = new List<DqError>();
    }

    [DebuggerDisplay("[p] {Text,nq}")]
    public class DqParagraph
    {
        public DqParagraph(string text, DqStyle style)
        {
            Text = text;
            Style = style;
        }

        public int Index { get; set; }
        public string Text { get; set; }
        public string Number { get; set; }
        public DqStyle Style { get; }
        public DqParagraphMeta Meta { get; } = new DqParagraphMeta();

        public string GetPureText() => Text.Replace("{PageBreak}", string.Empty);
    }
}
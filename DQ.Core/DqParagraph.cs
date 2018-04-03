using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace DQ.Core
{
    public class DqReference
    {
        [CanBeNull]
        public string Number { get; set; }

        public bool IsMissing { get; set; }
    }

    public class DqParagraphMeta
    {
        public List<DqReference> FigureReferences { get; } = new List<DqReference>();
        public List<DqReference> FigureDeclarations { get; } = new List<DqReference>();

        public List<DqReference> TableReferences { get; } = new List<DqReference>();
        public List<DqReference> TableDeclarations { get; } = new List<DqReference>();

        public List<DqReference> SourceReferences { get; } = new List<DqReference>();

        public bool IsHeader { get; set; }

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
        public string Text { get; }
        public DqStyle Style { get; }
        public DqParagraphMeta Meta { get; } = new DqParagraphMeta();
    }
}
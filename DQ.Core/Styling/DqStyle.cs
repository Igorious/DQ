using System.Diagnostics;
using JetBrains.Annotations;

namespace DQ.Core.Styling 
{
    public enum DqStyleType
    {
        Other,
        Paragraph,
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class DqStyle
    {
        public string ID { get; set; }
        public DqStyleType Type { get; set; }
        public DqStyleBasis Current { get; set; } = new DqStyleBasis();

        [CanBeNull] public string BaseStyleID { get; set; }
        public DqStyle BaseStyle { get; set; }

        public bool IsDefault => Current.IsDefault ?? false;
        public bool IsBold => Current.IsBold ?? BaseStyle.IsBold;
        public decimal FontSize => Current.FontSize ?? BaseStyle.FontSize;
        public string FontName => Current.FontName ?? BaseStyle.FontName;
        public int OutlineLevel => Current.OutlineLevel ?? BaseStyle.OutlineLevel;
        public int InlineLevel => Current.InlineLevel ?? BaseStyle.InlineLevel;
        public decimal Indent => Current.Indent ?? BaseStyle.Indent;
        public decimal OtherIndent => Current.OtherIndent ?? BaseStyle.OtherIndent;
        public decimal SpacingBetweenLines => Current.SpacingBetweenLines ?? BaseStyle.SpacingBetweenLines;
        public DqAligment Aligment => Current.Aligment ?? BaseStyle.Aligment;
        [CanBeNull] public DqNumbering Numbering => Current.Numbering ?? BaseStyle?.Numbering;

        private string DebuggerDisplay
        {
            get
            {
                var s = $"[Style] {ID}";
                if (IsDefault) s += " (default)";
                return s;
            }
        }
    }
}
using System.Collections.Generic;

namespace DQ.Core
{
    public class DqFontError : DqError 
    {
        public DqFontError(string message) : base(message) { }
        public override string AssociatedImage { get; } = "Font";
    }

    public class DqFontSizeError : DqError 
    {
        public DqFontSizeError(string message) : base(message) { }
        public override string AssociatedImage { get; } = "FontSize";
    }

    public class DqAlignmentError : DqError 
    {
        public DqAlignmentError(string message) : base(message) { }
        public override string AssociatedImage { get; } = "Alignment";
    }

    public class DqWarning : DqError 
    {
        public DqWarning(string message) : base(message) { }
        public override string AssociatedImage { get; } = "Warning";
    }

    public class DqError
    {
        public DqError(string message) => Message = message;

        public string Message { get; }
        public virtual string AssociatedImage { get; } = "Wrong";
    }

    public sealed class PageAnalyzer
    {
        public static readonly DqPageMargin ExpectedPageMargin = new DqPageMargin(
            top: 2,
            left: 3,
            bottom: 2,
            right: 1);

        public static readonly DqPageSize ExpectedPageSize = new DqPageSize(
            width: 21.0m,
            height: 29.7m);

        private string MarginToString(DqPageMargin pageMargin) => 
            $"(лево: {pageMargin.Left}, право: {pageMargin.Right}, верх: {pageMargin.Top}, низ: {pageMargin.Bottom})";

        public IReadOnlyCollection<DqError> Analyze(DqDocument document)
        {
            var errors = new List<DqError>();

            for (var i = 0; i < document.Sections.Count; ++i)
            {
                var section = document.Sections[i];

                if (section.PageMargin != ExpectedPageMargin)
                {
                    errors.Add(new DqError($"Неверные поля, секция #{i + 1}. Текущие: {MarginToString(section.PageMargin)}; ожидаемые: {MarginToString(ExpectedPageMargin)}"));
                }

                if (section.PageSize != ExpectedPageSize)
                {
                    errors.Add(new DqError($"Неверный размер страницы, секция #{i + 1}. Ожидается A4."));
                }

                if (i == 0 && section.Footers.First?.HasPageNumber == true)
                {
                    errors.Add(new DqError($"Титульный лист содержит номер страницы."));
                }

                if (i != 0 && (section.Footers.First?.HasPageNumber == false || section.Footers.Even?.HasPageNumber == false || section.Footers.Default?.HasPageNumber == false))
                {
                    errors.Add(new DqError($"Страницы не пронумерованы, секция #{i + 1}"));
                }
            }

            return errors;
        }
    }
}

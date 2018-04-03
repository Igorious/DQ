using System.Collections.Generic;

namespace DQ.Core
{
    public sealed class DqError
    {
        public DqError(string message) => Message = message;

        public string Message { get; }
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

        public IReadOnlyCollection<DqError> Analyze(DqDocument document)
        {
            var errors = new List<DqError>();

            for (var i = 0; i < document.Sections.Count; ++i)
            {
                var section = document.Sections[i];

                if (section.PageMargin != ExpectedPageMargin)
                {
                    errors.Add(new DqError($"Page margin error, Section #{i + 1}"));
                }

                if (section.PageSize != ExpectedPageSize)
                {
                    errors.Add(new DqError($"Page size error, Section #{i + 1}"));
                }

                if (i == 0 && section.Footers.First?.HasPageNumber == true)
                {
                    errors.Add(new DqError($"Title has page number"));
                }

                if (i != 0 && (section.Footers.First?.HasPageNumber == false || section.Footers.Even?.HasPageNumber == false || section.Footers.Default?.HasPageNumber == false))
                {
                    errors.Add(new DqError($"Other pages have no page numbers, Section #{i + 1}"));
                }
            }

            return errors;
        }
    }
}

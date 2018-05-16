using System;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace DQ.Core 
{
    public sealed class BookParser
    {
        public SourceAnalyzer.Book TryParse(string textRaw)
        {
            var text = Regex.Replace(textRaw, @"^\s*\d+\.?\s*", string.Empty);

            var parts = Regex.Split(text, @"//|(?<!н)/|\.\s*(?:--|[-–—])").Select(s => s.Trim()).ToList();

            var pagesRegex = new Regex(@"^(?<Страниц>(?:\w+\.?\s*(?<СтраницаНачало>\d+)\s*(?:--|-|—|–|\.\.)\s*(?<СтраницаКонец>\d+)|(?<ЧислоСтраниц>\d+)\s*\w+)\.?)$");

            var book = new SourceAnalyzer.Book();

            foreach (var part in parts)
            {
                var pagesMatch = pagesRegex.Match(part);
                if (pagesMatch.Success)
                {
                    book.PagesText = pagesMatch.Value;
                    if (pagesMatch.Groups["ЧислоСтраниц"].Success) book.Pages = int.Parse(pagesMatch.Groups["ЧислоСтраниц"].Value);
                    if (pagesMatch.Groups["СтраницаНачало"].Success) book.PageStart = int.Parse(pagesMatch.Groups["СтраницаНачало"].Value);
                    if (pagesMatch.Groups["СтраницаКонец"].Success) book.PageEnd = int.Parse(pagesMatch.Groups["СтраницаКонец"].Value);
                    parts.Remove(part);
                    break;
                }
            }

            var publisherRegex = new Regex(@"^(?<Город>[-/\p{L}\.]+?)\s*:\s*(?<НазваниеИздательства>.+?),?\s*(?<Год>\d{4})(?:[-–—](?<ГодКонец>\d{4}))?$");
            foreach (var part in parts)
            {
                var publisherMatch = publisherRegex.Match(part);
                if (publisherMatch.Success)
                {
                    book.PublisherCity = publisherMatch.Groups["Город"].Value;
                    book.PublisherName = publisherMatch.Groups["НазваниеИздательства"].Value;
                    book.PublisherYear = int.Parse(publisherMatch.Groups["Год"].Value);
                    if (publisherMatch.Groups["ГодКонец"].Success)
                    {
                        book.PublisherYearEnd = int.Parse(publisherMatch.Groups["ГодКонец"].Value);
                    }
                    parts.Remove(part);
                    break;
                }
            }

            var numRegex = new Regex(@"(?:№|No.)\s*(\d+)");
            foreach (var part in parts)
            {
                var numMatch = numRegex.Match(part);
                if (numMatch.Success)
                {
                    book.Number = int.Parse(numMatch.Groups[1].Value);
                    parts.Remove(part);
                    break;
                }
            }

            var volumeRegex = new Regex(@"(\d+) [тТ]\.");
            foreach (var part in parts)
            {
                var volumeMatch = volumeRegex.Match(part);
                if (volumeMatch.Success)
                {
                    book.Volume = int.Parse(volumeMatch.Groups[1].Value);
                    parts.Remove(part);
                    break;
                }
            }

            var editionRegex = new Regex(@"(?<Издание>(\d+)(?:-\w{1,2})?\s+(выд|изд)\.?)");
            foreach (var part in parts)
            {
                var editionMatch = editionRegex.Match(part);
                if (editionMatch.Success)
                {
                    book.Edition = int.Parse(editionMatch.Groups[1].Value);
                    if (book.Lang == null)
                    {
                        book.Lang = editionMatch.Groups[2].Value == "выд"? "by" : "ru";
                    }
                    parts.Remove(part);
                    break;
                }
            }



            var others = R(@"(?<Другие>(?:\[\s*)?(?:и\s+др\.?|і\s+інш\.?)(\s*\])?)");

            var authorsRegex = new Regex(R(@"^(?<Авторы1><Автор>?\s*(?:,?\s*<Автор>\s*)*)[,\.]?(?:<Другие>\s*)?[,\.]?")
                .Replace("<Автор>", Person.Pattern)
                .Replace("<Другие>", others));
            var authors1Match = authorsRegex.Match(parts[0]);
            if (authors1Match.Success)
            {
                parts[0] = parts[0].Substring(startIndex: authors1Match.Length);
                book.Authors1 = Person.ParseMany(authors1Match.Value);
                if (authors1Match.Groups["Другие"].Success)
                {
                    book.HasOtherAuthors = true;
                    if (book.Lang == null)
                    {
                        book.Lang = authors1Match.Groups["Другие"].Value.Contains("інш")? "be" : "ru";
                    }
                }
            }

            if (parts.Count >= 2)
            {
                var authors2Match = authorsRegex.Match(parts[1]);
                if (authors2Match.Success)
                {
                    parts[1] = parts[1].Substring(startIndex: authors2Match.Length);
                    book.Authors2 = Person.ParseMany(authors2Match.Value);
                    if (authors2Match.Groups["Другие"].Success)
                    {
                        book.HasOtherAuthors = true;
                        if (book.Lang == null)
                        {
                            book.Lang = authors2Match.Groups["Другие"].Value.Contains("інш")? "be" : "ru";
                        }
                    }
                }

                var editorRegex = new Regex(R(@"(под(?: общ\.)? ред\.|редкол\.:|рэдкал\.:|пад рэд\.|сост\.)\s*<Автор>\s*(?<РедакторТитул>\(гал\.\s*рэд\.\))?\s*(?:<Другие>)?\s*")
                    .Replace("<Автор>", Person.Pattern)
                    .Replace("<Другие>", others));
                var editorMatch = editorRegex.Match(parts[1]);
                if (editorMatch.Success)
                {
                    parts[1] = parts[1].Replace(editorMatch.Value, string.Empty);
                    book.Editor = Person.Parse(editorMatch.Value);
                    book.EditorText = editorMatch.Groups[1].Value;
                    if (editorMatch.Groups["РедакторТитул"].Success)
                    {
                        book.EditorTitle = editorMatch.Groups["РедакторТитул"].Value;
                    }


                    if (editorMatch.Groups["Другие"].Success)
                    {
                        book.HasOtherEditors = true;
                        if (book.Lang == null)
                        {
                            book.Lang = editorMatch.Groups["Другие"].Value.Contains("інш") || editorMatch.Groups["Другие"].Value.Contains("рэдкал")? "be" : "ru";
                        }
                    }

                    if (book.Lang == null)
                    {
                        book.Lang = editorMatch.Groups[1].Value.Contains("рэд")? "be" : "ru";
                    }
                }
            }

            parts = parts.Where(p => p.Any(char.IsLetterOrDigit)).ToList();

            if (parts.Count == 2)
            {
                book.Collectivity = parts[1].Trim().Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Single().Trim();
            }
            book.Title = parts[0].Trim();

            return book;
        }

        private static string R([RegexPattern] string s) => s;
    }
}
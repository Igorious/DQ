using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace DQ.Core 
{
    public enum SourceType
    {
        None,
        Unknown,
        Book,
        WebSite,
        WebPage,
        WebUnknown,
    }

    public class SourceAnalyzer
    {
        public void Analyze(Node root)
        {
            var sourcesPart = root.Children.SingleOrDefault(p => p.Type == MainPartType.Bibliography);
            if (sourcesPart == null) return;
            foreach (var content in sourcesPart.ContentParagraphs)
            {
                var sources = content.Meta.SourceDeclarations.OfType<DqSource>().ToList();
                if (sources.Count == 0) continue;

                Recognize(sources);
                var source = sources.First();
                if (source.SourceType == SourceType.Unknown)
                {
                    content.Meta.Errors.Add(new DqError("Источник имеет неверный формат"));
                }
                else if (source.SourceType == SourceType.WebUnknown)
                {
                    content.Meta.Errors.Add(new DqError("Web-источник имеет неверный формат"));
                }
                else if (source.Formatted != source.Paragraph.Text)
                {
                    content.Meta.Errors.Add(new DqError($"Ожидаемый формат данных: {source.Formatted}"));
                }
            }
        }

        private static void Recognize(IEnumerable<DqSource> sources)
        {           
            foreach (var source in sources)
            {
                var text = source.Paragraph.Text;

                new BookParser().TryParse(text);

                var electronicResource = ElectronicResource.TryParse(text);
                if (electronicResource != null)
                {
                    source.SourceType = electronicResource.SourceType;
                    source.Formatted = electronicResource.ToString();
                    continue;
                }

                var book = Book.TryParse(text);
                if (book != null)
                {
                    source.SourceType = SourceType.Book;
                    source.Formatted = book.ToString();
                    continue;
                }

                if (ElectronicResource.CanBeElectronicResource(text))
                {
                    source.SourceType = SourceType.WebUnknown;
                    continue;
                }

                source.SourceType = SourceType.Unknown;
            }
        }

        public class ElectronicResource
        {
            public static bool CanBeElectronicResource(string text)
            {
                var keyWords = new[] {"http:/", "https:/", "www.", ".asp", ".ru", ".by", ".com", ".org", ".html"};
                if (keyWords.Any(text.Contains)) return true;
                var keyRegex = new[] {R(@"Электронный\s+ресурс"), R(@"Electronic\s+resource"), R(@"Режим\s+доступа"), R(@"Mode\s+of\s+access")};
                if (keyRegex.Any(r => Regex.IsMatch(text, r))) return true;
                return false;
            }

            static ElectronicResource()
            {
                var title = R(@"(?<Название>[^/\\]+)");
                var separator1 = R(@"\s*(?:\\|\\\s?\\|/|/\s?/)\s*");
                var separator2 = R(@"\s*[\.,]?\s*(?:--|-|—|–)\s*");
                var siteName = R(@"(?:(?<Сайт>[^\[]+)(?<ЭлектронныйРесурс>\[\s*(Электронный\s+ресурс|Electronic\s+resource)\s*\]))");
                var year = R(@"(?<Год>\d{4})");
                var no = R(@"(?:(?:#|№|No\.?)\s*(?<Номер>\d+))");
                var modeOfAccess = R(@"(?<РежимДоступа>(?:Режим\s+доступа|Mode\s+of\s+access)\s*:\s*(?<Ссылка>\S+[\w\d_~:/?#\[\]@!$&'()*+,;=%]))");
                var dateOfAccess = R(@"(?<ДатаДоступа>(?:Дата\s+доступа|Date\s+of\s+access)\s*:\s*(?<Дата>\d{2}\.\d{2}\.\d{4}))");
                var publisher = R(@"(?<Публикатор>.+)");
                var publised = R(@"(?:(?<Город>[^:]+)\s*:\s*)?(?<Год>\d{4})");

                var webPage = B(@"(?<Авторы1><Автор>\s*(?:,?\s*<Автор>\s*)*)?[,\.]?<Название><SEP1>(?<Авторы2><Автор>+\s*(?:,?\s*<Автор>\s*)*<SEP1>)?<Сайт><SEP2>(?:<Год><SEP2>)?(?:<Номер><SEP2>)?<РежимДоступа><SEP2><ДатаДоступа>");
                WebPageRegex = new Regex(webPage
                    .Replace("<Автор>", Person.Pattern)
                    .Replace("<Название>", title)
                    .Replace("<SEP1>", separator1)
                    .Replace("<SEP2>", separator2)
                    .Replace("<Сайт>", siteName)
                    .Replace("<Год>", year)
                    .Replace("<Номер>", no)
                    .Replace("<РежимДоступа>", modeOfAccess)
                    .Replace("<ДатаДоступа>", dateOfAccess)
                    .ToString(), RegexOptions.Compiled);

                var webSite = B(@"<Сайт>(?:<SEP1><Публикатор>)?<SEP2>(?:<Издан><SEP2>)?<РежимДоступа><SEP2><ДатаДоступа>");
                WebSiteRegex = new Regex(webSite
                    .Replace("<Сайт>", siteName)
                    .Replace("<SEP1>", separator1)
                    .Replace("<Публикатор>", publisher)
                    .Replace("<SEP2>", separator2)
                    .Replace("<Издан>", publised)
                    .Replace("<РежимДоступа>", modeOfAccess)
                    .Replace("<ДатаДоступа>", dateOfAccess)
                    .ToString(), RegexOptions.Compiled);
            }

            public static ElectronicResource TryParse(string text)
            {
                return TryParseWebPage(text) ?? TryParseWebSite(text);
            }

            public static ElectronicResource TryParseWebSite(string text)
            {
                var match = WebSiteRegex.Match(text);
                if (!match.Success) return null;

                var electronicResource = new ElectronicResource {SourceType = SourceType.WebSite};

                electronicResource.Site = match.Groups["Сайт"].Value.TrimPunctuation();
                electronicResource.ElectronicSourceText = match.Groups["ЭлектронныйРесурс"].Value.Trim();

                electronicResource.PublisherName = match.Groups["Публикатор"].Value.TrimPunctuation();

                var sityGroup = match.Groups["Город"];
                if (sityGroup.Success)
                {
                    electronicResource.PublisherCity = sityGroup.Value.TrimPunctuation();
                }

                var yearGroup = match.Groups["Год"];
                if (yearGroup.Success)
                {
                    electronicResource.PublisherYear = int.Parse(yearGroup.Value);
                }

                electronicResource.ModeOfAccess = match.Groups["РежимДоступа"].Value.TrimPunctuation();
                electronicResource.Hyperlink = match.Groups["Ссылка"].Value.TrimPunctuation();

                electronicResource.DateOfAccess = match.Groups["ДатаДоступа"].Value.TrimPunctuation();
                electronicResource.Date = DateTime.ParseExact(match.Groups["Дата"].Value, "dd.MM.yyyy", null);

                return electronicResource;
            }

            public static ElectronicResource TryParseWebPage(string text)
            {
                var match = WebPageRegex.Match(text);
                if (!match.Success) return null;

                var electronicResource = new ElectronicResource {SourceType = SourceType.WebPage};

                electronicResource.Authors1 = Person.ParseMany(match.Groups["Авторы1"].Value);

                electronicResource.Title = match.Groups["Название"].Value.Trim();

                electronicResource.Authors2 = Person.ParseMany(match.Groups["Авторы2"].Value);

                var yearGroup = match.Groups["Год"];
                if (yearGroup.Success)
                {
                    electronicResource.PublisherYear = int.Parse(yearGroup.Value);
                }

                var noGroup = match.Groups["Номер"];
                if (noGroup.Success)
                {
                    electronicResource.No = int.Parse(noGroup.Value);
                }

                electronicResource.Site = match.Groups["Сайт"].Value.TrimPunctuation();
                electronicResource.ElectronicSourceText = match.Groups["ЭлектронныйРесурс"].Value.Trim();

                electronicResource.ModeOfAccess = match.Groups["РежимДоступа"].Value.TrimPunctuation();
                electronicResource.Hyperlink = match.Groups["Ссылка"].Value.TrimPunctuation();

                electronicResource.DateOfAccess = match.Groups["ДатаДоступа"].Value.TrimPunctuation();
                electronicResource.Date = DateTime.ParseExact(match.Groups["Дата"].Value, "dd.MM.yyyy", null);

                return electronicResource;
            }

            public override string ToString()
            {
                if (SourceType == SourceType.WebSite)
                {
                    var result = new StringBuilder();
                    result.Append(Site).Append(" ").Append(ElectronicSourceText);
                    if (PublisherCity != null || PublisherYear != null)
                    {
                        result.Append(". – ");
                        if (PublisherCity != null)
                        {
                            result.Append(PublisherCity);
                            if (PublisherYear != null) result.Append(", ");
                        }
                        if (PublisherYear != null) result.Append(PublisherYear);
                    }
                    result.Append(". – ").Append(ModeOfAccess);
                    result.Append(". – ").Append(DateOfAccess);
                    result.Append(".");
                    return result.ToString();
                }
                else
                {
                    var result = new StringBuilder();
                    if (Authors1.Any()) result.Append(Authors1.First().ToString("r")).Append(" ");
                    result.Append(Title);
                    if (Authors2.Any()) result.Append(" / ").Append(string.Join(", ", Authors2));
                    result.Append(" // ").Append(Site).Append(" ").Append(ElectronicSourceText);
                    if (PublisherYear != null) result.Append(". – ").Append(PublisherYear);
                    if (No != null) result.Append(". – ").Append(No);
                    result.Append(". – ").Append(ModeOfAccess);
                    result.Append(". – ").Append(DateOfAccess);
                    result.Append(".");
                    return result.ToString();
                }
            }

            public static Regex WebPageRegex { get; private set; }
            public static Regex WebSiteRegex { get; private set; }

            public IReadOnlyList<Person> Authors1 { get; private set; }
            public string Title { get; private set; }
            public IReadOnlyList<Person> Authors2 { get; private set; }
            public string ElectronicSourceText { get; private set; }
            public int? PublisherYear { get; private set; }
            public string PublisherCity { get; set; }
            public string PublisherName { get; set; }
            public int? No { get; private set; }
            public string Site { get; private set; }
            public string ModeOfAccess { get; private set; }
            public string Hyperlink { get; private set; }
            public string DateOfAccess { get; private set; }
            public DateTime Date { get; private set; }

            public SourceType SourceType { get; private set; }
        }

        public static StringBuilder TrimStart(StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return sb;

            var i = 0;
            for (; i < sb.Length; ++i)
            {
                if (!char.IsWhiteSpace(sb[i])) break;
            }

            if (i > 0)
            {
                sb.Remove(0, i);
            }

            return sb;
        }

        public static StringBuilder TrimEnd(StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return sb;

            int i = sb.Length - 1;
            for (; i >= 0; i--)
                if (!char.IsWhiteSpace(sb[i]))
                    break;

            if (i < sb.Length - 1)
                sb.Length = i + 1;

            return sb;
        }



        public class Book
        {
            protected bool Equals(Book other) =>
                (Authors1?.SequenceEqual(other.Authors1) ?? true)
                && string.Equals(Title, other.Title)
                && (Authors2?.SequenceEqual(other.Authors2) ?? true)
                && HasOtherAuthors == other.HasOtherAuthors
                && Collectivity == other.Collectivity
                && Equals(Editor, other.Editor)
                && HasOtherEditors == other.HasOtherEditors 
                && string.Equals(EditorText, other.EditorText)
                && Edition == other.Edition 
                && string.Equals(PublisherCity, other.PublisherCity)                 
                && string.Equals(PublisherName, other.PublisherName) 
                && PublisherYear == other.PublisherYear
                && Pages == other.Pages 
                && PageStart == other.PageStart 
                && PageEnd == other.PageEnd 
                && Number == other.Number
                && Volume == other.Volume
                && PublisherYearEnd == other.PublisherYearEnd;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Book)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Authors1 != null? Authors1.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Title != null? Title.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Authors2 != null? Authors2.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ HasOtherAuthors.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Editor != null? Editor.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ HasOtherEditors.GetHashCode();
                    hashCode = (hashCode * 397) ^ Edition.GetHashCode();
                    hashCode = (hashCode * 397) ^ (PublisherCity != null? PublisherCity.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (PublisherName != null? PublisherName.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ PublisherYear.GetHashCode();
                    hashCode = (hashCode * 397) ^ Pages.GetHashCode();
                    hashCode = (hashCode * 397) ^ PageStart.GetHashCode();
                    hashCode = (hashCode * 397) ^ PageEnd.GetHashCode();
                    hashCode = (hashCode * 397) ^ Number.GetHashCode();
                    return hashCode;
                }
            }

            static Book()
            {
                var title = R(@"(?<Название>[^/\\]+)");
                var others = R(@"(?<Другие>(?:\[\s*)?(?:и\s+др\.?|і\s+інш\.?)(\s*\])?)");
                OthersRegex = new Regex(others, RegexOptions.Compiled);
                var digest = R(@"(?<Сборник>[^/\\]+)");
                var edition = R(@"(?<Издание>\d+(?:-\w{1,2})?\s+\w+\.?)");
                var publisher = R(@"(?:(?<Город>[^:]+):(?<НазваниеИздательства>[^\d]+),?\s*(?<Год>\d{4}))");
                var pages = R(@"(?<Страниц>(?:\w+\.?\s*(?<СтраницаНачало>\d+)\s*(?:--|-|—|–|\.\.)\s*(?<СтраницаКонец>\d+)|(?<ЧислоСтраниц>\d+)\s*\w+)\.?)");
                var separator1 = R(@"\s*(?:\\|\\\s?\\|/|/\s?/)\s*");
                var separator2 = R(@"\s*[\.,]?\s*(?:--|-|—|–)\s*");

                var editor = R(@"(?:;\s*(?<Редактор>\P{Lu}+<Автор>))");
                var editorRegex = editor
                    .Replace("<Автор>", Person.Pattern);

                var book = B(@"(?<Авторы1><Автор>?\s*(?:,?\s*<Автор>\s*)*)[,\.]?\s*<Название><SEP1>(?<Авторы2><Автор>+\s*(?:,?\s*<Автор>\s*)*(?:<Другие>\s*)?)(?:<Редактор>(?:\s*<Другие>\s*)?)?(<SEP1><Сборник>)?<SEP2>(?:<Издание><SEP2>)?<Издательство><SEP2><Страниц>");
                Regex = new Regex(book
                    .Replace("<Автор>", Person.Pattern)
                    .Replace("<Название>", title)
                    .Replace("<SEP1>", separator1)
                    .Replace("<Другие>", others)
                    .Replace("<Редактор>", editorRegex)
                    .Replace("<SEP2>", separator2)
                    .Replace("<Сборник>", digest)
                    .Replace("<Издание>", edition)
                    .Replace("<Издательство>", publisher)
                    .Replace("<Страниц>", pages)
                    .ToString(), RegexOptions.Compiled);
            }

            public IReadOnlyList<Person> Authors1 { get; set; } = new List<Person>();
            public string Title { get; set; }
            public IReadOnlyList<Person> Authors2 { get; set; } = new List<Person>();
            public bool HasOtherAuthors { get; set; }
            public string Collectivity {get; set; }
            public Person Editor { get; set; }
            public string EditorText { get; set; }
            public string EditorTitle { get; set; }
            public bool HasOtherEditors { get; set; }
            public int? Edition { get; set; }
            public string PublisherCity { get; set; }
            public string PublisherName { get; set; }
            public int? PublisherYear { get; set; }
            public int? PublisherYearEnd { get; set; }
            public int? Pages { get; set; }
            public int? PageStart { get; set; }
            public int? PageEnd { get; set; }
            public int? Volume { get; set; }
            public string PagesText { get; set; }
            public int? Number { get; set; }
            public string Lang { get; set; }

            public static Book TryParse(string text)
            {
                var match = Regex.Match(text);
                if (!match.Success) return null;

                var book = new Book();

                book.Authors1 = Person.ParseMany(match.Groups["Авторы1"].Value);

                book.Title = match.Groups["Название"].Value.Trim();

                book.Authors2 = Person.ParseMany(match.Groups["Авторы2"].Value);
                book.HasOtherAuthors = OthersRegex.IsMatch(match.Groups["Авторы2"].Value);

                var editorGroup = match.Groups["Редактор"];
                if (editorGroup.Success)
                {
                    book.Editor = Person.Parse(editorGroup.Value);
                    book.EditorText = editorGroup.Value.TrimPunctuation();
                    book.HasOtherEditors = OthersRegex.IsMatch(editorGroup.Value);
                }

                //book.Edition = match.Groups["Издание"].Value.TrimPunctuation();

                book.PublisherCity = match.Groups["Город"].Value.TrimPunctuation();
                book.PublisherName = match.Groups["НазваниеИздательства"].Value.TrimPunctuation();
                book.PublisherYear = int.Parse(match.Groups["Год"].Value);

                book.PagesText = match.Groups["Страниц"].Value.TrimPunctuation();
                if (match.Groups["ЧислоСтраниц"].Success) book.Pages = int.Parse(match.Groups["ЧислоСтраниц"].Value);
                if (match.Groups["СтраницаНачало"].Success) book.PageStart = int.Parse(match.Groups["СтраницаНачало"].Value);
                if (match.Groups["СтраницаКонец"].Success) book.PageEnd = int.Parse(match.Groups["СтраницаКонец"].Value);

                return book;
            }

            public override string ToString()
            {
                var result = new StringBuilder();
                if (Authors1.Any()) result.Append(Authors1.First().ToString("r")).Append(" ");
                result.Append(Title).Append(" / ");
                result.Append(string.Join(", ", Authors2));
                if (HasOtherAuthors) result.Append(Lang == "by" ? " [і інш.]" : " [и др.]"); // TODO.
                if (Collectivity != null)
                {
                    result.Append(Collectivity);
                }

                if (Editor != null)
                {
                    if (Collectivity != null || Authors2.Any())
                    {
                        result.Append(" ; ");
                    }

                    result.Append(EditorText).Append(" ").Append(Editor);
                    if (EditorTitle != null)
                    {
                        result.Append(" (").Append(EditorTitle).Append(")");
                    }
                    if (HasOtherEditors)
                    {
                        result.Append(Lang == "by"? " [і інш.]" : " [и др.]");
                    }
                }
                if (Edition != null)
                {
                    result.Append(". – ").Append(Edition).Append("-е ").Append(Lang == "by"? "выд." : "изд.");
                }
                result.Append(". – ").Append(PublisherCity).Append(" : ").Append(PublisherName).Append(", ").Append(PublisherYear);
                if (PublisherYearEnd != null)
                {
                    result.Append("–").Append(PublisherYearEnd);
                }

                if (Pages != null)
                {
                    result.Append(". – ").Append(Pages).Append(" с.");
                }
                return result.Replace("..", ".").ToString();
            }

            public static Regex Regex { get; private set; }
            public static Regex OthersRegex { get; private set; }
        }

        private static string R([RegexPattern] string s) => s;

        private static StringBuilder B([RegexPattern] string s) => new StringBuilder(s);
    }
}
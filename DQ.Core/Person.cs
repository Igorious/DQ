using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace DQ.Core 
{
    public class Person
    {
        static Person()
        {
            var surname = R(@"(?<Фамилия>\p{Lu}[\w-]+)");
            var name = R(@"(?<Имя>\p{Lu}[\w]?\.)");
            var patronymic = R(@"(?<Отчество>\p{Lu}[\w]?\.)");

            var author = B(@"(<Фамилия>,?\s*<Имя>\s*<Отчество>?|<Имя>\s*<Отчество>?\s*<Фамилия>)");
            Pattern = author
                .Replace("<Фамилия>", surname)
                .Replace("<Имя>", name)
                .Replace("<Отчество>", patronymic)
                .ToString();

            Regex = new Regex(Pattern, RegexOptions.Compiled);
        }

        public string Surname { get; private set; }
        public string Name { get; private set; }
        public string Patronymic { get; private set; }

        public Person([NotNull] string surname, [NotNull] string name, [CanBeNull] string patronymic = null)
        {
            Surname = surname;
            Name = name;
            Patronymic = patronymic;
        }

        public static Person Parse(Match match)
        {
            return new Person(match.Groups["Фамилия"].Value.Trim(),
                match.Groups["Имя"].Value.Trim(),
                match.Groups["Отчество"].Value.Trim());
        }

        public static IReadOnlyList<Person> ParseMany(string text)
        {
            return Regex.Matches(text).Cast<Match>().Select(Parse).ToList();
        }

        public static Person Parse(string text)
        {
            return Parse(Regex.Match(text));
        }

        public static string Pattern;
        public static Regex Regex;

        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append(Name).Append(Patronymic).Append(" ").Append(Surname);
            return result.ToString();
        }

        public string ToString(string format)
        {
            if (format != "r") return ToString();
            var result = new StringBuilder();
            result.Append(Surname).Append(", ").Append(Name).Append(Patronymic);
            return result.ToString();
        }

        private static string R([RegexPattern] string s) => s;

        private static StringBuilder B([RegexPattern] string s) => new StringBuilder(s);

        protected bool Equals(Person other)
        {
            return string.Equals(Surname, other.Surname) && string.Equals(Name, other.Name) && string.Equals(Patronymic, other.Patronymic);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Person)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Surname != null? Surname.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Patronymic != null? Patronymic.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
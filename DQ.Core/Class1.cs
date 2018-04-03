using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DQ.Core
{
    public sealed class DqNumberingLevel
    {
        public DqNumberingLevel(string text) => Text = text;

        public string Text { get; }
    }

    public sealed class DqNumbering
    {
        public int Id { get; private set; } = -1;
        public List<DqNumberingLevel> Levels { get; } = new List<DqNumberingLevel>();

        public DqNumbering Clone(int id)
        {
            var copy = (DqNumbering) MemberwiseClone();
            copy.Id = id;
            return copy;
        }
    }

    class Token
    {
        public Token(DqParagraph paragraph, int index, int level)
        {
            Paragraph = paragraph;
            Index = index;
            Level = level;
        }

        public DqParagraph Paragraph { get; }
        public int Index { get; }
        public int Level { get; }
    }

    [DebuggerDisplay("[Node] {HeaderParagraph.Text,nq}")]
    public class Node
    {
        public Node(DqParagraph paragraph) => HeaderParagraph = paragraph;

        public DqParagraph HeaderParagraph { get; }
        public List<DqParagraph> ContentParagraphs { get; } = new List<DqParagraph>();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public List<Node> Children { get; } = new List<Node>();
    }

    public sealed class ChapterParser
    {
        public Node Find(DqDocument document)
        {
            var numbered = document.Paragraphs.Select((p, i) => (p, i)).Where(x => IsNumberedParagraph(x.p) || IsMainPart(x.p)).ToList();

            var list = new List<Token>();

            var partNames = new List<Token>();

            foreach (var n in numbered)
            {
                if (!list.Any())
                {
                    list.Add(new Token(n.p, n.i, GetLevel(n.p)));
                    continue;
                }

                if (n.i - list.Last().Index == 1)
                {
                    list.Add(new Token(n.p, n.i, GetLevel(n.p)));
                }
                else
                {
                    var isGreater = true;
                    for (var i = 1; i < list.Count; ++i)
                    {
                        if (list[i - 1].Level >= list[i].Level)
                        {
                            isGreater = false;
                        }
                    }

                    if (isGreater)
                    {
                        partNames.AddRange(list);
                    }
                    list.Clear();
                    list.Add(new Token(n.p, n.i, GetLevel(n.p)));
                }
            }

            var root = new Node(new DqParagraph("{root}", document.Styles.First(s => s.IsDefault)));
            var lastLevel = 0;

            var stack = new Stack<Node>();
            stack.Push(root);

            partNames.ForEach(pn => pn.Paragraph.Meta.IsHeader = true);

            var lastIndex = 0;
            foreach (var partName in partNames)
            {
                var targetNode = stack.Peek().Children.LastOrDefault() ?? stack.Peek();
                for (int i = lastIndex; i < partName.Paragraph.Index; i++)
                {
                    targetNode.ContentParagraphs.Add(document.Paragraphs[i]);
                }
                lastIndex = partName.Paragraph.Index;

                if (partName.Level == lastLevel)
                {
                    stack.Peek().Children.Add(new Node(partName.Paragraph));
                }
                else if (partName.Level > lastLevel)
                {
                    var last = stack.Peek().Children.Last();
                    stack.Push(last);
                    stack.Peek().Children.Add(new Node(partName.Paragraph));
                    lastLevel = partName.Level;
                }
                else
                {
                    var currentLevel = partName.Level;
                    while (currentLevel < lastLevel)
                    {
                        stack.Pop();
                        lastLevel--;
                    }
                    stack.Peek().Children.Add(new Node(partName.Paragraph));
                }
            }

            return root;
        }

        private int GetLevel(DqParagraph paragraph)
        {
            if (paragraph.Style.OutlineLevel < 9)
            {
                return paragraph.Style.OutlineLevel;
            }

            var isTop = Regex.IsMatch(paragraph.Text, @"^\s*(Глава|Раздел|Часть)\s*\d+\s*[\D\S]", RegexOptions.IgnoreCase);
            if (isTop) return 1;

            var numberPart = Regex.Match(paragraph.Text, @"^\s*\d+(\.\d+)*\s*[\D\S]", RegexOptions.IgnoreCase).Value;
            var m = Regex.Matches(numberPart, @"\b(\d+)\b", RegexOptions.IgnoreCase);
            return m.Count;
        }

        private bool IsNumberedParagraph(DqParagraph paragraph) =>
            paragraph.Style.Numbering != null
            || Regex.IsMatch(paragraph.Text, @"^\s*(Глава|Раздел|Часть|Падзел)?\s*\d+(\.\d+)*\s*[\D\S]", RegexOptions.IgnoreCase);

        private bool IsMainPart(DqParagraph paragraph)
        {
            var text = paragraph.Text.Trim();
            return new string[]
            {
                "реферат", "рэферат", "abstract",
                "змест", "содержание",
                "введение", "уводзіны",
                "вынікі", "заключение",
                "спіс выкарыстаных крыніц", "список использованых источников"
            }.Any(c => c.Equals(text, StringComparison.CurrentCultureIgnoreCase));
        }
    }


    public class Class1
    {
        public (Node, DqDocument) Go()
        {
            //var path = @"C:\Users\Игорь\Documents\Жураховский И. В. - Визуализация кинетического метода Монте-Карло.docx";
            var path = @"C:\Users\Игорь\Documents\Уводзіны.work.docx";
            var dqDocument = new DocxParser().Parse(path);
            var result = new PageAnalyzer().Analyze(dqDocument);
            var root = new ChapterParser().Find(dqDocument);
            new DqReferenceParser().ParseReferences(dqDocument);
            new ParagraphAnalyzer().Analyze(dqDocument);
            return (root, dqDocument);
        }
    }
}

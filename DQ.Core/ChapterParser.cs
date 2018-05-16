using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DQ.Core 
{
    internal sealed class HeaderFinder
    {      
        public IList<Token> GetHeaders(DqDocument document)
        {
            var numberedParagraphs = document.Paragraphs.Select((p, i) => (p, i)).Where(x => IsNumberedParagraph(x.p) || IsMainPart(x.p) || IsAnnex(x.p)).ToList();

            var list = new List<Token>();
            var partNames = new List<Token>();
            foreach (var numberedParagraph in numberedParagraphs)
            {
                if (string.Equals(numberedParagraph.p.Text, "приложение б", StringComparison.OrdinalIgnoreCase))
                {
                    ;
                }

                if (!list.Any())
                {
                    list.Add(new Token(numberedParagraph.p, numberedParagraph.i, GetLevel(numberedParagraph.p)));
                    continue;
                }

                if (numberedParagraph.i - list.Last().Index == 1 && !IsChapter(numberedParagraph.p) && !IsMainPart(numberedParagraph.p))
                {
                    list.Add(new Token(numberedParagraph.p, numberedParagraph.i, GetLevel(numberedParagraph.p)));
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
                    list.Add(new Token(numberedParagraph.p, numberedParagraph.i, GetLevel(numberedParagraph.p)));
                }
            }

            partNames.AddRange(list);
            foreach (var partName in partNames)
            {
                partName.Paragraph.Meta.IsHeader = true;
            }

            return partNames;
        }

        private bool IsNumberedParagraph(DqParagraph paragraph) =>
            IsChapter(paragraph)
            || Regex.IsMatch(paragraph.Text, @"^\d+(\.\d+)*\s*[\D\S]", RegexOptions.IgnoreCase);

        private bool IsMainPart(DqParagraph paragraph)
        {
            var text = paragraph.Text.Trim();
            return MainParts.TypeByText.Keys
                .Any(c => c.Equals(text.Replace("{PageBreak}", ""), StringComparison.CurrentCultureIgnoreCase));
        }

        private int GetLevel(DqParagraph paragraph)
        {
            if (paragraph.Style.OutlineLevel < 8 && paragraph.Style.OutlineLevel > 0)
            {
                return paragraph.Style.OutlineLevel;
            }

            if (IsChapter(paragraph)) return 0;
            if (IsAnnex(paragraph))
            {
                return 1;
            }

            var numberPart = Regex.Match(paragraph.Text, @"^\s*\d+(\.\d+)*\s*[\D\S]", RegexOptions.IgnoreCase).Value;
            var m = Regex.Matches(numberPart, @"\b(\d+)\b", RegexOptions.IgnoreCase);
            return m.Count == 0
                   ? 0
                   : m.Count - 1;
        }

        private bool IsChapter(DqParagraph paragraph) => 
            Regex.IsMatch(paragraph.Text, @"^\s*(Глава|Раздел|Часть|Падзел)\s*\d+(\s*[\D\S])?", RegexOptions.IgnoreCase);

        private bool IsAnnex(DqParagraph paragraph) =>
            Regex.IsMatch(paragraph.Text, @"^\s*Приложение\s+[абв]\s*$", RegexOptions.IgnoreCase);
    }

    internal sealed class HeaderHierarchyService
    {
        public Node GetHierarchy(IEnumerable<Token> headers, DqDocument document)
        {
            var root = new Node(new DqParagraph("{root}", document.Styles.First(s => s.IsDefault)), null, -1);
            var lastLevel = 0;

            var stack = new Stack<Node>();
            stack.Push(root);

            var lastIndex = 0;
            foreach (var header in headers)
            {
                var kv = MainParts.TypeByText.FirstOrDefault(k => string.Equals(k.Key, header.Paragraph.Text, StringComparison.OrdinalIgnoreCase));
                var type = kv.Key != null? kv.Value : (MainPartType?) null;

                var targetNode = stack.Peek().Children.LastOrDefault() ?? stack.Peek();
                for (var i = lastIndex; i < header.Paragraph.Index; i++)
                {
                    targetNode.ContentParagraphs.Add(document.Paragraphs[i + 1]);
                }

                lastIndex = header.Paragraph.Index;

                if (header.Level == lastLevel)
                {
                    stack.Peek().Children.Add(new Node(header.Paragraph, type, header.Level));
                }
                else if (header.Level > lastLevel)
                {
                    var last = stack.Peek().Children.Last();
                    stack.Push(last);
                    stack.Peek().Children.Add(new Node(header.Paragraph, type, header.Level));
                    lastLevel = header.Level;
                }
                else
                {
                    var currentLevel = header.Level;
                    while (currentLevel < lastLevel - 1)
                    {
                        stack.Pop();
                        lastLevel--;
                    }

                    stack.Peek().Children.Add(new Node(header.Paragraph, type, header.Level));
                }
            }

            {
                var targetNode = stack.Peek().Children.LastOrDefault() ?? stack.Peek();
                for (var i = lastIndex; i < document.Paragraphs.Count - 1; i++)
                {
                    targetNode.ContentParagraphs.Add(document.Paragraphs[i + 1]);
                }
            }

            TryRemoveSourcesFromHierarhy(root);

            return root;
        }

        private static void TryRemoveSourcesFromHierarhy(Node root)
        {
            var sourcesNode = root.Children.LastOrDefault(c => c.Type == MainPartType.Sources);
            if (sourcesNode == null) return;
            
            var sourceIndexStart = root.Children.IndexOf(sourcesNode) + 1;
            var partAfterSources = root.Children.Skip(sourceIndexStart).FirstOrDefault(c => c.Type != null);

            var sourceIndexEnd = partAfterSources != null
                ? root.Children.IndexOf(partAfterSources)
                : root.Children.Count;
            
            var sources = root.Children.GetRange(sourceIndexStart, sourceIndexEnd - sourceIndexStart);
            foreach (var sourceNode in sources)
            {
                sourceNode.HeaderParagraph.Meta.IsHeader = false;
                sourcesNode.ContentParagraphs.Add(sourceNode.HeaderParagraph);
            }

            root.Children.RemoveRange(sourceIndexStart, sourceIndexEnd - sourceIndexStart);
        }
    }

    public sealed class ChapterParser
    {
        public Node Find(DqDocument document)
        {
            new NumberingService().RestoreNumbering(document);
            var headers = new HeaderFinder().GetHeaders(document);
            return new HeaderHierarchyService().GetHierarchy(headers, document);         
        }

    }
}
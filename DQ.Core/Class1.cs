using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DQ.Core
{
    public sealed class DqNumberingLevel
    {
        public DqNumberingLevel(string text) => Text = text;

        public string Text { get; }

        public decimal? Indent { get; set; }
    }

    public sealed class DqNumbering
    {
        public int Id { get; set; } = -1;
        public List<DqNumberingLevel> Levels { get; } = new List<DqNumberingLevel>();

        public DqNumbering Clone(int id)
        {
            var copy = (DqNumbering) MemberwiseClone();
            copy.Id = id;
            return copy;
        }
    }

    [DebuggerDisplay("{Paragraph.Text,nq}")]
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

    [DebuggerDisplay("[Node] {Type} {HeaderParagraph.Text,nq}")]
    public class Node
    {
        public Node(DqParagraph paragraph, MainPartType? type, int level)
        {
            HeaderParagraph = paragraph;
            Type = type;
            Level = level;
            paragraph.Meta.Node = this;
        }

        public DqParagraph HeaderParagraph { get; }
        public MainPartType? Type { get; }
        public int Level { get; }
        public List<DqParagraph> ContentParagraphs { get; } = new List<DqParagraph>();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public List<Node> Children { get; } = new List<Node>();
    }

    public class MainParts
    {
        public static IReadOnlyDictionary<string, MainPartType> TypeByText = new Dictionary<string, MainPartType>
        {
            { "реферат", MainPartType.Abstract },
            { "рэферат", MainPartType.Abstract },
            { "abstract", MainPartType.Abstract },

            { "змест", MainPartType.Toc },
            { "содержание", MainPartType.Toc },

            { "введение", MainPartType.Intro },
            { "уводзіны", MainPartType.Intro },

            { "вынікі", MainPartType.Outro },
            { "заключение", MainPartType.Outro },

            { "спіс выкарыстаных крыніц", MainPartType.Sources },
            { "список использованных источников", MainPartType.Sources },
            { "список использованной литературы", MainPartType.Sources },

            { "приложения", MainPartType.Annex },
           // { "приложение a", MainPartType.Annex },
        };
    }

    public enum MainPartType
    {
        Abstract,
        Toc,
        Intro,
        Chapter,
        Outro,
        Sources,     	
        Annex,
    }

    public class Class1
    {
        public (Node, DqDocument) Go(string path)
        {
            var dqDocument = new DocxParser().Parse(path);
            var result = new PageAnalyzer().Analyze(dqDocument);
            dqDocument.Paragraphs.Insert(0, new DqParagraph("<Информация о полях документа>", dqDocument.Styles.First(s => s.IsDefault)) { Index = -1 });
            dqDocument.Paragraphs[0].Meta.Errors.AddRange(result);
            var root = new ChapterParser().Find(dqDocument);
            new DqReferenceParser().ParseReferences(dqDocument, root);
            new ParagraphAnalyzer().Analyze(dqDocument, root);
            //new SourceAnalyzer().Analyze(root);
            return (root, dqDocument);
        }
    }
}

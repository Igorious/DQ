using System.Linq;

namespace DQ.Core
{
    public sealed class DqReferenceParser
    {
        public void ParseReferences(DqDocument document, Node root)
        {
            var figureReferenceParser = new DqFigureReferenceParser();
            var tableReferenceParser = new DqTableReferenceParser();
            var sourceReferenceParser = new DqSourceReferenceParser();

            foreach (var paragraph in document.Paragraphs)
            {
                figureReferenceParser.Parse(paragraph);
                tableReferenceParser.Parse(paragraph);
                sourceReferenceParser.Parse(paragraph, root);
            }

            var structure = document.Paragraphs.SelectMany(p => p.Meta.Structure).OfType<DqNumberedElement>().ToList();

            var allFigureDeclarations = structure.Where(s => s.Type == DqStructureElementType.FigureDeclaration).ToList();
            var allFigureReferences = structure.Where(s => s.Type == DqStructureElementType.FigureReference).ToList();

            foreach (var figureReference in allFigureReferences)
            {
                figureReference.IsMissing = allFigureDeclarations.All(fd => fd.Number != figureReference.Number);
            }

            for (var i = 1; i < allFigureDeclarations.Count; i++)
            {
                var n1 = DqNumber.TryParse(allFigureDeclarations[i - 1].Number);
                var n2 = DqNumber.TryParse(allFigureDeclarations[i].Number);
                if (n1 != null && n2 != null && n1.CompareTo(n2) >= 0)
                {
                    allFigureDeclarations[i].Paragraph.Meta.Errors.Add(
                        new DqError($"Неправильный порядок нумерации ({allFigureDeclarations[i].Number} после {allFigureDeclarations[i - 1].Number})"));
                }
            }

            foreach (var figureDeclaration in allFigureDeclarations)
            {
                var firstReference = allFigureReferences.FirstOrDefault(r => r.Number == figureDeclaration.Number);
                if (firstReference == null)
                {
                    figureDeclaration.IsMissing = true;
                }
                else if (firstReference.Paragraph.Index >= figureDeclaration.Paragraph.Index)
                {
                    firstReference.Paragraph.Meta.Errors.Add(new DqError("Первая ссылка должна быть до рисунка."));
                }
            }

            var allTableDeclarations = structure.Where(s => s.Type == DqStructureElementType.TableDeclaration).ToList();
            var allTableReferences = structure.Where(s => s.Type == DqStructureElementType.TableReference).ToList();

            foreach (var tableReference in allTableReferences)
            {
                tableReference.IsMissing = allTableDeclarations.All(fd => fd.Number != tableReference.Number);
            }

            for (var i = 1; i < allTableDeclarations.Count; i++)
            {
                var n1 = DqNumber.TryParse(allTableDeclarations[i - 1].Number);
                var n2 = DqNumber.TryParse(allTableDeclarations[i].Number);
                if (n1 != null && n2 != null && n1.CompareTo(n2) >= 0)
                {
                    allTableDeclarations[i].Paragraph.Meta.Errors.Add(
                        new DqError($"Неправильный порядок нумерации ({allTableDeclarations[i].Number} после {allTableDeclarations[i - 1].Number})"));
                }
            }

            foreach (var tableDeclaration in allTableDeclarations)
            {
                var firstReference = allTableReferences.FirstOrDefault(r => r.Number == tableDeclaration.Number);
                if (firstReference == null)
                {
                    tableDeclaration.IsMissing = true;
                }
                else if (firstReference.Paragraph.Index >= tableDeclaration.Paragraph.Index)
                {
                    firstReference.Paragraph.Meta.Errors.Add(new DqError("Первая ссылка должна быть до таблицы."));
                }
            }

            var allSourceDeclarations = structure.Where(s => s.Type == DqStructureElementType.SourceDeclaration).ToList();
            var allSourceReferences = structure.Where(s => s.Type == DqStructureElementType.SourceReference).ToList();

            foreach (var sourceReference in allSourceReferences)
            {
                sourceReference.IsMissing = allSourceDeclarations.All(fd => fd.Number != sourceReference.Number);
            }

            foreach (var sourceDeclaration in allSourceDeclarations)
            {
                var firstReference = allSourceReferences.FirstOrDefault(r => r.Number == sourceDeclaration.Number);
                if (firstReference == null)
                {
                    sourceDeclaration.IsMissing = true;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DQ.Core;
using Microsoft.Win32;

namespace DQ
{
    public class RichTextBoxEx : RichTextBox
    {
        public static readonly DependencyProperty DocumentBindingProperty = DependencyProperty.Register(
            nameof(DocumentBinding), typeof(FlowDocument), typeof(RichTextBoxEx), new PropertyMetadata(OnDocumentChanged));

        public FlowDocument DocumentBinding
        {
            get { return (FlowDocument)GetValue(DocumentBindingProperty); }
            set { SetValue(DocumentBindingProperty, value); }
        }

        private static void OnDocumentChanged
            (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = (RichTextBoxEx)d;
            thisControl.OnDocumentChanged();
        }

        private void OnDocumentChanged()
        {
            if (DocumentBinding?.Parent != null)
            {
                var parent = (RichTextBoxEx)DocumentBinding.Parent;
                parent.Document = new FlowDocument();
            }

            Document = DocumentBinding ?? new FlowDocument();
        }
    }

    public class ParagraphViewModel
    {
        public int Index { get; set; }
        public FlowDocument Document { get; set; }
        public FlowDocument Elements { get; set; }
        public WrapPanel Notes { get; set; }
    }

    public class NodeViewModel
    {
        public int Index { get; set; }
        public string Header { get; set; }
        public List<NodeViewModel> Children { get; set; } = new List<NodeViewModel>();
    }

    public partial class MainWindow
    {
        public static BitmapImage Image { get; private set; }
        public static BitmapImage Table { get; private set; }

        public MainWindow()
        {
            Image = new BitmapImage(new Uri(@"pack://application:,,,/DQ;component/Resources/Placeholder.png"));
            Table = new BitmapImage(new Uri(@"pack://application:,,,/DQ;component/Resources/Table.png"));
            Image.Freeze();
            Table.Freeze();
            InitializeComponent();
        }

        private static NodeViewModel ToVm(DqPart node) =>
            new NodeViewModel
            {
                Index = node.Start.Index,
                Header = node.Start.GetPureText(),
                Children = node is DqMainPart dqMainPart
                    ? dqMainPart.Children.Select(ToVm).ToList()
                    : new List<NodeViewModel>(),
            };

        private static ParagraphViewModel ToVm(DqParagraph p)
        {
            return new ParagraphViewModel
            {
                Index = p.Index + 1,
                Document = new FlowDocument(Convert(p)),
                Elements = ToMetaStructure(p),
                Notes = ToMetaErrors(p),
            };
        }

        private static Paragraph Convert(DqParagraph p)
        {
            var r = new Regex("({IMG}|{TBL})");
            var textParts = r.Split(p.Text);

            var lengthConverter = new LengthConverter();
            var value = (double)lengthConverter.ConvertFromInvariantString(p.Style.Indent.ToString(CultureInfo.InvariantCulture) + "cm");
            var result = new Paragraph
            {
                TextIndent = value,
                TextAlignment = (TextAlignment)p.Style.Aligment,
            };


            foreach (var textPart in textParts)
            {
                switch (textPart)
                {
                    case "{IMG}":
                        result.Inlines.Add(new Image { Source = Image, Width = Image.Width, Height = Image.Height });
                        break;

                    case "{TBL}":
                        result.Inlines.Add(new Image { Source = Table, Width = Image.Width, Height = Image.Height });
                        break;

                    default:
                        result.Inlines.Add(ToTextRun(p, textPart));
                        break;
                }
            }

            return result;
        }

        private static WrapPanel ToMetaErrors(DqParagraph p)
        {
            var errorsDoc = new WrapPanel();

            foreach (var metaError in p.Meta.Errors)
            {
                var grid = new Grid();

                grid.Children.Add(new Image
                {
                    ToolTip = metaError.Message,
                    Source = new BitmapImage(new Uri($@"pack://application:,,,/DQ;component/Resources/{metaError.AssociatedImage}.png")),
                    Width = 32,
                });    
                
                if (metaError.GetType() != typeof(DqError) && metaError.GetType() != typeof(DqWarning))
                {
                    grid.Background = Brushes.IndianRed;
                }

                errorsDoc.Children.Add(grid);
            }

            foreach (var figureDeclaration in p.Meta.FigureDeclarations.Where(d => d.IsMissing))
            {
                errorsDoc.Children.Add(new Image
                {
                    ToolTip = $"Нет ссылки на рисунок {figureDeclaration.Number}",
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/DQ;component/Resources/Wrong.png")),
                    Width = 32,
                });   
            }

            foreach (var figureReference in p.Meta.FigureReferences.Where(d => d.IsMissing))
            {
                errorsDoc.Children.Add(new Image
                {
                    ToolTip = $"Нет объявления рисунка {figureReference.Number}",
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/DQ;component/Resources/Wrong.png")),
                    Width = 32,
                });
            }

            foreach (var tableDeclaration in p.Meta.TableDeclarations.Where(d => d.IsMissing))
            {
                errorsDoc.Children.Add(new Image
                {
                    ToolTip = $"Нет ссылки на таблицу {tableDeclaration.Number}",
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/DQ;component/Resources/Wrong.png")),
                    Width = 32,
                });
            }

            foreach (var tableReference in p.Meta.TableReferences.Where(d => d.IsMissing))
            {
                errorsDoc.Children.Add(new Image
                {
                    ToolTip = $"Нет объявления таблицы {tableReference.Number}",
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/DQ;component/Resources/Wrong.png")),
                    Width = 32,
                });
            }

            foreach (var sourceDeclaration in p.Meta.SourceDeclarations.Where(d => d.IsMissing))
            {
                errorsDoc.Children.Add(new Image
                {
                    ToolTip = $"Нет ссылки на источник {sourceDeclaration.Number}",
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/DQ;component/Resources/Wrong.png")),
                    Width = 32,
                });
            }

            foreach (var sourceReference in p.Meta.SourceReferences.Where(d => d.IsMissing))
            {
                errorsDoc.Children.Add(new Image
                {
                    ToolTip = $"Нет объявления источника {sourceReference.Number}",
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/DQ;component/Resources/Wrong.png")),
                    Width = 32,
                });
            }

            return errorsDoc;
        }

        private static FlowDocument ToMetaStructure(DqParagraph p)
        {
            var structureDoc = new FlowDocument();

            if (p.Meta.IsHeader)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Заголовок")
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var figureDeclaration in p.Meta.FigureDeclarations)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Подпись рисунка {figureDeclaration.Number} ")
                {
                    Foreground = Brushes.DarkOliveGreen,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var figureReference in p.Meta.FigureReferences)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Ссылка на рисунок {figureReference.Number} ")
                {
                    Foreground = Brushes.ForestGreen,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var tableDeclaration in p.Meta.TableDeclarations)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Название таблицы {tableDeclaration.Number} ")
                {
                    Foreground = Brushes.Blue,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var tableReference in p.Meta.TableReferences)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Ссылка на таблицу {tableReference.Number} ")
                {
                    Foreground = Brushes.BlueViolet,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var sourceReference in p.Meta.SourceReferences)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Ссылка на источник {sourceReference.Number} ")
                {
                    Foreground = Brushes.Orange,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var sourceReference in p.Meta.SourceDeclarations)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Источник {sourceReference.Number} ")
                {
                    Foreground = Brushes.DarkGoldenrod,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            return structureDoc;
        }

        private static Run ToTextRun(DqParagraph p, string text)
        {
            return new Run(text)
            {
                FontSize = (double)p.Style.FontSize,
                FontFamily = new FontFamily(p.Style.FontName),
                FontWeight = p.Style.IsBold? FontWeights.Bold : FontWeights.Normal,
            };
        }

        private void DataGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            dataGrid.Columns[0].Width = new DataGridLength(50);
            dataGrid.Columns[1].Width = new DataGridLength(e.NewSize.Width - 250 - 8);
            dataGrid.Columns[2].Width = new DataGridLength(200);
            dataGrid.UpdateLayout();
        }

        private void DoGoToPart(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var dc = (NodeViewModel) button.DataContext;
            DataGrid.SelectedIndex = dc.Index + 1;
            DataGrid.ScrollIntoView(DataGrid.SelectedItem, DataGrid.Columns[1]);
        }

        private void DoLoad(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Word documents (*.docx)|*.docx",
            };
            if (openFileDialog.ShowDialog() == true)
            {
                Load(openFileDialog.FileName);
            }
        }

        private void Load(string path)
        {
            var document = new Class1().Go(path);

            TreeView.ItemsSource = GetStructure(document);
            DataGrid.ItemsSource = document.Paragraphs.Select(ToVm).ToList();
        }

        private IEnumerable<NodeViewModel> GetStructure(DqDocument dqDocument)
        {
            if (dqDocument.Structure.Abstracts.Any())
            {
                foreach (var @abstract in dqDocument.Structure.Abstracts)
                {
                    yield return ToVm(@abstract);
                }
            }
            else
            {
                yield return CreateMissingNode("реферат");
            }

            if (dqDocument.Structure.Toc != null)
            {
                yield return ToVm(dqDocument.Structure.Toc);
            }

            if (dqDocument.Structure.Introduction != null)
            {
                yield return ToVm(dqDocument.Structure.Introduction);
            }
            else
            {
                yield return CreateMissingNode("введение");
            }

            if (dqDocument.Structure.MainPart != null)
            {
                foreach (var dqMainPart in dqDocument.Structure.MainPart.Children)
                {
                    yield return ToVm(dqMainPart);
                }
            }

            if (dqDocument.Structure.Conclusion != null)
            {
                yield return ToVm(dqDocument.Structure.Conclusion);
            }
            else
            {
                yield return CreateMissingNode("заключение");
            }

            if (dqDocument.Structure.Bibliography != null)
            {
                yield return ToVm(dqDocument.Structure.Bibliography);
            }
            else
            {
                yield return CreateMissingNode("список литературы");
            }

            if (dqDocument.Structure.Appendixes != null)
            {
                yield return ToVm(dqDocument.Structure.Appendixes);
            }

            NodeViewModel CreateMissingNode(string title) => new NodeViewModel
            {
                Index = 0,
                Header = $"[{title.ToUpper()} ОТСУТСТВУЕТ]",
            };
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Load(files[0]);
            }
        }
    }
}

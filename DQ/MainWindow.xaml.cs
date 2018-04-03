using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using DQ.Core;

namespace DQ
{
    public class TreeViewEx : TreeView
    {
        protected override DependencyObject GetContainerForItemOverride() => new TreeViewItemEx();

        protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeViewItemEx;
    }

    public class TreeViewItemEx : TreeViewItem
    {
        public TreeViewItemEx()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (this.VisualChildrenCount > 0)
            {
                Grid grid = GetVisualChild(0) as Grid;
                if (grid != null && grid.ColumnDefinitions.Count == 3)
                {
                    grid.ColumnDefinitions.RemoveAt(1);
                }               
            }
        }

        protected override DependencyObject GetContainerForItemOverride() => new TreeViewItemEx();

        protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeViewItemEx;
    }

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
            var thisControl = (RichTextBoxEx) d;
            thisControl.OnDocumentChanged();
        }

        private void OnDocumentChanged()
        {
            if (DocumentBinding?.Parent != null)
            {
                var parent = (RichTextBoxEx) DocumentBinding.Parent;
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
        public FlowDocument Notes { get; set; }
    }

    public class NodeViewModel
    {
        public string Header { get; set; }
        public List<NodeViewModel> Children { get; set; } = new List<NodeViewModel>();
    }

    public partial class MainWindow
    {
        public static BitmapImage Image { get; private set; }
        public static BitmapImage Table { get; private set; }

        public MainWindow()
        {
            Image = new BitmapImage(new Uri(@"D:\Placeholder.png"));
            Table = new BitmapImage(new Uri(@"D:\Table.png"));
            Image.Freeze();
            Table.Freeze();
            InitializeComponent();
            var (root, document) = new Class1().Go();

            var vms = ToVm(root).Children;
            TreeView.ItemsSource = vms;
            DataGrid.ItemsSource = document.Paragraphs.Select(ToVm).ToList();
        }

        private static NodeViewModel ToVm(Node node) =>
            new NodeViewModel
            {
                Header = node.HeaderParagraph.Text,
                Children = node.Children.Select(ToVm).ToList(),
            };

        private static ParagraphViewModel ToVm(DqParagraph p)
        {
            return new ParagraphViewModel
            {
                Index = p.Index,
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
            var value = (double) lengthConverter.ConvertFromInvariantString(p.Style.GetIndent().ToString(CultureInfo.InvariantCulture) + "cm");
            var result = new Paragraph
            {
                TextIndent = value,
                TextAlignment = (TextAlignment) p.Style.GetAligment(),
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

        private static FlowDocument ToMetaErrors(DqParagraph p)
        {
            var errorsDoc = new FlowDocument();

            foreach (var metaError in p.Meta.Errors)
            {              
                errorsDoc.Blocks.Add(new Paragraph(new Run(metaError.Message)
                {
                    Foreground = Brushes.Red,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            return errorsDoc;
        }

        private static FlowDocument ToMetaStructure(DqParagraph p)
        {
            var structureDoc = new FlowDocument();         

            foreach (var figureDeclaration in p.Meta.FigureDeclarations)
            {   
                structureDoc.Blocks.Add(new Paragraph(new Run($"Figure Declaration: {figureDeclaration.Number} ")
                {
                    Foreground = figureDeclaration.IsMissing ? Brushes.Red : Brushes.DarkOliveGreen,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var figureReference in p.Meta.FigureReferences)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Figure Reference: {figureReference.Number} ")
                {
                    Foreground = figureReference.IsMissing ? Brushes.Red : Brushes.GreenYellow,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var tableDeclaration in p.Meta.TableDeclarations)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Table Declaration: {tableDeclaration.Number} ")
                {
                    Foreground = tableDeclaration.IsMissing ? Brushes.Red : Brushes.Blue,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var tableReference in p.Meta.TableReferences)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Table Reference: {tableReference.Number} ")
                {
                    Foreground = tableReference.IsMissing ? Brushes.Red : Brushes.BlueViolet,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                }));
            }

            foreach (var sourceReference in p.Meta.SourceReferences)
            {
                structureDoc.Blocks.Add(new Paragraph(new Run($"Source Reference: {sourceReference.Number} ")
                {
                    Foreground = sourceReference.IsMissing ? Brushes.Red : Brushes.Orange,
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
                FontSize = (double) p.Style.GetFontSize(),
                FontFamily = new FontFamily(p.Style.GetFontName()),
                FontWeight = p.Style.GetIsBold() ? FontWeights.Bold : FontWeights.Normal,
            };
        }

        private void DataGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var dataGrid = (DataGrid) sender;
            dataGrid.Columns[0].Width = new DataGridLength(50);
            dataGrid.Columns[1].Width = new DataGridLength(e.NewSize.Width - 250 - 8);
            dataGrid.Columns[2].Width = new DataGridLength(200);
            dataGrid.UpdateLayout();
        }
    }
}

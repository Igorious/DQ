using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DQ.Core.Tests
{
    [TestClass] public class UnitTest1
    {
        private void Check(SourceAnalyzer.Book expectedResult, string text)
        {
            var actualResult = new BookParser().TryParse(text);
            Assert.AreEqual(expectedResult, actualResult);
            Assert.AreEqual(text, actualResult.ToString());
        }

        [TestMethod]
        public void BookSingleAuthor() => Check(new SourceAnalyzer.Book
        {
            Authors1 = new[] { new Person("Котаў", "А.", "I.") },
            Title = "Гісторыя Беларусі і сусветная цывілізацыя",
            Authors2 = new[] { new Person("Котаў", "A.", "I.") },
            Edition = 2,
            PublisherCity = "Мінск",
            PublisherName = "Энцыклапедыкс",
            PublisherYear = 2003,
            Pages = 168,
        }, "Котаў, А.I. Гісторыя Беларусі і сусветная цывілізацыя / A.I. Котаў. – 2-е выд. – Мінск : Энцыклапедыкс, 2003. – 168 с.");

        [TestMethod]
        public void BookTwoAuthors() => Check(new SourceAnalyzer.Book
        {
            Authors1 = new[] { new Person("Шотт", "А.", "В.") },
            Title = "Курс лекций по частной хирургии",
            Authors2 = new[] { new Person("Шотт", "А.", "В."), new Person("Шотт", "В.", "А.") },
            PublisherCity = "Минск",
            PublisherName = "Асар",
            PublisherYear = 2004,
            Pages = 525,
        }, "Шотт, А.В. Курс лекций по частной хирургии / А.В. Шотт, В.А. Шотт. – Минск : Асар, 2004. – 525 с.");

        [TestMethod]
        public void BookTwoAuthorsAndEditorAuthors() => Check(new SourceAnalyzer.Book
        {
            Authors1 = new[] { new Person("Чикатуева", "Л.", "А.") },
            Title = "Маркетинг : учеб. пособие",
            Authors2 = new[] { new Person("Чикатуева", "Л.", "А."), new Person("Третьякова", "Н.", "В.") },
            EditorText = "под ред.",
            Editor = new Person("Федько", "В.", "П."),
            PublisherCity = "Ростов н/Д",
            PublisherName = "Феникс",
            PublisherYear = 2004,
            Pages = 413,
        }, "Чикатуева, Л.А. Маркетинг : учеб. пособие / Л.А. Чикатуева, Н.В. Третьякова ; под ред. В.П. Федько. – Ростов н/Д : Феникс, 2004. – 413 с.");

        [TestMethod]
        public void BookThreeAuthorsAndEditorAuthors() => Check(new SourceAnalyzer.Book
        {
            Authors1 = new[] { new Person("Дайнеко", "А.", "Е.") },
            Title = "Экономика Беларуси в системе всемирной торговой организации",
            Authors2 = new[] { new Person("Дайнеко", "А.", "Е."), new Person("Забавский", "Г.", "В."), new Person("Василевская", "М.", "В.") },
            EditorText = "под ред.",
            Editor = new Person("Дайнеко", "А.", "Е."),
            PublisherCity = "Минск",
            PublisherName = "Ин-т аграр. экономики",
            PublisherYear = 2004,
            Pages = 323,
        }, "Дайнеко, А.Е. Экономика Беларуси в системе всемирной торговой организации / А.Е. Дайнеко, Г.В. Забавский, М.В. Василевская ; под ред. А.Е. Дайнеко. – Минск : Ин-т аграр. экономики, 2004. – 323 с.");

        [TestMethod]
        public void BookManyAuthors1() => Check(new SourceAnalyzer.Book
        {
            Authors1 = new Person[] { },
            Title = "Культурология : учеб. пособие для вузов",
            Authors2 = new[] { new Person("Лапина", "С.", "В.") },
            HasOtherAuthors = true,
            EditorText = "под общ. ред.",
            Editor = new Person("Лапиной", "С.", "В."),
            Edition = 2,
            PublisherCity = "Минск",
            PublisherName = "ТетраСистемс",
            PublisherYear = 2004,
            Pages = 495,
        }, "Культурология : учеб. пособие для вузов / С.В. Лапина [и др.] ; под общ. ред. С.В. Лапиной. – 2-е изд. – Минск : ТетраСистемс, 2004. – 495 с.");

        [TestMethod]
        public void BookManyAuthors2() => Check(new SourceAnalyzer.Book
        {
            Title = "Комментарий к Трудовому кодексу Республики Беларусь",
            Authors2 = new[] { new Person("Андреев", "И.", "С.") },
            HasOtherAuthors = true,
            EditorText = "под общ. ред.",
            Editor = new Person("Василевича", "Г.", "А."),
            PublisherCity = "Минск",
            PublisherName = "Амалфея",
            PublisherYear = 2000,
            Pages = 1071,
        }, "Комментарий к Трудовому кодексу Республики Беларусь / И.С. Андреев [и др.] ; под общ. ред. Г.А. Василевича. – Минск : Амалфея, 2000. – 1071 с.");

        // ???
        //[TestMethod]
        //public void BookManyAuthors3() => Check(new SourceAnalyzer.Book
        //{
        //    Authors1 = new Person[] { },
        //    Title = "Основы геологии Беларуси",
        //    Authors2 = new[] { new Person("Махнач", "А.", "С.") },
        //    HasOtherAuthors = true,
        //    EditorText = "под общ. ред.",
        //    Editor =new Person("Махнача", "А.", "С."),
        //    PublisherCity = "Минск",
        //    PublisherYear = 2004,
        //    Pages = 391,
        //}, "Основы геологии Беларуси / А.С. Махнач [и др.] ; НАН Беларуси, Ин-т геол. наук ; под общ. ред. А.С. Махнача. – Минск, 2004. – 391 с.");

        [TestMethod]
        public void BookCollectivity1() => Check(new SourceAnalyzer.Book
        {
            Title = "Сборник нормативно-технических материалов по энергосбережению",
            Collectivity = "Ком. по энергоэффективности при Совете Министров Респ. Беларусь",
            EditorText = "сост.",
            Editor = new Person("Филипович", "А.", "В."),
            PublisherCity = "Минск",
            PublisherName = "Лоранж-2",
            PublisherYear = 2004,
            Pages = 393,
        }, "Сборник нормативно-технических материалов по энергосбережению / Ком. по энергоэффективности при Совете Министров Респ. Беларусь ; сост. А.В. Филипович. – Минск : Лоранж-2, 2004. – 393 с.");

        [TestMethod]
        public void BookCollectivity2() => Check(new SourceAnalyzer.Book
            {
                Title = "Национальная стратегия устойчивого социально-экономического развития Республики Беларусь на период до 2020 г.",
                Collectivity = "Нац. комис. по устойчивому развитию Респ. Беларусь",
                EditorText = "редкол.:",
                Editor = new Person("Александрович", "Л.", "М."),
                HasOtherEditors = true,
                PublisherCity = "Минск",
                PublisherName = "Юнипак",
                PublisherYear = 2004,
                Pages = 202,
            }, "Национальная стратегия устойчивого социально-экономического развития Республики Беларусь на период до 2020 г. / "
               + "Нац. комис. по устойчивому развитию Респ. Беларусь ; редкол.: Л.М. Александрович [и др.]. – Минск : Юнипак, 2004. – 202 с.");

        [TestMethod]
        public void BookCollectivity3() => Check(new SourceAnalyzer.Book
            {
                Title = "Военный энциклопедический словарь",
                Collectivity = "М-во обороны Рос. Федерации, Ин-т воен. истории",
                EditorText = "редкол.:",
                Editor = new Person("Горкин", "А.", "П."),
                HasOtherEditors = true,
                PublisherCity = "М.",
                PublisherName = "Большая рос. энцикл. : РИПОЛ классик",
                PublisherYear = 2002,
                Pages = 1663,
            }, "Военный энциклопедический словарь / "
               + "М-во обороны Рос. Федерации, Ин-т воен. истории ; редкол.: А.П. Горкин [и др.]. – М. : Большая рос. энцикл. : РИПОЛ классик, 2002. – 1663 с.");

        // TODO: Многотомное издание.

        [TestMethod]
        public void MultiVolume1() => Check(new SourceAnalyzer.Book
            {
                Title = "Гісторыя Беларусі : у 6 т.",
                EditorText = "рэдкал.:",
                Editor = new Person("Касцюк", "М."),
                HasOtherEditors = true,
                PublisherCity = "М.",
                PublisherName = "Большая рос. энцикл. : РИПОЛ классик",
                PublisherYear = 2000,
                PublisherYearEnd = 2005,
            }, "Гісторыя Беларусі : у 6 т. / рэдкал.: М. Касцюк (гал. рэд.) [і інш.]. – Мінск : Экаперспектыва, 2000–2005. – 6 т.");
    }
}

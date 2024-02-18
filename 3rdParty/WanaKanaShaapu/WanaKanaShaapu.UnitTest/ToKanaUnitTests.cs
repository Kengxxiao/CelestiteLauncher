using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class ToKanaUnitTests
    {
        [TestCase("", "")]
        public void ToKana_WhenPassedAnEmptyString_ReturnsAnEmptyString(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("座禅‘zazen’スタイル", "座禅「ざぜん」スタイル")]
        [TestCase("batsuge-mu", "ばつげーむ")]
        [TestCase("chya", "ちゃ")]
        [TestCase("chyx", "chyx")]
        [TestCase("shyp", "shyp")]
        [TestCase("ltsb", "ltsb")]
        [TestCase("chuu", "ちゅう")]
        public void ToKana_WhenPassedInput_ConvertsItCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("ヶ", "ヶ")]
        [TestCase("ヵ", "ヵ")]
        [TestCase("1", "1")]
        [TestCase("@", "@")]
        [TestCase("#", "#")]
        [TestCase("$", "$")]
        [TestCase("%", "%")]
        public void ToKana_WhenPassedTheSymbolsThatAreNotToBeConverted_ReturnsThemAsTheyAre(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("onaji BUTTSUUJI", "おなじ ブッツウジ")]
        [TestCase("ONAJI buttsuuji", "オナジ ぶっつうじ")]
        public void ToKana_WhenPassedLowerAndUpperCaseLatinChacters_ReturnsThemConvertedToHiraganaAndKatakanaRespectively(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("WaniKani", "わにかに")]
        public void ToKana_WhenPassedMixedCaseWithStandaloneUppercaseChars_ReturnsThemConvertedToHiragana(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("kwi kuxi kuli kwe kuxe kule kwo kuxo kulo", "くぃ くぃ くぃ くぇ くぇ くぇ くぉ くぉ くぉ")]
        public void ToKana_WhenPassedRyukyuan_ConvertsThemCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("ワニカニ AiUeO 鰐蟹 12345 @#$%", "ワニカニ アいウえオ 鰐蟹 12345 @#$%")]
        public void ToKana_WhenNonRomajiIsPassed_ReturnsItAsIs(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("n", "ん")]
        [TestCase("shin", "しん")]
        [TestCase("nn", "んん")]
        [TestCase("onn", "おんん")]
        [TestCase("onna", "おんな")]
        [TestCase("nnn", "んんん")]
        [TestCase("onnna", "おんんな")]
        [TestCase("nnnn", "んんんん")]
        [TestCase("nyan", "にゃん")]
        [TestCase("nnyann", "んにゃんん")]
        [TestCase("nnnyannn", "んんにゃんんん")]
        [TestCase("n'ya", "んや")]
        [TestCase("kin'ya", "きんや")]
        [TestCase("shin'ya", "しんや")]
        [TestCase("kinyou", "きにょう")]
        [TestCase("kin'you", "きんよう")]
        [TestCase("kin'yu", "きんゆ")]
        [TestCase("ichiban warui", "いちばん わるい")]
        public void ToKana_WhenPassedNConsonant_TransliteratesItCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("we", true, "ゑ")]
        [TestCase("wi", true, "ゐ")]
        [TestCase("WI", true, "ヰ")]
        [TestCase("WE", true, "ヱ")]
        public void ToKana_WhenPassedObsoleteKanaFlagTrue_ReturnsObsoleteKanaString(string input, bool useObsoleteKana, string expectedOutput)
        {
            string result = WanaKana.ToKana(input, new DefaultOptions { UseObsoleteKana = useObsoleteKana });

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("wanakana", "わにBanaに")]
        public void ToKana_WhenPassedCustomMapping_ReturnsACorrectString(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input, new DefaultOptions { CustomKanaMapping = new Dictionary<string, string> { { "na", "に" }, { "ka", "Bana" } } });

            Assert.AreEqual(result, expectedOutput);
        }
    }
}
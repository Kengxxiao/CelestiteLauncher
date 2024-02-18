using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class StripOkuriganaUnitTests 
    {
        [TestCase("ふふフフ", "ふふフフ")]
        [TestCase("abc", "abc")]
        [TestCase("ふaふbフcフ", "ふaふbフcフ")]
        public void StripOkurigana_WhenPassedNonKanjiInput_ReturnsTheSameInput(string input, string expectedOutput)
        {
            string result = WanaKana.StripOkurigana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("踏み込む", "踏み込")]
        [TestCase("使い方", "使い方")]
        [TestCase("申し申し", "申し申")]
        [TestCase("お腹", "お腹")]
        [TestCase("お祝い", "お祝")]
        public void StripOkurigana_WhenPassedKanjiWithOkurigana_StripsTheStringCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.StripOkurigana(input);
                
            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("お腹", true, "腹")]
        [TestCase("踏み込む", true, "踏み込む")]
        [TestCase("お祝い", true, "祝い")]
        public void StripOkurigana_WhenPassedLeadingAsTrue_ReturnsAStringWithoutLeadingOkurigana(string input, bool leading, string expectedOutput)
        {
            string result = WanaKana.StripOkurigana(input, leading: leading);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("おはら", "お腹", "おはら")]
        [TestCase("ふみこむ", "踏み込む", "ふみこ")]
        public void StripOkurigana_WhenPassedHiraganaAndMatchedKanji_ReturnsHiraganaWithoutOkuriganaMatched(string input, string matchKanji, string expectedOutput)
        {
            string result = WanaKana.StripOkurigana(input, matchKanji: matchKanji);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("おみまい", "お見舞い", true, "みまい")]
        [TestCase("おいわい", "お祝い", true, "いわい")]
        [TestCase("おはら", "お腹", true, "はら")]
        public void StripOkurigana_WhenPassedHiraganaMatchedKanjiAndLeadingTrue_ReturnsHiraganaWithoutOkuriganaMatched(string input, string matchKanji, bool leading, string expectedOutput)
        {
            string result = WanaKana.StripOkurigana(input, matchKanji: matchKanji, leading: leading);

            Assert.AreEqual(result, expectedOutput);
        }
    }
}

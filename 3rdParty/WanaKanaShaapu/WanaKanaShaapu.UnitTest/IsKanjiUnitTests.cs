using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class IsKanjiUnitTests
    {
        [TestCase("あAア")]
        [TestCase("勢い")]
        [TestCase("🐸")]
        [TestCase("あ")]
        [TestCase("ア")]
        [TestCase("あア")]
        [TestCase("１２隻")]
        [TestCase("12隻")]
        [TestCase("隻。")]
        public void IsKanji_WhenPassedNonKanjiChars_ReturnsFalse(string input)
        {
            var result = WanaKana.IsKanji(input);

            Assert.False(result);
        }

        [TestCase("刀")]
        [TestCase("切腹")]
        public void IsKanji_WhenPassedKanji_ReturnsTrue(string input)
        {
            var result = WanaKana.IsKanji(input);

            Assert.True(result);
        }
    }
}

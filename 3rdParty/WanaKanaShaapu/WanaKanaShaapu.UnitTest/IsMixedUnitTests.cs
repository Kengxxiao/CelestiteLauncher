using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class IsMixedUnitTests
    {
        [TestCase("Aあア")]
        [TestCase("Aア")]
        [TestCase("Aあ")]
        public void IsMixed_WhenPassedRomajiAndKana_ReturnsTrue(string input)
        {
            var result = WanaKana.IsMixed(input);

            Assert.True(result);
        }

        [TestCase("お腹A")]
        public void IsMixed_WhenPassedRomajiKanaAndKanji_ReturnsTrue(string input)
        {
            var result = WanaKana.IsMixed(input);

            Assert.True(result);
        }

        [TestCase("お腹A", false)]
        public void IsMixed_WhenPassedKanjiAndKanjiSetToFalse_ReturnsFalse(string input, bool passKanji)
        {
            var result = WanaKana.IsMixed(input, passKanji);

            Assert.IsFalse(result);
        }

        [TestCase("ab")]
        public void IsMixed_WhenPassedRomaji_ReturnsFalse(string input)
        {
            var result = WanaKana.IsMixed(input);

            Assert.IsFalse(result);
        }

        [TestCase("あア")]
        [TestCase("２あア")]
        [TestCase("お腹")]
        [TestCase("腹")]
        [TestCase("A")]
        [TestCase("あ")]
        [TestCase("ア")]
        public void IsMixed_WhenPassedJapaneseNotMixedWithLatinChars_ReturnsFalse(string input)
        {
            var result = WanaKana.IsMixed(input);

            Assert.IsFalse(result);
        }
    }
}
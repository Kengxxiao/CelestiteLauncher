using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class IsKanaUnitTests
    {
        [TestCase("This shall fail")]
        [TestCase("あAア")]
        [TestCase("A")]
        public void IsKana_WhenPassedLatinCharacters_ReturnsFalse(string input)
        {
            var result = WanaKana.IsKana(input);

            Assert.False(result);
        }

        [TestCase("あ")]
        public void IsKana_WhenPassedHiragana_ReturnsTrue(string input)
        {
            var result = WanaKana.IsKana(input);

            Assert.True(result);
        }

        [TestCase("ア")]
        public void IsKana_WhenPassedKatakana_ReturnsTrue(string input)
        {
            var result = WanaKana.IsKana(input);

            Assert.True(result);
        }

        [TestCase("あーア")]
        public void IsKana_WhenPassedKana_ReturnsTrue(string input)
        {
            var result = WanaKana.IsKana(input);

            Assert.True(result);
        }
    }
}
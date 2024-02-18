using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class IsKatakanaUnitTests
    {

        [TestCase("あ")]
        [TestCase("A")]
        [TestCase("あア")]
        public void IsKatakana_WhenPassedNonKatakanaChars_ReturnsFalse(string input)
        {
            var result = WanaKana.IsKatakana(input);

            Assert.False(result);
        }

        [TestCase("ゲーム")]
        [TestCase("アア")]
        [TestCase("ア")]
        public void IsKatakana_WhenPassedKatakana_ReturnsTrue(string input)
        {
            var result = WanaKana.IsKatakana(input);

            Assert.True(result);
        }
    }
}

using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class IsRomajiUnitTests
    {
        [TestCase("Tōkyō and Ōsaka")]
        public void IsRomaji_WhenPassedLatinAndMacronCharacters_ReturnsTrue(string input)
        {
            var result = WanaKana.IsRomaji(input);

            Assert.True(result);
        }

        [TestCase("12a*b&c-d")]
        [TestCase("A")]
        [TestCase("xYz")]
        [TestCase("0123456789")]
        public void IsRomaji_WhenPassedLatinCharactersPunctuationAndNumerals_ReturnsTrue(string input)
        {
            var result = WanaKana.IsRomaji(input);

            Assert.True(result);
        }

        [TestCase("あアA")]
        public void IsRomaji_WhenPassedKanaAndLatinCharacters_ReturnsFalse(string input)
        {
            var result = WanaKana.IsRomaji(input);

            Assert.False(result);
        }

        [TestCase("お願い")]
        [TestCase("熟成")]
        [TestCase("ｈｅｌｌｏ")]
        public void IsRomaji_WhenPassedNonRomajiInput_ReturnsFalse(string input)
        {
            var result = WanaKana.IsRomaji(input);

            Assert.False(result);
        }

        [TestCase("a！b&cーd")]
        public void IsRomaji_WhenPassedLatinAndJapaneseCharacters_ReturnsFalse(string input)
        {
            var result = WanaKana.IsRomaji(input);

            Assert.False(result);
        }

        [TestCase("a！b&cーd", "[！ー]")]
        public void IsRomaji_WhenPassedLatinAndJapaneseCharactersAndRegexToIgnoreThem_ReturnsTrue(string input, string allowed)
        {
            var result = WanaKana.IsRomaji(input, allowed);

            Assert.True(result);
        }
    }
}

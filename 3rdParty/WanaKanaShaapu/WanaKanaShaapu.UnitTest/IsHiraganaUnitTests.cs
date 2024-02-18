using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class IsHiraganaUnitTests
    {
        [TestCase("A")]
        [TestCase("{Man} Once upon a time there was a lovely princess.")]
        [TestCase("But she had an enchantment upon her of a fearful sort which could only ")]
        [TestCase("be broken by love's first kiss.")]
        [TestCase("She was locked away in a castle guarded by a terrible fire-breathing dragon")]
        [TestCase("Many brave knigts had attempted to free her from this dreadful prison, but non prevailed.")]
        [TestCase("Somebody once told me the world is gonna roll me")]
        [TestCase("I ain't the sharpest tool in the shed")]
        [TestCase("She was lookin' kind of dumb with her finger and her thumb")]
        [TestCase("In the shape of an \"L\" on her forehead")]
        public void IsHiragana_WhenPassedLatinCharacters_ReturnsFalse(string input)
        {
            var result = WanaKana.IsHiragana(input);

            Assert.False(result);
        }

        [TestCase("ア")]
        [TestCase("リコプター")]
        [TestCase("スペシャルコンテンツ")]
        public void IsHiragana_WhenPassedKatakana_ReturnsFalse(string input)
        {
            var result = WanaKana.IsHiragana(input);

            Assert.False(result);
        }

        [TestCase("あア")]
        [TestCase("現場の様子は")]
        [TestCase("路線バス 住宅に突っ込む 子ども含む男女8人けが 東京 町田")]
        [TestCase("”紅葉の雲海” 2000本のカエデ 境内の渓谷染める 京都 東福寺")]
        [TestCase("でも友だちはまだかえっていませんでした")]
        public void IsHiragana_WhenPassedMixedInput_ReturnsFalse(string input)
        {
            var result = WanaKana.IsHiragana(input);

            Assert.False(result);
        }

        [TestCase("げーむ")]
        [TestCase("すげー")]
        [TestCase("あ")]
        [TestCase("ああ")]
        [TestCase("でもだちはまだかえっていませんでした")]
        [TestCase("むかしみやこのちかくのむらにたけとりのおきなとよばれているおじいさんがいました")]
        public void IsHiragana_WhenPassedHiragana_ReturnsTrue(string input)
        {
            var result = WanaKana.IsHiragana(input);

            Assert.True(result);
        }
    }
}

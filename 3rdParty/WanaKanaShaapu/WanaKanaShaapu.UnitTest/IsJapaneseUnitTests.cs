using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class IsJapaneseUnitTests
    {
        [TestCase(" ")]
        [TestCase("泣き虫.!~$")]
        public void IsJapanese_WhenPassedLatinPunctuation_ReturnsFalse(string input)
        {
            var result = WanaKana.IsJapanese(input);

            Assert.False(result);
        }

        [TestCase("A泣き虫")]
        [TestCase("A")]
        [TestCase("0123456789")]
        public void IsJapanese_WhenPassedLatinCharacters_ReturnsFalse(string input)
        {
            var result = WanaKana.IsJapanese(input);

            Assert.False(result);
        }

        [TestCase("泣き虫")]
        public void IsJapanese_WhenPassedKanjiAndKana_ReturnsTrue(string input)
        {
            var result = WanaKana.IsJapanese(input);

            Assert.True(result);
        }

        [TestCase("あア")]
        [TestCase("ﾊﾝｶｸｶﾀｶﾅ")]
        public void IsJapanese_WhenPassedKana_ReturnsTrue(string input)
        {
            var result = WanaKana.IsJapanese(input);

            Assert.True(result);
        }

        [TestCase("２月")]
        [TestCase("０１２３４５６７８９")]
        [TestCase("２０１１年")]
        public void IsJapanese_WhenPassedZenkakuNumbers_ReturnsTrue(string input)
        {
            var result = WanaKana.IsJapanese(input);

            Assert.True(result);
        }

        [TestCase("泣き虫。！〜＄")]
        [TestCase("泣き虫。＃！〜〈〉《》〔〕［］【】（）｛｝〝〟")]
        [TestCase("　")]
        public void IsJapanese_WhenPassedJapanesePunctuation_ReturnsTrue(string input)
        {
            var result = WanaKana.IsJapanese(input);

            Assert.True(result);
        }

        [TestCase("≪偽括弧≫", "[≪≫]")]
        public void IsJapanese_WhenPassedAllowedChars_ReturnsTrue(string input, string allowed)
        {
            var result = WanaKana.IsJapanese(input, allowed);

            Assert.True(result);
        }

        [TestCase("泣き虫。！〜２￥ｚｅｎｋａｋｕ")]
        [TestCase("ＭｅＴｏｏ")]
        [TestCase("＃ＭｅＴｏｏ、これを前に「ＫＵＲＯＳＨＩＯ」は、都内で報道陣を前に水中探査ロボットの最終点検の様子を公開しました。イルカのような形をした探査ロボットは、全長３メートル、重さは３５０キロあります。《はじめに》冒頭、安倍総理大臣は、ことしが明治元年から１５０年にあたることに触れ「明治という新しい時代が育てたあまたの人材が、技術優位の欧米諸国が迫る『国難』とも呼ぶべき危機の中で、わが国が急速に近代化を遂げる原動力となった。今また、日本は少子高齢化という『国難』とも呼ぶべき危機に直面している。もう１度、あらゆる日本人にチャンスを創ることで、少子高齢化も克服できる」と呼びかけました。《働き方改革》続いて安倍総理大臣は、具体的な政策課題の最初に「働き方改革」を取り上げ、「戦後の労働基準法制定以来、７０年ぶりの大改革だ。誰もが生きがいを感じて、その能力を思う存分発揮すれば少子高齢化も克服できる」と述べました。そして、同一労働同一賃金の実現や、時間外労働の上限規制の導入、それに労働時間でなく成果で評価するとして労働時間の規制から外す「高度プロフェッショナル制度」の創設などに取り組む考えを強調しました。")]
        public void IsJapanese_WhenPassedJapaneseCharacters_ReturnsTrue(string input)
        {
            var result = WanaKana.IsJapanese(input);

            Assert.True(result);
        }
    }
}

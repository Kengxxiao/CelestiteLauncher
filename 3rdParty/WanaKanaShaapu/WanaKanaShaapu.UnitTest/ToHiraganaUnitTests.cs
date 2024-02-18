using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class ToHiraganaUnitTests
    {
        [TestCase("", "")]
        public void ToHiragana_WhenPassedAnEmptyString_ReturnsAnEmptyString(string input, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("a", "あ")]
        [TestCase("i", "い")]
        [TestCase("u", "う")]
        [TestCase("e", "え")]
        [TestCase("o", "お")]
        [TestCase("la", "ぁ")]
        [TestCase("xa", "ぁ")]
        [TestCase("li", "ぃ")]
        [TestCase("xi", "ぃ")]
        [TestCase("lu", "ぅ")]
        [TestCase("xu", "ぅ")]
        [TestCase("le", "ぇ")]
        [TestCase("xe", "ぇ")]
        [TestCase("lo", "ぉ")]
        [TestCase("xo", "ぉ")]
        [TestCase("yi", "い")]
        [TestCase("wu", "う")]
        [TestCase("whu", "う")]
        [TestCase("xa", "ぁ")]
        [TestCase("xi", "ぃ")]
        [TestCase("xu", "ぅ")]
        [TestCase("xe", "ぇ")]
        [TestCase("xo", "ぉ")]
        [TestCase("xyi", "ぃ")]
        [TestCase("xye", "ぇ")]
        [TestCase("ye", "いぇ")] 
        [TestCase("wha", "うぁ")]
        [TestCase("whi", "うぃ")]
        [TestCase("whe", "うぇ")]
        [TestCase("who", "うぉ")]
        [TestCase("wi", "うぃ")]
        [TestCase("we", "うぇ")]
        [TestCase("va", "ゔぁ")]
        [TestCase("vi", "ゔぃ")]
        [TestCase("vu", "ゔ")]
        [TestCase("ve", "ゔぇ")]
        [TestCase("vo", "ゔぉ")]
        [TestCase("vyi", "ゔぃ")]
        [TestCase("vye", "ゔぇ")]
        [TestCase("vya", "ゔゃ")]
        [TestCase("vyu", "ゔゅ")]
        [TestCase("vyo", "ゔょ")]
        [TestCase("ka", "か")]
        [TestCase("ki", "き")]
        [TestCase("ku", "く")]
        [TestCase("ke", "け")]
        [TestCase("ko", "こ")]
        [TestCase("lka", "ヵ")]
        [TestCase("lke", "ヶ")]
        [TestCase("xka", "ヵ")]
        [TestCase("xke", "ヶ")]
        [TestCase("kya", "きゃ")]
        [TestCase("kyi", "きぃ")]
        [TestCase("kyu", "きゅ")]
        [TestCase("kye", "きぇ")]
        [TestCase("kyo", "きょ")]
        [TestCase("ca", "か")] 
        [TestCase("ci", "き")] 
        [TestCase("cu", "く")] 
        [TestCase("ce", "け")] 
        [TestCase("co", "こ")] 
        [TestCase("lca", "ヵ")]
        [TestCase("lce", "ヶ")]
        [TestCase("xca", "ヵ")]
        [TestCase("xce", "ヶ")]
        [TestCase("qya", "くゃ")]
        [TestCase("qyu", "くゅ")]
        [TestCase("qyo", "くょ")]
        [TestCase("qwa", "くぁ")]
        [TestCase("qwi", "くぃ")]
        [TestCase("qwu", "くぅ")]
        [TestCase("qwe", "くぇ")]
        [TestCase("qwo", "くぉ")]
        [TestCase("qa", "くぁ")]
        [TestCase("qi", "くぃ")]
        [TestCase("qe", "くぇ")]
        [TestCase("qo", "くぉ")]
        [TestCase("kwa", "くぁ")] 
        [TestCase("kwi", "くぃ")] 
        [TestCase("kwe", "くぇ")] 
        [TestCase("kwo", "くぉ")] 
        [TestCase("qyi", "くぃ")] 
        [TestCase("qye", "くぇ")] 
        [TestCase("ga", "が")]
        [TestCase("gi", "ぎ")]
        [TestCase("gu", "ぐ")]
        [TestCase("ge", "げ")]
        [TestCase("go", "ご")]
        [TestCase("gya", "ぎゃ")]
        [TestCase("gyi", "ぎぃ")]
        [TestCase("gyu", "ぎゅ")]
        [TestCase("gye", "ぎぇ")]
        [TestCase("gyo", "ぎょ")]
        [TestCase("gwa", "ぐぁ")]
        [TestCase("gwi", "ぐぃ")]
        [TestCase("gwu", "ぐぅ")]
        [TestCase("gwe", "ぐぇ")]
        [TestCase("gwo", "ぐぉ")]
        [TestCase("sa", "さ")]
        [TestCase("si", "し")]
        [TestCase("su", "す")]
        [TestCase("se", "せ")]
        [TestCase("so", "そ")]
        [TestCase("shi", "し")]
        [TestCase("za", "ざ")]
        [TestCase("zi", "じ")]
        [TestCase("zu", "ず")]
        [TestCase("ze", "ぜ")]
        [TestCase("zo", "ぞ")]
        [TestCase("ji", "じ")]
        [TestCase("sya", "しゃ")]
        [TestCase("syi", "しぃ")]
        [TestCase("syu", "しゅ")]
        [TestCase("sye", "しぇ")]
        [TestCase("syo", "しょ")]
        [TestCase("sha", "しゃ")]
        [TestCase("shu", "しゅ")]
        [TestCase("she", "しぇ")]
        [TestCase("sho", "しょ")]
        [TestCase("shya", "しゃ")]
        [TestCase("shyu", "しゅ")]
        [TestCase("shye", "しぇ")]
        [TestCase("shyo", "しょ")]
        [TestCase("swa", "すぁ")]
        [TestCase("swi", "すぃ")]
        [TestCase("swu", "すぅ")]
        [TestCase("swe", "すぇ")]
        [TestCase("swo", "すぉ")]
        [TestCase("zya", "じゃ")]
        [TestCase("zyi", "じぃ")]
        [TestCase("zyu", "じゅ")]
        [TestCase("zye", "じぇ")]
        [TestCase("zyo", "じょ")]
        [TestCase("ja", "じゃ")]
        [TestCase("ju", "じゅ")]
        [TestCase("je", "じぇ")]
        [TestCase("jo", "じょ")]
        [TestCase("jya", "じゃ")]
        [TestCase("jyi", "じぃ")]
        [TestCase("jyu", "じゅ")]
        [TestCase("jye", "じぇ")]
        [TestCase("jyo", "じょ")]
        [TestCase("ta", "た")]
        [TestCase("ti", "ち")]
        [TestCase("tu", "つ")]
        [TestCase("te", "て")]
        [TestCase("to", "と")]
        [TestCase("chi", "ち")]
        [TestCase("tsu", "つ")]
        [TestCase("ltu", "っ")]
        [TestCase("xtu", "っ")]
        [TestCase("ltsu", "っ")]
        [TestCase("tya", "ちゃ")]
        [TestCase("tyi", "ちぃ")]
        [TestCase("tyu", "ちゅ")]
        [TestCase("tye", "ちぇ")]
        [TestCase("tyo", "ちょ")]
        [TestCase("cha", "ちゃ")]
        [TestCase("chu", "ちゅ")]
        [TestCase("che", "ちぇ")]
        [TestCase("cho", "ちょ")]
        [TestCase("cya", "ちゃ")]
        [TestCase("cyi", "ちぃ")]
        [TestCase("cyu", "ちゅ")]
        [TestCase("cye", "ちぇ")]
        [TestCase("cyo", "ちょ")]
        [TestCase("chya", "ちゃ")]
        [TestCase("chyu", "ちゅ")]
        [TestCase("chye", "ちぇ")]
        [TestCase("chyo", "ちょ")]
        [TestCase("tsa", "つぁ")]
        [TestCase("tsi", "つぃ")]
        [TestCase("tse", "つぇ")]
        [TestCase("tso", "つぉ")]
        [TestCase("tha", "てゃ")]
        [TestCase("thi", "てぃ")]
        [TestCase("thu", "てゅ")]
        [TestCase("the", "てぇ")]
        [TestCase("tho", "てょ")]
        [TestCase("twa", "とぁ")]
        [TestCase("twi", "とぃ")]
        [TestCase("twu", "とぅ")]
        [TestCase("twe", "とぇ")]
        [TestCase("two", "とぉ")]
        [TestCase("da", "だ")]
        [TestCase("di", "ぢ")]
        [TestCase("du", "づ")]
        [TestCase("de", "で")]
        [TestCase("do", "ど")]
        [TestCase("dya", "ぢゃ")]
        [TestCase("dyi", "ぢぃ")]
        [TestCase("dyu", "ぢゅ")]
        [TestCase("dye", "ぢぇ")]
        [TestCase("dyo", "ぢょ")]
        [TestCase("dha", "でゃ")]
        [TestCase("dhi", "でぃ")]
        [TestCase("dhu", "でゅ")]
        [TestCase("dhe", "でぇ")]
        [TestCase("dho", "でょ")]
        [TestCase("dwa", "どぁ")]
        [TestCase("dwi", "どぃ")]
        [TestCase("dwu", "どぅ")]
        [TestCase("dwe", "どぇ")]
        [TestCase("dwo", "どぉ")]
        [TestCase("na", "な")]
        [TestCase("ni", "に")]
        [TestCase("nu", "ぬ")]
        [TestCase("ne", "ね")]
        [TestCase("no", "の")]
        [TestCase("nya", "にゃ")]
        [TestCase("nyi", "にぃ")]
        [TestCase("nyu", "にゅ")]
        [TestCase("nye", "にぇ")]
        [TestCase("nyo", "にょ")]
        [TestCase("ha", "は")]
        [TestCase("hi", "ひ")]
        [TestCase("hu", "ふ")]
        [TestCase("he", "へ")]
        [TestCase("ho", "ほ")]
        [TestCase("fu", "ふ")]
        [TestCase("hya", "ひゃ")]
        [TestCase("hyi", "ひぃ")]
        [TestCase("hyu", "ひゅ")]
        [TestCase("hye", "ひぇ")]
        [TestCase("hyo", "ひょ")]
        [TestCase("fya", "ふゃ")]
        [TestCase("fyu", "ふゅ")]
        [TestCase("fyo", "ふょ")]
        [TestCase("fwa", "ふぁ")]
        [TestCase("fwi", "ふぃ")]
        [TestCase("fwu", "ふぅ")]
        [TestCase("fwe", "ふぇ")]
        [TestCase("fwo", "ふぉ")]
        [TestCase("fa", "ふぁ")]
        [TestCase("fi", "ふぃ")]
        [TestCase("fe", "ふぇ")]
        [TestCase("fo", "ふぉ")]
        [TestCase("fyi", "ふぃ")]
        [TestCase("fye", "ふぇ")]
        [TestCase("ba", "ば")]
        [TestCase("bi", "び")]
        [TestCase("bu", "ぶ")]
        [TestCase("be", "べ")]
        [TestCase("bo", "ぼ")]
        [TestCase("bya", "びゃ")]
        [TestCase("byi", "びぃ")]
        [TestCase("byu", "びゅ")]
        [TestCase("bye", "びぇ")]
        [TestCase("byo", "びょ")]
        [TestCase("pa", "ぱ")]
        [TestCase("pi", "ぴ")]
        [TestCase("pu", "ぷ")]
        [TestCase("pe", "ぺ")]
        [TestCase("po", "ぽ")]
        [TestCase("pya", "ぴゃ")]
        [TestCase("pyi", "ぴぃ")]
        [TestCase("pyu", "ぴゅ")]
        [TestCase("pye", "ぴぇ")]
        [TestCase("pyo", "ぴょ")]
        [TestCase("ma", "ま")]
        [TestCase("mi", "み")]
        [TestCase("mu", "む")]
        [TestCase("me", "め")]
        [TestCase("mo", "も")]
        [TestCase("mya", "みゃ")]
        [TestCase("myi", "みぃ")]
        [TestCase("myu", "みゅ")]
        [TestCase("mye", "みぇ")]
        [TestCase("myo", "みょ")]
        [TestCase("ya", "や")]
        [TestCase("yu", "ゆ")]
        [TestCase("yo", "よ")]
        [TestCase("xya", "ゃ")]
        [TestCase("xyu", "ゅ")]
        [TestCase("xyo", "ょ")]
        [TestCase("ra", "ら")]
        [TestCase("ri", "り")]
        [TestCase("ru", "る")]
        [TestCase("re", "れ")]
        [TestCase("ro", "ろ")]
        [TestCase("rya", "りゃ")]
        [TestCase("ryi", "りぃ")]
        [TestCase("ryu", "りゅ")]
        [TestCase("rye", "りぇ")]
        [TestCase("ryo", "りょ")]
        [TestCase("wa", "わ")]
        [TestCase("wo", "を")]
        [TestCase("lwa", "ゎ")]
        [TestCase("xwa", "ゎ")]
        [TestCase("n", "ん")]
        [TestCase("nn", "んん")]
        [TestCase("xn", "ん")]
        // double consonants
        [TestCase("atta", "あった")]
        [TestCase("gakkounakatta", "がっこうなかった")]
        [TestCase("babba", "ばっば")]
        [TestCase("cacca", "かっか")]
        [TestCase("chaccha", "ちゃっちゃ")]
        [TestCase("dadda", "だっだ")]
        [TestCase("fuffu", "ふっふ")]
        [TestCase("gagga", "がっが")]
        [TestCase("hahha", "はっは")]
        [TestCase("jajja", "じゃっじゃ")]
        [TestCase("kakka", "かっか")]
        [TestCase("mamma", "まっま")]
        [TestCase("nanna", "なんな")]
        [TestCase("pappa", "ぱっぱ")]
        [TestCase("qaqqa", "くぁっくぁ")]
        [TestCase("rarra", "らっら")]
        [TestCase("sassa", "さっさ")]
        [TestCase("shassha", "しゃっしゃ")]
        [TestCase("tatta", "たった")]
        [TestCase("tsuttsu", "つっつ")]
        [TestCase("vavva", "ゔぁっゔぁ")]
        [TestCase("wawwa", "わっわ")]
        [TestCase("yayya", "やっや")]
        [TestCase("zazza", "ざっざ")]
        // other
        [TestCase("NLTU", "んっ")]

        public void ToHiragana_WhenPassedRomaji_ConvertsItToHiraganaCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("ヶ", "ヶ")]
        [TestCase("ヵ", "ヵ")]
        [TestCase("1", "1")]
        [TestCase("@", "@")]
        [TestCase("#", "#")]
        [TestCase("$", "$")]
        [TestCase("%", "%")]
        public void ToHiragana_WhenPassedCertainSymbols_ReturnsThemUnconverted(string input, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("!", "！")]
        [TestCase("?", "？")]
        [TestCase(".", "。")]
        [TestCase(":", "：")]
        [TestCase("/", "・")]
        [TestCase(",", "、")]
        [TestCase("~", "〜")]
        [TestCase("-", "ー")]
        [TestCase("‘", "「")]
        [TestCase("’", "」")]
        [TestCase("“", "『")]
        [TestCase("”", "』")]
        [TestCase("[", "［")]
        [TestCase("]", "］")]
        [TestCase("(", "（")]
        [TestCase(")", "）")]
        [TestCase("{", "｛")]
        [TestCase("}", "｝")]
        public void ToHiragana_WhenPassedPunctuation_ConvertsItCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("toukyou, オオサカ", "とうきょう、 おおさか")]
        [TestCase("#22 ２２漢字、toukyou, オオサカ", "#22 ２２漢字、とうきょう、 おおさか")]
        public void ToHiragana_WhenPassedMixedInput_ConvertsItCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("only カナ", true, "only かな")]
        [TestCase("only カナ", false, "おんly かな")]
        public void ToHiragana_WhenPassedRomajiFlagIsEngabled_OnlyConvertsKatakana(string input, bool passRomaji, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input, new DefaultOptions { PassRomaji = passRomaji });

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("wi", true, "ゐ")]
        [TestCase("we", true, "ゑ")]
        [TestCase("wi", false, "うぃ")]
        [TestCase("IROHANIHOHETO", true, "いろはにほへと")]
        [TestCase("CHIRINURUWO", true, "ちりぬるを")]
        [TestCase("WAKAYOTARESO", true, "わかよたれそ")]
        [TestCase("TSUNENARAMU", true, "つねならむ")]
        [TestCase("UWINOOKUYAMA", true, "うゐのおくやま")]
        [TestCase("KEFUKOETE", true, "けふこえて")]
        [TestCase("ASAKIYUMEMISHI", true, "あさきゆめみし")]
        [TestCase("WEHIMOSESU", true, "ゑひもせす")]
        public void ToHiragana_WhenObsoleteKanaFlagIsTrue_ConvertsObsoleteKanaCorrectly(string input, bool useObsoleteKana, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input, new DefaultOptions { UseObsoleteKana = useObsoleteKana });

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("only convert the katakana: ヒラガナ", true, "only convert the katakana: ひらがな")]
        [TestCase("only カナ", true, "only かな")]
        [TestCase("座禅‘zazen’スタイル", true, "座禅‘zazen’すたいる")]
        public void ToHiragana_WhenPassedPassRomajiSetToTrue_ReturnsRomajiUnconverted(string input, bool passRomaji, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input, new DefaultOptions { PassRomaji = passRomaji });

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("スーパー", "すうぱあ")]
        [TestCase("ラーメン", "らあめん")]
        [TestCase("バンゴー", "ばんごう")]
        [TestCase("バツゴー", "ばつごう")]
        [TestCase("ばつげーむ", "ばつげーむ")]
        [TestCase("てすート", "てすーと")]
        [TestCase("てすー戸", "てすー戸")]
        [TestCase("手巣ート", "手巣ーと")]
        [TestCase("tesート", "てsーと")]
        [TestCase("ートtesu", "ーとてす")]
        public void ToHiragana_WhenPassedALongVowelMark_ConvertsItCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("バケル", "ばける")]
        [TestCase("すたーいる", "すたーいる")]
        [TestCase("アメリカじん", "あめりかじん")]
        public void ToHiragana_WhenPassedKana_ConvertsItCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("aiueo", "AIUEO")]
        public void ToHiragana_WhenPassedLowerAndUpperCaseRomaji_ConvertsItTheSameWay(string lowercase, string uppercase)
        {
            string lowercaseResult = WanaKana.ToHiragana(lowercase);
            string uppercaseResult = WanaKana.ToHiragana(uppercase);

            Assert.AreEqual(lowercaseResult, uppercaseResult);
        }

        [TestCase("ラーメン", false, "らーめん")]
        public void ToHiragana_WhenPassedConvertLongVowelMarkSetToFalse_ReturnsCorrectOutput(string input, bool convertLongVowelMark, string expectedOutput)
        {
            string result = WanaKana.ToHiragana(input, new DefaultOptions {  ConvertLongVowelMark = convertLongVowelMark });

            Assert.AreEqual(result, expectedOutput);
        }
    }
}
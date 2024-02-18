using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class ToRomajiUnitTests
    {
        [TestCase("", "")]
        public void ToRomaji_WhenPassedAnEmptyString_ReturnsAnEmptyString(string input, string expectedOutput)
        {
            string result = WanaKana.ToRomaji(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("ヶ", "ヶ")]
        [TestCase("ヵ", "ヵ")]
        [TestCase("1", "1")]
        [TestCase("@", "@")]
        [TestCase("#", "#")]
        [TestCase("$", "$")]
        [TestCase("%", "%")]
        public void ToRomaji_WhenPassedCertainSymbols_ReturnsThemUnconverted(string input, string expectedOutput)
        {
            string result = WanaKana.ToRomaji(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("！", "！", "!")]
        [TestCase("？", "？", "?")]
        [TestCase("。", "。", ".")]
        [TestCase("：", "：", ":")]
        [TestCase("・", "・", "/")]
        [TestCase("、", "、", ",")]
        [TestCase("〜", "〜", "~")]
        [TestCase("ー", "ー", "-")]
        [TestCase("「", "「", "‘")]
        [TestCase("」", "」", "’")]
        [TestCase("『", "『", "“")]
        [TestCase("』", "』", "”")]
        [TestCase("［", "［", "[")]
        [TestCase("］", "］", "]")]
        [TestCase("（", "（", "(")]
        [TestCase("）", "）", ")")]
        [TestCase("｛", "｛", "{")]
        [TestCase("｝", "｝", "}")]
        public void ToRomaji_WhenPassedJapanesePunctuation_ConvertsItCorrectly(string inputHiragana, string inputKatakana, string expectedOutput)
        {
            string resultHiragana = WanaKana.ToRomaji(inputHiragana);
            string resultKatakana = WanaKana.ToRomaji(inputKatakana);

            Assert.AreEqual(resultHiragana, resultKatakana, expectedOutput);
        }

        [TestCase("か", "カ", "ka")]
        [TestCase("き", "キ", "ki")]
        [TestCase("く", "ク", "ku")]
        [TestCase("け", "ケ", "ke")]
        [TestCase("こ", "コ", "ko")]
        [TestCase("きゃ", "キャ", "kya")]
        [TestCase("きゅ", "キュ", "kyu")]
        [TestCase("きょ", "キョ", "kyo")]
        [TestCase("さ", "サ", "sa")]
        [TestCase("し", "シ", "shi")]
        [TestCase("す", "ス", "su")]
        [TestCase("せ", "セ", "se")]
        [TestCase("そ", "ソ", "so")]
        [TestCase("しゃ", "シャ", "sha")]
        [TestCase("しゅ", "シュ", "shu")]
        [TestCase("しょ", "ショ", "sho")]
        [TestCase("た", "タ", "ta")]
        [TestCase("ち", "チ", "chi")]
        [TestCase("つ", "ツ", "tsu")]
        [TestCase("て", "テ", "te")]
        [TestCase("と", "ト", "to")]
        [TestCase("ちゃ", "チャ", "cha")]
        [TestCase("ちゅ", "チュ", "chu")]
        [TestCase("ちょ", "チョ", "cho")]
        [TestCase("な", "ナ", "na")]
        [TestCase("に", "ニ", "ni")]
        [TestCase("ぬ", "ヌ", "nu")]
        [TestCase("ね", "ネ", "ne")]
        [TestCase("の", "ノ", "no")]
        [TestCase("にゃ", "ニャ", "nya")]
        [TestCase("にゅ", "ニュ", "nyu")]
        [TestCase("にょ", "ニョ", "nyo")]
        [TestCase("は", "ハ", "ha")]
        [TestCase("ひ", "ヒ", "hi")]
        [TestCase("ふ", "フ", "fu")]
        [TestCase("へ", "ヘ", "he")]
        [TestCase("ほ", "ホ", "ho")]
        [TestCase("ひゃ", "ヒャ", "hya")]
        [TestCase("ひゅ", "ヒュ", "hyu")]
        [TestCase("ひょ", "ヒョ", "hyo")]
        [TestCase("ま", "マ", "ma")]
        [TestCase("み", "ミ", "mi")]
        [TestCase("む", "ム", "mu")]
        [TestCase("め", "メ", "me")]
        [TestCase("も", "モ", "mo")]
        [TestCase("みゃ", "ミャ", "mya")]
        [TestCase("みゅ", "ミュ", "myu")]
        [TestCase("みょ", "ミョ", "myo")]
        [TestCase("ら", "ラ", "ra")]
        [TestCase("り", "リ", "ri")]
        [TestCase("る", "ル", "ru")]
        [TestCase("れ", "レ", "re")]
        [TestCase("ろ", "ロ", "ro")]
        [TestCase("りゃ", "リャ", "rya")]
        [TestCase("りゅ", "リュ", "ryu")]
        [TestCase("りょ", "リョ", "ryo")]
        [TestCase("や", "ヤ", "ya")]
        [TestCase("ゆ", "ユ", "yu")]
        [TestCase("よ", "ヨ", "yo")]
        [TestCase("わ", "ワ", "wa")]
        [TestCase("ゐ", "ヰ", "wi")]
        [TestCase("ゑ", "ヱ", "we")]
        [TestCase("を", "ヲ", "wo")]
        // dakuten
        [TestCase("が", "ガ", "ga")]
        [TestCase("ぎ", "ギ", "gi")]
        [TestCase("ぐ", "グ", "gu")]
        [TestCase("げ", "ゲ", "ge")]
        [TestCase("ご", "ゴ", "go")]
        [TestCase("ぎゃ", "ギャ", "gya")]
        [TestCase("ぎゅ", "ギュ", "gyu")]
        [TestCase("ぎょ", "ギョ", "gyo")]
        [TestCase("ざ", "ザ", "za")]
        [TestCase("じ", "ジ", "ji")]
        [TestCase("ず", "ズ", "zu")]
        [TestCase("ぜ", "ゼ", "ze")]
        [TestCase("ぞ", "ゾ", "zo")]
        [TestCase("じゃ", "ジャ", "ja")]
        [TestCase("じゅ", "ジュ", "ju")]
        [TestCase("じょ", "ジョ", "jo")]
        [TestCase("だ", "ダ", "da")]
        [TestCase("ぢ", "ヂ", "ji")]
        [TestCase("づ", "ヅ", "zu")]
        [TestCase("で", "デ", "de")]
        [TestCase("ど", "ド", "do")]
        [TestCase("ぢゃ", "ヂャ", "ja")]
        [TestCase("ぢゅ", "ヂュ", "ju")]
        [TestCase("ぢょ", "ヂョ", "jo")]
        [TestCase("ば", "バ", "ba")]
        [TestCase("び", "ビ", "bi")]
        [TestCase("ぶ", "ブ", "bu")]
        [TestCase("べ", "ベ", "be")]
        [TestCase("ぼ", "ボ", "bo")]
        [TestCase("びゃ", "ビャ", "bya")]
        [TestCase("びゅ", "ビュ", "byu")]
        [TestCase("びょ", "ビョ", "byo")]
        [TestCase("ぱ", "パ", "pa")]
        [TestCase("ぴ", "ピ", "pi")]
        [TestCase("ぷ", "プ", "pu")]
        [TestCase("ぺ", "ペ", "pe")]
        [TestCase("ぽ", "ポ", "po")]
        [TestCase("ぴゃ", "ピャ", "pya")]
        [TestCase("ぴゅ", "ピュ", "pyu")]
        [TestCase("ぴょ", "ピョ", "pyo")]
        // little kana
        [TestCase("ぁ", "ァ", "a")]
        [TestCase("ぃ", "ィ", "i")]
        [TestCase("ぅ", "ゥ", "u")]
        [TestCase("ぇ", "ェ", "e")]
        [TestCase("ぉ", "ォ", "o")]
        [TestCase("っ", "ッ", "")]
        [TestCase("ゃ", "ャ", "ya")]
        [TestCase("ゅ", "ュ", "yu")]
        [TestCase("ょ", "ョ", "yo")]
        // n
        [TestCase("ん", "ン", "n")]
        [TestCase("んん", "ンン", "nn")]
        [TestCase("あんない", "アンナイ", "annai")]
        [TestCase("ぐんま", "グンマ", "gunma")]
        // double consonants
        [TestCase("あった", "アッタ", "atta")]
        [TestCase("がっこうなかった", "ガッコウナカッタ", "gakkounakatta")]
        [TestCase("けっか", "ケッカ", "kekka")]
        [TestCase("さっさと", "サッサト", "sassato")]
        [TestCase("ずっと", "ズット", "zutto")]
        [TestCase("きっぷ", "キップ", "kippu")]
        [TestCase("ざっし", "ザッシ", "zasshi")]
        [TestCase("いっしょ", "イッショ", "issho")]
        [TestCase("こっち", "コッチ", "kotchi")]
        [TestCase("まっちゃ", "マッチャ", "matcha")]
        [TestCase("みっつ", "ミッツ", "mittsu")]
        [TestCase("ばっば", "バッバ", "babba")]
        [TestCase("かっか", "カッカ", "kakka")]
        [TestCase("ちゃっちゃ", "チャッチャ", "chatcha")]
        [TestCase("だっだ", "ダッダ", "dadda")]
        [TestCase("ふっふ", "フッフ", "fuffu")]
        [TestCase("がっが", "ガッガ", "gagga")]
        [TestCase("はっは", "ハッハ", "hahha")]
        [TestCase("じゃっじゃ", "ジャッジャ", "jajja")]
        [TestCase("かっか", "カッカ", "kakka")]
        [TestCase("まっま", "マッマ", "mamma")]
        [TestCase("なんな", "ナンナ", "nanna")]
        [TestCase("ぱっぱ", "パッパ", "pappa")]
        [TestCase("らっら", "ラッラ", "rarra")]
        [TestCase("さっさ", "サッサ", "sassa")]
        [TestCase("しゃっしゃ", "シャッシャ", "shassha")]
        [TestCase("たった", "タッタ", "tatta")]
        [TestCase("つっつ", "ツッツ", "tsuttsu")]
        [TestCase("わっわ", "ワッワ", "wawwa")]
        [TestCase("ざっざ", "ザッザ", "zazza")]
        public void ToRomaji_WhenPassedKana_ConvertsItCorrectlyToHepburnRomaji(string inputHiragana, string inputKatakana, string expectedOutput)
        {
            string resultHiragana = WanaKana.ToRomaji(inputHiragana);
            string resultKatakana = WanaKana.ToRomaji(inputKatakana);

            Assert.AreEqual(resultHiragana, resultKatakana, expectedOutput);
        }

        [TestCase("いろはにほへと", "イロハニホヘト", "irohanihoheto")]
        [TestCase("ちりぬるを", "チリヌルヲ", "chirinuruwo")]
        [TestCase("わかよたれそ", "ワカヨタレソ", "wakayotareso")]
        [TestCase("つねならむ", "ツネナラム", "tsunenaramu")]
        [TestCase("うゐのおくやま", "ウヰノオクヤマ", "uwinookuyama")]
        [TestCase("けふこえて", "ケフコエテ", "kefukoete")]
        [TestCase("あさきゆめみし", "アサキユメミシ", "asakiyumemishi")]
        [TestCase("ゑひもせすん", "ヱヒモセスン", "wehimosesun")]
        public void ToRomaji_WhenPassedKana_ReturnsItConvertedToLatinCharacters(string inputHiragana, string inputKatakana, string expectedOutput)
        {
            string resultHiragana = WanaKana.ToRomaji(inputHiragana);
            string resultKatakana = WanaKana.ToRomaji(inputKatakana);

            Assert.AreEqual(resultHiragana, resultKatakana, expectedOutput);
        }

        // hira long vowels
        [TestCase("がっこう", "gakkou")]
        [TestCase("とうきょう", "toukyou")]
        [TestCase("べんきょう", "benkyou")]
        [TestCase("でんぽう", "denpou")]
        [TestCase("きんようび", "kin'youbi")]
        [TestCase("こうし", "koushi")]
        // kata long vowels
        [TestCase("セーラー", "seeraa")]
        [TestCase("パーティー", "paateii")]
        [TestCase("ヒーター", "hiitaa")]
        [TestCase("タクシー", "takushii")]
        [TestCase("スーパーマン", "suupaaman")]
        [TestCase("バレーボール", "bareebooru")]
        [TestCase("ソール", "sooru")]
        //
        [TestCase("げーむ　ゲーム", "ge-mu geemu")]
        [TestCase("ばつげーむ", "batsuge-mu")]
        [TestCase("一抹げーむ", "一抹ge-mu")]
        [TestCase("缶コーヒー", "缶koohii")]
        public void ToRomaji_WhenPassedKanaWithLongVowels_ConvertsLongVowelsCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToRomaji(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("ひらがな　カタカナ", "hiragana katakana")]
        [TestCase("がっこうなかった", "gakkounakatta")]
        [TestCase("!!がっこうなかった!!", "!!gakkounakatta!!")]
        [TestCase("ったったった", "ttattatta")]
        [TestCase("っこ った っこ", "kko tta kko")]
        [TestCase("!!  っこ った っこ  !!", "!!  kko tta kko  !!")]
        [TestCase("ワニカニ　ガ　スゴイ　ダ", "wanikani ga sugoi da")]
        [TestCase("わにかに　が　すごい　だ", "wanikani ga sugoi da")]
        [TestCase("ワニカニ　が　すごい　だ", "wanikani ga sugoi da")]
        [TestCase("わにかにがすごいだ","wanikanigasugoida")]
        [TestCase("きんにくまん", "kinnikuman")]
        [TestCase("んんにんにんにゃんやん", "nnninninnyan'yan")]
        [TestCase("かっぱ　たった　しゅっしゅ ちゃっちゃ　やっつ", "kappa tatta shusshu chatcha yattsu")]
        [TestCase("っ", "")]
        [TestCase("ヶ", "ヶ")]
        [TestCase("ヵ", "ヵ")]
        [TestCase("ゃ", "ya")]
        [TestCase("ゅ", "yu")]
        [TestCase("ょ", "yo")]
        [TestCase("ぁ", "a")]
        [TestCase("ぃ", "i")]
        [TestCase("ぅ", "u")]
        [TestCase("ぇ", "e")]
        [TestCase("ぉ", "o")]
        [TestCase("おんよみ", "on'yomi")]
        [TestCase("んよ んあ んゆ", "n'yo n'a n'yu")]
        [TestCase("シンヨ", "shin'yo")]
        public void ToRomaji_WhenPassedInput_ConvertsItCorrectly(string input, string expectedOutput)
        {
            string result = WanaKana.ToRomaji(input);

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("ひらがな", true, "hiragana")]
        [TestCase(" !!ひら!!がな!!  ", true, " !!hira!!gana!!  ")]
        [TestCase("ひらがな ひらがな", true, "hiragana hiragana")]
        [TestCase("   ...   ひらがな@@@@@@@@@", true, "   ...   hiragana@@@@@@@@@")]
        [TestCase("カタカナ", true, "KATAKANA")]
        [TestCase("ひらがな カタカナ", true, "hiragana KATAKANA")]
        [TestCase("ソール", true, "SOORU")]
        [TestCase("ワニカニ", true, "WANIKANI")]
        [TestCase("ワニカニ　が　すごい　だ", true, "WANIKANI ga sugoi da")]
        public void ToRomaji_WhenPassedUpcaseKatakanaTrue_ReturnsUppercaseLatinCharacters(string input, bool upcaseKatakana, string expectedOutput)
        {
            string result = WanaKana.ToRomaji(input, new DefaultOptions { UpcaseKatakana = upcaseKatakana });

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("つじぎり", "tuzigili")]
        public void ToRomaji_WhenPassedACustomMap_ReturnsACorrectString(string input, string expectedOutput)
        {
            var map = new DefaultOptions { CustomRomajiMapping = new Dictionary<string, string> { { "じ", "zi" }, { "つ", "tu" }, { "り", "li" } } };
            string result = WanaKana.ToRomaji(input, map);

            Assert.AreEqual(result, expectedOutput);
        }
    }
}

namespace WanaKanaShaapu
{
    public static class Constants
    {
        public const char KanaMin = '\u3040';
        public const char KanaMax = '\u30ff';
        public const char PunctuationMin = '\u3000';
        public const char PunctuationMax = '\u303f';
        public const char Choonpu = 'ー';
        public const char KanjiMin = '\u4e00';
        public const char KanjiMax = '\u9fa0';
        public const char RomajiMin = '\uff20';
        public const char RomajiMax = '\uff50';
        public const char JapanesePunctuationMin = '\u3000';
        public const char JapanesePunctuationMax = '\u303f';
        public const char ZenzakuMin = '\uff00';
        public const char ZenzakuMax = '\uffef';
        public const string EnglishVowels = "aeiouAEIOU";
        public const string SokuonHiragana = "っ";
        public const string SokuonKatakana = "ッ";
        public const char ProlongedSoundMark = '\u30fc';
        public const char KanaSlashDot = '\u30fb';
        public const int HiraKataSwitch = 0x60;

        readonly public static CharacterRange HiraganaChars = new CharacterRange('\u3040', '\u309f');
        readonly public static CharacterRange KatakanaChars = new CharacterRange('\u30a0', '\u30ff');
        readonly public static CharacterRange HankakuKatakana = new CharacterRange('\uff66', '\uff9f');
        readonly public static CharacterRange KanjiChars = new CharacterRange('\u4e00', '\u9fa0');
        readonly public static CharacterRange ZenkakuNumbers = new CharacterRange('\uff10', '\uff19');
        readonly public static CharacterRange ZenkakuUppercaseChars = new CharacterRange('\uff21', '\uff3a');
        readonly public static CharacterRange ZenkakuLowercaseChars = new CharacterRange('\uff41', '\uff5a');
        readonly public static CharacterRange ZenkakuCurrencyChars = new CharacterRange('\uffe0', '\uffee');
        readonly public static CharacterRange CJKPunctuationChars = new CharacterRange('\u3000', '\u303f');
        readonly public static CharacterRange RomajiChars = new CharacterRange('\u0000', '\u007f');
        readonly public static CharacterRange ZenkakuPunct1 = new CharacterRange('\uff01', '\uff0f');
        readonly public static CharacterRange ZenkakuPunct2 = new CharacterRange('\uff1a', '\uff1f');
        readonly public static CharacterRange ZenkakuPunct3 = new CharacterRange('\uff3b', '\uff3f');
        readonly public static CharacterRange ZenkakuPunct4 = new CharacterRange('\uff5b', '\uff60');
        readonly public static CharacterRange KanaPunctChars = new CharacterRange('\uff61', '\uff65');
        readonly public static CharacterRange KatakanaPunctChars = new CharacterRange('\u30fb', '\u30fc');

        readonly public static CharacterRange[] JapanesePunctuationRanges = new CharacterRange[]
        {
            ZenkakuPunct1,
            ZenkakuPunct2,
            ZenkakuPunct3,
            ZenkakuPunct4,
            ZenkakuCurrencyChars,
            CJKPunctuationChars,
            KanaPunctChars,
            KatakanaPunctChars
        };

        readonly public static CharacterRange[] MacronCharacterRanges = new CharacterRange[]
        {
            new('\u0100', '\u0101'),
            new('\u0112', '\u0113'),
            new('\u012a', '\u012b'),
            new('\u014C', '\u014D'),
            new('\u016A', '\u016B')
        };

        readonly public static CharacterRange[] KanaCharacterRanges = new CharacterRange[]
        {
            HiraganaChars,
            KatakanaChars
        };

        readonly public static CharacterRange[] JapaneseCharacterRanges = new CharacterRange[]
        {
            HiraganaChars,
            KatakanaChars,
            HankakuKatakana,
            KanjiChars,
            ZenkakuNumbers,
            ZenkakuUppercaseChars,
            ZenkakuLowercaseChars,
            ZenkakuCurrencyChars,
            ZenkakuPunct1,
            ZenkakuPunct2,
            ZenkakuPunct3,
            ZenkakuPunct4,
            CJKPunctuationChars,
            KanaPunctChars,
            KatakanaPunctChars
        };

        public static (string Kana, string Transliteration)[] BasicRomaji = new (string Kana, string Transliteration)[]
        {
            ("あ","a"),     ("い", "i"),   ("う", "u"),   ("え", "e"),   ("お", "o"),
            ("か", "ka"),   ("き", "ki"),  ("く", "ku"),  ("け", "ke"),  ("こ", "ko"),
            ("さ", "sa"),   ("し", "shi"), ("す", "su"),  ("せ", "se"),  ("そ", "so"),
            ("た", "ta"),   ("ち", "chi"), ("つ", "tsu"), ("て", "te"),  ("と", "to"),
            ("な", "na"),   ("に", "ni"),  ("ぬ", "nu"),  ("ね", "ne"),  ("の", "no"),
            ("は", "ha"),   ("ひ", "hi"),  ("ふ", "fu"),  ("へ", "he"),  ("ほ", "ho"),
            ("ま", "ma"),   ("み", "mi"),  ("む", "mu"),  ("め", "me"),  ("も", "mo"),
            ("ら", "ra"),   ("り", "ri"),  ("る", "ru"),  ("れ", "re"),  ("ろ", "ro"),
            ("や", "ya"),   ("ゆ", "yu"),  ("よ", "yo"),
            ("わ", "wa"),   ("ゐ", "wi"),  ("ゑ", "we"),  ("を", "wo"),
            ("ん", "n"),
            ("が", "ga"),   ("ぎ", "gi"),  ("ぐ", "gu"),  ("げ", "ge"),  ("ご", "go"),
            ("ざ", "za"),   ("じ", "ji"),  ("ず", "zu"),  ("ぜ", "ze"),  ("ぞ", "zo"),
            ("だ", "da"),   ("ぢ", "ji"),  ("づ", "zu"),  ("で", "de"),  ("ど", "do"),
            ("ば", "ba"),   ("び", "bi"),  ("ぶ", "bu"),  ("べ", "be"),  ("ぼ", "bo"),
            ("ぱ", "pa"),   ("ぴ", "pi"),  ("ぷ", "pu"),  ("ぺ", "pe"),  ("ぽ", "po"),
            ("ゔぁ", "va"), ("ゔぃ", "vi"), ("ゔ", "vu"),  ("ゔぇ", "ve"), ("ゔぉ", "vo")
        };

        public static (string JSymbol, string Symbol)[] SpecialSymbolsJpRomaji = new (string JSymbol, string Symbol)[]
        {
            ("。", "."),
            ("、", ","),
            ("：", ":"),
            ("・", "/"),
            ("！", "!"),
            ("？", "?"),
            ("〜", "~"),
            ("ー", "-"),
            ("「", "‘"),
            ("」", "’"),
            ("『", "“"),
            ("』", "”"),
            ("［", "["),
            ("］", "]"),
            ("（", "("),
            ("）", ")"),
            ("｛", "{"),
            ("｝", "}"),
            ("　", " "),
        };

        public static char[] AmbiguousVowels = { 'あ', 'い', 'う', 'え', 'お', 'や', 'ゆ', 'よ' };

        public static (string Kana, string Transliteration)[] SmallYJpRomaji = new (string Kana, string Transliteration)[]
        {
            ( "ゃ", "ya"), ("ゅ", "yu"), ("ょ", "yo")
        };

        public static (string Kana, string Transliteration)[] smallYExtra = new (string Kana, string Transliteration)[]
        {
            ("ぃ", "yi"), ("ぇ", "ye")
        };

        public static (string Kana, string Transliteration)[] smallAIUEO = new (string Kana, string Transliteration)[]
        {
            ("ぁ", "a"),
            ("ぃ", "i"),
            ("ぅ", "u"),
            ("ぇ", "e"),
            ("ぉ", "o"),
        };

        public static string[] yoonKana =
        {
            "き",
            "に",
            "ひ",
            "み",
            "り",
            "ぎ",
            "び",
            "ぴ",
            "ゔ",
            "く",
            "ふ",
        };

        public static (string Kana, string Transliteration)[] yoonExceptions = new (string Kana, string Transliteration)[]
        {
            ("し", "sh"),
            ("ち", "ch"),
            ("じ", "j"),
            ("ぢ", "j"),
        };

        public static (string Kana, string Transliteration)[] smallKana = new (string Kana, string Transliteration)[]
        {
            ("っ", ""),
            ("ゃ", "ya"),
            ("ゅ", "yu"),
            ("ょ", "yo"),
            ("ぁ", "a"),
            ("ぃ", "i"),
            ("ぅ", "u"),
            ("ぇ", "e"),
            ("ぉ", "o"),
        };

        public static string[] KanaAsSymbol =
        {
            "ヶ",
            "ヵ"
        };

        public static Dictionary<char, string> SokuonWhitelist = new()
        {
            { 'b', "b" },
            { 'c', "t" },
            { 'd', "d" },
            { 'f', "f" },
            { 'g', "g" },
            { 'h', "h" },
            { 'j', "j" },
            { 'k', "k" },
            { 'm', "m" },
            { 'p', "p" },
            { 'q', "q" },
            { 'r', "r" },
            { 's', "s" },
            { 't', "t" },
            { 'v', "v" },
            { 'w', "w" },
            { 'x', "x" },
            { 'z', "z" }
        };

        public static Dictionary<string, (string Romaji, string Kana)[]> BasicKunrei = new()
        {
            { "a", new (string Romaji, string Kana)[] { ("", "あ") } },
            { "i", new (string Romaji, string Kana)[] { ("", "い") } },
            { "u", new (string Romaji, string Kana)[] { ("", "う") } },
            { "e", new (string Romaji, string Kana)[] { ("", "え") } },
            { "o", new (string Romaji, string Kana)[] { ("", "お") } },
            { "k", new (string Romaji, string Kana)[] { ("a", "か"), ("i", "き"), ("u", "く"), ("e", "け"), ("o", "こ") } },
            { "s", new (string Romaji, string Kana)[] { ("a", "さ"), ("i", "し"), ("u", "す"), ("e", "せ"), ("o", "そ") } },
            { "t", new (string Romaji, string Kana)[] { ("a", "た"), ("i", "ち"), ("u", "つ"), ("e", "て"), ("o", "と") } },
            { "n", new (string Romaji, string Kana)[] { ("a", "な"), ("i", "に"), ("u", "ぬ"), ("e", "ね"), ("o", "の") } },
            { "h", new (string Romaji, string Kana)[] { ("a", "は"), ("i", "ひ"), ("u", "ふ"), ("e", "へ"), ("o", "ほ") } },
            { "m", new (string Romaji, string Kana)[] { ("a", "ま"), ("i", "み"), ("u", "む"), ("e", "め"), ("o", "も") } },
            { "y", new (string Romaji, string Kana)[] { ("a", "や"), ("u", "ゆ"), ("o", "よ") } },
            { "r", new (string Romaji, string Kana)[] { ("a", "ら"), ("i", "り"), ("u", "る"), ("e", "れ"), ("o", "ろ") } },
            { "w", new (string Romaji, string Kana)[] { ("a", "わ"), ("i", "ゐ"), ("e", "ゑ"), ("o", "を"), } },
            { "g", new (string Romaji, string Kana)[] { ("a", "が"), ("i", "ぎ"), ("u", "ぐ"), ("e", "げ"), ("o", "ご") } },
            { "z", new (string Romaji, string Kana)[] { ("a", "ざ"), ("i", "じ"), ("u", "ず"), ("e", "ぜ"), ("o", "ぞ") } },
            { "d", new (string Romaji, string Kana)[] { ("a", "だ"), ("i", "ぢ"), ("u", "づ"), ("e", "で"), ("o", "ど") } },
            { "b", new (string Romaji, string Kana)[] { ("a", "ば"), ("i", "び"), ("u", "ぶ"), ("e", "べ"), ("o", "ぼ") } },
            { "p", new (string Romaji, string Kana)[] { ("a", "ぱ"), ("i", "ぴ"), ("u", "ぷ"), ("e", "ぺ"), ("o", "ぽ") } },
            { "v", new (string Romaji, string Kana)[] { ("a", "ゔぁ"), ("i", "ゔぃ"), ("u", "ゔ"), ("e", "ゔぇ"), ("o", "ゔぉ") } }
        };

        public static (string Symbol, string JSymbol)[] SpecialSymbolsRomajiJp = new (string Symbol, string JSymbol)[]
        {
            (".", "。"),
            (",", "、"),
            (":", "："),
            ("/", "・"),
            ("!", "！"),
            ("?", "？"),
            ("~", "〜"),
            ("-", "ー"),
            ("‘", "「"),
            ("’", "」"),
            ("“", "『"),
            ("”", "』"),
            ("[", "［"),
            ("]", "］"),
            ("(", "（"),
            (")", "）"),
            ("{", "｛"),
            ("}", "｝")
        };

        public static (string Romaji, string Kana)[] Consonants = new (string Romaji, string Kana)[]
        {
            ("k", "き"),
            ("s", "し"),
            ("t", "ち"),
            ("n", "に"),
            ("h", "ひ"),
            ("m", "み"),
            ("r", "り"),
            ("g", "ぎ"),
            ("z", "じ"),
            ("d", "ぢ"),
            ("b", "び"),
            ("p", "ぴ"),
            ("v", "ゔ"),
            ("q", "く"),
            ("f", "ふ"),
        };

        public static (string Romaji, string Kana)[] SmallYRomajiJp = new (string Romaji, string Kana)[]
        {
            ("ya", "ゃ"),
            ("yi", "ぃ"),
            ("yu", "ゅ"),
            ("ye", "ぇ"),
            ("yo", "ょ"),
        };

        public static (string Romaji, string Kana)[] SmallVowels = new (string Romaji, string Kana)[]
        {
            ("a", "ぁ"),
            ("i", "ぃ"),
            ("u", "ぅ"),
            ("e", "ぇ"),
            ("o", "ぉ"),
        };

        // typing one should be the same as having typed the other instead
        public static (string Alias, string Alternative)[] Aliases = new (string Alias, string Alternative)[]
        {
            ("sh", "sy"), // sha -> sya
            ("ch", "ty"), // cho -> tyo
            ("cy", "ty"), // cyo -> tyo
            ("chy", "ty"), // chyu -> tyu
            ("shy", "sy"), // shya -> sya
            ("j", "zy"), // ja -> zya
            ("jy", "zy"), // jye -> zye

            // exceptions to above rules
            ("shi", "si"),
            ("chi", "ti"),
            ("tsu", "tu"),
            ("ji", "zi"),
            ("fu", "hu"),
        };

        public static (string Romaji, string Kana)[] SmallLetters
        {
            get
            {
                var list = new List<(string Romaji, string Kana)>
                {
                    ("tu", "っ"),
                    ("tsu", "っ"),
                    ("wa", "ゎ"),
                    ("ka", "ヵ"),
                    ("ke", "ヶ"),
                    ("ca", "ヵ"),
                    ("ce", "ヶ"),
                };

                list.AddRange(SmallVowels);
                list.AddRange(SmallYRomajiJp);

                return list.ToArray();
            }
        }

        // don't follow any notable patterns
        public static (string Romaji, string Kana)[] SpecialCases = new (string Romaji, string Kana)[]
        {
            ("yi", "い"),
            ("wu", "う"),
            ("ye", "いぇ"),
            ("wi", "うぃ"),
            ("we", "うぇ"),
            ("kwa", "くぁ"),
            ("whu", "う"),
            // because it's not thya for てゃ but tha
            // and tha is not てぁ, but てゃ
            ("tha", "てゃ"),
            ("thu", "てゅ"),
            ("tho", "てょ"),
            ("dha", "でゃ"),
            ("dhu", "でゅ"),
            ("dho", "でょ"),
        };

        public static (string Romaji, string Kana)[] AIUEOConstructions = new (string Romaji, string Kana)[]
        {
            ("wh", "う"),
            ("kw", "く"),
            ("qw", "く"),
            ("q", "く"),
            ("gw", "ぐ"),
            ("sw", "す"),
            ("ts", "つ"),
            ("th", "て"),
            ("tw", "と"),
            ("dh", "で"),
            ("dw", "ど"),
            ("fw", "ふ"),
            ("f", "ふ"),
        };

        public static (string Romaji, string Kana)[] ObsoleteKana = new (string Romaji, string Kana)[]
        {
            ("wi", "ゐ"),
            ("we", "ゑ"),
        };
    }
}
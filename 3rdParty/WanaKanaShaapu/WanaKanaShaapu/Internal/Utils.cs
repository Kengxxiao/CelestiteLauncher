using System.Runtime.InteropServices;

namespace WanaKanaShaapu.Internal
{
    internal static class Utils
    {
        internal static bool IsMacron(char c)
        {
            foreach (var range in Constants.MacronCharacterRanges)
            {
                if (range.IsCharacterWithinRange(c))
                    return true;
            }
            return false;
        }

        internal static bool IsSokuon(string input)
        {
            if (input == Constants.SokuonHiragana || input == Constants.SokuonKatakana)
                return true;

            if (input.Length == 2 && !WanaKana.IsJapanese(input))
            {
                if (input.First() == input.Last())
                    return true;
            }
            return false;
        }

        internal static string ConvertChoonpu(string input, int i, [Optional] DefaultOptions options, bool isDestinationRomaji)
        {
            string beforeChoonpu = input[i - 1].ToString();
            if (!options.ConvertLongVowelMark
                || WanaKana.IsRomaji(beforeChoonpu)
                || WanaKana.IsKanji(beforeChoonpu))
                return "ー";
            else
            {
                var syllable = TreeConstants.KanaToHepburnTree[beforeChoonpu].Data;
                var longVowel = syllable.Last().ToString();
                var longVowelConverted = TreeConstants.RomajiToKanaTree[longVowel].Data;
                if (!isDestinationRomaji)
                    return longVowelConverted == "お" ? "う" : longVowelConverted;
                return longVowel;
            }
        }

        internal static string KatakanaToHiragana(string input, [Optional] DefaultOptions options, [Optional] bool isDestinationRomaji)
        {
            string result = string.Empty;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                bool isPreviousCharHiragana = i > 0 && WanaKana.IsHiragana(input[i - 1].ToString());
                bool isCharInitialLongDash = i == 0;
                if (c == Constants.Choonpu && !isPreviousCharHiragana
                    && !isCharInitialLongDash)
                    result += ConvertChoonpu(result, i, options, isDestinationRomaji);
                else if (WanaKana.IsKatakana(c.ToString())
                    && c != Constants.Choonpu
                    && !Constants.KanaAsSymbol.Contains(c.ToString()))
                    result += (char)(c - Constants.HiraKataSwitch);
                else
                    result += c;
            }
            return result;
        }

        internal static string HiraganaToKatakana(string input, DefaultOptions options)
        {
            string result = string.Empty;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (WanaKana.IsHiragana(c.ToString()) && !isCharLongDash(c)
                    && !isCharSlashDot(c))
                    result += (char)(c + 0x60);
                else
                    result += c;
            }
            return result;
        }

        internal static string GetTokenTypeCompact(char c)
        {
            string letter = c.ToString();
            if (WanaKana.IsJapanese(letter) && (Char.IsLetter(c) || Char.IsWhiteSpace(c)))
                return "ja";
            else if (Char.IsAscii(c) && (Char.IsLetter(c) || Char.IsWhiteSpace(c)))
                return "en";
            else
                return "other";
        }

        internal static string GetTokenType(char c, string previousType, bool compact)
        {
            string letter = c.ToString();
            if (compact)
                return GetTokenTypeCompact(c);
            if (c == Constants.Choonpu)
                return previousType == "katakana" ? "katakana" : "hiragana";
            if (WanaKana.IsKanji(letter))
                return "kanji";
            else if (WanaKana.IsHiragana(letter))
                return "hiragana";
            else if (WanaKana.IsKatakana(letter))
                return "katakana";
            else if (Char.IsWhiteSpace(c))
                return "space";
            else if (Constants.ZenkakuNumbers.IsCharacterWithinRange(c))
                return "japaneseNumeral";
            else if (WanaKana.IsJapanese(letter) && Char.IsPunctuation(c))
                return "japanesePunctuation";
            else if (WanaKana.IsJapanese(letter))
                return "ja";
            else if (Char.IsAscii(c) && Char.IsLetter(c))
                return "en";
            else if (Char.IsDigit(c) && Char.IsAscii(c))
                return "englishNumeral";
            else if (Char.IsPunctuation(c))
                return "englishPunctuation";
            else
                return "other";
        }

        internal static Dictionary<string, Node> AddObsoleteKana()
        {
            var treeCopy = TreeBuilder.BuildRomajiToKanaTree();
            foreach (var kana in Constants.ObsoleteKana)
                treeCopy[kana.Romaji.First().ToString()].Children[kana.Romaji.Last().ToString()].Data = kana.Kana;
            return treeCopy;
        }

        internal static Dictionary<string, Node> CreateCustomTree(DefaultOptions options)
        {
            var treeCopy = TreeBuilder.BuildRomajiToKanaTree();
            foreach (var pair in options.CustomKanaMapping)
                ChangeNodeData(treeCopy, pair.Key, pair.Value);
            return treeCopy;
        }

        internal static void ChangeNodeData(Dictionary<string, Node> tree, string key, string value)
        {
            Node node = tree[key.First().ToString()];
            if (key.Length == 1 || !node.Children.Any())
                node.Data = value;
            else if (node.Children.ContainsKey(key[1].ToString()))
                ChangeNodeData(tree[key.First().ToString()].Children, key[1..], value);
        }

        internal static List<string> SliceInput(string input)
        {
            List<string> inputArray = new List<string>();
            string temp = string.Empty;
            for (int i = 1; i < input.Length; i++)
            {
                if ((char.IsLower(input[i]) && char.IsLower(input[i - 1]))
                    || (char.IsUpper(input[i]) && char.IsUpper(input[i - 1])) || char.IsWhiteSpace(input[i - 1]))
                    temp += input[i - 1];
                else
                {
                    temp += input[i - 1];
                    //ignore standalone uppercase consonant chars for they are supposed to be converted to hiragana
                    if (temp.Length == 1 && !Constants.EnglishVowels.Contains(temp))
                    {
                        temp = temp.ToLower();
                        continue;
                    }
                    inputArray.Add(temp);
                    temp = string.Empty;
                }
            }
            temp += input.Last();
            inputArray.Add(temp);
            return inputArray;
        }

        internal static bool isCharLongDash(char c)
        {
            if (c == Constants.ProlongedSoundMark)
                return true;
            return false;
        }
        internal static bool isCharSlashDot(char c)
        {
            if (c == Constants.KanaSlashDot)
                return true;
            return false;
        }

        internal static string OkuriganaToStrip(string input, [Optional] bool leading)
        {
            string result = string.Empty;

            if (leading)
            {
                foreach (char c in input)
                {
                    if (!WanaKana.IsKanji(c.ToString()))
                        result += c;
                    else
                        return "^" + result;
                }
            }
            for (int i = input.Length - 1; i >= 0; i--)
            {
                char c = input[i];
                if (!WanaKana.IsKanji(c.ToString()))
                    result += c;
                else
                    return result + "$";
            }
            return result;
        }
    }
}
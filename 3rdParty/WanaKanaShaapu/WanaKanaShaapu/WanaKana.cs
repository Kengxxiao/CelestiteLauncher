using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WanaKanaShaapu.Internal;

namespace WanaKanaShaapu
{
    public static class WanaKana
    {
        /// <summary>
        /// Tests if <c>input</c> is hiragana.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <returns><c>true</c> if the input is entirely hiragana. Otherwise <c>false</c>.</returns>
        public static bool IsHiragana(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            foreach (char c in input)
            {
                if (Constants.HiraganaChars.IsCharacterWithinRange(c) || c == Constants.Choonpu)
                    continue;
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tests if <c>input</c> only includes kanji, kana, zenkaku numbers, or ja punctuation/symbols.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <param name="allowed">The regex expression to pass specified chars.</param>
        /// <returns><c>true</c> if the input is entirely Japanese. Otherwise <c>false</c>.</returns>
        public static bool IsJapanese(string input, [Optional] string allowed)
        {
            if (allowed != null)
                input = Regex.Replace(input, allowed, string.Empty);

            foreach (char c in input)
            {
                var isInRange = Constants.JapaneseCharacterRanges.Any(ranges => ranges.IsCharacterWithinRange(c));

                if (isInRange)
                    continue;
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tests if <c>input</c> is kana (katakana and/or hiragana).
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <returns><c>true</c> if the input is entirely kana. Otherwise <c>false</c>.</returns>
        public static bool IsKana(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            foreach (char c in input)
            {
                if (IsHiragana(c.ToString()) || IsKatakana(c.ToString()))
                    continue;
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tests if <c>input</c> is kanji.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <returns><c>true</c> if the input is entirely kanji. Otherwise <c>false</c>.</returns>
        public static bool IsKanji(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            foreach (char c in input)
            {
                if (Constants.KanjiChars.IsCharacterWithinRange(c))
                    continue;
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tests if <c>input</c> is katakana.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <returns><c>true</c> if the input is entirely katakana. Otherwise <c>false</c>.</returns>
        public static bool IsKatakana(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            foreach (char c in input)
            {
                if (Constants.KatakanaChars.IsCharacterWithinRange(c))
                    continue;
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Tests if <c>input</c> contains a mix of romaji and kana, defaults to pass through kanji.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <param name="passKanji">The optional config to pass through kanji.</param>
        /// <returns><c>true</c> if the input is mixed. Otherwise <c>false</c>.</returns>
        public static bool IsMixed(string input, [Optional] bool? passKanji)
        {
            bool hasKanji = false;

            if (passKanji.HasValue && !passKanji.Value)
                hasKanji = input.Any(chars => IsKanji(chars.ToString()));

            return input.Any(chars => IsHiragana(chars.ToString())
                || input.Any(chars => IsKatakana(chars.ToString())))
                && input.Any(chars => IsRomaji(chars.ToString()))
                && !hasKanji;
        }

        /// <summary>
        /// Tests if <c>input</c> is romaji.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <param name="allowed">The regex expression to pass specified chars.</param>
        /// <returns><c>true</c> if the input is entirely romaji. Otherwise <c>false</c>.</returns>
        public static bool IsRomaji(string input, [Optional] string allowed)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            if (allowed != null)
                input = Regex.Replace(input, allowed, string.Empty);

            foreach (char c in input)
            {
                if (Constants.RomajiChars.IsCharacterWithinRange(c) || Utils.IsMacron(c))
                    continue;
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Strips okurigana.
        /// </summary>
        /// <param name="input">The string to strip from.</param>
        /// <param name="leading">The optional configuration allows to strip the leading okurigana.</param>
        /// <param name="matchKanji">The optional configuration matches the input to kanji.</param>
        /// <returns>The input stripped from okurigana.</returns>
        public static string StripOkurigana(string input, [Optional] bool leading, [Optional] string matchKanji)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            bool isLeadingWithoutInitialKana = !IsKana(input.First().ToString()) && leading;
            bool isTrailingWithoutFinalKana = !IsKana(input.Last().ToString()) && !leading;
            bool isInvalidMatcher = (!string.IsNullOrEmpty(matchKanji)
                && !matchKanji.Any(chars => IsKanji(chars.ToString())))
                || (string.IsNullOrEmpty(matchKanji) && IsKana(input));

            if (!IsJapanese(input) || isLeadingWithoutInitialKana
                || isTrailingWithoutFinalKana
                || isInvalidMatcher)
                return input;

            string okuriganaRegex = !string.IsNullOrEmpty(matchKanji)
                ? Utils.OkuriganaToStrip(matchKanji, leading)
                : Utils.OkuriganaToStrip(input, leading);

            return Regex.Replace(input, okuriganaRegex, string.Empty);
        }

        /// <summary>
        /// Converts input to hiragana.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="options">Default config for WanaKana.</param>
        /// <returns>The string converted to hiragana.</returns>
        public static string ToHiragana(string input, [Optional] DefaultOptions options)
        {
            if (options is null)
                options = new DefaultOptions();
            if (options.PassRomaji)
                return Utils.KatakanaToHiragana(input, options);
            else if (IsMixed(input))
            {
                string convertedKatakana = Utils.KatakanaToHiragana(input, options);
                return ToKana(convertedKatakana.ToLower(), options);
            }
            else if (IsRomaji(input) || input.Any(chars => Char.IsPunctuation(chars)))
                return ToKana(input.ToLower(), options);

            return Utils.KatakanaToHiragana(input, options);
        }

        /// <summary>
        /// Converts romaji to kana, lowercase text will result in hiragana and uppercase text will result in katakana.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="options">Default config for WanaKana.</param>
        /// <returns>The string converted to kana.</returns>
        public static string ToKana(string input, [Optional] DefaultOptions options)
        {
            string convertedString = string.Empty;
            var tree = TreeConstants.RomajiToKanaTree;
            if (options is null)
                options = new DefaultOptions();
            if (options.CustomKanaMapping.Count != 0)
                tree = Utils.CreateCustomTree(options);
            if (options.UseObsoleteKana)
                tree = Utils.AddObsoleteKana();
            if (input.Any(char.IsUpper))
            {
                var slicedInput = Utils.SliceInput(input);
                foreach (string slice in slicedInput)
                {
                    if (slice.Any(char.IsUpper))
                        convertedString += ToKatakana(TreeTraverser.TraverseTree(convertedString, slice.ToLower(), tree, tree, options));
                    else
                        convertedString += TreeTraverser.TraverseTree(convertedString, slice, tree, tree, options);
                }
                return convertedString;
            }

            return TreeTraverser.TraverseTree(convertedString, input, tree, tree, options);
        }

        /// <summary>
        /// Converts input to katakana.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="options">Default config for WanaKana.</param>
        /// <returns>The string converted to hiragana.</returns>
        public static string ToKatakana(string input, [Optional] DefaultOptions options)
        {
            if (options is null)
                options = new DefaultOptions();
            if (options.PassRomaji)
                return Utils.HiraganaToKatakana(input, options);
            else if (IsMixed(input) || IsRomaji(input)
                || input.Any(chars => Char.IsPunctuation(chars)))
            {
                string convertedHiragana = ToKana(input.ToLower(), options);
                return Utils.HiraganaToKatakana(convertedHiragana, options);
            }

            return Utils.HiraganaToKatakana(input, options);
        }

        /// <summary>
        /// Splits <c>input</c> into array of strings separated by opinionated token types 
        /// <c>'en', 'ja', 'englishNumeral', 'japaneseNumeral','englishPunctuation', 'japanesePunctuation','kanji', 'hiragana', 'katakana', 'space', 'other'</c>.  
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="compact">If <c>true</c> then many same-language tokens are combined (spaces + text, kanji + kana, numeral + punctuation).</param>
        /// <returns>The Tokenization object with an array of token values and token array containing {type, value}.</returns>
        public static Tokenization Tokenize(string input, [Optional] bool compact)
        {
            string currentType = string.Empty;
            string previousType = string.Empty;
            string token = string.Empty;

            Tokenization tokenization = new Tokenization();
            if (string.IsNullOrEmpty(input))
                return tokenization;
            foreach (char c in input)
            {
                currentType = Utils.GetTokenType(c, previousType, compact);
                if (string.IsNullOrEmpty(previousType) || currentType == previousType)
                    token += c;
                else
                {
                    if (compact && Char.IsWhiteSpace(c) && (previousType == "en" || previousType == "ja"))
                    {
                        token += c;
                        continue;
                    }
                    tokenization.Tokens.Add(new Token(previousType, token));
                    token = string.Empty;
                    token += c;
                }
                previousType = currentType;
            }
            tokenization.Tokens.Add(new Token(currentType, token));
            return tokenization;
        }

        private static ConcurrentDictionary<string, string> _romajiCaches = [];

        /// <summary>
        /// Convert kana to romaji.
        /// </summary>
        /// <param name="kana">The kana string to convert.</param>
        /// <param name="options">Default config for WanaKana.</param>
        /// <returns>The string converted to romaji.</returns>
        public static string ToRomaji(string kana, [Optional] DefaultOptions options)
        {
            if (string.IsNullOrEmpty((kana))) return string.Empty;
            if (_romajiCaches.TryGetValue(kana, out var romajiCache))
                return romajiCache;
            if (options is null)
                options = DefaultOptions.Default;
            if (options.Romanization != "hepburn")
                return kana;
            string result = string.Empty;
            string convertedString = string.Empty;
            bool katakanaToUpper = options != null && options.UpcaseKatakana;
            var tree = TreeConstants.KanaToHepburnTree;
            if (katakanaToUpper)
            {
                Tokenization kanaTokens = Tokenize(kana);
                foreach (var token in kanaTokens.Tokens)
                {
                    if (token.Type == "katakana")
                        result += TreeTraverser.TraverseTree(convertedString, Utils.KatakanaToHiragana(token.Value, options, true), tree, tree, options).ToUpper();
                    else
                        result += TreeTraverser.TraverseTree(convertedString, token.Value, tree, tree, options);
                }
                return result;
            }
            kana = Utils.KatakanaToHiragana(kana, options, true);
            var romajiConv = TreeTraverser.TraverseTree(convertedString, kana, tree, tree, options);
            _romajiCaches[kana] = romajiConv;
            return romajiConv;
        }
    }
}
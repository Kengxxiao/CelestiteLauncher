using NUnit.Framework;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    internal class TreeTraverserUnitTests
    {
        private Dictionary<string, Node> _romajiTree;
        private Dictionary<string, Node> _hiraganaTree;

        [OneTimeSetUp]
        public void SetupTestFixture()
        {
            _hiraganaTree = TreeBuilder.BuildKanaToHepburnTree();
            _romajiTree = TreeBuilder.BuildRomajiToKanaTree();
        }

        [TestCaseSource("RomajiToKanaTestCases")]
        public void BuildRomajiToKana_WhenBuilt_HasCorrectValues(RomajiToKanaTestCase testCase)
            => testCase.Test(_romajiTree);

        static RomajiToKanaTestCase[] RomajiToKanaTestCases =
        {
            new RomajiToKanaTestCase{ Input = "kyakya", ExpectedOutput = "きゃきゃ"},
            new RomajiToKanaTestCase{ Input = "@@@@@@@", ExpectedOutput = "@@@@@@@" },
            new RomajiToKanaTestCase{ Input = "@@@a@@@@", ExpectedOutput = "@@@あ@@@@"},
            new RomajiToKanaTestCase{ Input = "ka", ExpectedOutput = "か"},
            new RomajiToKanaTestCase{ Input = "@@@ka@@@@", ExpectedOutput = "@@@か@@@@"},
            new RomajiToKanaTestCase{ Input = "a", ExpectedOutput = "あ"},
            new RomajiToKanaTestCase{ Input = "aaa", ExpectedOutput = "あああ"},
            new RomajiToKanaTestCase{ Input = "++a", ExpectedOutput = "++あ"},
            new RomajiToKanaTestCase{ Input = "++a++", ExpectedOutput = "++あ++"},
            new RomajiToKanaTestCase{ Input = "あ+a++", ExpectedOutput = "あ+あ++"},
            new RomajiToKanaTestCase{ Input = "kya", ExpectedOutput = "きゃ"},
            new RomajiToKanaTestCase{ Input = "wha", ExpectedOutput = "うぁ"},
            new RomajiToKanaTestCase{ Input = "qa", ExpectedOutput = "くぁ"},
            new RomajiToKanaTestCase{ Input = "xn", ExpectedOutput = "ん"},
            new RomajiToKanaTestCase{ Input = "ja", ExpectedOutput = "じゃ"},
            new RomajiToKanaTestCase{ Input = "zya", ExpectedOutput = "じゃ"},
            new RomajiToKanaTestCase{ Input = "sha", ExpectedOutput = "しゃ"},
            new RomajiToKanaTestCase{ Input = "sya", ExpectedOutput = "しゃ"},
            new RomajiToKanaTestCase{ Input = "syi", ExpectedOutput = "しぃ"},
            new RomajiToKanaTestCase{ Input = "shi", ExpectedOutput = "し"},
            new RomajiToKanaTestCase{ Input = "si", ExpectedOutput = "し"},
            new RomajiToKanaTestCase{ Input = "fu", ExpectedOutput = "ふ"},
            new RomajiToKanaTestCase{ Input = "hu", ExpectedOutput = "ふ"},
            new RomajiToKanaTestCase{ Input = "xo", ExpectedOutput = "ぉ"},
            new RomajiToKanaTestCase{ Input = "xwa", ExpectedOutput = "ゎ"},
            new RomajiToKanaTestCase{ Input = "ltu", ExpectedOutput = "っ"},
            new RomajiToKanaTestCase{ Input = "yi", ExpectedOutput = "い"},
            new RomajiToKanaTestCase{ Input = "dha", ExpectedOutput = "でゃ"},
            new RomajiToKanaTestCase{ Input = "kka", ExpectedOutput = "っか"},
            new RomajiToKanaTestCase{ Input = "sse", ExpectedOutput = "っせ"},
            new RomajiToKanaTestCase{ Input = "kkya", ExpectedOutput = "っきゃ"},
        };

        internal class RomajiToKanaTestCase
        {
            public string Input { get; set; }
            public string ExpectedOutput { get; set; }

            public void Test(Dictionary<string, Node> tree)
            {
                string result = string.Empty;
                var output = TreeTraverser.TraverseTree(result, Input, tree, tree);

                Assert.AreEqual(output, ExpectedOutput, $"Expected '{Input}' => {ExpectedOutput} but was '{Input}' => {output}");
            }
        }

        [TestCaseSource("KanaToHepburnTestCases")]
        public void KanaToHepburnTree_WhenBuilt_HasCorrectValues(KanaToHepburnTestCase testCase)
            => testCase.Test(_hiraganaTree);

        static KanaToHepburnTestCase[] KanaToHepburnTestCases =
        {
            new KanaToHepburnTestCase{ Input = "み", ExpectedOutput = "mi"},
            new KanaToHepburnTestCase{ Input = "。", ExpectedOutput = "."},
            new KanaToHepburnTestCase{ Input = "ょ", ExpectedOutput = "yo"},
            new KanaToHepburnTestCase{ Input = "ふょ", ExpectedOutput = "fyo"},
            new KanaToHepburnTestCase{ Input = "みょ", ExpectedOutput = "myo"},
            new KanaToHepburnTestCase{ Input = "にゅ", ExpectedOutput = "nyu"},
            new KanaToHepburnTestCase{ Input = "ひぇ", ExpectedOutput = "hye"},
            new KanaToHepburnTestCase{ Input = "くぃ", ExpectedOutput = "kyi"},
            new KanaToHepburnTestCase{ Input = "じゃ", ExpectedOutput = "ja"},
            new KanaToHepburnTestCase{ Input = "ぢゃ", ExpectedOutput = "ja"},
            new KanaToHepburnTestCase{ Input = "じぃ", ExpectedOutput = "jyi"},
            new KanaToHepburnTestCase{ Input = "じぇ", ExpectedOutput = "je"},
            new KanaToHepburnTestCase{ Input = "っみ", ExpectedOutput = "mmi"},
            new KanaToHepburnTestCase{ Input = "んあ", ExpectedOutput = "n'a"},
            new KanaToHepburnTestCase{ Input = "ん", ExpectedOutput = "n"},
        };

        internal class KanaToHepburnTestCase
        {
            public string Input { get; set; }
            public string ExpectedOutput { get; set; }

            public void Test(Dictionary<string, Node> tree)
            {
                string result = string.Empty;
                var output = TreeTraverser.TraverseTree(result, Input, tree, tree);

                Assert.AreEqual(output, ExpectedOutput, $"Expected '{Input}' => {ExpectedOutput} but was '{Input}' => {output}");
            }
        }
    }
}

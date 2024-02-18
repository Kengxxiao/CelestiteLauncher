using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    internal class RomajiToKanaTreeBuilderUnitTests
    {
        private Dictionary<string, Node> _romajiTree;

        [OneTimeSetUp]
        public void Setup()
        {
            _romajiTree = TreeBuilder.BuildRomajiToKanaTree();
        }

        [Test]
        public void BuildRomajiToKanaTree_WhenBuilt_HasCorrectValues()
        {
            Assert.AreEqual(_romajiTree["a"].Data, "あ");
            Assert.AreEqual(_romajiTree["k"].Children["a"].Data, "か");
            Assert.AreEqual(_romajiTree["w"].Children["h"].Children["a"].Data, "うぁ");
            Assert.AreEqual(_romajiTree["q"].Children["a"].Data, "くぁ");
            Assert.AreEqual(_romajiTree["x"].Children["n"].Data, "ん");
            Assert.AreEqual(_romajiTree["j"].Children["a"].Data, "じゃ");
            Assert.AreEqual(_romajiTree["s"].Children["h"].Children["a"].Data, "しゃ");
            Assert.AreEqual(_romajiTree["s"].Children["h"].Children["i"].Data, "し");
            Assert.AreEqual(_romajiTree["s"].Children["i"].Data, "し");
            Assert.AreEqual(_romajiTree["f"].Children["u"].Data, "ふ");
            Assert.AreEqual(_romajiTree["h"].Children["u"].Data, "ふ");
            Assert.AreEqual(_romajiTree["x"].Children["o"].Data, "ぉ");
            Assert.AreEqual(_romajiTree["x"].Children["w"].Children["a"].Data, "ゎ");
            Assert.AreEqual(_romajiTree["l"].Children["t"].Children["u"].Data, "っ");
            Assert.AreEqual(_romajiTree["y"].Children["i"].Data, "い");
            Assert.AreEqual(_romajiTree["d"].Children["h"].Children["a"].Data, "でゃ");
            Assert.AreEqual(_romajiTree["n"].Data, "ん");
            Assert.AreEqual(_romajiTree["x"].Children["n"].Data, "ん");
            Assert.AreEqual(_romajiTree["n"].Children["'"].Data, "ん");
            Assert.AreEqual(_romajiTree["k"].Children["k"].Children["a"].Data, "っか");
            Assert.AreEqual(_romajiTree["s"].Children["s"].Children["e"].Data, "っせ");
            Assert.AreEqual(_romajiTree["k"].Children["k"].Children["y"].Children["a"].Data, "っきゃ");
        }
    }
}
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    internal class KanaToHepburnTreeBuilderUnitTests
    {
        private Dictionary<string, Node> _hepburnTree;

        [OneTimeSetUp]
        public void Setup()
        {
            _hepburnTree = TreeBuilder.BuildKanaToHepburnTree();
        }

        [Test]
        public void BuildKanaToHepburnTree_WhenBuilt_HasCorrectValues()
        {
            Assert.AreEqual(_hepburnTree["み"].Data, "mi");
            Assert.AreEqual(_hepburnTree["。"].Data, ".");
            Assert.AreEqual(_hepburnTree["ょ"].Data, "yo");
            Assert.AreEqual(_hepburnTree["ふ"].Children["ょ"].Data, "fyo");
            Assert.AreEqual(_hepburnTree["み"].Children["ょ"].Data, "myo");
            Assert.AreEqual(_hepburnTree["に"].Children["ゅ"].Data, "nyu");
            Assert.AreEqual(_hepburnTree["ひ"].Children["ぇ"].Data, "hye");
            Assert.AreEqual(_hepburnTree["く"].Children["ぃ"].Data, "kyi");
            Assert.AreEqual(_hepburnTree["じ"].Children["ゃ"].Data, "ja");
            Assert.AreEqual(_hepburnTree["ぢ"].Children["ゃ"].Data, "ja");
            Assert.AreEqual(_hepburnTree["じ"].Children["ぃ"].Data, "jyi");
            Assert.AreEqual(_hepburnTree["じ"].Children["ぇ"].Data, "je");
            Assert.AreEqual(_hepburnTree["っ"].Children["み"].Data, "mmi");
            Assert.AreEqual(_hepburnTree["ん"].Children["あ"].Data, "n'a");
            Assert.AreEqual(_hepburnTree["ん"].Data, "n");
        }
    }
}
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class CustomMappingUnitTests
    {
        [TestCase("wanakana", "わにBanaに")]
        public void ToKana_WhenPassedCustomMapping_ReturnsACorrectString(string input, string expectedOutput)
        {
            string result = WanaKana.ToKana(input, new DefaultOptions { CustomKanaMapping = new Dictionary<string, string> { { "na", "に" }, { "ka", "Bana" } } });

            Assert.AreEqual(result, expectedOutput);
        }

        [TestCase("つじぎり", "it's called rōmaji!!!", "つじぎり")]
        public void ToRomaji_WhenPassedInvalidRomanization_DoesntPerformConversion(string input, string romanization, string expectedOutput)
        {
            string result = WanaKana.ToRomaji(input, new DefaultOptions { Romanization = romanization });

            Assert.AreEqual(result, expectedOutput);
        }

        [Test]
        public void WanaKana_WhenPassedMultipleCustomMappings_ReplacesThePreviousOnesAndConvertsCorrectly()
        {
            var map = new DefaultOptions { CustomRomajiMapping = new Dictionary<string, string> { { "じ", "zi" }, { "つ", "tu" }, { "り", "li" } } };
            string result = WanaKana.ToRomaji("つじぎり", map);

            Assert.AreEqual(result, "tuzigili");

            map = new DefaultOptions { CustomRomajiMapping = new Dictionary<string, string> { { "じ", "bi" }, { "つ", "bu" }, { "り", "bi" } } };
            result = WanaKana.ToRomaji("つじぎり", map);

            Assert.AreEqual(result, "bubigibi");

            map = new DefaultOptions { CustomKanaMapping = new Dictionary<string, string> { { "na", "に" }, { "ka", "Bana" } } };
            result = WanaKana.ToKana("wanakana", map);

            Assert.AreEqual(result, "わにBanaに");

            map = new DefaultOptions { CustomKanaMapping = new Dictionary<string, string> { { "na", "り" }, { "ka", "Cabana" } } };
            result = WanaKana.ToKana("wanakana", map);

            Assert.AreEqual(result, "わりCabanaり");
        }
    }
}

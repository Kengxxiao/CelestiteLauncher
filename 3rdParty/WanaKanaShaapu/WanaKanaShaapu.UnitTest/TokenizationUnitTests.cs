using NUnit.Framework;
using WanaKanaShaapu;

namespace WanaKanaShaapu.UnitTests
{
    [TestFixture]
    public class TokenizationUnitTests
    {
        [Test]
        public void Tokenization_WhenPassedAListOfTokens_ReturnsCorrectValues()
        {
            Tokenization myTokenization = new Tokenization();
            myTokenization.Tokens.Add(new Token("en", "Hello"));
            myTokenization.Tokens.Add(new Token("space", " "));
            myTokenization.Tokens.Add(new Token("other", "ваникани"));
            myTokenization.Tokens.Add(new Token("katakana", "カタ"));

            string[] expectedValues = { "Hello", " ", "ваникани", "カタ" };

            CollectionAssert.AreEquivalent(expectedValues, myTokenization.Values);
        }
    }
}

namespace WanaKanaShaapu
{
    public class Tokenization
    {
        public List<Token> Tokens { get; set; } = new List<Token>();
        public string[] Values { get { return Tokens.Select(token => token.Value).ToArray(); } }
    }
}
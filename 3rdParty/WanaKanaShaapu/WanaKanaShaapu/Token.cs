namespace WanaKanaShaapu
{
    readonly public struct Token
    {
        readonly public string Type { get; }
        readonly public string Value { get; }

        public Token(string type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
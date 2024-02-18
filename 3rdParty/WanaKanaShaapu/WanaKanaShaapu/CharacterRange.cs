namespace WanaKanaShaapu
{
    public struct CharacterRange
    {
        public readonly char Min { get; }
        public readonly char Max { get; }
        public CharacterRange(char min, char max)
        {
            Min = min;
            Max = max;
        }

        public bool IsCharacterWithinRange(char c)
        {
            return c >= Min && c <= Max;
        }
    }
}

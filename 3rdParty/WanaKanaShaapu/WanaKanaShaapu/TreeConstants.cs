namespace WanaKanaShaapu
{
    public static class TreeConstants
    {
        private static Dictionary<string, Node>? _kanaToHepburnTree;
        public static readonly Dictionary<string, Node> KanaToHepburnTree = LazyInitializer.EnsureInitialized(ref _kanaToHepburnTree, TreeBuilder.BuildKanaToHepburnTree);

        private static Dictionary<string, Node>? _romajiToKanaTree;
        public static readonly Dictionary<string, Node> RomajiToKanaTree = LazyInitializer.EnsureInitialized(ref _romajiToKanaTree, TreeBuilder.BuildRomajiToKanaTree);
    }
}
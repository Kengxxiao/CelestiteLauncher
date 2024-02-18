
namespace WanaKanaShaapu
{
    public class TreeBuilder
    {
        private static void BuildSubtree(Node node, (string Romaji, string Kana)[] constant)
        {
            foreach (var (romaji, kana) in constant)
                AddNode(node, romaji, kana);
        }

        private static void AddToTree(Dictionary<string, Node> tree, (string Romaji, string Kana)[] constant)
        {
            foreach (var (romaji, kana) in constant)
            {
                string firstChar = romaji.First().ToString();
                if (!tree.ContainsKey(firstChar))
                    tree.Add(firstChar, new Node(string.Empty));

                AddNode(tree[firstChar], romaji[1..], kana);
            }
        }

        private static void AddNode(Node node, string romaji, string kana)
        {
            while (true)
            {
                switch (romaji.Length)
                {
                    case 0:
                        return;
                    case 1 when !node.Children.ContainsKey(romaji):
                        node.Children.Add(romaji, new Node(kana));
                        break;
                    case 1 when node.Children.ContainsKey(romaji):
                        node.Children[romaji].Data = kana;
                        break;
                    default:
                        {
                            var firstChar = romaji.First().ToString();
                            if (!node.Children.ContainsKey(firstChar)) node.Children.Add(firstChar.First().ToString(), new Node(string.Empty));
                            node = node.Children[firstChar];
                            romaji = romaji[1..];
                            continue;
                        }
                }

                break;
            }
        }

        public static Dictionary<string, Node> BuildRomajiToKanaTree()
        {
            var tree = new Dictionary<string, Node>();

            //basic
            foreach (var (mainKey, value) in Constants.BasicKunrei)
            {
                if (value.Length == 1)
                    tree.Add(mainKey, new Node(value.First().Kana));
                else
                {
                    tree.Add(mainKey, new Node(string.Empty));
                    foreach (var subpair in value)
                    {
                        tree[mainKey].Children.Add(subpair.Romaji, new Node(subpair.Kana));
                    }
                }
            }

            //special symbols
            foreach (var (symbol, jSymbol) in Constants.SpecialSymbolsRomajiJp)
                tree.Add(symbol, new Node(jSymbol));

            //add tya, sya, etc.
            AddToTree(tree, Constants.Consonants);
            foreach (var (romaji, kana) in Constants.Consonants)
            {
                foreach (var (s, kana1) in Constants.SmallYRomajiJp)
                    AddNode(tree[romaji], s, kana + kana1);
            }

            //things like うぃ, くぃ, etc.
            AddToTree(tree, Constants.AIUEOConstructions);
            foreach (var (romaji, kana) in Constants.AIUEOConstructions)
            {
                foreach (var (s, kana1) in Constants.SmallVowels)
                {
                    AddNode(
                        romaji.Length == 1
                            ? tree[romaji]
                            : tree[romaji.First().ToString()].Children[romaji.Last().ToString()], s, kana + kana1);
                }
            }

            // different ways to write ん
            string[] nExpressions = { "n", "n'", "xn" };
            foreach (var expression in nExpressions)
            {
                if (expression.Length == 1)
                    tree["n"].Data = "ん";
                else
                {
                    if (!tree.ContainsKey(expression.First().ToString()))
                        tree.Add(expression.First().ToString(), new Node(string.Empty));
                    tree[expression.First().ToString()].Children.Add(expression.Last().ToString(), new Node("ん"));
                }
            }

            // c is equivalent to k, but not for chi, cha, etc. that's why we have to make a copy of k
            tree.Add("c", new Node(string.Empty, new Dictionary<string, Node>(tree["k"].Children)));

            //aliases
            foreach (var (alias, alternative) in Constants.Aliases)
            {
                var nodeCopy =
                    new Node(tree[alternative[0].ToString()].Children[alternative[1].ToString()]);
                switch (alias.Length)
                {
                    case 1:
                        tree[alias] = nodeCopy;
                        break;
                    case 2:
                        tree[alias[0].ToString()].Children[alias[1].ToString()] = nodeCopy;
                        break;
                    default:
                        tree[alias[0].ToString()].Children[alias[1].ToString()].Children[alias[2].ToString()] = nodeCopy;
                        break;
                }
            }

            //x & l subtree
            BuildSubtree(tree["x"], Constants.SmallLetters);
            tree.Add("l", new Node(string.Empty));
            BuildSubtree(tree["l"], Constants.SmallLetters);


            //add or modify special cases
            AddToTree(tree, Constants.SpecialCases);

            //tsu
            foreach (var (romaji, _) in Constants.Consonants)
            {
                var subtreeCopy = new Dictionary<string, Node>(tree[romaji].Children);
                foreach (var node in subtreeCopy.Values)
                    addTsu(node);
                tree[romaji].Children.Add(romaji, new Node(string.Empty, subtreeCopy));
            }
            string[] consonants = { "c", "y", "w", "j" };
            foreach (var consonant in consonants)
            {
                var subtreeCopy = new Dictionary<string, Node>(tree[consonant].Children);
                foreach (var node in subtreeCopy.Values)
                    addTsu(node);
                tree[consonant].Children.Add(consonant, new Node(string.Empty, subtreeCopy));
            }
            // nn should not be っん
            tree["n"].Children.Remove("n");

            return tree;
        }

        private static void addTsu(Node node)
        {
            if (node.Data.Length != 0)
                node.Data = "っ" + node.Data;
            foreach (var childNode in node.Children)
                addTsu(childNode.Value);
        }


        public static Dictionary<string, Node> BuildKanaToHepburnTreeWithoutTsuAndNSubtree()
        {
            var tree = new Dictionary<string, Node>();

            //basic: go through basic romaji and add a node for each
            foreach (var (kana, transliteration) in Constants.BasicRomaji)
            {
                tree.Add(kana, new Node(transliteration));
            }

            //add nodes for special symbols
            foreach (var (jSymbol, symbol) in Constants.SpecialSymbolsJpRomaji)
            {
                tree.Add(jSymbol, new Node(symbol));
            }

            //add nodes for ya yu yo
            foreach (var (kana, transliteration) in Constants.SmallYJpRomaji)
            {
                tree.Add(kana, new Node(transliteration));
            }

            //add nodes for small a i u e o
            foreach (var (kana, transliteration) in Constants.smallAIUEO)
            {
                tree.Add(kana, new Node(transliteration));
            }

            //YOON_KANA for each Yoon Kana:
            //   find the node in the tree
            //   add a child from smallY and smallYExtra
            // e.g. くゃ => kya, くゅ => kyu
            foreach (var kana in Constants.yoonKana)
            {
                char firstRomajiChar = tree[kana].Data[0];
                foreach (var (s, transliteration) in Constants.SmallYJpRomaji)
                {
                    tree[kana].Children.Add(s, new Node(firstRomajiChar + transliteration));
                }
                foreach (var (s, transliteration) in Constants.smallYExtra)
                {
                    tree[kana].Children.Add(s, new Node(firstRomajiChar + transliteration));
                }
            }

            //YOON_KANA for each exceptional Yoon Kana:
            //   find the node in the tree
            //   add a child from smallY
            // e.g. じゃ -> ja 
            foreach (var kana in Constants.yoonExceptions)
            {
                foreach (var (s, transliteration) in Constants.SmallYJpRomaji)
                {
                    var newNode = new Node(kana.Transliteration + transliteration[1]);
                    tree[kana.Kana].Children.Add(s, newNode);
                }
                tree[kana.Kana].Children.Add("ぃ", new Node(kana.Transliteration + "yi"));
                tree[kana.Kana].Children.Add("ぇ", new Node(kana.Transliteration + "e"));
            }

            return tree;
        }

        public static Dictionary<string, Node> BuildKanaToHepburnTree()
        {
            var tree = BuildKanaToHepburnTreeWithoutTsuAndNSubtree();
            var tsuChildren = BuildKanaToHepburnTreeWithoutTsuAndNSubtree();

            tree.Add("っ", new Node(string.Empty, tsuChildren));

            foreach (var node in tree["っ"].Children.Values)
                ResolveTsu(node);

            static void ResolveTsu(Node node)
            {
                char firstLetter = node.Data[0];
                if (Constants.SokuonWhitelist.TryGetValue(firstLetter, out var sokuonMapping))
                {
                    node.Data = sokuonMapping + node.Data;
                }
                foreach (var childNode in node.Children)
                    ResolveTsu(childNode.Value);
            }

            foreach (var kana in Constants.AmbiguousVowels)
            {
                tree["ん"].Children.Add(kana.ToString(), new Node("n'" + tree[kana.ToString()].Data));
            }

            return tree;
        }
    }
}
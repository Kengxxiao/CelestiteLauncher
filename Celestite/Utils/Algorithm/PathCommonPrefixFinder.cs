using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;

namespace Celestite.Utils.Algorithm
{

    public class PathCommonPrefixFinder
    {
        public static string GetCommonPath(IEnumerable<string> paths, bool addWide = false)
        {
            var root = new TrieNode();
            // 构建 Trie 树
            foreach (var path in paths)
            {
                InsertPath(root, path);
            }

            // 获取共同前缀
            return GetCommonPrefix(root, addWide);
        }

        private static void InsertPath(TrieNode root, string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)[..^1];
            var node = segments.Aggregate(root, (current, segment) => current.GetOrCreateChildNode(segment));

            node.IsEnd = true; // 标记路径的最后一个节点
        }

        private static string GetCommonPrefix(TrieNode root, bool addWide = false)
        {
            var prefix = string.Empty;
            var node = root;

            // 获取所有路径的共同前缀
            while (node is { Count: 1, IsEnd: false })
            {
                var next = node.GetFirstChild();
                prefix += ZString.Concat('/', next.Key);
                node = next.Value;
            }

            return !addWide ? prefix : ZString.Concat(prefix, "/*");
        }
    }

    public class TrieNode
    {
        public Dictionary<string, TrieNode> Children { get; } = [];
        public bool IsEnd { get; set; }
        public int Count => Children.Count;

        public TrieNode GetOrCreateChildNode(string segment)
        {
            if (Children.TryGetValue(segment, out var childNode)) return childNode;
            childNode = new TrieNode();
            Children[segment] = childNode;

            return childNode;
        }

        public KeyValuePair<string, TrieNode> GetFirstChild()
        {
            return Children.First();
        }
    }
}

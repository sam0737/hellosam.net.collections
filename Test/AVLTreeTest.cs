using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hellosam.Net.Collections.Test
{
    [TestClass]
    public class AVLTreeTest
    {
        [TestMethod]
        public void Construction()
        {
            var d = new AVLTree<string, string>();
        }

        [TestMethod]
        public void InsertionBalancedTest()
        {
            var d = new AVLTree<int, int>();
            d.Add(5, 0);
            d.Add(1, 0);
            d.Add(10, 0);
            CheckTree(d, "<5<1><10>>");
        }

        [TestMethod]
        public void InsertionRightRightTest()
        {
            var d = new AVLTree<int, int>();
            d.Add(1, 0);
            d.Add(5, 0);
            d.Add(10, 0);
            CheckTree(d, "<5<1><10>>");
        }

        [TestMethod]
        public void InsertionLeftLeftTest()
        {
            var d = new AVLTree<int, int>();
            d.Add(10, 0);
            d.Add(5, 0);
            d.Add(1, 0);
            CheckTree(d, "<5<1><10>>");
        }

        [TestMethod]
        public void InsertionLeftRightTest()
        {
            var d = new AVLTree<int, int>();
            d.Add(10, 0);
            d.Add(1, 0);
            d.Add(5, 0);
            CheckTree(d, "<5<1><10>>");
        }

        [TestMethod]
        public void InsertionRightLeftTest()
        {
            var d = new AVLTree<int, int>();
            d.Add(1, 0);
            d.Add(10, 0);
            d.Add(5, 0);
            CheckTree(d, "<5<1><10>>");
        }


        [TestMethod]
        public void InsertionDeepRightLeftTest()
        {
            var d = new AVLTree<int, int>();
            d.Add(10, 0);
            d.Add(5, 0);
            d.Add(11, 0);
            CheckTree(d, "<10<5><11>>");
            d.Add(20, 0);
            CheckTree(d, "<10<5><11<><20>>>");
            d.Add(15, 0);
            CheckTree(d, "<10<5><15<11><20>>>");
        }

        [TestMethod]
        public void MoreTest()
        {
            var d = new AVLTree<int, int>();
            d.Add(25, 0);
            CheckTree(d, "<25<><>>");
            d.Add(42, 0);
            CheckTree(d, "<25<><42>>");
            d.Add(55, 0);
            CheckTree(d, "<42<25><55>>");
            d.Add(72, 0);
            CheckTree(d, "<42<25><55<><72>>>");
            d.Add(24, 0);
            CheckTree(d, "<42<25<24><>><55<><72>>>");
            d.Add(96, 0);
            CheckTree(d, "<42<25<24><>><72<55><96>>>");
            d.Add(28, 0);
            CheckTree(d, "<42<25<24><28>><72<55><96>>>");

            d.Remove(28);
            CheckTree(d, "<42<25<24><>><72<55><96>>>");
            d.Remove(24);
            CheckTree(d, "<42<25><72<55><96>>>");
            d.Remove(42);
            CheckTree(d, "<55<25><72<><96>>>");
            d.Remove(55);
            CheckTree(d, "<72<25><96>>");
        }

        [TestMethod]
        public void EvenMoreTest()
        {
            var d = new AVLTree<int, int>();
            var sequence =
                "95,91,33,48,74,53,21,47,1,58,75,98,32,10,46,26,3,96,31,61,13,85,51,93,90,92,64,39,66,34,22,81,57,7,45,88,99,94,84,11,0,29,41,17,18,2,44,56,82,6,73,50,89,62,27,16,60,54,30,5,65,80,69,19,43,79,40,8,14,78,52,97,4,28,36,67,20,87,76,15,24,86,55,70,71,42,35,25,77,83,23,12,59,68,37,49,63,9,72,38"
                    .Split(',').Select(int.Parse).ToArray();
            
            int size = 0;
            foreach (var i in sequence)
            {
                size++;
                d.Add(i, i * 100);
                Assert.AreEqual(size, d.Root.Size);
                CheckBalance(d);
            }

            var removeSequence = 
                "64,70,55,54,46,21,87,53,96,51,23,24,9,40,12,39,99,27,75,11,50,31,2,18,7,34,95,83,86,20,42,92,48,63,22,89,47,68,36,84,1,19,28,25,93,15,81,43,71,65,78,8,10,76,17,91,98,69,37,79,33,13,45,73,35,0,66,77,58,41,29,72,59,56,62,32,60,49,82,97,3,88,4,80,74,57,52,5,94,26,16,30,67,14,85,61,44,38,6,90"
                    .Split(',').Select(int.Parse).ToArray();
            foreach (var i in removeSequence)
            {
                size--;
                d.Remove(i);
                Assert.AreEqual(size, d.Root == null ? 0 : d.Root.Size);
                CheckBalance(d);
            }
        }

        [TestMethod]
        public void RandomTest()
        {
            for (int i = 0; i < 20; i++)
                SingleRandomTest();
        }

        private static void SingleRandomTest()
        {
            var d = new AVLTree<int, int>();
            var sequence = Enumerable.Range(0, 100).Shuffle(new Random()).ToArray();
            int size = 0;
            foreach (var i in sequence)
            {
                d.Add(i, i*100);
                CheckBalance(d);
            }
            
            var removeSequence = Enumerable.Range(0, 50).Shuffle(new Random()).ToArray();
            foreach (var i in removeSequence)
            {
                d.Remove(i);
                CheckBalance(d);
            }

            var reAddSequence = removeSequence.Shuffle(new Random()).ToArray();
            foreach (var i in reAddSequence)
            {
                d.Add(i, i * 100);
                CheckBalance(d);
            }

            foreach (var i in d.Keys.ToArray())
            {
                d.Remove(i);
                CheckBalance(d);
            }
            Assert.IsNull(d.Root);
        }

        private static void CheckBalance(AVLTree<int, int> avlTree)
        {
            int count = 0;
            GetNodeDepthAndCheckBalance(avlTree.Root, out count);
            Assert.AreEqual(count, avlTree.Root != null ? avlTree.Root.Size : 0);
        }

        private static int GetNodeDepthAndCheckBalance(AVLTreeNode<KeyValuePair<int, int>> node, out int count)
        {
            count = 0;
            if (node == null) return 0;
            int l_count;
            int r_count;
            var leftDepth = GetNodeDepthAndCheckBalance(node.LeftChild, out l_count);
            var rightDepth = GetNodeDepthAndCheckBalance(node.RightChild, out r_count);
            count = l_count + r_count + 1;
            Assert.AreEqual(count, node.Size);

            var actualBalance = rightDepth - leftDepth;
            Assert.AreEqual(actualBalance, node.Balance);
            Assert.IsTrue(actualBalance >= -1 && actualBalance <= 1);

            return Math.Max(leftDepth, rightDepth) + 1;
        }

        private static Regex specMatch = new Regex("^<([0-9]+)");

        private static void CheckTree(AVLTree<int, int> tree, string spec)
        {
            Assert.AreEqual(spec.Length, CheckTreeNode(tree.Root, spec), "Spec consumption length");
            CheckBalance(tree);
        }

        private static int CheckTreeNode(BinaryTreeNode<KeyValuePair<int, int>> node, string spec)
        {
            var match = specMatch.Match(spec);
            if (match.Success)
            {
                var value = int.Parse(match.Groups[1].Value);

                Assert.AreEqual(value, node.Value.Key);

                var begin = match.Groups[1].Index + match.Groups[1].Length;
                var leftLength = CheckTreeNode(node.LeftChild, spec.Substring(begin));
                var rightLength = CheckTreeNode(node.RightChild, spec.Substring(begin + leftLength));

                return begin + leftLength + rightLength + 1;
            }
            else if (spec.StartsWith("<>"))
            {
                Assert.IsNull(node);
                return 2;
            }
            else if (spec.StartsWith(">"))
            {
                Assert.IsNull(node);
                return 0;
            }
            else
            {
                Assert.Fail("Unrecognized spec: " + spec.Substring(20) + "...");
                return 0;
            }
        }

        public static string DumpTree(AVLTree<int, int> tree, string spec)
        {
            return DumpTreeNode(tree.Root);
        }

        public static string DumpTreeNode(BinaryTreeNode<KeyValuePair<int, int>> node, int depth = 0)
        {
            string indent = "";
            for (int i = 0; i < depth; i++)
            {
                indent += "   ";
            }

            if (node == null) return indent + "<>" + Environment.NewLine;
            if (node.LeftChild == null && node.RightChild == null)
            {
                return indent + "<" + node.Value.Key + ">" + Environment.NewLine;
            }
            return
                indent + "<" + node.Value.Key + Environment.NewLine +
                DumpTreeNode(node.LeftChild, depth + 1) +
                DumpTreeNode(node.RightChild, depth + 1);
        }
    }
}
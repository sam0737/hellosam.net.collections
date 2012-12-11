using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hellosam.Net.Collections.Test
{
    [TestClass]
    public class BinaryTreeTest
    {
        [TestMethod]
        public void Construction()
        {
            var d = new BinaryTree<string, string>();
        }

        [TestMethod]
        public void SequentialTest()
        {
            var seq = Enumerable.Range(0, 20).ToArray();
            for (int i = 0; i < 20; i++)
                RemoveSingleTest(seq, i);
        }

        [TestMethod]
        public void RandomTest()
        {
            var seq = Enumerable.Range(0, 20).Shuffle(new Random()).ToArray();
            for (int i = 0; i < 20; i++)
                RemoveSingleTest(seq, i);
        }

        [TestMethod]
        public void FurtherRandomTest()
        {
            var seq = Enumerable.Range(0, 100).Shuffle(new Random()).ToArray();
            var seqRemove = Enumerable.Range(0, 100).Where(i => i%3 == 0).Shuffle(new Random()).ToArray();

            var d = new BinaryTree<int, int>();
            foreach (var i in seq)
                d.Add(i, i*100);
            foreach (var i in seqRemove)
                d.Remove(i);

            Assert.AreEqual(string.Join(",", seq.Except(seqRemove).OrderBy(i => i).ToArray()),
                            string.Join(",", d.Keys.ToArray()));
        }

        [TestMethod]
        public void IndexTest()
        {
            var d = new BinaryTree<int, bool>();
            d.Add(10, true);
            d.Add(20, true);
            d.Add(30, true);
            d.Add(40, true);
            d.Add(25, true);
            Assert.AreEqual(2, d.IndexOfKey(25));
        }

        [TestMethod]
        public void Enumeration()
        {
            var d = new BinaryTree<int, bool>();
            d.Add(5, true);
            d.Add(2, true);
            d.Add(1, true);
            d.Add(3, true);
            d.Add(15, true);
            d.Add(10, true);
            d.Add(20, true);

            d.TraversalOrder = TraversalMode.InOrder;
            Assert.AreEqual("1,2,3,5,10,15,20", string.Join(",", d.Select(i => i.Key).ToArray()));
            d.TraversalOrder = TraversalMode.PreOrder;
            Assert.AreEqual("5,2,1,3,15,10,20", string.Join(",", d.Select(i => i.Key).ToArray()));
            d.TraversalOrder = TraversalMode.PostOrder;
            Assert.AreEqual("1,3,2,10,20,15,5", string.Join(",", d.Select(i => i.Key).ToArray()));
        }

        private static void RemoveSingleTest(int[] sequence, int remove)
        {
            var d = new BinaryTree<int, int>();

            foreach (var i in sequence)
                d.Add(i, i*100);

            Assert.IsTrue(d.Remove(remove), "Remove " + remove);
            Assert.AreEqual(string.Join(",", sequence.Where(i => i != remove).OrderBy(i => i).ToArray()),
                            string.Join(",", d.Keys.ToArray()),
                            "Remove " + remove);

        }
    }
}
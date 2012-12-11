using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hellosam.Net.Collections
{
    /// <summary>
    /// A Binary Tree node that holds an element and references to other tree nodes
    /// </summary>
    public class BinaryTreeNode<T>
    {
        /// <summary>
        /// The value stored at the node
        /// </summary>
        public T Value { get; set; }

        private BinaryTreeNode<T> _leftChild;

        /// <summary>
        /// Gets or sets the left child node
        /// </summary>
        public virtual BinaryTreeNode<T> LeftChild
        {
            get { return _leftChild; }
            set { _leftChild = value; ResetSize(); }
        }

        private BinaryTreeNode<T> _rightChild;

        /// <summary>
        /// Gets or sets the right child node
        /// </summary>
        public virtual BinaryTreeNode<T> RightChild
        {
            get { return _rightChild; }
            set { _rightChild = value; ResetSize(); }
        }

        /// <summary>
        /// Gets or sets the parent node
        /// </summary>
        public virtual BinaryTreeNode<T> Parent { get; set; }
        
        public virtual int Size { get; set; }

        protected virtual void ResetSize()
        {
            Size =
                (LeftChild == null ? 0 : LeftChild.Size) +
                (RightChild == null ? 0 : RightChild.Size) +
                1;
        }

        /// <summary>
        /// Gets whether the node is a leaf (has no children)
        /// </summary>
        public virtual bool IsLeaf
        {
            get { return this.ChildCount == 0; }
        }

        /// <summary>
        /// Gets whether the node is the left child of its parent
        /// </summary>
        public virtual bool IsLeftChild
        {
            get { return this.Parent != null && this.Parent.LeftChild == this; }
        }

        /// <summary>
        /// Gets whether the node is the right child of its parent
        /// </summary>
        public virtual bool IsRightChild
        {
            get { return this.Parent != null && this.Parent.RightChild == this; }
        }

        /// <summary>
        /// Gets the number of children this node has
        /// </summary>
        public virtual int ChildCount
        {
            get
            {
                int count = 0;

                if (this.LeftChild != null)
                    count++;

                if (this.RightChild != null)
                    count++;

                return count;
            }
        }

        /// <summary>
        /// Create a new instance of a Binary Tree node
        /// </summary>
        public BinaryTreeNode(T value)
        {
            this.Value = value;
        }
    }

    /// <summary>
    /// Specifies the mode of scanning through the tree
    /// </summary>
    public enum TraversalMode
    {
        InOrder = 0,
        PostOrder,
        PreOrder
    }

    /// <summary>
    /// Binary Tree data structure
    /// </summary>
    public class BinaryTree<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>
    {
        private BinaryTreeNode<KeyValuePair<TKey, TValue>> head = null;
        protected readonly IComparer<TKey> comparer;
        private TraversalMode traversalMode = TraversalMode.InOrder;

        /// <summary>
        /// Gets or sets the root of the tree (the top-most node)
        /// </summary>
        public virtual BinaryTreeNode<KeyValuePair<TKey, TValue>> Root
        {
            get { return head; }
            set { head = value; }
        }

        /// <summary>
        /// Gets whether the tree is read-only
        /// </summary>
        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the number of elements stored in the tree
        /// </summary>
        public virtual int Count
        {
            get { return head == null ? 0 : head.Size; }
        }

        /// <summary>
        /// Gets or sets the traversal mode of the tree
        /// </summary>
        public virtual TraversalMode TraversalOrder
        {
            get { return traversalMode; }
            set { traversalMode = value; }
        }

        /// <summary>
        /// Creates a new instance of a Binary Tree
        /// </summary>
        public BinaryTree()
        {
            comparer = Comparer<TKey>.Default;
        }

        public BinaryTree(IComparer<TKey> comparer)
        {
            this.comparer = comparer;
        }


        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        /// Adds a new element to the tree
        /// </summary>
        public virtual void Add(KeyValuePair<TKey,TValue> value)
        {
            BinaryTreeNode<KeyValuePair<TKey, TValue>> node =
                new BinaryTreeNode<KeyValuePair<TKey, TValue>>(value);
            this.Add(node);
        }

        /// <summary>
        /// Adds a node to the tree
        /// </summary>
        public virtual void Add(BinaryTreeNode<KeyValuePair<TKey, TValue>> node)
        {
            if (this.head == null) //first element being added
            {
                this.head = node; //set node as root of the tree
                node.Size = 1;
            }
            else
            {
                if (node.Parent == null)
                {
                    node.Parent = head; //start at head
                    node.Size = 1;
                }
                node.Parent.Size++;

                //Node is inserted on the left side if it is smaller or equal to the parent
                bool insertLeftSide =
                    comparer.Compare(node.Value.Key, node.Parent.Value.Key) <= 0;

                if (insertLeftSide) //insert on the left
                {
                    if (node.Parent.LeftChild == null)
                    {
                        node.Parent.LeftChild = node; //insert in left
                    }
                    else
                    {
                        node.Parent = node.Parent.LeftChild; //scan down to left child
                        this.Add(node); //recursive call
                    }
                }
                else //insert on the right
                {
                    if (node.Parent.RightChild == null)
                    {
                        node.Parent.RightChild = node; //insert in right
                    }
                    else
                    {
                        node.Parent = node.Parent.RightChild;
                        this.Add(node);
                    }
                }
            }
        }
        
        protected virtual BinaryTreeNode<KeyValuePair<TKey, TValue>> Find(TKey key)
        {
            var node = this.Root; //start at head
            while (node != null)
            {
                var r = comparer.Compare(key, node.Value.Key);
                if (r == 0) //parameter value found
                    return node;
                else
                {
                    //Search left if the value is smaller than the current node
                    if (r < 0)
                        node = node.LeftChild; //search left
                    else
                        node = node.RightChild; //search right
                }
            }
            return null;
        }
        
        /// <summary>
        /// Returns whether a value is stored in the tree
        /// </summary>
        public virtual bool Contains(KeyValuePair<TKey, TValue> value)
        {
            var node = Find(value.Key);
            if (node== null)
                return false;
            return Comparer<TValue>.Default.Compare(node.Value.Value, value.Value) == 0;
        }

        public virtual bool ContainsKey(TKey key)
        {
            return Find(key) != null;
        }

        /// <summary>
        /// Removes a value from the tree and returns whether the removal was successful.
        /// </summary>
        public virtual bool Remove(KeyValuePair<TKey, TValue> value)
        {
            var removeNode = Find(value.Key);
            return this.Remove(removeNode);
        }

        public virtual bool Remove(TKey key)
        {
            var removeNode = Find(key);
            return this.Remove(removeNode);
        }

        /// <summary>
        /// Removes a node from the tree and returns whether the removal was successful.
        /// </summary>>
        public virtual bool Remove(BinaryTreeNode<KeyValuePair<TKey, TValue>> removeNode)
        {
            if (removeNode == null)
                return false; //value doesn't exist or not of this tree

            //Note whether the node to be removed is the root of the tree
            bool wasHead = (removeNode == head);

            if (this.Count == 1)
            {
                this.head = null; //only element was the root
            }
            else if (removeNode.IsLeaf) //Case 1: No Children
            {
                var parent = removeNode.Parent;
                while (parent != null)
                {
                    parent.Size--;
                    parent = parent.Parent;
                }

                //Remove node from its parent
                if (removeNode.IsLeftChild)
                    removeNode.Parent.LeftChild = null;
                else
                    removeNode.Parent.RightChild = null;

                removeNode.Parent = null;
            }
            else if (removeNode.ChildCount == 1) //Case 2: One Child
            {
                var parent = removeNode.Parent;
                while (parent != null)
                {
                    parent.Size--;
                    parent = parent.Parent;
                }

                if (removeNode.LeftChild != null)
                {
                    //Put left child node in place of the node to be removed
                    removeNode.LeftChild.Parent = removeNode.Parent; //update parent

                    if (wasHead)
                        this.Root = removeNode.LeftChild; //update root reference if needed
                    else
                    {
                        if (removeNode.IsLeftChild) //update the parent's child reference
                            removeNode.Parent.LeftChild = removeNode.LeftChild;
                        else
                            removeNode.Parent.RightChild = removeNode.LeftChild;
                    }
                }
                else //Has right child
                {
                    //Put left node in place of the node to be removed
                    removeNode.RightChild.Parent = removeNode.Parent; //update parent

                    if (wasHead)
                        this.Root = removeNode.RightChild; //update root reference if needed
                    else
                    {
                        if (removeNode.IsLeftChild) //update the parent's child reference
                            removeNode.Parent.LeftChild = removeNode.RightChild;
                        else
                            removeNode.Parent.RightChild = removeNode.RightChild;
                    }
                }

                removeNode.Parent = null;
                removeNode.LeftChild = null;
                removeNode.RightChild = null;
            }
            else //Case 3: Two Children
            {
                // Find the nearest element with only 1 or less children.
                // From the right subtree, find the left most children
                var successorNode = removeNode.RightChild;
                while (successorNode.LeftChild != null)
                    successorNode = successorNode.LeftChild;
                
                /*
                 * TODO: If the caller retrieved the node from IndexOfKey, 
                 * then proceed for removal, and rely on the .Value property,
                 * the value is changed surprisingly.
                 */
                removeNode.Value = successorNode.Value; // Swap the value

                this.Remove(successorNode); //recursively remove the inorder predecessor
            }

            
            return true;
        }

        /// <summary>
        /// Removes all the elements stored in the tree
        /// </summary>
        public virtual void Clear()
        {
            this.Root = null;
        }

        /// <summary>
        /// Returns the depth of a subtree rooted at the parameter value
        /// </summary>
        public virtual int GetDepth(KeyValuePair<TKey, TValue> value)
        {
            var node = this.Find(value.Key);
            return this.GetDepth(node);
        }

        /// <summary>
        /// Returns the depth of a subtree rooted at the parameter node
        /// </summary>
        public virtual int GetDepth(BinaryTreeNode<KeyValuePair<TKey, TValue>> startNode)
        {
            int depth = 0;

            if (startNode == null)
                return depth;

            var parentNode = startNode.Parent; //start a node above
            while (parentNode != null)
            {
                depth++;
                parentNode = parentNode.Parent; //scan up towards the root
            }

            return depth;
        }

        
        public ICollection<TKey> Keys
        {
            get { return (from i in this select i.Key).ToArray(); }
        }


        public ICollection<TValue> Values
        {
            get { return (from i in this select i.Value).ToArray(); }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var node = Find(key);
            if (node == null)
            {
                value = default(TValue); 
                return false;
            }
            value = node.Value.Value;
            return true;
        }

        public TValue this[TKey key]
        {
            get
            {
                var node = Find(key);
                if (node == null) throw new KeyNotFoundException();
                return node.Value.Value;
            }
            set
            {
                var node = Find(key);
                if (node == null) throw new KeyNotFoundException();
                node.Value = new KeyValuePair<TKey, TValue>(node.Value.Key, value);
            }
        }

        public int IndexOfKey(TKey key)
        {
            BinaryTreeNode<KeyValuePair<TKey, TValue>> node;
            return IndexOfKey(key, out node);
        }

        public int IndexOfKey(TKey key, out BinaryTreeNode<KeyValuePair<TKey, TValue>> node)
        {
            int size = 0;
            node = this.Root; //start at head
            while (node != null)
            {
                var r = comparer.Compare(key, node.Value.Key);
                if (r == 0) //parameter value found
                    return size + (node.LeftChild != null ? node.LeftChild.Size : 0);
                else
                {
                    //Search left if the value is smaller than the current node
                    if (r < 0)
                    {
                        node = node.LeftChild; //search left
                    }
                    else
                    {
                        size++;
                        if (node.LeftChild != null)
                            size += node.LeftChild.Size;
                        node = node.RightChild; //search right
                    }
                }
            }
            node = null;
            return -1;
        }

        /// <summary>
        /// Returns an enumerator to scan through the elements stored in tree.
        /// The enumerator uses the traversal set in the TraversalMode property.
        /// </summary>
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            switch (this.TraversalOrder)
            {
                case TraversalMode.InOrder:
                    return GetInOrderEnumerator();
                case TraversalMode.PostOrder:
                    return GetPostOrderEnumerator();
                case TraversalMode.PreOrder:
                    return GetPreOrderEnumerator();
                default:
                    return GetInOrderEnumerator();
            }
        }

        /// <summary>
        /// Returns an enumerator to scan through the elements stored in tree.
        /// The enumerator uses the traversal set in the TraversalMode property.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that visits node in the order: left child, parent, right child
        /// </summary>
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetInOrderEnumerator()
        {
            return new BinaryTreeInOrderEnumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that visits node in the order: left child, right child, parent
        /// </summary>
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetPostOrderEnumerator()
        {
            return new BinaryTreePostOrderEnumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that visits node in the order: parent, left child, right child
        /// </summary>
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetPreOrderEnumerator()
        {
            return new BinaryTreePreOrderEnumerator(this);
        }

        /// <summary>
        /// Copies the elements in the tree to an array using the traversal mode specified.
        /// </summary>
        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array)
        {
            this.CopyTo(array, 0);
        }

        /// <summary>
        /// Copies the elements in the tree to an array using the traversal mode specified.
        /// </summary>
        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int startIndex)
        {
            var enumerator = this.GetEnumerator();

            for (int i = startIndex; i < array.Length; i++)
            {
                if (enumerator.MoveNext())
                    array[i] = enumerator.Current;
                else
                    break;
            }
        }

        /// <summary>
        /// Compares two elements to determine their positions within the tree.
        /// </summary>
        public static int CompareElements(IComparable x, IComparable y)
        {
            return x.CompareTo(y);
        }

        internal abstract class BinaryTreeEnumeratorBase : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private BinaryTreeNode<KeyValuePair<TKey, TValue>> current;
            private BinaryTree<TKey, TValue> tree;
            protected Queue<BinaryTreeNode<KeyValuePair<TKey, TValue>>> traverseQueue;

            public BinaryTreeEnumeratorBase(BinaryTree<TKey, TValue> tree)
            {
                this.tree = tree;

                //Build queue
                traverseQueue = new Queue<BinaryTreeNode<KeyValuePair<TKey, TValue>>>();
                visitNode(this.tree.Root);
            }

            protected abstract void visitNode(BinaryTreeNode<KeyValuePair<TKey, TValue>> node);

            public KeyValuePair<TKey, TValue> Current
            {
                get { return current.Value; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                current = null;
                tree = null;
            }

            public void Reset()
            {
                current = null;
            }

            public bool MoveNext()
            {
                if (traverseQueue.Count > 0)
                    current = traverseQueue.Dequeue();
                else
                    current = null;

                return (current != null);
            }
        }


        /// <summary>
        /// Returns an inorder-traversal enumerator for the tree values
        /// </summary>
        internal class BinaryTreeInOrderEnumerator : BinaryTreeEnumeratorBase
        {
            public BinaryTreeInOrderEnumerator(BinaryTree<TKey, TValue> tree)
                : base(tree)
            {
            }

            protected override void visitNode(BinaryTreeNode<KeyValuePair<TKey, TValue>> node)
            {
                if (node == null)
                    return;
                else
                {
                    visitNode(node.LeftChild);
                    traverseQueue.Enqueue(node);
                    visitNode(node.RightChild);
                }
            }
        }

        /// <summary>
        /// Returns a postorder-traversal enumerator for the tree values
        /// </summary>
        internal class BinaryTreePostOrderEnumerator : BinaryTreeEnumeratorBase
        {
            public BinaryTreePostOrderEnumerator(BinaryTree<TKey, TValue> tree)
                : base(tree)
            {
            }

            protected override void visitNode(BinaryTreeNode<KeyValuePair<TKey, TValue>> node)
            {
                if (node == null)
                    return;
                else
                {
                    visitNode(node.LeftChild);
                    visitNode(node.RightChild);
                    traverseQueue.Enqueue(node);
                }
            }
        }

        /// <summary>
        /// Returns an preorder-traversal enumerator for the tree values
        /// </summary>
        internal class BinaryTreePreOrderEnumerator : BinaryTreeEnumeratorBase
        {
            public BinaryTreePreOrderEnumerator(BinaryTree<TKey, TValue> tree)
                : base(tree)
            {
            }

            protected override void visitNode(BinaryTreeNode<KeyValuePair<TKey, TValue>> node)
            {
                if (node == null)
                    return;
                else
                {
                    traverseQueue.Enqueue(node);
                    visitNode(node.LeftChild);
                    visitNode(node.RightChild);
                }
            }
        }
    }
}
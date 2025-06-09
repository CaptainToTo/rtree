using System;
using System.Collections.Generic;

namespace RTree
{
    public class RTree<P, B>  where P : IPoint<P> where B : IBounds<P, B>
    {
        private Node<P, B> _root;

        public int Count => _root.Count;
        public int Height => _root.Height;
        public B Bounds => _root.Bounds;

        public RTree(B b, int capacity)
        {
            if (capacity < 2)
                throw new ArgumentException("R Tree node capacity must be >= 2.");

            _root = new LeafNode<P, B>(null, b, capacity);
        }

        public void Insert(P p)
        {
            var newRoot = _root.Insert(p);
            if (newRoot != null)
                _root = newRoot;
        }

        public List<P> GetPointsIn(B b)
        {
            var result = new List<P>();
            _root.AddPointsTo(result, b);
            return result;
        }

        public void GetPointsInNonAlloc(List<P> result, B b)
        {
            _root.AddPointsTo(result, b);
        }

        public void Remove(P p)
        {
            _root.Remove(p);
        }

        public override string ToString()
        {
            return "RTree w/ height of " + Height + ", containing " + Count + " points:\n" + _root.ToString(" ");
        }
    }
}
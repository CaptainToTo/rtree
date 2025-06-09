using System;
using System.Collections.Generic;

namespace RTree
{
    internal abstract class Node<P, B> where P : IPoint<P> where B : IBounds<P, B>
    {
        public bool IsRoot => Parent == null;
        public InternalNode<P, B> Parent { get; internal set; }

        public B Bounds { get; internal set; }

        public readonly int Capacity;
        public int Height { get; protected set; }

        public int Count { get; internal set; }

        private Queue<(B lo, B hi)> _parentSplits;
        public bool IsOriginal => _parentSplits.Count > 0;

        public Node(InternalNode<P, B> parent, B b, int capacity, int height)
        {
            Parent = parent;
            Bounds = b;
            Capacity = capacity;
            Height = height;
            Count = 0;
            _parentSplits = new Queue<(B lo, B hi)>();
        }

        public abstract Node<P, B> Insert(P p);

        public abstract void AddPointsTo(List<P> result, B b);

        public abstract bool Remove(P p);

        public void AddSplit((B lo, B hi) split)
        {
            _parentSplits.Enqueue(split);
        }

        public (B lo, B hi) CheckSplit()
        {
            if (IsOriginal)
                return _parentSplits.Peek();
            return default;
        }

        public void DropSplit()
        {
            if (IsOriginal)
                _parentSplits.Dequeue();
        }

        public abstract string ToString(string spacer);
    }

    internal class LeafNode<P, B> : Node<P, B> where P : IPoint<P> where B : IBounds<P, B>
    {
        private List<P> _points;

        public LeafNode(InternalNode<P, B> parent, B b, int capacity) : base(parent, b, capacity, 1)
        {
            _points = new List<P>(capacity + 1);
        }

        public override Node<P, B> Insert(P p)
        {
            if (!Bounds.Contains(p))
                return null;

            _points.Add(p);
            Count++;
            if (_points.Count > Capacity)
                return Split();
            return null;
        }

        private Node<P, B> Split()
        {
            if (IsRoot)
            {
                var newBounds = FindSplit();
                var newRoot = new InternalNode<P, B>(null, Bounds, Capacity, Height + 1);
                newRoot.Count = Count;
                var hi = new LeafNode<P, B>(newRoot, newBounds.hi, Capacity);
                Parent = newRoot;
                Bounds = newBounds.lo;
                AddSplit(newBounds);

                for (int i = 0; i < _points.Count; i++)
                {
                    if (hi.Bounds.Contains(_points[i]))
                    {
                        hi.Insert(_points[i]);
                        _points.RemoveAt(i);
                        Count--;
                        i--;
                    }
                }
                newRoot.AddChild(this);
                newRoot.AddChild(hi);
                return newRoot;
            }
            else
            {
                var newBounds = FindSplit();
                var hi = new LeafNode<P, B>(Parent, newBounds.hi, Capacity);
                Bounds = newBounds.lo;
                AddSplit(newBounds);

                for (int i = 0; i < _points.Count; i++)
                {
                    if (hi.Bounds.Contains(_points[i]))
                    {
                        hi.Insert(_points[i]);
                        _points.RemoveAt(i);
                        Count--;
                        i--;
                    }
                }
                
                Parent.SetDirectChild(this);
                return Parent.AddChild(hi);
            }
        }

        private (B lo, B hi) FindSplit()
        {
            int axis = 0;
            int bestAxis = 0;
            double bestMedian = double.PositiveInfinity;
            foreach (var comparer in P.Comparers())
            {
                _points.Sort(comparer);
                var median = Bounds.Normalize(_points[_points.Count / 2], axis);
                if (Math.Abs(median - 0.5) < Math.Abs(bestMedian - 0.5))
                {
                    bestMedian = median;
                    bestAxis = axis;
                }
                axis++;
            }
            return Bounds.Split(bestMedian, bestAxis);
        }

        public override void AddPointsTo(List<P> result, B b)
        {
            if (b.Contains(Bounds))
            {
                for (int i = 0; i < _points.Count; i++)
                {
                    result.Add(_points[i]);
                }
            }
            else if (Bounds.Intersects(b))
            {
                // throw new Exception("checking for points on leaf: " + ToString());
                for (int i = 0; i < _points.Count; i++)
                {
                    if (b.Contains(_points[i]))
                    {
                        result.Add(_points[i]);
                    }
                }
            }
        }

        public override bool Remove(P p)
        {
            if (!Bounds.Contains(p))
                return false;

            for (int i = 0; i < _points.Count; i++)
            {
                if (_points[i].Equals(p))
                {
                    _points.RemoveAt(i);
                    Count--;
                    return true;
                }
            }
            return false;
        }

        public override string ToString(string spacer)
        {
            var str = "Leaf Node " + Bounds.ToString() + " w/ " + Count + " points:\n";
            if (_points.Count == 0)
                str += spacer;
            for (int i = 0; i < _points.Count; i++)
            {
                str += spacer + (i < _points.Count - 1 ? "├- " : "└- "); ;
                str += _points[i].ToString();
                if (i < _points.Count - 1)
                    str += "\n";
            }
            return str;
        }
    }

    internal class InternalNode<P, B> : Node<P, B>  where P : IPoint<P> where B : IBounds<P, B>
    {
        private List<Node<P, B>> _children;
        private Node<P, B> _directChild;

        private (B lo, B hi) UseDirectChildSplit()
        {
            var split = _directChild.CheckSplit();
            _directChild.DropSplit();
            if (!_directChild.IsOriginal)
                _directChild = null;
            return split;
        }

        public InternalNode(InternalNode<P, B> parent, B b, int capacity, int height) : base(parent, b, capacity, height)
        {
            _children = new List<Node<P, B>>(capacity + 1);
        }

        public override Node<P, B> Insert(P p)
        {
            if (!Bounds.Contains(p))
                return null;

            Count++;
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i].Bounds.Contains(p))
                    return _children[i].Insert(p);
            }
            return null;
        }

        public Node<P, B> AddChild(Node<P, B> n)
        {
            _children.Add(n);
            n.Parent = this;

            if (n.IsOriginal)
                SetDirectChild(n);

            if (_children.Count > Capacity)
                return Split();

            return null;
        }

        public void SetDirectChild(Node<P, B> n)
        {
            if (n == _directChild)
                return;

            var lo = n.CheckSplit().lo;
            var hi = n.CheckSplit().hi;

            B curLo = _directChild != null ? _directChild.CheckSplit().lo : default;
            B curHi = _directChild != null ? _directChild.CheckSplit().hi : default;

            if (
                // if direct child split hasn't been set yet
                (_directChild == null && Bounds.Contains(lo) && Bounds.Contains(hi)) ||

                // if new split is the largest within this node
                (Bounds.Contains(lo) && Bounds.Contains(hi) &&
                lo.Size() + hi.Size() > curLo.Size() + curHi.Size())
            )
            {
                _directChild = n;
            }
        }

        private void FindDirectChild()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i].IsOriginal)
                    SetDirectChild(_children[i]);
            }
        }

        private Node<P, B> Split()
        {
            if (_directChild == null)
                FindDirectChild();

            if (IsRoot)
            {
                var newBounds = UseDirectChildSplit();
                var newRoot = new InternalNode<P, B>(null, Bounds, Capacity, Height + 1);
                newRoot.Count = Count;
                var hi = new InternalNode<P, B>(newRoot, newBounds.hi, Capacity, Height);
                Bounds = newBounds.lo;
                AddSplit(newBounds);

                for (int i = 0; i < _children.Count; i++)
                {
                    if (hi.Bounds.Contains(_children[i].Bounds))
                    {
                        hi.AddChild(_children[i]);
                        hi.Count += _children[i].Count;
                        Count -= _children[i].Count;
                        _children.RemoveAt(i);
                        i--;
                    }
                }

                FindDirectChild();
                newRoot.AddChild(this);
                newRoot.AddChild(hi);
                return newRoot;
            }
            else
            {
                var newBounds = UseDirectChildSplit();
                var hi = new InternalNode<P, B>(Parent, newBounds.hi, Capacity, Height);
                Bounds = newBounds.lo;
                AddSplit(newBounds);

                for (int i = 0; i < _children.Count; i++)
                {
                    if (hi.Bounds.Contains(_children[i].Bounds))
                    {
                        hi.AddChild(_children[i]);
                        hi.Count += _children[i].Count;
                        Count -= _children[i].Count;
                        _children.RemoveAt(i);
                        i--;
                    }
                }

                FindDirectChild();
                Parent.SetDirectChild(this);
                return Parent.AddChild(hi);
            }
        }

        public override void AddPointsTo(List<P> result, B b)
        {
            if (!b.Intersects(Bounds))
                return;

            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i].Bounds.Intersects(b))
                    _children[i].AddPointsTo(result, b);
            }
        }

        public override bool Remove(P p)
        {
            if (!Bounds.Contains(p))
                return false;

            var removed = false;
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i].Bounds.Contains(p))
                {
                    removed = _children[i].Remove(p);
                    if (removed)
                        break;
                }
            }

            if (!removed)
                return false;

            Count--;

            if (Count == 0)
            {
                Condense();
            }

            return true;
        }

        private void Condense()
        {
            _children.Clear();
            var leaf = new LeafNode<P, B>(this, Bounds, Capacity);
            _children.Add(leaf);
            Height = 2;
        }

        public override string ToString(string spacer)
        {
            var str = "Internal Node " + Bounds.ToString() + " w/ " + Count + " points, height of " + Height + ":\n";
            if (_children.Count == 0)
                str += spacer;
            for (int i = 0; i < _children.Count; i++)
            {
                str += spacer + (i < _children.Count - 1 ? "├- " : "└- ");
                str += _children[i].ToString(spacer + (i < _children.Count - 1 ? "|  " : "   "));
                if (i < _children.Count - 1)
                    str += "\n";
            }
            return str;
        }
    }
}
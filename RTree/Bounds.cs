using System;
using System.Collections.Generic;

namespace RTree
{
    public interface IPoint<P>
    {
        public bool Equals(in P p);
        public abstract static IEnumerable<IComparer<P>> Comparers();
    }

    public readonly struct Point : IPoint<Point>
    {
        public readonly int id;
        public readonly double x;
        public readonly double y;

        public Point(int id, double x, double y)
        {
            this.id = id;
            this.x = x;
            this.y = y;
        }

        public bool Equals(in Point p)
        {
            return p.id == id;
        }

        public static IEnumerable<IComparer<Point>> Comparers()
        {
            yield return _compareX;
            yield return _compareY;
        }

        static IEnumerable<IComparer<Point>> IPoint<Point>.Comparers()
        {
            return Comparers();
        }

        private static readonly IComparer<Point> _compareX = Comparer<Point>.Create((x, y) => Comparer<double>.Default.Compare(x.x, y.x));
        private static readonly IComparer<Point> _compareY = Comparer<Point>.Create((x, y) => Comparer<double>.Default.Compare(x.y, y.y));

        public override string ToString()
        {
            return "point " + id + ": <" + Math.Round(x, 4).ToString() + ", " + Math.Round(y, 4).ToString() + ">";
        }

    }

    public interface IBounds<P, B>
    {
        public double Size();
        public bool Contains(in B b);
        public bool Contains(in P p);
        public bool Intersects(in B b);
        public double Normalize(in P p, int axis);
        public (B lo, B hi) Split(double splitAt, int axis);
    }

    public readonly struct Axis : IBounds<double, Axis>
    {
        public readonly double Min;
        public readonly double Max;

        public Axis(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(in Axis b) => Min <= b.Min && b.Max <= Max;

        public bool Contains(in double p) => Min <= p && p < Max;

        public bool Intersects(in Axis b) => Min <= b.Max && Max >= b.Min;

        public double Normalize(in double p, int axis) => (p - Min) / (Max - Min);

        public double Size() => Max - Min;

        public (Axis lo, Axis hi) Split(double splitAt, int axis) =>
                (new Axis(Min, ((Max - Min) * splitAt) + Min),
                new Axis(((Max - Min) * splitAt) + Min, Max));
    }

    public readonly struct Bounds : IBounds<Point, Bounds>
    {
        public readonly double MinX;
        public readonly double MinY;
        public readonly double MaxX;
        public readonly double MaxY;

        public Bounds(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public double Size()
        {
            return (MaxX - MinX) * (MaxY - MinY);
        }

        public bool Contains(in Bounds b)
        {
            return MinX <= b.MinX &&
            MinY <= b.MinY &&
            MaxX >= b.MaxX &&
            MaxY >= b.MaxY;
        }

        public bool Intersects(in Bounds b)
        {
            return MinX <= b.MaxX &&
            MinY <= b.MaxY &&
            MaxX >= b.MinX &&
            MaxY >= b.MinY;
        }

        public bool Contains(in Point p)
        {
            return MinX <= p.x && p.x < MaxX &&
                MinY <= p.y && p.y < MaxY;
        }

        public double Normalize(in Point p, int axis)
        {
            if (axis == 0)
            {
                return (p.x - MinX) / (MaxX - MinX);
            }
            return (p.y - MinY) / (MaxY - MinY);
        }

        public (Bounds lo, Bounds hi) Split(double splitAt, int axis)
        {
            if (axis == 0)
            {
                Bounds lo = new Bounds(MinX, MinY, ((MaxX - MinX) * splitAt) + MinX, MaxY);
                Bounds hi = new Bounds(((MaxX - MinX) * splitAt) + MinX, MinY, MaxX, MaxY);
                return (lo, hi);
            }
            else
            {
                Bounds lo = new Bounds(MinX, MinY, MaxX, ((MaxY - MinY) * splitAt) + MinY);
                Bounds hi = new Bounds(MinX, ((MaxY - MinY) * splitAt) + MinY, MaxX, MaxY);
                return (lo, hi);
            }
        }

        public override string ToString()
        {
            return "[min: (" + Math.Round(MinX, 4).ToString() + ", " + Math.Round(MinY, 4).ToString() + "), max: (" + Math.Round(MaxX, 4).ToString() + ", " + Math.Round(MaxY, 4).ToString() + ")]";
        }
    }
}
using RTree;
using FileInitializer;

namespace RTree.Tests;

public class UnitTest1
{
    [Fact]
    public void Insert()
    {
        Logs.InitPath("logs/insert");
        Logs.InitFiles("logs/insert/tree.log");

        var tree = new RTree<Point, Bounds>(new Bounds(0, 0, 1, 1), 16);
        var seed = DateTimeOffset.UtcNow.Millisecond;
        var r = new Random(seed);
        File.AppendAllText("logs/insert/tree.log", "seed is: " + seed + "\n\n\n");

        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var count = 10000000;

        for (int i = 0; i < count; i++)
        {
            var p = new Point(i, r.NextDouble(), r.NextDouble());
            tree.Insert(p);
        }

        var end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        File.AppendAllText("logs/insert/tree.log", "insertion of " + count + " points took " + (end - start) + "ms, tree has height of " + tree.Height + "\n\n");
        // File.AppendAllText("logs/insert/tree.log", tree.ToString() + "\n\n\n\n");

        var searches = 1000;
        long avg = 0;
        double area = 0;
        List<Point> result = new();

        for (int i = 0; i < searches; i++)
        {
            var minX = r.NextDouble();
            var minY = r.NextDouble();
            var bounds = new Bounds(minX, minY, minX + 0.5, minY + 0.5);
            area += bounds.Size();
            result.Clear();
            start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            tree.GetPointsInNonAlloc(result, bounds);
            avg += DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
        }

        File.AppendAllText("logs/insert/tree.log", searches + " searches with avg size of " + Math.Round((area / searches), 2) + "units^2 took on avg " + (avg / searches) + "ms\n\n");

        Assert.True(tree.Count == count, "tree did not insert " + count + " points, only got " + tree.Count);
    }

    [Fact]
    public void Remove()
    {
        Logs.InitPath("logs/remove");
        Logs.InitFiles("logs/remove/tree.log");

        var tree = new RTree<Point, Bounds>(new Bounds(0, 0, 1, 1), 4);
        var seed = DateTimeOffset.UtcNow.Millisecond;
        var r = new Random(659);
        File.AppendAllText("logs/remove/tree.log", "seed is: " + seed + "\n\n\n");

        var count = 20;

        for (int i = 0; i < count; i++)
        {
            var p = new Point(i, r.NextDouble(), r.NextDouble());
            tree.Insert(p);
        }

        File.AppendAllText("logs/remove/tree.log", tree.ToString() + "\n\n\n\n");

        tree.Remove(new Point(3, 0.1754, 0.0522));
        tree.Remove(new Point(0, 0.0045, 0.1802));
        tree.Remove(new Point(19, 0.1981, 0.1397));

        tree.Remove(new Point(11, 0.3662, 0.2516));
        tree.Remove(new Point(12, 0.2857, 0.3839));

        tree.Remove(new Point(16, 0.2969, 0.5101));
        tree.Remove(new Point(10, 0.3786, 0.5249));
        tree.Remove(new Point(7, 0.2637, 0.5896));

        File.AppendAllText("logs/remove/tree.log", tree.ToString() + "\n\n\n\n");

        tree.Insert(new Point(3, 0.1754, 0.0522));
        tree.Insert(new Point(0, 0.0045, 0.1802));
        tree.Insert(new Point(19, 0.1981, 0.1397));

        tree.Insert(new Point(11, 0.3662, 0.2516));
        tree.Insert(new Point(12, 0.2857, 0.3839));

        tree.Insert(new Point(16, 0.2969, 0.5101));
        tree.Insert(new Point(10, 0.3786, 0.5249));
        tree.Insert(new Point(7, 0.2637, 0.5896));

        File.AppendAllText("logs/remove/tree.log", tree.ToString() + "\n\n\n\n");

        Assert.True(tree.Count == 19, "tree should have had point 5 removed");
    }

    [Fact]
    public void InsertAndRemove()
    {
        Logs.InitPath("logs/iar");
        Logs.InitFiles("logs/iar/tree.log");

        var tree = new RTree<Point, Bounds>(new Bounds(0, 0, 1, 1), 16);
        var seed = DateTimeOffset.UtcNow.Millisecond;
        var r = new Random(seed);
        File.AppendAllText("logs/iar/tree.log", "seed is: " + seed + "\n\n\n");

        long avgInsert = 0;
        long inserts = 0;
        long avgRemove = 0;
        long removals = 0;

        var count = 10000000;
        var chance = 0.01;

        List<(bool exists, Point p)> points = new();

        for (int i = 0; i < count; i++)
        {
            if (r.NextDouble() < chance && tree.Count > 0)
            {
                var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                int pick = (int)(r.NextInt64() % points.Count);
                var pair = points[pick];
                if (!pair.exists)
                    continue;
                tree.Remove(pair.p);
                points[pick] = (false, pair.p);
                var end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                avgRemove += end - start;
                removals++;
            }
            else
            {
                var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var p = new Point(i, r.NextDouble(), r.NextDouble());
                tree.Insert(p);
                points.Add((true, p));
                var end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                avgInsert += end - start;
                inserts++;
            }
        }


        File.AppendAllText("logs/iar/tree.log", "avg insert time: " + (avgInsert / (float)inserts) + "ms ; avg removal time: " + (avgRemove / (float)removals) + "ms\n\n");
        // File.AppendAllText("logs/insert/tree.log", tree.ToString() + "\n\n\n\n");
    }

    public readonly struct Point4D : IPoint<Point4D>
    {
        public readonly int id;
        public readonly double x;
        public readonly double y;
        public readonly double z;
        public readonly double w;

        public Point4D(int id, double x, double y, double z, double w)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public bool Equals(in Point4D p) => id == p.id;

        public static IEnumerable<IComparer<Point4D>> Comparers()
        {
            yield return _compareX;
            yield return _compareY;
            yield return _compareZ;
            yield return _compareW;
        }

        private static readonly IComparer<Point4D> _compareX = Comparer<Point4D>.Create((x, y) => Comparer<double>.Default.Compare(x.x, y.x));
        private static readonly IComparer<Point4D> _compareY = Comparer<Point4D>.Create((x, y) => Comparer<double>.Default.Compare(x.y, y.y));
        private static readonly IComparer<Point4D> _compareZ = Comparer<Point4D>.Create((x, y) => Comparer<double>.Default.Compare(x.z, y.z));
        private static readonly IComparer<Point4D> _compareW = Comparer<Point4D>.Create((x, y) => Comparer<double>.Default.Compare(x.w, y.w));

        public override string ToString()
        {
            return "point " + id + ": <" + Math.Round(x, 4).ToString() + ", " + Math.Round(y, 4).ToString() + ", " + Math.Round(z, 4).ToString() + ", " + Math.Round(w, 4).ToString() + ">";
        }
    }

    public readonly struct Bounds4D : IBounds<Point4D, Bounds4D>
    {
        public readonly Axis x;
        public readonly Axis y;
        public readonly Axis z;
        public readonly Axis w;

        public Bounds4D(double minX, double minY, double minZ, double minW,
            double maxX, double maxY, double maxZ, double maxW)
        {
            x = new Axis(minX, maxX);
            y = new Axis(minY, maxY);
            z = new Axis(minZ, maxZ);
            w = new Axis(minW, maxW);
        }

        public bool Contains(in Bounds4D b) => x.Contains(b.x) && y.Contains(b.y) && z.Contains(b.z) && w.Contains(b.w);

        public bool Contains(in Point4D p) => x.Contains(p.x) && y.Contains(p.y) && z.Contains(p.z) && w.Contains(p.w);

        public bool Intersects(in Bounds4D b) => x.Intersects(b.x) && y.Intersects(b.y) && z.Intersects(b.z) && w.Intersects(b.w);

        public double Normalize(in Point4D p, int axis)
        {
            if (axis == 0)
                return x.Normalize(p.x, 0);
            else if (axis == 1)
                return y.Normalize(p.y, 0);
            else if (axis == 2)
                return z.Normalize(p.z, 0);
            else
                return w.Normalize(p.w, 0);
        }

        public double Size() => x.Size() * y.Size() * z.Size() * w.Size();

        public (Bounds4D lo, Bounds4D hi) Split(double splitAt, int axis)
        {
            if (axis == 0)
            {
                var split = x.Split(splitAt, 0);
                return (new Bounds4D(split.lo.Min, y.Min, z.Min, w.Min, split.lo.Max, y.Max, z.Max, w.Max),
                    new Bounds4D(split.hi.Min, y.Min, z.Min, w.Min, split.hi.Max, y.Max, z.Max, w.Max));
            }
            else if (axis == 1)
            {
                var split = y.Split(splitAt, 0);
                return (new Bounds4D(x.Min, split.lo.Min, z.Min, w.Min, x.Max, split.lo.Max, z.Max, w.Max),
                    new Bounds4D(x.Min, split.hi.Min, z.Min, w.Min, x.Max, split.hi.Max, z.Max, w.Max));
            }
            else if (axis == 2)
            {
                var split = z.Split(splitAt, 0);
                return (new Bounds4D(x.Min, y.Min, split.lo.Min, w.Min, x.Max, y.Max, split.lo.Max, w.Max),
                    new Bounds4D(x.Min, y.Min, split.hi.Min, w.Min, x.Max, y.Max, split.hi.Max, w.Max));
            }
            else
            {
                var split = w.Split(splitAt, 0);
                return (new Bounds4D(x.Min, y.Min, z.Min, split.lo.Min, x.Max, y.Max, z.Max, split.lo.Max),
                    new Bounds4D(x.Min, y.Min, z.Min, split.hi.Min, x.Max, y.Max, z.Max, split.hi.Max));
            }
        }

        public override string ToString()
        {
            return "[min: (" + Math.Round(x.Min, 4).ToString() + ", " + Math.Round(y.Min, 4).ToString() + ", " + Math.Round(z.Min, 4).ToString() + ", " + Math.Round(w.Min, 4).ToString() + "), max: (" +
            Math.Round(x.Max, 4).ToString() + ", " + Math.Round(y.Max, 4).ToString() + ", " + Math.Round(z.Max, 4).ToString() + ", " + Math.Round(w.Max, 4).ToString() + ")]";
        }
    }

    [Fact]
    public void Test4D()
    {
        Logs.InitPath("logs/insert");
        Logs.InitFiles("logs/insert/tree.log");

        var tree = new RTree<Point4D, Bounds4D>(new Bounds4D(0, 0, 0, 0, 1, 1, 1, 1), 16);
        var seed = DateTimeOffset.UtcNow.Millisecond;
        var r = new Random(seed);
        File.AppendAllText("logs/insert/tree.log", "seed is: " + seed + "\n\n\n");

        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var count = 10000000;

        for (int i = 0; i < count; i++)
        {
            var p = new Point4D(i, r.NextDouble(), r.NextDouble(), r.NextDouble(), r.NextDouble());
            tree.Insert(p);
        }

        var end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        File.AppendAllText("logs/insert/tree.log", "insertion of " + count + " points took " + (end - start) + "ms, tree has height of " + tree.Height + "\n\n");
        // File.AppendAllText("logs/insert/tree.log", tree.ToString() + "\n\n\n\n");

        var searches = 1000;
        long avg = 0;
        double area = 0;
        List<Point4D> result = new();

        // tree.GetPointsInNonAlloc(result, new Bounds4D(0.2, 0.2, 0.2, 0.2, 0.8, 0.8, 0.8, 0.8));

        // var str = "";
        // foreach (var p in result)
        // {
        //     str += p + "\n";
        // }
        // File.AppendAllText("logs/insert/tree.log", str + "\n\n\n\n");

        for (int i = 0; i < searches; i++)
        {
            var minX = r.NextDouble();
            var minY = r.NextDouble();
            var minZ = r.NextDouble();
            var minW = r.NextDouble();
            var bounds = new Bounds4D(minX, minY, minZ, minW, minX + 0.8, minY + 0.8, minZ + 0.8, minW + 0.8);
            area += bounds.Size();
            result.Clear();
            start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            tree.GetPointsInNonAlloc(result, bounds);
            avg += DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start;
        }

        File.AppendAllText("logs/insert/tree.log", searches + " searches with avg size of " + Math.Round((area / searches), 2) + "units^4 took on avg " + (avg / searches) + "ms\n\n");

        Assert.True(tree.Count == count, "tree did not insert " + count + " points, only got " + tree.Count);
    }
}

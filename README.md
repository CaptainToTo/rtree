# Modified R-Tree

A Modified R-Tree created with C# which subdivides parent node bounds instead of tightly fitting around contents.
I made this as an experience. It isn't fully tested, and I have no idea if this is useful or not.

To create an R-Tree, it requires a Point type, and Bounds type, which implement the `IPoint` and `IBounds` interfaces respectively.
A Bounds type can consist of a bunch of `Axis` structs for convenience.

I'm probably not going to continue working on this.

using System.Data;
using System.Numerics;

namespace Delaunay2D
{
    public class QuarterEdge
    {

        public Vector2 Location // The origin vertex of the QuarterEdge
        { get; private set; }

        public QuarterEdge NextQuarterEdge // The QuarterEdge reached by counter-clockwise rotation
        { get; set; }

        public QuarterEdge RotationQuarterEdge // The next in the set of four QuarterEdges defining a QuadEdge
        { get; set; }

        public QuarterEdge()
        {
            Location = new Vector2(0.0f,0.0f);
            NextQuarterEdge = this;
            RotationQuarterEdge = this;
        }

        // Implementation follows quarter_edge from https://ianthehenry.com/posts/delaunay/
        public QuarterEdge Next()
        {
            return NextQuarterEdge;
        }

        public QuarterEdge Rot()
        {
            return RotationQuarterEdge;
        }

        public QuarterEdge Sym()
        {
            return Rot().Rot();
        }

        public QuarterEdge Tor()
        {
            return Rot().Rot().Rot();
        }

        public QuarterEdge Prev()
        {
            return Rot().Next().Rot();
        }

        public QuarterEdge LNext()
        {
            return Tor().Next().Rot();
        }

        public Vector2 Dest()
        {
            return Sym().Location;
        }

        public void Flip()
        {
            // Convert QuarterEdge spanning a quadrangle so that it
            // spans the other two points of the quadrangle
            QuarterEdge a = this.Prev();
            QuarterEdge b = this.Sym().Prev();
            Splice(this, a);
            Splice(this.Sym(), b);
            Splice(this, a.LNext());
            Splice(this.Sym(), b.LNext());
            Location = a.Dest();
            Sym().Location = b.Dest(); // NB: Dest == Sym.Location
        }

        public override string ToString()
        {
            return $"QuarterEdge from {this.Location} to {this.Dest()}";
        }

        // Static methods

        public static QuarterEdge CreateQuadEdge(Vector2 start, Vector2 end)
        {
            QuarterEdge startEnd = new();
            QuarterEdge leftRight = new();
            QuarterEdge endStart = new();
            QuarterEdge rightLeft = new();

            // Set the origin points for those quarter-edges which are actually defined
            // leftRight and rightLeft do not have a location (!)
            startEnd.Location = start;
            endStart.Location = end;

            // A QuadEdge is defined by these being circular
            startEnd.RotationQuarterEdge = leftRight;
            leftRight.RotationQuarterEdge = endStart;
            endStart.RotationQuarterEdge = rightLeft;
            rightLeft.RotationQuarterEdge = startEnd;

            // Initially the vertex-vertex quarter-edges have _themselves_
            // as their "next"s
            startEnd.NextQuarterEdge = startEnd;
            endStart.NextQuarterEdge = endStart;

            // However the face-face quarter-edges point to _each other_
            leftRight.NextQuarterEdge = rightLeft;
            rightLeft.NextQuarterEdge = leftRight;

            // Just return the starting QuarterEdge to define the QuadEdge
            return startEnd;
        }

        public static void Splice(QuarterEdge a, QuarterEdge b)
        {
            // Voodoo
            SwapNexts(a.Next().Rot(), b.Next().Rot());
            SwapNexts(a, b);
        }

        public static void SwapNexts(QuarterEdge a, QuarterEdge b)
        {
            QuarterEdge aNext = a.Next();
            a.NextQuarterEdge = b.Next();
            b.NextQuarterEdge = aNext;
        }

        public static QuarterEdge CreateTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            QuarterEdge ab = CreateQuadEdge(a,b);
            QuarterEdge bc = CreateQuadEdge(b,c);
            QuarterEdge ca = CreateQuadEdge(c,a);

            Splice(ab.Sym(), bc);
            Splice(bc.Sym(), ca);
            Splice(ca.Sym(), ab);

            return ab;
        }

        public static QuarterEdge Connect(QuarterEdge a, QuarterEdge b)
        {
            QuarterEdge newEdge = CreateQuadEdge(a.Dest(), b.Location);
            Splice(newEdge, a.LNext());
            Splice(newEdge.Sym(), b);
            return newEdge;
        }

        public static void Sever(QuarterEdge edge)
        {
            Splice(edge,edge.Prev());
            Splice(edge.Sym(), edge.Sym().Prev());
        }

        public static QuarterEdge InsertPoint(QuarterEdge polyEdge, Vector2 point)
        {
            QuarterEdge firstSpoke = CreateQuadEdge(polyEdge.Location, point);
            Splice(firstSpoke, polyEdge);
            QuarterEdge spoke = firstSpoke;
            QuarterEdge edge = polyEdge;
            // Do-while means that the code always executes at least once
            do {
                spoke = Connect(edge,spoke.Sym());
                // Original post explicitly sets Tor() and Rot() locations
                // to null, but that seems unnecessary here
                edge = spoke.Prev();
            } while (edge.LNext() != firstSpoke);
            return firstSpoke;
        }

    }
}
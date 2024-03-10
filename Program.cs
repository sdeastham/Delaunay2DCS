
using System.Diagnostics;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Random;

namespace Delaunay2D
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Console.WriteLine("Hello, World!");
            bool debug = true;

            // Generate vector of points
            int nPoints = 100;
            Vector2[] points = new Vector2[nPoints];
            System.Random rng;

            // Set to an int if debugging, otherwise use null
            int? seed = 1095132;
            if (seed != null)
            {
                rng = new SystemRandomSource((int)seed);
            }
            else
            {
                rng = SystemRandomSource.Default;
            }

            double xMin = 0.0;
            double xMax = 100.0;
            double yMin = 0.0;
            double yMax = 200.0;
            double xSpan = xMax - xMin;
            double ySpan = yMax - yMin;
            for (int i=0; i<nPoints; i++)
            {
                points[i] = new Vector2((float)(rng.NextDouble() * xSpan + xMin),(float)(rng.NextDouble() * ySpan + yMin));
                Console.WriteLine($"{points[i].X,6:f2}, {points[i].Y,6:f2}");
            }

            // Need to define a special "boundary triangle"
            float xyPad = (float)(xSpan + ySpan);
            Vector2 lowerLeftBound = new(-xyPad + (float)(xMin - xSpan/2.0),-xyPad + (float)yMin);
            Vector2 lowerRightBound = new(xyPad + (float)(xMax + xSpan/2.0),-xyPad + (float)yMin);
            Vector2 upperBound = new((float)(xMin + xSpan/2.0),xyPad + (float)(yMax + ySpan/2.0));
            QuarterEdge firstQE = QuarterEdge.CreateTriangle(lowerLeftBound,lowerRightBound,upperBound);

            // Start adding points...
            QuarterEdge currentQE = firstQE;
            //for (int i=0; i<nPoints; i++)
            foreach( Vector2 point in points )
            {
                if (debug) Console.WriteLine($"Placing point at {point.X,6:f2}/{point.Y,6:f2}");
                Vector2[] triangle = new Vector2[3];
                // For debugging
                List<QuarterEdge> testedEdges = [];
                bool triangleFound = false;
                QuarterEdge qe1, qe2, qe3;
                while (!triangleFound)
                {
                    // Define the triangle by moving clockwise from the current edge
                    if (debug)
                    {
                        Console.WriteLine($"Testing edge from {currentQE.Location} to {currentQE.Dest()}");
                        if (testedEdges.Any(item => item == currentQE))
                        {
                            throw new InvalidOperationException("Trapped in circular logic loop");
                        }
                        testedEdges.Add(currentQE);
                    }
                    qe1 = currentQE;
                    qe2 = qe1.LNext();
                    qe3 = qe2.LNext();
                    triangle[0] = qe1.Location;
                    triangle[1] = qe2.Location;
                    triangle[2] = qe3.Location;

                    // We've found the triangle if the point is left of all three edges
                    bool pointOnLeft = IsTriangleOrientedAreaNegative([qe1.Location,qe2.Location,point]);
                    if (pointOnLeft)
                    {
                        triangleFound = (IsTriangleOrientedAreaNegative([qe2.Location,qe3.Location,point]) && 
                                         IsTriangleOrientedAreaNegative([qe3.Location,qe1.Location,point]));
                    }

                    if (!triangleFound)
                    {
                        // Was the point to the left of firstQE, or to the right?                        
                        if (pointOnLeft)
                        {
                            currentQE = qe1.Next();
                        }
                        else
                        {
                            currentQE = qe1.Sym();
                        }
                    }
                }

                // Triangle has been found!
                //QuarterEdge newSpoke = QuarterEdge.InsertPoint(currentQE,point);
                currentQE = QuarterEdge.InsertPoint(currentQE,point);
            }

            List<QuarterEdge> edges = [];
            // Use a recursive pattern to get all the edges,
            // starting from the last one created (the only
            // we can have total confidence still exists
            TraverseMesh(currentQE,edges);
            Plotting.PlotMesh(edges);
            /*
            foreach (QuarterEdge edge in edges)
            {
                Console.WriteLine(edge);
            }
            */
        }

        public static void TraverseMesh(QuarterEdge edge, List<QuarterEdge> edgeList)
        {
            QuarterEdge nextEdge = edge;
            do {
                // If this edge has not previously been seen, store it and then
                // start searching from its end point
                if (!edgeList.Any(item => item == nextEdge))
                {
                    edgeList.Add(nextEdge);
                    TraverseMesh(nextEdge.Sym(),edgeList);
                }
                nextEdge = nextEdge.Next();
            } while (nextEdge != edge);
        }

        public static bool IsTriangleOrientedAreaNegative(Vector2[] triangle)
        {
            // If negative, that indicates that the vertices of the triangles are listed in
            // clockwise order; if positive, they are listed in counter-clockwise order.
            // Equivalently, for directed edge A->B, this function will return true if
            // point C is left of AB.
            Vector2 a = triangle[0];
            Vector2 b = triangle[1];
            Vector2 c = triangle[2];
            float [,] x = {{ a.X, a.Y, 1 },
                           { b.X, b.Y, 1 },
                           { c.X, c.Y, 1 }};
            Matrix<float> m = Matrix<float>.Build.DenseOfArray(x);
            return m.Determinant() < 0;
        }

        public static bool IsPointInCircle(Vector2[] triangle, Vector2 point)
        {
            // WARNING: Triangle points must be clockwise! Otherwise the sign
            // flips and positive means point is outside circle, not inside
            
            // TODO: This could be made more efficient by storing each point's magnitude
            Vector2 a = triangle[0];
            Vector2 b = triangle[1];
            Vector2 c = triangle[2];
            float az = MagnitudeSquared(a);
            float bz = MagnitudeSquared(b);
            float cz = MagnitudeSquared(c);
            float pz = MagnitudeSquared(point);
            float [,] x = {{ a.X - point.X, a.Y - point.Y, az - pz },
                           { b.X - point.X, b.Y - point.Y, bz - pz },
                           { c.X - point.X, c.Y - point.Y, cz - pz }};
            Matrix<float> m = Matrix<float>.Build.DenseOfArray(x);
            return m.Determinant() > 0;
        }

        public static float MagnitudeSquared(Vector2 point)
        {
            return point.X * point.X + point.Y * point.Y;
        }
    }
}
﻿
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
            int nPoints = 4;
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
            Vector2[] boundaryVertices = [lowerLeftBound,lowerRightBound,upperBound];
            QuarterEdge[] boundaryEdges = [firstQE,firstQE.LNext(),firstQE.LNext().LNext()];

            // Start adding points...
            QuarterEdge currentQE = firstQE;
            //for (int i=0; i<nPoints; i++)
            foreach( Vector2 point in points )
            {
                if (debug) Console.WriteLine($"Placing point at {point.X,6:f2}/{point.Y,6:f2}");
                Vector2[] triangle = new Vector2[3];
                // For debugging
                List<QuarterEdge> testedEdges = [];
                // TODO: Need to add logic for a colinear point
                bool triangleFound = false;
                QuarterEdge qe1, qe2, qe3;
                while (!triangleFound)
                {
                    // Define the triangle by moving clockwise from the current edge
                    if (debug)
                    {
                        Console.WriteLine($"Testing edge from {currentQE.Location} to {currentQE.Dest()}");
                        // Can end up in a circular loop if the point is outside the original bounding triangle
                        // Would be easier to do this if we just created a bounding square..
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
                QuarterEdge spoke = QuarterEdge.InsertPoint(currentQE,point);

                // Now the hard part - the actual Delaunay..ing
                // Traverse the ring of points around the new point, and check to see if they need to flip
                // Remember: we defined triangles as being on the left of a given edge (for the purposes of
                // determining which triangle we were inserting into).
                bool doFlips = true;
                if (doFlips)
                {
                    QuarterEdge firstEdge = currentQE;
                    do{
                        // Check if we need to flip
                        Vector2 A = currentQE.Location;
                        Vector2 B = currentQE.Dest();
                        Vector2 C = currentQE.Next().Dest();
                        Vector2 D = currentQE.Prev().Dest();

                        // Never flip a boundary edge
                        bool boundaryEdge = boundaryEdges.Contains(currentQE) || boundaryEdges.Contains(currentQE.Sym());
                        bool flip = !boundaryEdge;
                        Console.WriteLine($"Passed boundary edge test: {flip}");
                        if (flip)
                        {
                            // Only flip if this will not cause an inside-out triangle (?)
                            bool boundaryA = boundaryVertices.Contains(A);
                            // Cannot both be boundary vertices (as already checked for a boundary edge)
                            bool boundaryB = (!boundaryA) && boundaryVertices.Contains(B);
                            bool boundaryVertex = boundaryA || boundaryB;
                            // This needs fixing
                            if (boundaryA)
                            {
                                // Use the triangle defined by the boundary vertex and the two points which the
                                // flip would move things to
                                triangle = [A, currentQE.Next().Dest(), currentQE.Prev().Dest()];
                            }
                            if (boundaryB)
                            {
                                triangle = [B, currentQE.Sym().Next().Dest(), currentQE.Sym().Prev().Dest()];
                            }
                            flip = (boundaryVertex && 
                                    IsTriangleOrientedAreaNegative([triangle[0],triangle[1],point]) && 
                                    IsTriangleOrientedAreaNegative([triangle[1],triangle[2],point]) && 
                                    IsTriangleOrientedAreaNegative([triangle[2],triangle[0],point]));
                            Console.WriteLine($"Passed inside-out triangle test: {flip} [{boundaryA}/{boundaryB}]");
                        }
                        // Hardest check - do not flip if the edge is separating the newly-inserted point from a boundary vertex
                        if (flip)
                        {
                            // Figure out which two vertices the flipped edge will connect to
                            // If these are a boundary vertex and the inserted point, don't do it!
                            Vector2 testVtxA = currentQE.Prev().Dest();
                            Vector2 testVtxB = currentQE.Sym().Prev().Dest();
                            flip = !((boundaryVertices.Contains(testVtxA) && (testVtxB == point)) || 
                                    (boundaryVertices.Contains(testVtxB) && (testVtxA == point)));
                            Console.WriteLine($"Passed boundary vertex test: {flip}");
                        }
                        // Final check - whether or not we actually do want to flip!
                        if (flip)
                        {
                            flip = IsPointInCircle([A,C,B],D) || IsPointInCircle([A,B,D],C);
                            Console.WriteLine($"Passed point in circle test: {flip}");
                        }
                        if (flip)
                        {
                            // Store the edge that will become the next one after the flip is complete
                            QuarterEdge nextQE = currentQE.Sym().Next();
                            // Do the flip!
                            Console.WriteLine(currentQE);
                            currentQE.Flip();
                            Console.WriteLine(currentQE);
                            // Reverse to the previous member (ring has expanded)
                            //currentQE = currentQE.Prev();
                            currentQE = nextQE;
                            Console.WriteLine(currentQE);
                        }
                        else
                        {
                            // Advance to the next member of the ring
                            // Flip the edge so that the new point is still
                            // left of the current edge
                            currentQE = currentQE.LNext().LNext().Sym();
                        }
                    } while (firstEdge != currentQE);
                }
            }

            // Use a recursive pattern to get all the edges,
            // starting from the last one created (the only
            // we can have total confidence still exists
            List<QuarterEdge> edges = [];
            TraverseMesh(currentQE,edges);
            //Plotting.PlotMesh(edges,boundaryVertices);
            Plotting.PlotMesh(edges);
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
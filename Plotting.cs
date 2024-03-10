using ScottPlot;
using System.Numerics;
namespace Delaunay2D
{
    public static class Plotting
    {
        public static void PlotMesh(List<QuarterEdge> edges, Vector2[] boundaryVertices)
        {
            ScottPlot.Plot axes = new();
            foreach (QuarterEdge edge in edges)
            {
                if (boundaryVertices.Contains(edge.Location) || boundaryVertices.Contains(edge.Dest()))
                {
                    continue;
                }
                axes.Add.Line(edge.Location.X,edge.Location.Y,edge.Dest().X,edge.Dest().Y);
            }
            axes.SavePng("Mesh.png",400,300);
        }

        public static void PlotMesh(List<QuarterEdge> edges)
        {
            ScottPlot.Plot axes = new();
            foreach (QuarterEdge edge in edges)
            {
                axes.Add.Line(edge.Location.X,edge.Location.Y,edge.Dest().X,edge.Dest().Y);
            }
            axes.SavePng("Mesh.png",400,300);
        }
    }
}
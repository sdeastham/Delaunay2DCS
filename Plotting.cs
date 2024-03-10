using ScottPlot;
namespace Delaunay2D
{
    public static class Plotting
    {
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
using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_SweepByPath : TestCase
    {
        public Geometry_SweepByPath()
        {
            Title = "路径扫掠";
            Description = "多截面沿路径扫掠生成壳体";
            Tags.Add("几何");
            Tags.Add("扫掠");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var shape = ShapeIO.Open(GetResourcePath("sweep/2.igs"));
            if(shape == null)
            {
                return;
            }

            var edges = shape.GetChildren(EnumTopoShapeType.Topo_EDGE);


            int pathIdx = 18;
            var path = edges[pathIdx];
            var path2 = edges[pathIdx+1];
            edges.RemoveAt(pathIdx);
            edges.RemoveAt(pathIdx);
            //foreach (var edge in edges)
            //{
            //    render.ShowShape(edge, ColorTable.RandomColor());
            //}
            render.ShowSceneNode(BrepSceneNode.Create(path, ColorTable.RandomColor(), true));
            render.ShowSceneNode(BrepSceneNode.Create(path2, ColorTable.RandomColor(), true));

            TopoShapeList sections = new TopoShapeList();
            for(int ii=0; ii<3; ii++)
            {
                sections.Add(edges[ii]);
                render.ShowSceneNode(BrepSceneNode.Create(edges[ii], ColorTable.RandomColor(), true));
            }
            sections.Add(edges[edges.Count-1]);
            render.ShowSceneNode(BrepSceneNode.Create(edges[edges.Count - 1], ColorTable.RandomColor(), true));
            //var shell = FeatureTool.Loft(edges, false, true);
            var shell = FeatureTool.SweepBySections(sections, path, EnumSweepTransitionMode.RoundCorner, false, true, true);
            render.ShowSceneNode(BrepSceneNode.Create(shell, ColorTable.RandomColor(), true));
        }
    }
}

using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_Wrap : TestCase
    {
        public Geometry_Wrap()
        {
            Title = "曲线缠绕";
            Description = "将平面曲线沿柱面缠绕投影";
            Tags.Add("几何");
            Tags.Add("缠绕");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {           
            var dxf = ShapeIO.Open(GetResourcePath("dxf/tiger.dxf"));

            render.ShowSceneNode(BrepSceneNode.Create(dxf, ColorTable.LightGreen, true));

            var circle = CurveBuilder.MakeCircle(new GPnt(), 50, new GDir(0, 0, 1));
            var face = FeatureTool.Extrude(circle, 500, new GDir(0, 0, -1));

            foreach (var edge in dxf.GetChildren(EnumTopoShapeType.Topo_EDGE))
            {
                var curves = AdvFeatureTool.Wrapping(edge, face);

                render.ShowSceneNode(BrepSceneNode.Create(curves, ColorTable.Red, true));
            }
            render.ShowSceneNode(BrepSceneNode.Create(face, ColorTable.LightGray, true));
        }
    }
}

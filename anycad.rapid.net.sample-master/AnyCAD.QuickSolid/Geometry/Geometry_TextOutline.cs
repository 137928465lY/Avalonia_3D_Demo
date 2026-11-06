using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_TextOutline : TestCase
    {
        public Geometry_TextOutline()
        {
            Title = "文字轮廓";
            Description = "生成文字轮廓线并沿曲线放置坐标系";
            Tags.Add("几何");
            Tags.Add("文字");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var wires = AdvCurveBuilder.MakeText("招财进宝", "");

            var edges = wires.GetChildren(EnumTopoShapeType.Topo_EDGE);
            for(var i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];

                var curve = new ParametricCurve(edge);
                var ps = curve.SplitByUniformLength(20, GP.Resolution());
                foreach(var p in ps)
                {
                    var d1 = curve.D1(p);
                    var dir = d1.GetVectors()[0];
                    var trf = new GAx2(d1.GetPoint(), new GDir(0, 0, 1), new GDir(dir));
                    var ax = AxWidget.Create(new Vector3(20), 1);
                    ax.SetTransform(Matrix4d.From(trf.ToTrsf()));

                    render.ShowSceneNode(ax);
                }
            }

            render.ShowSceneNode(BrepSceneNode.Create(wires, ColorTable.玫瑰棕色, true));
        }
    }
}

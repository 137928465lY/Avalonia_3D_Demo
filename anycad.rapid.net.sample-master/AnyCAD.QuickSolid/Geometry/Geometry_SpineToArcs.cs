using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_SplineToArcs : TestCase
    {
        public Geometry_SplineToArcs()
        {
            Title = "样条转弧";
            Description = "将B样条边缘离散为圆弧序列";
            Tags.Add("几何");
            Tags.Add("圆弧拟合");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var face = ShapeIO.Open(GetResourcePath("Split/face.brep"));
            var edges = face.GetChildren(EnumTopoShapeType.Topo_EDGE);
            foreach(var edge in edges)
            {
                ParametricCurve pc = new ParametricCurve();
                if (!pc.Initialize(edge))
                    continue;

                if(pc.GetCurveType() == EnumCurveType.CurveType_BSplineCurve)
                {
                    var simpleCurve = AdvCurveBuilder.SplineToArcs(edge, 0.01);
                    foreach(var curve in simpleCurve)
                    {
                        render.ShowSceneNode(BrepSceneNode.Create(curve, ColorTable.Red, true));
                    }

                    var node = BrepSceneNode.Create(edge, ColorTable.Green, true);
                    render.ShowSceneNode(node);
                    node.SetTransform(Matrix4d.makeTranslate(0,0,1));
                }
            }
        }
    }
}

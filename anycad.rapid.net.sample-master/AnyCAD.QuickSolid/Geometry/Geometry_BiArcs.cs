using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_BiArcsBasic : TestCase
    {
        public Geometry_BiArcsBasic()
        {
            Title = "样条转圆弧";
            Description = "用双圆弧逼近B样条边缘曲线";
            Tags.Add("几何");
            Tags.Add("圆弧拟合");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            //Case1(render);
            Case2(render);
        }
        void Case1(IRenderView render)
        {
            var face = ShapeIO.Open(GetResourcePath("Split/face.brep"));
            var edges = face.GetChildren(EnumTopoShapeType.Topo_EDGE);
            var plane = new GPln();

            foreach (var edge in edges)
            {
                var node = BrepSceneNode.Create(edge, ColorTable.Green, true);
                render.ShowSceneNode(node);
                node.SetTransform(Matrix4d.makeTranslate(0, 0, 1));

                var simpleCurve = AdvCurveBuilder.SplineToArcs(edge, 0.1);
                foreach (var curve in simpleCurve)
                {
                    render.ShowSceneNode(BrepSceneNode.Create(curve, ColorTable.Red, true));
                }
            }
        }
        void Case2(IRenderView render)
        {
            string fileName1 = GetResourcePath("wire/wire_points.brep");
            var wire = ShapeIO.Open(fileName1);
            if (wire == null)
                return;

           // wire = FixShapeTool.FixWire(wire, 0.1);

            render.ShowSceneNode(BrepSceneNode.Create(wire, 0.1));

            ParametricCurve pc = new ParametricCurve(wire);
            if (!pc.IsValidGeometry())
                return;
            var arrD = pc.SplitByUniformLength(2, 0.01);
            ParticleSceneNode ps = ParticleSceneNode.Create((uint)arrD.Count, ColorTable.Red, 5);
            uint ii = 0;
            foreach (var d in arrD)
            {
                var pt = pc.Value(d);
                ps.SetPosition(ii, Vector3.From(pt));
                ++ii;
            }
            ps.RequestUpdate();

            render.ShowSceneNode(ps);

        }
    }
}

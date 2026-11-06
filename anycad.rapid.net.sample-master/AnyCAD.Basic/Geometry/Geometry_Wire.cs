using AnyCAD.Foundation;
using System.Collections.Generic;


namespace AnyCAD.Demo.Geometry
{
    class Geometry_Wire : TestCase
    {
        public Geometry_Wire()
        {
            Title = "提取线框";
            Description = "从封头STEP模型中提取指定边成线框。";
            Tags.Add("几何");
            Tags.Add("线框");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            Case1(render);
            //Case2(render);
        }

        void Case1(IRenderView render)
        {
            string fileName = GetResourcePath("wire/封头1.STEP");
            var shape = ShapeIO.Open(fileName);
            if (shape == null)
                return;

            List<int> edgeIds = new List<int>() { 40, 8, 7, 47 };
            TopoShapeList edges = new TopoShapeList();
            foreach (int edgeId in edgeIds)
            {
                var edge = shape.FindChild(EnumTopoShapeType.Topo_EDGE, edgeId);
                edges.Add(edge);
            }

            var wire = CurveBuilder.MakeWire(edges);

            var wireNode = BrepSceneNode.Create(wire, ColorTable.Red, true);
            render.ShowSceneNode(wireNode);
            wireNode.SetChildrenPickable(false);

            render.ShowSceneNode(BrepSceneNode.Create(shape, ColorTable.Gray, true));
        }

        void Case2(IRenderView render)
        {
            string fileName1 = GetResourcePath("wire/wire_points.brep");
            var wire = ShapeIO.Open(fileName1);
            if (wire == null)
                return;
            
            render.ShowSceneNode(BrepSceneNode.Create(wire, 0.1));

            ParametricCurve pc = new ParametricCurve(wire);
            if (!pc.IsValidGeometry())
                return;
            var arrD = pc.SplitByUniformAbscissa(400);
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

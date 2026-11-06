using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_TopoExplor : TestCase
    {
        public Geometry_TopoExplor()
        {
            Title = "拓扑遍历";
            Description = "遍历面边并按弧长均匀取点显示。";
            Tags.Add("几何");
            Tags.Add("拓扑");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
           var fileName = GetResourcePath("models/YONGSHENGDA_9998.igs");
           var shape = ShapeIO.Open(fileName);
            if (shape == null)
                return;

            var face = shape.FindChild(EnumTopoShapeType.Topo_FACE, 12);
            var edges = face.GetChildren(EnumTopoShapeType.Topo_EDGE);

            var wire = CurveBuilder.MakeWire(edges);
            if (wire == null)
                return;

            

            var curve = new ParametricCurve();
            curve.Initialize(wire);
            var ulist = curve.SplitByUniformLength(1, 0.2);


            var node = new ParticleSceneNode((uint)ulist.Count, ColorTable.Red, 2);
            for (int ii=0; ii<ulist.Count; ++ii)
            {
                node.SetPosition((uint)ii, Vector3.From(curve.Value(ulist[ii])));
            }
            node.UpdateBoundingBox();

            render.ShowSceneNode(BrepSceneNode.Create(wire, ColorTable.GoldEnrod, true));
            render.ShowSceneNode(node);
        }
    }
}

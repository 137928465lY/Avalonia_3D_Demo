using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Projection : TestCase
    {
        public Geometry_Projection()
        {
            Title = "曲线投影";
            Description = "将空间线框投影到平面上。";
            Tags.Add("几何");
            Tags.Add("投影");
            Level = 1;
        }

        public override void Run(IRenderView render)
        {
            var wire = ShapeIO.Open(GetResourcePath("proj/wire.brep"));
            if (wire == null)
            {
                return;
            }

            render.ShowSceneNode(BrepSceneNode.Create(wire, ColorTable.Red, true));
            var box = wire.GetBBox();
            var minB = box.CornerMin();
            var maxB = box.CornerMax();
            var pln = CurveBuilder.MakePlanarFace(new GPln(new GPnt(0, 0, 10), new GDir(0, 0, 1)), minB.x - 100, maxB.x + 100, minB.y - 100, maxB.y + 100);

            var newWire = ProjectionTool.ProjectOnSurface(wire, pln);
            render.ShowSceneNode(BrepSceneNode.Create(newWire, ColorTable.Green, true));
        }
    }
}

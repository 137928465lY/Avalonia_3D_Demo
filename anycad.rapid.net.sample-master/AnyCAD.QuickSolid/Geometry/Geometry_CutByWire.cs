using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_CutByWires : TestCase
    {
        public Geometry_CutByWires()
        {
            Title = "线框切孔";
            Description = "用边界线在曲面上开孔并补面";
            Tags.Add("几何");
            Tags.Add("切孔");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var faces = ShapeIO.Open(GetResourcePath("CutByWire/face.brep"));
            var w1 = ShapeIO.Open(GetResourcePath("CutByWire/wire.brep"));
            var w2 = ShapeIO.Open(GetResourcePath("CutByWire/wire2.brep"));

            var f0 = AdvSurfaceBuilder.AddHoles(faces, new TopoShapeList { w1, w2 });
            render.ShowSceneNode(BrepSceneNode.Create(f0, ColorTable.AliceBlue, true));

            var f1 = AdvSurfaceBuilder.MakeSurface(faces, w1);
            render.ShowSceneNode(BrepSceneNode.Create(f1, ColorTable.LightPink, true));

            var f2 = AdvSurfaceBuilder.MakeSurface(faces, w2);
            render.ShowSceneNode(BrepSceneNode.Create(f2, ColorTable.Purple, true));
        }
    }
}

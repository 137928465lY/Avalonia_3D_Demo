using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Fillet : TestCase
    {
        public Geometry_Fillet()
        {
            Title = "实体倒圆角";
            Description = "对立方体指定边做等半径倒圆角。";
            Tags.Add("几何");
            Tags.Add("倒角");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var shape1 = ShapeBuilder.MakeBox(GP.XOY(), 100, 100, 100);

            var fillet = FeatureTool.Fillet(shape1, new Uint32List(new uint[] { 5, 4 }), new DoubleList(new double[] { 5, 5 }));

            render.ShowSceneNode(BrepSceneNode.Create(fillet, ColorTable.Aqua, true));
        }
    }
}

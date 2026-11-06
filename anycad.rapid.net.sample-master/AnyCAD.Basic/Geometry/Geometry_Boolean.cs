using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Boolean : TestCase
    {
        public Geometry_Boolean()
        {
            Title = "布尔运算";
            Description = "演示盒体与球体的交、并、差布尔运算。";
            Tags.Add("几何");
            Tags.Add("布尔");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var box = ShapeBuilder.MakeBox(GP.XOY(), 10, 10, 10);
            var sphere = ShapeBuilder.MakeBox(new GAx2(new GPnt(5,5,0), new GDir(0,0,1)), 10, 10, 10);

            render.ShowSceneNode(BrepSceneNode.Create(box, ColorTable.Red, true));
            render.ShowSceneNode(BrepSceneNode.Create(sphere, ColorTable.Blue, true));

            var common = BooleanTool.Common(box, sphere);
            var node = BrepSceneNode.Create(common, ColorTable.Green, true);
            render.ShowSceneNode(node);
            node.SetTransform(Matrix4d.makeTranslation(-20, 0, 0));
            node.RequestUpdate();

            var cut = BooleanTool.Cut(box, sphere);
            node = BrepSceneNode.Create(cut, ColorTable.Honeydew, true);
            render.ShowSceneNode(node);
            node.SetTransform(Matrix4d.makeTranslation(0, 20, 0));
            node.RequestUpdate();

            var cut2 = BooleanTool.Cut(sphere, box);
            node = BrepSceneNode.Create(cut2, ColorTable.Violet, true);
            render.ShowSceneNode(node);
            node.SetTransform(Matrix4d.makeTranslation(0, -20, 0));
            node.RequestUpdate();

            var fuse = BooleanTool.Fuse(box, sphere);
            fuse = BooleanTool.Unify(fuse, true, true, true);
            node = BrepSceneNode.Create(fuse, ColorTable.LightYellow, true);
            render.ShowSceneNode(node);
            node.SetTransform(Matrix4d.makeTranslation(20, 0, 0));
            node.RequestUpdate();
        }
    }
}

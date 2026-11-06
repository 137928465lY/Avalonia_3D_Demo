using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_Unfold : TestCase
    {
        public Geometry_Unfold()
        {
            Title = "钣金展开";
            Description = "将钣金件展开为平面";
            Tags.Add("几何");
            Tags.Add("展开");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var solid = ShapeIO.Open(GetResourcePath("adv/sheet.STEP"));

            var plan = SheetMetalTool.Unfold(solid, 0.5, 0.1);

            render.ShowSceneNode(BrepSceneNode.Create(solid, ColorTable.AliceBlue, true));
            var node = BrepSceneNode.Create(plan, ColorTable.LightCoral, true);
            render.ShowSceneNode(node);
            node.SetTransform(Matrix4d.makeTranslate(0, 0, -100));
        }
    }
}

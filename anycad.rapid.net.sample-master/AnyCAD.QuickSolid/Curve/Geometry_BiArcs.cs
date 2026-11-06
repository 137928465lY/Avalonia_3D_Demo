using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_BiArcs : TestCase
    {
        public Geometry_BiArcs()
        {
            Title = "双圆弧拟合";
            Description = "将面边缘曲线与双圆弧拟合结果对比显示";
            Tags.Add("几何");
            Tags.Add("圆弧拟合");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            QuickSolidEngineManager.Instance().Initialize();

            var face = ShapeIO.Open(GetResourcePath("Split/face.brep"));
            var edges = face.GetChildren(EnumTopoShapeType.Topo_EDGE);
            var plane = new GPln();
            foreach(var edge in edges)
            {
                var node = BrepSceneNode.Create(edge, ColorTable.Green, true);
                render.ShowSceneNode(node);
                node.SetTransform(Matrix4d.makeTranslate(0, 0, 1));

                render.ShowSceneNode(BrepSceneNode.Create(edge, ColorTable.Red, true));
            }
        }
    }
}

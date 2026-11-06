using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Sketch : TestCase
    {
        public Geometry_Sketch()
        {
            Title = "基础草图";
            Description = "绘制直线、圆、椭圆、圆弧等基础曲线。";
            Tags.Add("几何");
            Tags.Add("草图");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var line = CurveBuilder.MakeLine(new GPnt(10, 10, 10), new GPnt(-10, -10, -10));
            render.ShowSceneNode(BrepSceneNode.Create(line, new Vector3(1, 1, 0), true));

            var ellips = CurveBuilder.MakeEllipse(GP.Origin(), 10, 5, GP.DX(), GP.DZ());
            render.ShowSceneNode(BrepSceneNode.Create(ellips, ColorTable.Blue, true));

            var circle = CurveBuilder.MakeCircle(GP.Origin(), 4, new GDir(0, 1, 0));
            render.ShowSceneNode(BrepSceneNode.Create(circle, ColorTable.Green, true));

            var arc = CurveBuilder.MakeArcOfCircle(GP.Origin(), new GPnt(50, 0, 50), new GPnt(20, 0, 40));
            render.ShowSceneNode(BrepSceneNode.Create(arc, ColorTable.Red, true));

            var rect = CurveBuilder.MakeRectangle(GP.XOY(), 30, 40, 5, false);
            render.ShowSceneNode(BrepSceneNode.Create(rect, ColorTable.Green, true));
        }
        public override void OnSelectionChanged(IRenderView render, PickedResult result)
        {
            var node = BrepSceneNode.Cast(result.GetItem().GetNode());
            if(node == null)
                return;

            var tool = CurveBuilder.MakeVertex(result.GetItem().GetPosition().ToPnt());
            var compound = BooleanTool.Split(node.GetTopoShape(), tool);
            if (compound == null)
                return;

            var edges = compound.GetChildren(EnumTopoShapeType.Topo_EDGE);
            foreach (var edge in edges)
            {
                render.ShowSceneNode(BrepSceneNode.Create(edge, ColorTable.Red, true));
            }

            render.RemoveSceneNode(node.GetUuid());

            render.RequestDraw(EnumUpdateFlags.Scene);
        }
    }
}

using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Analysis_Distance : TestCase
    {
        public Analysis_Distance()
        {
            Title = "距离计算";
            Description = "计算两个几何对象的距离";
            Tags.Add("拓扑");
            Tags.Add("分析");
            Level = 1;
        }
        public override void Run(IRenderView renderer)
        {
            string fileName = GetResourcePath("Holes.stp");
            var shape = ShapeIO.Open(fileName);
            if (shape == null)
                return;
            renderer.ShowSceneNode(BrepSceneNode.Create(shape, null, null, 0, true));

            var bbox = shape.GetBBox();

            var shape2 = ShapeBuilder.MakeBox(new GAx2(bbox.CornerMax(), GP.DZ()), 100, 100, 10);
            renderer.ShowSceneNode(BrepSceneNode.Create(shape2, ColorTable.LightSlateGray, true));

            ExtremaShapeShape ess = new ExtremaShapeShape();
            if (!ess.Initialize(shape, shape2, 0.001))
                return;

            var pt1 = ess.GetPointOnShape1(0);
            var pt2 = ess.GetPointOnShape2(0);

            var line = CurveBuilder.MakeLine(pt1, pt2);

            
            renderer.ShowSceneNode(BrepSceneNode.Create(line, ColorTable.Red, true));
        }
    }
}

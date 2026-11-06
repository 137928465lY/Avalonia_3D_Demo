using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Analysis_ShapeExp : TestCase
    {
        public Analysis_ShapeExp()
        {
            Title = "拓扑遍历";
            Description = "遍历拓扑，获取需要的曲线";
            Tags.Add("几何");
            Tags.Add("分析");
            Level = 1;
        }
        public override void Run(IRenderView renderer)
        {
            //var fullPath = DialogUtil.OpenFileDialog("选择模型文件", ShapeIO.ReaderFilters());
            //if (fullPath.IsEmpty())
            //    return;
            string fullPath = GetResourcePath("Holes.stp");
            var shape = ShapeIO.Open(fullPath);
            if (shape == null)
                return;

            var shapeExplor = new ShapeExplor();
            shapeExplor.AddShape(shape);
            shapeExplor.Build();

            var edgeCount = shapeExplor.GetEdgeCount();
            for(uint ii=0; ii<edgeCount; ++ii)
            {
                var edge = shapeExplor.GetEdge(ii);
                if(edge.GetCurveType() == EnumCurveType.CurveType_Line)
                    renderer.ShowSceneNode(BrepSceneNode.Create(edge.GetShape(), ColorTable.Blue, true));
                else
                    renderer.ShowSceneNode(BrepSceneNode.Create(edge.GetShape(), ColorTable.Red, true));
            }
        }
    }
}

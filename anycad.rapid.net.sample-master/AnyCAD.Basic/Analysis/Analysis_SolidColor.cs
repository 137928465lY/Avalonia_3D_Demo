using AnyCAD.Foundation;
using System;

namespace AnyCAD.Demo.Geometry
{
    class Analysis_SolidColor : TestCase
    {
        public Analysis_SolidColor()
        {
            Title = "实体遍历";
            Description = "拓扑遍历，设置面颜色";
            Tags.Add("几何");
            Tags.Add("分析");
            Level = 1;
        }
        public override void Run(IRenderView renderer)
        {
            var cyl = ShapeBuilder.MakeCylinder(new GAx2(), 10, 20, 0);

            var node = BrepSceneNode.Create(cyl, ColorTable.Gray, true);
            renderer.ShowSceneNode(node);
            SolidExplor se = new SolidExplor();
            se.Initialize(cyl);

            var faceIds = se.GetSharedFaceIDsByIndex(2);
            foreach(var id in faceIds)
            {
                node.SetFaceColor(id, ColorTable.HotPink);
            }
        }
    }
 
}

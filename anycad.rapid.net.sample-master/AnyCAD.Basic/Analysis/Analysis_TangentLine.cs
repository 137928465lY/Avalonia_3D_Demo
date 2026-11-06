using AnyCAD.Foundation;
using System;

namespace AnyCAD.Demo.Geometry
{
    class Analysis_TangentLine : TestCase
    {
        public Analysis_TangentLine()
        {
            Title = "计算切线";
            Description = "计算和显示相切曲线";
            Tags.Add("几何");
            Tags.Add("分析");
            Level = 1;
        }
        public override void Run(IRenderView renderer)
        {
            var cirlce = CurveBuilder.MakeCircle(GP.Origin(), 30, GP.DZ());
            var pt = new GPnt(30, 1, 0);
            var line = CurveBuilder.MakeTangentLine(cirlce, pt, new GLin(pt, new GDir(1, 0, 0)), Math.PI / 2, 100);

            renderer.ShowSceneNode(BrepSceneNode.Create(cirlce, ColorTable.Red, true));
            renderer.ShowSceneNode(BrepSceneNode.Create(line, ColorTable.Green, true));
        }
    }
}

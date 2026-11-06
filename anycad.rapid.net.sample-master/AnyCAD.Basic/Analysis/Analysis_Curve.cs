using AnyCAD.Foundation;
using System;

namespace AnyCAD.Demo.Geometry
{
    class Analysis_Curve : TestCase
    {
        public Analysis_Curve()
        {
            Title = "曲线分析";
            Description = "曲线离散、切线可视化";
            Tags.Add("几何");
            Tags.Add("分析");
            Level = 1;
        }
        public override void Run(IRenderView renderer)
        {
            var ellipse = CurveBuilder.MakeEllipse(GP.Origin(), 10, 5, GP.DX(), GP.DZ());
            renderer.ShowSceneNode(BrepSceneNode.Create(ellipse, ColorTable.Blue, true));

            ParametricCurve pc = new ParametricCurve(ellipse);

            var paramsList = pc.SplitByUniformLength(1, 0.01);

            uint itemCount = (uint)paramsList.Count;
            var points = new ParticleSceneNode(itemCount, ColorTable.Green, 5.0f);
            var lines = new SegmentsSceneNode(itemCount, ColorTable.Red, 2);

            Random random = new Random();
            for (int ii = 0; ii < paramsList.Count; ++ii)
            {
                var value = pc.D1(paramsList[ii]);
                var pos = value.GetPoint();
                var dir = value.GetVectors()[0];
                var end = new GPnt(pos.XYZ().Added(dir.XYZ()));

                lines.SetPositions((uint)ii, Vector3.From(pos), Vector3.From(end));
                lines.SetColors((uint)ii, ColorTable.Red, Vector3.From(random.NextDouble(), random.NextDouble(), random.NextDouble()));

                points.SetPosition((uint)ii, Vector3.From(pos));
            }

            renderer.ShowSceneNode(points);
            renderer.ShowSceneNode(lines);
        }
    }
 
}

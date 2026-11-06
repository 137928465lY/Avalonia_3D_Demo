using AnyCAD.Foundation;
using System;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Extrema : TestCase
    {
        public Geometry_Extrema()
        {
            Title = "曲线极值";
            Description = "计算两曲线间的最近点与极值。";
            Tags.Add("几何");
            Tags.Add("极值");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var arc = CurveBuilder.MakeArcOfCircle(new GCirc(GP.XOY(), 10), 0, Math.PI);
            var points = new GPntList
            {
                new GPnt(0,0,0),
                new GPnt(2,5,0),
                new GPnt(15,15,0)
            };
            var line = CurveBuilder.MakeBSpline(points);

            var extrema = new ExtremaCurveCurve();
            extrema.Initialize(arc, line);
            int count = extrema.GetPointCount();
            for (int ii = 0; ii < count; ++ii)
            {                
                var point = extrema.GetPoint1(ii);
                var node = new PrimitiveSceneNode(GeometryBuilder.AtomSphere(), null);
                node.SetTransform(Matrix4d.makeTranslation(Vector3d.From(point)));
                render.ShowSceneNode(node);
            }

            render.ShowSceneNode(BrepSceneNode.Create(arc, ColorTable.Red, true));
            render.ShowSceneNode(BrepSceneNode.Create(line, ColorTable.Green, true));
        }
    }
}

using AnyCAD.Foundation;

namespace AnyCAD.Demo.Analysis
{
    internal class Analysis_SplitCurve : TestCase
    {
        public Analysis_SplitCurve()
        {
            Title = "样条曲线离散";
            Description = "离散样条曲线";
            Tags.Add("几何");
            Tags.Add("分析");
            Level = 1;
        }
        public override void Run(IRenderView renderer)
        {
            {
                var points = new GPntList();
                points.Add(new GPnt(0, 0, 0));
                points.Add(new GPnt(1, 0, 0));
                points.Add(new GPnt(3, 5, 0));
                points.Add(new GPnt(8, 9, 0));
                points.Add(new GPnt(50, -10, 0));


                var edge = CurveBuilder.MakeBSpline(points);

                var curve = new ParametricCurve(edge);
                var paramsList = curve.SplitByUniformLength(1, GP.Resolution());

                var node = new ParticleSceneNode((uint)paramsList.Count, ColorTable.IndianRed, 5);
                var node2 = new ParticleSceneNode((uint)paramsList.Count, ColorTable.Auqamarin, 5);
                for (int i = 0; i < paramsList.Count; ++i)
                {
                    var vv = curve.Value(paramsList[i]);
                    node.SetPosition((uint)i, Vector3.From(vv));
                    node2.SetPosition((uint)i, Vector3.From(vv));
                }

                renderer.ShowSceneNode(node);
                renderer.ShowSceneNode(BrepSceneNode.Create(edge, ColorTable.Azure, true));


                node2.SetTransform(Matrix4d.makeScale(0.5f, 0.5f, 0.5f));
                node2.RequestUpdate();
                renderer.ShowSceneNode(node2);
            }
            //{
            //    var points = new GPntList();
            //    points.Add(new GPnt(0, 0, 10));
            //    points.Add(new GPnt(1, 0, 10));
            //    points.Add(new GPnt(3, 5, 10));
            //    points.Add(new GPnt(8, 9, 10));
            //    points.Add(new GPnt(50, -10, 10));


            //    var edge = CurveBuilder.MakeBSpline(points);
            //    var curve = new ParametricCurve(edge);
            //    var paramsList = curve.SplitByTangential(0.09, 0.01);
            //    var node = new ParticleSceneNode((uint)paramsList.Count, ColorTable.RosyBrown, 5);
            //    for (int i = 0; i < paramsList.Count; ++i)
            //    {
            //        //var vv = curve.D1(paramsList[i]);
            //        var pt = curve.Value(paramsList[i]);
            //        node.SetPosition((uint)i, Vector3.From(pt));
            //    }

            //    renderer.ShowSceneNode(node);
            //    renderer.ShowShape(edge, ColorTable.Azure);
            //}
        }
    }
}

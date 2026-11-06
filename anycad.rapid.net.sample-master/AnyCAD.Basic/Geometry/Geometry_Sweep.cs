using AnyCAD.Foundation;
using System.Xml.Linq;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Sweep : TestCase
    {
        public Geometry_Sweep()
        {
            Title = "椭圆扫掠";
            Description = "沿样条路径扫掠椭圆截面对比过渡模式。";
            Tags.Add("几何");
            Tags.Add("扫掠");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var sketch = CurveBuilder.MakeEllipse(GP.Origin(), 5, 3, GP.DX(), GP.DZ());

            // 1. Sweep
            {
                GPntList points = new GPntList { GP.Origin(), new GPnt(20, 10, 30), new GPnt(50, 50, 50), };
                var path = CurveBuilder.MakeBSpline(points);
                render.ShowSceneNode(BrepSceneNode.Create(path, ColorTable.Green, true));

                {
                    var feature = FeatureTool.SweepByFrenet(sketch, path, EnumSweepTransitionMode.RoundCorner, false, true, true);
                    render.ShowSceneNode(BrepSceneNode.Create(feature, ColorTable.Blue, true));
                }

                {
                    var feature = FeatureTool.SweepByFrenet(sketch, path, EnumSweepTransitionMode.RoundCorner, true, false, false);
                    var node = BrepSceneNode.Create(feature, ColorTable.RoyalBlue, true); render.ShowSceneNode(node);
                    node.SetTransform(Matrix4d.makeTranslation(50, 50, 0));
                    node.RequestUpdate();
                }
                {
                    var feature2 = FeatureTool.Sweep(sketch, path, EnumGeomFillTrihedron.ConstantNormal);
                    var node = BrepSceneNode.Create(feature2, ColorTable.Green, true); render.ShowSceneNode(node);
                    node.SetTransform(Matrix4d.makeTranslation(-50,-50,0));
                    node.RequestUpdate();
                }


                render.RequestDraw();
            }

            //// 2. Revol
            //{
            //    var feature = FeatureTool.Revolve(sketch, new GAx1(new GPnt(-20, 0, 0), GP.DY()), 90);

            //    render.ShowShape(feature, ColorTable.Green);
            //}
            //// 3. Loft
            //{
            //    var baseWire = CurveBuilder.MakeRectangle(new GAx2(new GPnt(50, -50, 0), GP.DZ()), 20, 20, 5, false);
            //    var topWire = CurveBuilder.MakeCircle(new GPnt(60, -40, 40), 5, GP.DZ());

            //    var loft = FeatureTool.Loft(baseWire, topWire, true);
            //    render.ShowShape(loft, ColorTable.Red);
            //}

        }
    }
}

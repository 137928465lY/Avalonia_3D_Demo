using AnyCAD.Foundation;
using System;
using System.IO;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_SweepCable : TestCase
    {
        public Geometry_SweepCable()
        {
            Title = "电缆扫掠";
            Description = "沿自然路径扫掠生成电缆形状。";
            Tags.Add("几何");
            Tags.Add("扫掠");
            Level = 1;
        }
        void TestCase1(IRenderView render)
        {
            GPntList points = new GPntList();
            string fileName = GetResourcePath("data/CableViewInfo.txt");
            using (StreamReader reader = File.OpenText(fileName))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var items = line.Split(',');
                    if (items.Length < 3)
                        continue;

                    double x = double.Parse(items[0]);
                    double y = double.Parse(items[1]);
                    double z = double.Parse(items[2]);

                    points.Add(new GPnt(x, y, z));

                }

            }

            var path = CurveBuilder.MakePolygon(points, false);
            render.ShowSceneNode(BrepSceneNode.Create(path, ColorTable.Red, true));

            var curve = new ParametricCurve(path);
            var rt = curve.D1(curve.FirstParameter());

            var position = rt.GetPoint();
            var dir = new GDir(rt.GetVectors()[0]);
            var sketch = CurveBuilder.MakeCircle(position, 10, dir);
            render.ShowSceneNode(BrepSceneNode.Create(sketch, new Vector3(0.8f), true));
            var pipe = FeatureTool.SweepByFrenet(sketch, path, EnumSweepTransitionMode.RoundCorner, false);

            try
            {

                if (pipe != null)
                {
                    render.ShowSceneNode(BrepSceneNode.Create(pipe, new Vector3(0.8f), true));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        void TestCase2(IRenderView render)
        {
            var path = ShapeIO.Open(GetResourcePath("sweep/path2.igs"));
            var sketch = ShapeIO.Open(GetResourcePath("sweep/prof.igs"));

            var we = new WireExplor(sketch);
            var wire = we.GetOuterWire();

            render.ShowSceneNode(BrepSceneNode.Create(path, ColorTable.Red, true));
            var pipe = FeatureTool.SweepByFrenet(wire, path, EnumSweepTransitionMode.RoundCorner, true);
            var node = BrepSceneNode.Create(pipe, ColorTable.Chocolate, true);
            render.ShowSceneNode(node);
            node.SetDisplayFilter(EnumShapeFilter.Face);
        }
        public override void Run(IRenderView render)
        {
            TestCase2(render);
        }
    }
}

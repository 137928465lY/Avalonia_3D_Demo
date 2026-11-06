using AnyCAD.Foundation;
using System;
using System.IO;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Complex : TestCase
    {
        public Geometry_Complex()
        {
            Title = "复杂曲线造型";
            Description = "导入多组样条曲线构造复杂曲面。";
            Tags.Add("几何");
            Tags.Add("曲线");
            Level = 1;
        }

        TopoShape CreateSpline(string fileName)
        {
            using (var sr = new StreamReader(fileName))
            {
                string line;
                GPntList points = new GPntList();
                while ((line = sr.ReadLine()) != null)
                {
                    var temp = line.Split(',');
                    points.Add(new GPnt(double.Parse(temp[0]), double.Parse(temp[1]), double.Parse(temp[2])));
                }
                if (points.Count > 1)
                {
                    return CurveBuilder.MakeBSpline(points);
                }
            }

            return null;
        }
        public override void Run(IRenderView render)
        {
            TopoShapeList auxCurves = new TopoShapeList();
            for(int ii=0; ii<4; ++ii)
            {
                char name = (char)((int)('A') + ii);
                string fileName = String.Format("data/complex/{0}.sldcrv", name);
                var curve = CreateSpline(GetResourcePath(fileName));
                if(curve!=null)
                {
                    render.ShowSceneNode(BrepSceneNode.Create(curve, ColorTable.Hex(0xFF0000), true));
                    auxCurves.Add(curve);
                }
            }

            for(int ii=0; ii<12; ++ii)
            {
                string fileName = String.Format("data/complex/{0}.sldcrv", ii);
                var curve = CreateSpline(GetResourcePath(fileName));
                if (curve != null)
                {
                    render.ShowSceneNode(BrepSceneNode.Create(curve, ColorTable.Hex(0xFF00FF), true));
                    auxCurves.Add(curve);
                }
            }
           
        }
    }
}

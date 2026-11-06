using AnyCAD.Foundation;
using System.IO;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_LoftM : TestCase
    {
        public Geometry_LoftM()
        {
            Title = "多截面放样";
            Description = "读取多组轮廓曲线放样转子叶片。";
            Tags.Add("几何");
            Tags.Add("放样");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            TopoShapeList tg = new TopoShapeList();
            GPntList points = new GPntList();

            using (var sr = new StreamReader(GetResourcePath("data/Stage4_Rotor4_Profile.curve")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("# Profile"))
                    {
                        if(points.Count > 0)
                        {
                            var temp2 = CurveBuilder.MakeBSpline(points);
                            if (temp2 != null)
                                tg.Add(temp2);
                        }

                        points = new GPntList();
                    }
                    else
                    {
                        var temp = line.Split('\t');
                        points.Add(new GPnt(double.Parse(temp[0]), double.Parse(temp[1]), double.Parse(temp[2])));
                    }
                }
            }


            var temp1 = FeatureTool.Loft(tg, true, true);
            render.ShowSceneNode(BrepSceneNode.Create(temp1, ColorTable.Hex(0xFF0000), true));

        

        }
    }
}

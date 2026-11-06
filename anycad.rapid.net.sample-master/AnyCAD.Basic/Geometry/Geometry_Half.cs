using AnyCAD.Foundation;
using System;
using System.IO;
using System.Text;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_HalfSpace : TestCase
    {
        public Geometry_HalfSpace()
        {
            Title = "半空间裁剪";
            Description = "用半空间平面裁剪B样条曲面。";
            Tags.Add("几何");
            Tags.Add("裁剪");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            GPntList points = new GPntList();
            for (int ii = 0; ii < 5; ++ii)
                for (int jj = 0; jj < 5; ++jj)
                {
                    double c = ii + jj - 2;
                    points.Add(new GPnt(ii, jj, c));
                }

            var surf = SurfaceBuilder.PointsToBSplineSurface(points, 5);
            var halfSpace = ShapeBuilder.MakeHalfSpace(new GPnt(), new GDir(0, 0, 1));
            surf = BooleanTool.Cut(surf, halfSpace);

            render.ShowSceneNode(BrepSceneNode.Create(surf, ColorTable.Red, true));

        }
    }
}

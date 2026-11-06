using AnyCAD.Foundation;
using System;
using System.IO;
using System.Text;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Sew : TestCase
    {
        public Geometry_Sew()
        {
            Title = "曲面缝合";
            Description = "将两个IGES曲面片缝合成整体。";
            Tags.Add("几何");
            Tags.Add("缝合");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var face1 = ShapeIO.Open(GetResourcePath("boolean/face1.igs"));
            var face2 = ShapeIO.Open(GetResourcePath("boolean/face2.igs"));

            var face = BooleanTool.Fuse(face1, face2);

            render.ShowSceneNode(BrepSceneNode.Create(face, ColorTable.LightBlue, true));
        }
    }
}

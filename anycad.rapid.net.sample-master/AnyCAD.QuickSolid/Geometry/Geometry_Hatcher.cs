using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_Hatcher : TestCase
    {
        public Geometry_Hatcher()
        {
            Title = "剖面线填充";
            Description = "在带孔平面上生成剖面填充线";
            Tags.Add("几何");
            Tags.Add("填充");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            // 1. Make face with a hole.
            var c1 = CurveBuilder.MakeCircle(GP.Origin(), 30, GP.DZ());
            var c2 = CurveBuilder.MakeCircle(GP.Origin(), 100, GP.DZ());

            var face = CurveBuilder.MakePlanarFace(c2);
            var face2 = CurveBuilder.MakePlanarFace(c1);
            var shape = BooleanTool.Cut(face, face2);
            //render.ShowShape(shape, ColorTable.Blue);

            var faces = shape.GetChildren(EnumTopoShapeType.Topo_FACE);

            // 2. Hatch the face.
            foreach (var item in faces)
            {
                HatchHatcher hh = new HatchHatcher(item);
                hh.Build();
                var material = BasicMaterial.Create("line");
                material.SetColor(new Vector3(0.5f, 0.5f, 1.0f));
                var node = hh.Create(material);

                // 3. Show the hatching lines.
                render.ShowSceneNode(node);
            }

        }
    }
}

using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_OffsetSolid : TestCase
    {
        public Geometry_OffsetSolid()
        {
            Title = "实体偏置";
            Description = "对导入实体做等距偏置生成薄壁。";
            Tags.Add("几何");
            Tags.Add("偏置");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            //var solid = ShapeBuilder.MakeCylinder(new GAx2(), 10, 10, System.Math.PI/4 * 5);
            var solid = ShapeIO.Open(OpenModelFile());            

            render.ShowSceneNode(BrepSceneNode.Create(solid, ColorTable.Red, true));
            {
                var ss = FeatureTool.OffsetShape(solid, 0.1);
                if (ss == null)
                    return;

                var material = MeshPhongMaterial.Create("tansparent");
                material.SetOpacity(0.5f);
                material.SetTransparent(true);
                material.SetFaceSide(EnumFaceSide.DoubleSide);
                material.SetColor(ColorTable.LightYellow);

                var node = BrepSceneNode.Create(ss, material, null);
                render.ShowSceneNode(node);
            }
            
        }
    }
}

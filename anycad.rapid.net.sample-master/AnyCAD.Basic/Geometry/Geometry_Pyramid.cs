using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Geometry_Pyramid : TestCase
    {
        public Geometry_Pyramid()
        {
            Title = "棱锥阵列";
            Description = "生成不同边数的棱锥。";
            Tags.Add("几何");
            Tags.Add("棱锥");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var material = MeshNormalMaterial.Create("contact.normal");

            for (uint ii = 0; ii < 10; ++ii)
            {
                TopoShape shape = ShapeBuilder.MakePyramid(new GAx2(new GPnt(ii * 11, 0, 0.1), GP.DZ()), ii + 3, 5, 5 + ii);
                var node = BrepSceneNode.Create(shape, material, null);
                render.ShowSceneNode(node);
            }
        }
    }
}

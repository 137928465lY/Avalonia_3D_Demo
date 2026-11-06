using AnyCAD.Foundation;


namespace AnyCAD.Demo.Geometry
{
    class Geometry_Hole : TestCase
    {
        public Geometry_Hole()
        {
            Title = "孔洞识别";
            Description = "识别面内孔并提取内边界线。";
            Tags.Add("几何");
            Tags.Add("孔洞");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            string fileName = GetResourcePath("models/hole.STEP");
            var shape = ShapeIO.Open(fileName);
            if (shape == null)
                return;

            var face = shape.FindChild(EnumTopoShapeType.Topo_FACE, 7);
            var wireExp = new WireExplor(face);
            var wires = wireExp.GetInnerWires();
            foreach(var wire in wires)
            {
                render.ShowSceneNode(BrepSceneNode.Create(wire, ColorTable.Red, true));
            }
            render.ShowSceneNode(BrepSceneNode.Create(shape,  ColorTable.LightYellow, true));
        }
    }
}

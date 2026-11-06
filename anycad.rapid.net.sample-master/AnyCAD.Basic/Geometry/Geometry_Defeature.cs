using AnyCAD.Foundation;


namespace AnyCAD.Demo.Geometry
{
    class Geometry_PosAxis : TestCase
    {
        public Geometry_PosAxis()
        {
            Title = "修复内部线";
            Description = "移除面内部多余边界线并修复面。";
            Tags.Add("几何");
            Tags.Add("修复");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            string fileName = GetResourcePath("feature/底封板.stp");
            var shape = ShapeIO.Open(fileName);
            if (shape == null)
                return;

            var fT = shape.GetShapeType();
            GlobalInstance.Log(fT.ToString());
            var faces = shape.GetChildren(EnumTopoShapeType.Topo_FACE);
            foreach( var face in faces )
            {
                var newShape = FixShapeTool.RemoveInternalWires(face, 100000);
                render.ShowSceneNode(BrepSceneNode.Create(newShape, ColorTable.LightBlue, true));
            }

        }
    }
}

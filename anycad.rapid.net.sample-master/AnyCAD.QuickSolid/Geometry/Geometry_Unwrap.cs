using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
namespace AnyCAD.Demo.Geometry
{
    class Geometry_Unwrap : TestCase
    {
        public Geometry_Unwrap()
        {
            Title = "曲面展开";
            Description = "将模型各面展开平铺到平面";
            Tags.Add("几何");
            Tags.Add("展开");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            var fileName = OpenModelFile();
            if (fileName.IsEmpty())
                return;

            var shape = ShapeIO.Open(fileName);
            if (shape == null)
                return;

            var faceList = shape.GetChildren(EnumTopoShapeType.Topo_FACE);

            int halfCount = faceList.Count / 2; int ii = 0;
            float x = 0, y = 0;
            float maxY = 0;
            foreach (var face in faceList)
            {
                var wires = AdvFeatureTool.Unwrap(face);
                var plane = CurveBuilder.MakePolygonFace(wires);
                if (plane != null)
                {
                    var node = BrepSceneNode.Create(plane, ColorTable.GreenYellow, true);
                    render.ShowSceneNode(node);
                    node.SetTransform(Matrix4d.makeTranslation(x, y, 0));
                    node.RequestUpdate();

                    var bbox = node.GetBoundingBox();
                    var sz = bbox.getSize();

                    if (ii == halfCount)
                    {
                        x = 0;
                        y = maxY;
                    }
                    else
                    {
                        x += (float)sz.x;
                    }

                    if (ii < halfCount)
                    {

                        maxY = System.Math.Max(maxY, (float)sz.y);
                    }
                    ++ii;
                }
            }

            render.ShowSceneNode(BrepSceneNode.Create(shape, ColorTable.Honeydew, true));
        }
    }
}

using AnyCAD.Foundation;

namespace AnyCAD.Demo.Geometry
{
    class Interop_ImportDXF : TestCase
    {
        public Interop_ImportDXF()
        {
            Title = "DXF导入";
            Description = "打开DXF文件并显示其中的边线";
            Tags.Add("数据交换");
            Tags.Add("导入");
            Level = 2;
        }
        public override void Run(IRenderView render)
        {
            //var fileName = DialogUtil.OpenFileDialog("DXF", ShapeIO.ReaderFilters());
            // if (fileName.IsEmpty())
            //     return;
           var fileName = GetResourcePath("dxf/tiger.dxf");
           var shapes = ShapeIO.Open(fileName);
           if (shapes == null)
                return;

           foreach(var edge in shapes.GetChildren(EnumTopoShapeType.Topo_EDGE))
            {
                render.ShowSceneNode(BrepSceneNode.Create(edge, ColorTable.Red, true));

            }
        }
    }
}

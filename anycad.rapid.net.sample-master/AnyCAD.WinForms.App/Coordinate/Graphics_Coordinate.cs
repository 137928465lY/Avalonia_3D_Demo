using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_Coordinate : TestCase
    {
        public Graphics_Coordinate()
        {
            Title = "坐标系显示";
            Description = "演示多坐标系的显示与管理";
            Tags.Add("显示");
            Tags.Add("坐标系");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            MultiCoordinateSystemForm dlg = new MultiCoordinateSystemForm();
            dlg.ShowDialog();
            dlg = null;
        }
    }
}

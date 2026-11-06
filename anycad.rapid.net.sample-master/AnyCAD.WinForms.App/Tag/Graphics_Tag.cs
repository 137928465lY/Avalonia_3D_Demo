using AnyCAD.Forms;
using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_Tag : TestCase
    {
        public Graphics_Tag()
        {
            Title = "标签标注";
            Description = "演示三维场景中的标签标注功能";
            Tags.Add("显示");
            Tags.Add("标签");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            NewForm3D dlg = new NewForm3D(render.Scene);
            dlg.ShowDialog();
            dlg = null;
        }
    }
}

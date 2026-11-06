using AnyCAD.Foundation;

namespace AnyCAD.Demo.Graphics
{
    class Interaction_Transform : TestCase
    {
        public Interaction_Transform()
        {
            Title = "对象变换";
            Description = "演示对象的移动、旋转与缩放变换";
            Tags.Add("交互");
            Tags.Add("变换");
            Level = 1;
        }
        public override void Run(IRenderView render)
        {
            var dlg = new TransformDlg();
            dlg.ShowDialog();
        }
    }
}

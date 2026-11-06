using AnyCAD.Foundation;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 接触阴影
    /// </summary>
    class Graphics_ContactShadow : TestCase
    {
        public Graphics_ContactShadow()
        {
            Title = "接触阴影";
            Description = "三个法线材质网格在白底上旋转，展示 ContactShadow 软接触阴影效果。";
            Tags.Add("显示");
            Tags.Add("阴影");
            Tags.Add("动画");
            Level = 2;
        }

        readonly List<SceneNode> mNodes = new List<SceneNode>();
        Vector3[] mBasePos = Array.Empty<Vector3>();
        Background mBackground;
        bool mSavedContactShadow;

        public override void Run(IRenderView render)
        {
            var mat = MeshNormalMaterial.Create("contact.normal");

            var boxShape = ShapeBuilder.MakeBox(new GAx2(new GPnt(0, 0, 0), GP.DZ()), 0.4, 0.4, 0.4);
            var boxNode = BrepSceneNode.Create(boxShape, mat, null);
            boxNode.SetPickable(false);
            boxNode.SetDisplayFilter(EnumShapeFilter.Face);
            mNodes.Add(boxNode);

            var sphShape = ShapeBuilder.MakeSphere(new GPnt(0, 0, 0), 0.3);
            var sphNode = BrepSceneNode.Create(sphShape, mat, null);
            sphNode.SetPickable(false);
            sphNode.SetDisplayFilter(EnumShapeFilter.Face);
            mNodes.Add(sphNode);

            var cylShape = ShapeBuilder.MakeCylinder(new GAx2(new GPnt(0, 0, 0), GP.DZ()), 0.18, 0.45, Math.PI * 2);
            var cylNode = BrepSceneNode.Create(cylShape, mat, null);
            cylNode.SetPickable(false);
            cylNode.SetDisplayFilter(EnumShapeFilter.Face);
            mNodes.Add(cylNode);


            mBasePos = new Vector3[3];
            for (int k = 0; k < 3; k++)
            {
                double ang = k / 3.0 * 2.0 * Math.PI;
                mBasePos[k] = new Vector3(Math.Cos(ang) / 2.0, Math.Sin(ang) / 2.0, 0.1);
            }
            for (int k = 0; k < mNodes.Count; k++)
            {
                render.ShowSceneNode(mNodes[k]);
                mNodes[k].SetTransform(Matrix4d.makeTranslation(mBasePos[k].x, mBasePos[k].y, mBasePos[k].z));
            }

            // ---- 白底 + 无环境光 ----
            mBackground = render.Viewer.GetBackground();
            render.Viewer.SetBackgroundColor(new Vector4(1, 1, 1, 1));
            render.SceneManager.GetLightManager().SetAmbientLightColor(new Vector3(0));

            // ---- ContactShadow ----
            var settings = render.Viewer.GetRenderSettings();
            mSavedContactShadow = settings.GetContactShadow();
            settings.SetContactShadow(true);
            settings.SetContactShadowBlur(3.5f);

            var cam = render.Camera;
            cam.SetProjectionType(EnumProjectionType.Perspective);
            cam.SetFovy(50.0);
            cam.SetNear(0.1f);
            cam.SetFar(100.0);
            cam.LookAt(new Vector3d(0.5, 2.0, 1.0), Vector3d.Zero, Vector3d.UNIT_Z);

            render.ShowCoordinateGrid(false);
            render.EnableAnimation(true);
        }

        public override void Animation(IRenderView render, float time)
        {
            float t = time * 0.5f; // ms -> s
            for (int k = 0; k < mNodes.Count; k++)
            {
                var pos = mBasePos[k];
                var trf = Matrix4d.makeTranslation(pos.x, pos.y, pos.z)
                        * Matrix4d.makeRotationAxis(Vector3d.UNIT_X, 0.6f * t)
                        * Matrix4d.makeRotationAxis(Vector3d.UNIT_Z, 1.2f * t);
                mNodes[k].SetTransform(trf);
                mNodes[k].RequestUpdate();
            }
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
            var settings = render.Viewer.GetRenderSettings();
            settings.SetContactShadow(mSavedContactShadow);
            render.Viewer.SetBackground(mBackground);
        }
    }
}

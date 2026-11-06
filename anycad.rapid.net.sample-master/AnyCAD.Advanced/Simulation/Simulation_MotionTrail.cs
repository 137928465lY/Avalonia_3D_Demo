using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;

namespace AnyCAD.Demo.Graphics
{
    class Simulation_MotionTrail : TestCase
    {
        public Simulation_MotionTrail()
        {
            Title = "运动轨迹";
            Description = "演示刚体运动并用粒子记录运动轨迹";
            Tags.Add("仿真");
            Tags.Add("轨迹");
            Level = 2;
        }
        BrepSceneNode mWorkpiece = null;
        ParticleSceneNode mMotionTrail = new ParticleSceneNode(1000, ColorTable.Red, 3.0f);
        RigidAnimation mAnimation;

        public override void Run(IRenderView render)
        {
            var material = MeshStandardMaterial.Create("workpiece");
            material.SetColor(new Vector3(0.9f));
            material.SetFaceSide(EnumFaceSide.DoubleSide);

            var shape = ShapeBuilder.MakeCylinder(GP.YOZ(), 5, 50, 0);
            mWorkpiece = BrepSceneNode.Create(shape, material, null);

            render.ShowSceneNode(mWorkpiece);

            var coord = new GAx2(new GPnt(0, 0, 5), GP.DZ());
            var toolShape = ShapeBuilder.MakeCone(coord, 0, 2, 5, 0);
            var toolNode = BrepSceneNode.Create(toolShape, ColorTable.Blue, true);
            render.ShowSceneNode(toolNode);


            render.ShowSceneNode(mMotionTrail);


            // Initialize Animation
            mAnimation = new RigidAnimation();

            var rotation = Matrix4d.makeRotationAxis(new Vector3d(1, 0, 0), Math.PI);
            var trf = Matrix4d.makeTranslation(-50, 0, 0) * rotation;

            mAnimation.Add(new MatrixAnimationClip(mWorkpiece, mWorkpiece.GetTransform(), trf, 0, 10));
            mAnimation.Add(new MatrixAnimationClip(toolNode, toolNode.GetTransform(), trf, 10, 15));
            mAnimation.Add(new RotateAnimationClip(toolNode, Vector3d.UNIT_Z, Math.PI*4, 16, 20));

            render.EnableAnimation(true);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
        }

        uint mCount = 0;
        public override void Animation(IRenderView render, float time)
        {
            if(mCount > 100)
            {
                mMotionTrail.SetVisible(false);
                render.RequestDraw(EnumUpdateFlags.Scene);
                return;
            }

            if(mAnimation.Play(time))
            {
                var trf = mWorkpiece.GetTransform();
                Vector3 tailPostion = new Vector3(0, 0, 5);
                tailPostion.applyMatrix4d(trf);
                mMotionTrail.SetPosition(mCount++, tailPostion);
                mMotionTrail.RequestUpdate();

                render.RequestDraw(EnumUpdateFlags.Scene);
            }


        }
     }
}

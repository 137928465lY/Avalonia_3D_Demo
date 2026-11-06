using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_BinaryFractalTree : TestCase
    {
        public Graphics_BinaryFractalTree()
        {
            Title = "二叉分形树";
            Description = "递归生成二叉分形树并随动画调整分支角度";
            Tags.Add("显示");
            Tags.Add("动画");
            Level = 1;
        }
        int mDepth = 8;
        float mInitialLength = 50;
        float mScale = 0.7f;
        float mAngleMin = 1.0f;
        float mAngleMax = 90.0f;
        float mCurrentAngle = 30.0f;

        List<Vector3> mBaseVertices = new List<Vector3>();
        List<Vector3> mColors = new List<Vector3>();
        ColorLookupTable mColorTable = new ColorLookupTable();

        float mMaxHeight;

        BufferGeometry mGeometry;
        PrimitiveSceneNode mNode;
        IRenderView mRender;
        public override void Run(IRenderView render)
        {
            mRender = render;
            mBaseVertices.Clear();
            mColors.Clear();
            mMaxHeight = 0;

            // 初始化颜色查找表 - 彩虹色
            mColorTable.SetMaxValue(mInitialLength * 2.0f);
            mColorTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));

            // 生成分形树
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 end = new Vector3(0, mInitialLength, 0);
            GenerateTree(start, end, mDepth);

            // 创建几何体
            CreateGeometry();

            // 创建材质
            var material = BasicMaterial.Create("tree");
            var color = ColorTable.Hex(0x228B22);
            material.SetUniform("diffuse", color);
            // 启用顶点颜色
            material.GetTemplate().SetVertexColors(true);

            mNode = new PrimitiveSceneNode(mGeometry, material);
            render.ShowSceneNode(mNode);
            render.EnableAnimation(true);
        }

        void CreateGeometry()
        {
            Float32Buffer vb = new Float32Buffer(0);
            for (int i = 0; i < mBaseVertices.Count; i++)
            {
                vb.Append((float)mBaseVertices[i].x, (float)mBaseVertices[i].y, (float)mBaseVertices[i].z);
            }

            Float32Buffer cb = new Float32Buffer(0);
            for (int i = 0; i < mColors.Count; i++)
            {
                cb.Append((float)mColors[i].x, (float)mColors[i].y, (float)mColors[i].z);
            }

            mGeometry = GeometryBuilder.CreateLines(new Float32Array(vb), null, new Float32Array(cb));
        }

        void RegenerateTree()
        {
            mBaseVertices.Clear();
            mColors.Clear();
            mMaxHeight = 0;

            Vector3 start = new Vector3(0, 0, 0);
            Vector3 end = new Vector3(0, mInitialLength, 0);
            GenerateTree(start, end, mDepth);

            // 更新颜色查找表的最大值
            if (mMaxHeight > 0)
                mColorTable.SetMaxValue(mMaxHeight);

            CreateGeometry();

            // 更新节点的几何体
            if (mNode != null)
            {
                mNode.SetGeometry(mGeometry);
            }
        }

        void GenerateTree(Vector3 start, Vector3 end, int depth)
        {
            if (depth == 0)
                return;

            // 添加当前线段
            mBaseVertices.Add(start);
            mBaseVertices.Add(end);

            // 使用颜色查找表根据Y坐标获取颜色 - 有限元彩虹风格
            var startClr = mColorTable.GetColor((float)start.y);
            var endClr = mColorTable.GetColor((float)end.y);
            mColors.Add(new Vector3(startClr.x, startClr.y, startClr.z));
            mColors.Add(new Vector3(endClr.x, endClr.y, endClr.z));

            // 更新最大高度
            if (end.y > mMaxHeight)
                mMaxHeight = (float)end.y;

            // 计算当前线段的方向和长度
            Vector3 dir = end - start;
            float length = (float)dir.length();
            dir.normalize();

            // 计算左分支终点
            float leftLength = length * mScale;
            Vector3 leftDir = RotateAroundZ(dir, mCurrentAngle);
            Vector3 leftEnd = end + leftDir * leftLength;

            // 计算右分支终点
            float rightLength = length * mScale;
            Vector3 rightDir = RotateAroundZ(dir, -mCurrentAngle);
            Vector3 rightEnd = end + rightDir * rightLength;

            // 递归生成分支
            GenerateTree(end, leftEnd, depth - 1);
            GenerateTree(end, rightEnd, depth - 1);
        }

        Vector3 RotateAroundZ(Vector3 dir, float angleDegrees)
        {
            float angleRad = (float)(angleDegrees * Math.PI / 180.0);
            float cos = (float)Math.Cos(angleRad);
            float sin = (float)Math.Sin(angleRad);

            float x = dir.x * cos - dir.y * sin;
            float y = dir.x * sin + dir.y * cos;
            float z = dir.z;

            return new Vector3(x, y, z);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
        }

        public override void Animation(IRenderView render, float time)
        {
            // 角度从 mAngleMin 变化到 mAngleMax，然后循环
            // 使用正弦函数实现平滑的来回变化
            float t = (float)(Math.Sin(time * 0.5) * 0.5 + 0.5); // 0 到 1
            float newAngle = mAngleMin + t * (mAngleMax - mAngleMin);

            // 只有当角度变化足够大时才重新生成分形树
            if (Math.Abs(newAngle - mCurrentAngle) > 0.1f)
            {
                mCurrentAngle = newAngle;
                RegenerateTree();
            }

            render.RequestDraw();
        }
    }
}

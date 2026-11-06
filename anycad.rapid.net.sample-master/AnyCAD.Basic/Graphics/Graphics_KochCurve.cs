using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_KochCurve : TestCase
    {
        public Graphics_KochCurve()
        {
            Title = "科赫雪花曲线";
            Description = "递归生成科赫分形曲线并配合彩虹色条";
            Tags.Add("显示");
            Tags.Add("分形");
            Level = 1;
        }
        int mIterations = 5;
        float mSize = 200;

        List<Vector3> mVertices = new List<Vector3>();
        ColorLookupTable mColorTable = new ColorLookupTable();

        BufferGeometry mGeometry;
        PrimitiveSceneNode mNode;
        IRenderView mRender;
        public override void Run(IRenderView render)
        {
            mRender = render;
            mVertices.Clear();

            // 初始化颜色查找表 - 彩虹色
            mColorTable.SetMaxValue(mSize);
            mColorTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));

            // 生成分形
            GenerateKochCurve();

            // 创建几何体
            CreateGeometry();

            // 创建材质
            var material = BasicMaterial.Create("koch");
            material.SetUniform("diffuse", ColorTable.Hex(0xFFFFFF));
            material.GetTemplate().SetVertexColors(true);

            mNode = new PrimitiveSceneNode(mGeometry, material);
            render.ShowSceneNode(mNode);

            // 显示颜色条
            ShowColorPalette();
        }

        void ShowColorPalette()
        {
            PaletteWidget pw = new PaletteWidget();
            pw.Update(mColorTable);
            mRender.ShowSceneNode(pw);
        }

        void GenerateKochCurve()
        {
            // 从左下角开始，水平向右
            Vector3 p1 = new Vector3(-mSize / 2, -mSize / 2, 0);
            Vector3 p2 = new Vector3(mSize / 2, -mSize / 2, 0);

            // 递归生成Koch曲线
            KochSegment(p1, p2, mIterations);
        }

        void KochSegment(Vector3 p1, Vector3 p2, int depth)
        {
            if (depth == 0)
            {
                // 到达递归终止，添加线段
                mVertices.Add(p1);
                mVertices.Add(p2);
                return;
            }

            // 将线段分成三等份
            float segLength = (float)(p2 - p1).length();
            Vector3 v = p2 - p1;
            v.normalize();
            Vector3 dir = v;

            Vector3 a = p1;
            Vector3 b = p1 + dir * (segLength / 3.0f);
            Vector3 c = p1 + dir * (segLength * 2.0f / 3.0f);
            Vector3 d = p2;

            // 计算顶点B绕C旋转60度的位置（向外）
            // 先计算BC的中点，然后计算等边三角形的高度
            Vector3 bcMid = (b + c) * 0.5;
            Vector3 bcV = c - b;
            bcV.normalize();
            Vector3 bcDir = bcV;
            float height = (b - c).length() * (float)Math.Sqrt(3) / 2;

            // 旋转90度得到垂直方向，然后乘以高度
            // 旋转90度：(x, y) -> (-y, x)
            Vector3 normal = new Vector3(-bcDir.y, bcDir.x, 0);
            Vector3 peak = bcMid + normal * height;

            // 递归处理四段
            KochSegment(a, b, depth - 1);
            KochSegment(b, peak, depth - 1);
            KochSegment(peak, c, depth - 1);
            KochSegment(c, d, depth - 1);
        }

        void CreateGeometry()
        {
            Float32Buffer vb = new Float32Buffer(0);
            Float32Buffer cb = new Float32Buffer(0);

            int count = mVertices.Count / 2;
            for (int i = 0; i < mVertices.Count; i += 2)
            {
                // 顶点1
                Vector3 p1 = mVertices[i];
                Vector3 p2 = mVertices[i + 1];
                vb.Append((float)p1.x, (float)p1.y, (float)p1.z);
                vb.Append((float)p2.x, (float)p2.y, (float)p2.z);

                // 根据线段中点的Y坐标获取颜色
                float midY = (float)((p1.y + p2.y) / 2.0);
                var clr = mColorTable.GetColor(midY + mSize / 2);
                cb.Append(clr.x, clr.y, clr.z);
                cb.Append(clr.x, clr.y, clr.z);
            }

            mGeometry = GeometryBuilder.CreateLines(new Float32Array(vb), null, new Float32Array(cb));
        }

        public override void Exit(IRenderView render)
        {
        }

        public override void Animation(IRenderView render, float time)
        {
        }
    }
}

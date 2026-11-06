using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// CAE 线云图 —— 框架结构弯矩图
    ///
    /// 建立一个门式框架（两柱一梁），横梁承受均布荷载 q：
    ///   1. 灰色骨架线显示框架结构（柱脚铰接、柱顶与梁刚接）；
    ///   2. 按结构力学公式计算各杆件弯矩：
    ///        梁（两端固支）：M(x) = q·x·(L-x)/2 - q·L^2/12
    ///        柱（柱顶与梁端弯矩平衡、柱脚铰接）：M(y) = M_end·y/H
    ///   3. 将弯矩值映射为顶点颜色（Rainbow 色带 + 调色板），
    ///      并沿杆件法向按比例偏移绘制弯矩轮廓线：
    ///      正弯矩画在受拉侧（梁下缘），负弯矩画在另一侧（梁上缘/柱外侧）。
    /// </summary>
    class Graphics_CAELine : TestCase
    {
        // ---- 框架结构参数（模型单位）----
        const double Span = 200.0;      // 框架跨度 L
        const double Height = 100.0;    // 立柱高度 H
        const double Load = 0.12;       // 横梁均布荷载 q

        // ---- 弯矩图绘制参数 ----
        const int BeamSamples = 64;     // 横梁弯矩图采样点数
        const int ColumnSamples = 40;   // 每根立柱弯矩图采样点数
        const double MomentScale = 8.0; // 弯矩值 → 法向偏移量的放大系数

        // ---- 弯矩特征值 ----
        double mEndMoment;              // 梁端固端弯矩 = -q·L^2/12
        double mMidMoment;              // 梁跨中弯矩   =  q·L^2/24

        ColorLookupTable mColorTable;

        public Graphics_CAELine()
        {
            Title = "框架结构弯矩图";
            Description = "门式框架在均布荷载作用下的弯矩图（CAE线云图）";
            Tags.Add("显示");
            Tags.Add("云图");
            Tags.Add("结构力学");
            Level = 2;
        }

        /// <summary>
        /// 两端固支梁在均布荷载 q 作用下的弯矩方程：
        /// M(x) = q·x·(L-x)/2 - q·L^2/12，x 为距左端的距离。
        /// </summary>
        double BeamMoment(double x)
        {
            return Load * x * (Span - x) / 2.0 - mEndMoment;
        }

        /// <summary>
        /// 立柱弯矩（柱顶与梁端弯矩平衡、柱脚铰接的近似线性分布）：
        /// M(y) = M_end · y / H，y 为距柱脚的高度。
        /// </summary>
        double ColumnMoment(double y)
        {
            return mEndMoment * y / Height;
        }

        /// <summary>
        /// 追加一个带弯矩颜色值的顶点。
        /// </summary>
        void AppendVertex(Float32Buffer positions, Float32Buffer colors,
            double x, double y, double z, double moment)
        {
            positions.Append((float)x, (float)y, (float)z);
            var clr = mColorTable.GetColor((float)moment);
            colors.Append(clr.x, clr.y, clr.z);
        }

        /// <summary>
        /// 生成一根杆件的弯矩图轮廓线。
        /// 沿杆件轴线等距采样，轮廓点 = 轴线点 + 法向偏移(M / MomentScale)，
        /// 相邻采样点连成线段，返回本杆件使用的顶点数。
        /// </summary>
        /// <param name="axis0">杆件起点</param>
        /// <param name="axis1">杆件终点</param>
        /// <param name="normal">弯矩正方向对应的法向</param>
        /// <param name="samples">采样点数</param>
        /// <param name="momentAt">给定杆长比例 t∈[0,1] 返回弯矩值</param>
        /// <param name="baseIndex">当前顶点数组的起始索引</param>
        int AppendMomentLine(Float32Buffer positions, Float32Buffer colors, Uint32Buffer indices,
            Vector3 axis0, Vector3 axis1, Vector3 normal,
            int samples, Func<double, double> momentAt, int baseIndex)
        {
            for (int i = 0; i < samples; ++i)
            {
                double t = (double)i / (samples - 1);
                double moment = momentAt(t);

                Vector3 axisPoint = axis0 + (axis1 - axis0) * (float)t;
                Vector3 offset = normal * (float)(moment / MomentScale);
                Vector3 pos = axisPoint + offset;

                AppendVertex(positions, colors, pos.x, pos.y, pos.z, moment);

                if (i > 0)
                {
                    indices.Append((uint)(baseIndex + i - 1));
                    indices.Append((uint)(baseIndex + i));
                }
            }
            return samples;
        }

        public override void Run(IRenderView render)
        {
            // 1. 计算弯矩特征值（用于调色与说明）
            mEndMoment = -Load * Span * Span / 12.0;  // 梁端固端弯矩
            mMidMoment = Load * Span * Span / 24.0;   // 梁跨中弯矩
            Console.WriteLine("框架弯矩图: 梁端 M = {0:F1}, 跨中 M = {1:F1}",
                mEndMoment, mMidMoment);

            // 2. 初始化调色板（Rainbow，范围覆盖弯矩正负极值）
            mColorTable = new ColorLookupTable();
            mColorTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));
            mColorTable.SetMinValue(-500.0f);
            mColorTable.SetMaxValue(500.0f);

            // 3. 建立框架结构（灰色骨架线）
            Float32Buffer framePositions = new Float32Buffer(0);
            Uint32Buffer frameIndices = new Uint32Buffer(0);

            // 节点：A 左下柱脚, B 左上柱顶, C 右上柱顶, D 右下柱脚
            framePositions.Append(0f, 0f, 0f);                     // A
            framePositions.Append(0f, (float)Height, 0f);         // B
            framePositions.Append((float)Span, (float)Height, 0f);// C
            framePositions.Append((float)Span, 0f, 0f);           // D
            framePositions.Append(-30f, 0f, 0f);                  // 地面基线起点
            framePositions.Append((float)(Span + 30), 0f, 0f);    // 地面基线终点

            frameIndices.Append(0); frameIndices.Append(1);       // 左柱 A-B
            frameIndices.Append(1); frameIndices.Append(2);       // 横梁 B-C
            frameIndices.Append(2); frameIndices.Append(3);       // 右柱 C-D
            frameIndices.Append(4); frameIndices.Append(5);       // 地面基线

            var frameGeometry = new BufferGeometry(EnumPrimitiveType.LINES);
            frameGeometry.AddAttribute(EnumAttributeSemantic.Position, EnumAttributeComponents.Three, framePositions);
            frameGeometry.SetIndex(frameIndices);

            var frameMaterial = LineMaterial.Create("frame-line");
            frameMaterial.SetColor(ColorTable.LightGray);
            frameMaterial.SetLineWidth(2);

            var frameNode = new PrimitiveSceneNode(frameGeometry, frameMaterial);
            frameNode.SetPickable(false);
            render.ShowSceneNode(frameNode);

            // 4. 生成弯矩图轮廓线（顶点按弯矩值着色）
            Float32Buffer mPositions = new Float32Buffer(0);
            Float32Buffer mColors = new Float32Buffer(0);
            Uint32Buffer mEdges = new Uint32Buffer(0);

            int baseIndex = 0;
            // 横梁 B-C：正弯矩向下（受拉侧），负弯矩向上
            baseIndex += AppendMomentLine(mPositions, mColors, mEdges,
                new Vector3(0f, (float)Height, 0f),
                new Vector3((float)Span, (float)Height, 0f),
                new Vector3(0f, -1f, 0f),
                BeamSamples, t => BeamMoment(t * Span), baseIndex);
            // 左柱 A-B：负弯矩向外（-X）
            baseIndex += AppendMomentLine(mPositions, mColors, mEdges,
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, (float)Height, 0f),
                new Vector3(1f, 0f, 0f),
                ColumnSamples, t => ColumnMoment(t * Height), baseIndex);
            // 右柱 C-D：负弯矩向外（+X）
            baseIndex += AppendMomentLine(mPositions, mColors, mEdges,
                new Vector3((float)Span, 0f, 0f),
                new Vector3((float)Span, (float)Height, 0f),
                new Vector3(-1f, 0f, 0f),
                ColumnSamples, t => ColumnMoment(t * Height), baseIndex);

            var momentGeometry = new BufferGeometry(EnumPrimitiveType.LINES);
            momentGeometry.AddAttribute(EnumAttributeSemantic.Position, EnumAttributeComponents.Three, mPositions);
            momentGeometry.AddAttribute(EnumAttributeSemantic.Color, EnumAttributeComponents.Three, mColors);
            momentGeometry.SetIndex(mEdges);

            var momentMaterial = BasicMaterial.Create("moment-line");
            momentMaterial.GetTemplate().SetVertexColors(true);
            momentMaterial.SetLineWidth(3);

            var momentNode = new PrimitiveSceneNode(momentGeometry, momentMaterial);
            momentNode.SetPickable(false);
            render.ShowSceneNode(momentNode);

            // 5. 显示调色板图例
            PaletteWidget palette = new PaletteWidget();
            palette.Update(mColorTable);
            render.ShowSceneNode(palette);
        }
    }
}

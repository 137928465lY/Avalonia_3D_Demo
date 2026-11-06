using AnyCAD.Foundation;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 粒子建筑动画：
    /// 用 GeometryBuilder 把墙/楼板/圆柱/穹顶等部件拼成一栋建筑，
    /// 汇合全部顶点成点云，逐帧 CPU 驱动"坠落 → 落地 → 回升归位"循环。
    ///
    /// 关键点：
    ///  - 各部件几何只用来"取顶点"，场景里显示的是点云（PointsMaterial + 顶点色）；
    ///  - 顶点缓冲以 DYNAMIC_DRAW 逐帧原地改写（Graphics_GeometryDynamic 同款套路）；
    ///  - 动画状态镜像在托管 float[] 里，避免每帧读回非托管缓冲。
    /// </summary>
    class Graphics_BuildingPoints : TestCase
    {
        public Graphics_BuildingPoints()
        {
            Title = "粒子建筑";
            Description = "GeometryBuilder 拼装建筑，顶点云化后做坠落→回升循环动画；BloomOutput 泛光 + 红色点阵地面（webgl_points_dynamic 同款效果）";
            Tags.Add("显示");
            Tags.Add("动画");
            Tags.Add("粒子");
            Level = 2;
        }

        // ---- 点云与动画状态 ----
        BufferGeometry mGeometry;
        BufferAttribute mPositionAttr;
        Float32Array mPositions;     //!< 活动顶点视图（与 geometry 的 Position 属性共享内存）
        float[] mCurrent;            //!< 托管镜像：当前顶点位置（x,y,z 逐顶点）
        float[] mInitial;            //!< 初始顶点位置（回升目标）
        uint mCount;                 //!< 顶点数
        readonly Random mRandom = new Random();

        int mDirection;              //!< 0=未开始 1=回升 -1=坠落
        float mDelay;                //!< 阶段间停留（秒）
        float mStart = 1.0f;         //!< 开场延迟（秒）
        uint mVerticesDown;
        uint mVerticesUp;
        const float FallSpeed = 30.0f;
        const float RiseSpeed = 40.0f;

        public override void Run(IRenderView render)
        {
            mDirection = 0;
            mDelay = 0;
            mStart = 1.0f;
            mVerticesDown = 0;
            mVerticesUp = 0;

            // ---- 拼建筑：GeometryBuilder 造部件 → 顶点汇入点云 ----
            var positions = new Float32Buffer(0);
            var colors = new Float32Buffer(0);
            BuildBuilding(positions, colors);

            var src = new Float32Array(positions);
            mCount = src.GetItemCount() / 3;
            mInitial = new float[mCount * 3];
            mCurrent = new float[mCount * 3];
            for (uint i = 0; i < mCount * 3; ++i)
            {
                mCurrent[i] = mInitial[i] = src.GetValue(i);
            }

            // ---- 点云几何 + 材质 ----
            mGeometry = GeometryBuilder.CreatePoints(new Float32Array(positions),
                new Float32Array(colors), null);
            mPositionAttr = mGeometry.GetAttribute(0);
            mPositionAttr.SetDataUsage(EnumBufferDataUsage.DYNAMIC_DRAW);
            mPositions = new Float32Array(mPositionAttr.GetData());

            var material = PointsMaterial.Create("building-points");
            material.SetVertexColors(true);
            material.SetSizeAttenuation(false);
            material.SetPointSize(3.0f);

            var node = new PrimitiveSceneNode(mGeometry, material);
            node.SetPickable(false);
            render.ShowSceneNode(node);

            // ---- 地面点阵（红色，对标示例 GridHelper 的红点阵衬托） ----
            const int GridN = 61;
            const float GridStep = 32f;
            var red = ColorTable.Hex(0xc05050);
            var gridPos = new Float32Buffer(GridN * GridN * 3);
            var gridCol = new Float32Buffer(GridN * GridN * 3);
            var gridHalf = (GridN - 1) * GridStep * 0.5f;
            for (int ix = 0; ix < GridN; ++ix)
            {
                for (int iy = 0; iy < GridN; ++iy)
                {
                    gridPos.Append(-gridHalf + ix * GridStep, -gridHalf + iy * GridStep, -0.5f);
                    gridCol.Append(red.x, red.y, red.z);
                }
            }
            var gridMaterial = PointsMaterial.Create("building-grid");
            gridMaterial.SetVertexColors(true);
            gridMaterial.SetSizeAttenuation(false);
            gridMaterial.SetPointSize(6f);
            var gridNode = new PrimitiveSceneNode(
                GeometryBuilder.CreatePoints(new Float32Array(gridPos), new Float32Array(gridCol), null),
                gridMaterial);
            gridNode.SetPickable(false);
            render.ShowSceneNode(gridNode);

            // ---- 渲染设置：保存原状（Exit 恢复），再切深背景 + BloomOutput 泛光 ----
            var rs = render.Viewer.GetRenderSettings();
            SaveRenderState(render, rs);

            // 深色背景：threshold=0 的全局泛光会把亮背景整屏当高光提取（过曝发白的根源），
            // 必须配深背景（对标示例 fog 色 0x000104）。
            render.SetBackgroundColor(0.0f, 0.004f, 0.016f, 1.0f);

            rs.SetToneMapping(EnumToneMapping.NoToneMapping);
            rs.SetToneMappingInOutput(true);   // 链末 OutputPass 统一做 sRGB（与 ToneMapping 二选一）
            var bloom = rs.GetBloom();
            bloom.SetEnabled(true);
            bloom.SetGlobalMode(true);         // 全局档：提取源 = 全场景合成图
            bloom.SetAdditiveBlend(true);      // 加性叠加档（强度不按 Strength² 复利）
            bloom.SetThreshold(0f);
            bloom.SetStrength(0.75f);          // three.js BloomPass(0.75) 同值
            bloom.SetRadius(0.5f);
            render.Viewer.SetPipeline("BloomOutput");

            // ---- 相机：斜俯视整栋楼（引擎 Z-up，Up 固定 +Z） ----
            var camera = render.Camera;
            camera.SetProjectionType(EnumProjectionType.Perspective);
            camera.SetNear(1.0f);
            camera.SetFar(10000.0f);
            camera.LookAt(new Vector3d(520, -480, 320), new Vector3d(0, 0, 110), Vector3d.UNIT_Z);

            render.ShowCoordinateGrid(false);
            render.EnableAnimation(true);
        }

        /// <summary>
        /// 用 GeometryBuilder 部件拼一栋建筑，把每个部件的顶点按摆位变换后汇入点云。
        /// 部件本地坐标 → 世界坐标的映射用委托传入，逐部件着色。
        /// </summary>
        void BuildBuilding(Float32Buffer positions, Float32Buffer colors)
        {
            // 裙楼（0~30）：四面墙 + 顶板
            AddWalls(positions, colors, 140, 30, 0, 30, 14, 3, ColorTable.Hex(0x8fa3b0));
            AddSlab(positions, colors, 140, 140, 30, 14, 14, ColorTable.Hex(0x77848f));

            // 塔身（30~130）
            AddWalls(positions, colors, 90, 100, 30, 130, 9, 10, ColorTable.Hex(0xb4c2cc));
            AddSlab(positions, colors, 90, 90, 130, 9, 9, ColorTable.Hex(0x8fa3b0));

            // 上部收分（130~175）
            AddWalls(positions, colors, 64, 45, 130, 175, 8, 5, ColorTable.Hex(0xcfd9e2));
            AddSlab(positions, colors, 64, 64, 175, 8, 8, ColorTable.Hex(0x9aa5b1));

            // 四角圆柱（0~175）
            var columnGeom = GeometryBuilder.CreateCylinder(6, 6, 175);
            foreach (var c in CornerOffsets(62))
            {
                var cx = c.x;
                var cy = c.y;
                AddPart(positions, colors, columnGeom, ColorTable.Hex(0xff9f43),
                    p => new float[] { cx + p.x, cy + p.y, p.z });
            }

            // 穹顶（球心 175，露出上半球）
            AddPart(positions, colors, GeometryBuilder.CreateSphere(34, 32, 20),
                ColorTable.Hex(0xf6c945), p => new float[] { p.x, p.y, 175 + p.z });

            // 尖塔 + 顶球
            AddPart(positions, colors, GeometryBuilder.CreateCylinder(3, 3, 45),
                ColorTable.Hex(0xe67e22), p => new float[] { p.x, p.y, 175 + 22.5f + p.z });
            AddPart(positions, colors, GeometryBuilder.CreateSphere(7, 16, 12),
                ColorTable.Hex(0xffffff), p => new float[] { p.x, p.y, 228 + p.z });
        }

        static Vector3[] CornerOffsets(float half)
        {
            return new Vector3[]
            {
                new Vector3(half, half, 0), new Vector3(-half, half, 0),
                new Vector3(half, -half, 0), new Vector3(-half, -half, 0),
            };
        }

        /// <summary>
        /// 一圈四面墙：沿 X 的两面 + 沿 Y 的两面，覆盖 [baseZ, topZ]。
        /// </summary>
        void AddWalls(Float32Buffer positions, Float32Buffer colors, float width, float height,
            float baseZ, float topZ, uint segX, uint segY, Vector3 color)
        {
            var geom = GeometryBuilder.CreatePlane(width, height, segX, segY);
            var half = width * 0.5f;
            var midZ = (baseZ + topZ) * 0.5f;

            // 平面几何在本地 XY 平面上（x∈[-w/2,w/2], y∈[-h/2,h/2]）。
            // 沿 X 的墙：x 保留，本地 y → 世界 z；沿 Y 的墙：本地 x → 世界 y。
            AddPart(positions, colors, geom, color,
                p => new float[] { p.x, half, midZ + p.y });
            AddPart(positions, colors, geom, color,
                p => new float[] { p.x, -half, midZ + p.y });
            AddPart(positions, colors, geom, color,
                p => new float[] { half, p.x, midZ + p.y });
            AddPart(positions, colors, geom, color,
                p => new float[] { -half, p.x, midZ + p.y });
        }

        /// <summary>水平楼板：本地 XY 平面直接放到高度 z。</summary>
        void AddSlab(Float32Buffer positions, Float32Buffer colors, float sizeX, float sizeY,
            float z, uint segX, uint segY, Vector3 color)
        {
            var geom = GeometryBuilder.CreatePlane(sizeX, sizeY, segX, segY);
            AddPart(positions, colors, geom, color, p => new float[] { p.x, p.y, z });
        }

        /// <summary>取部件几何的 Position 顶点，经 map 变换后连同颜色写入点云缓冲。</summary>
        void AddPart(Float32Buffer positions, Float32Buffer colors, BufferGeometry geom,
            Vector3 color, Func<Vector3, float[]> map)
        {
            var attr = geom.GetAttribute(0);
            var view = new Float32Array(attr.GetData());
            var itemCount = view.GetItemCount();
            for (uint i = 0; i < itemCount; i += 3)
            {
                var local = new Vector3(view.GetValue(i), view.GetValue(i + 1), view.GetValue(i + 2));
                var world = map(local);
                positions.Append(world[0], world[1], world[2]);
                colors.Append(color.x, color.y, color.z);
            }
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
            RestoreRenderState(render);
            mGeometry = null;
            mPositionAttr = null;
            mPositions = null;
        }

        // ---- 渲染状态保存/恢复（避免污染示例程序全局观感） ----
        EnumToneMapping mSavedToneMapping = EnumToneMapping.NoToneMapping;
        float mSavedExposure = 1f;
        bool mSavedToneMappingInOutput;
        bool mSavedBloomEnabled;
        bool mSavedBloomGlobal;
        bool mSavedBloomAdditive;
        float mSavedBloomThreshold;
        float mSavedBloomStrength = 1f;
        float mSavedBloomRadius = 0.5f;
        string mSavedPipeline;
        Background mSavedBackground;

        void SaveRenderState(IRenderView render, RenderSettings rs)
        {
            mSavedToneMapping = rs.GetToneMapping();
            mSavedExposure = rs.GetToneMappingExposure();
            mSavedToneMappingInOutput = rs.GetToneMappingInOutput();
            var bloom = rs.GetBloom();
            mSavedBloomEnabled = bloom.GetEnabled();
            mSavedBloomGlobal = bloom.GetGlobalMode();
            mSavedBloomAdditive = bloom.GetAdditiveBlend();
            mSavedBloomThreshold = bloom.GetThreshold();
            mSavedBloomStrength = bloom.GetStrength();
            mSavedBloomRadius = bloom.GetRadius();
            mSavedPipeline = render.Viewer.GetActivePipelineName();
            mSavedBackground = render.Viewer.GetBackground();
        }

        void RestoreRenderState(IRenderView render)
        {
            if (mSavedBackground != null)
            {
                render.Viewer.SetBackground(mSavedBackground);
                mSavedBackground = null;
            }
            var rs = render.Viewer.GetRenderSettings();
            rs.SetToneMapping(mSavedToneMapping);
            rs.SetToneMappingExposure(mSavedExposure);
            rs.SetToneMappingInOutput(mSavedToneMappingInOutput);
            var bloom = rs.GetBloom();
            bloom.SetEnabled(mSavedBloomEnabled);
            bloom.SetGlobalMode(mSavedBloomGlobal);
            bloom.SetAdditiveBlend(mSavedBloomAdditive);
            bloom.SetThreshold(mSavedBloomThreshold);
            bloom.SetStrength(mSavedBloomStrength);
            bloom.SetRadius(mSavedBloomRadius);
            if (!string.IsNullOrEmpty(mSavedPipeline))
                render.Viewer.SetPipeline(mSavedPipeline);
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        public override void Animation(IRenderView render, float time)
        {
            if (mPositions == null || mCount == 0)
                return;

            // time 为秒（与 Graphics_MorphHorseHerd 一致）；首帧/挂起后钳步长
            if (mLastTime < 0f)
                mLastTime = time;
            var delta = (time - mLastTime);
            mLastTime = time;
            if (delta <= 0f || delta > 0.2f)
            {
                render.RequestDraw(EnumUpdateFlags.Scene);
                return;
            }

            if (mStart > 0f)
            {
                mStart -= delta;
            }
            else if (mDirection == 0)
            {
                mDirection = -1;
            }

            if (mDirection < 0)
            {
                // 坠落：z>0 的顶点随机游走下坠，落到地面钉在 z=0。
                // 每帧重算落地数（已落地粒子若逐帧累加会早早 ≥ 总数，提前触发回升）。
                mVerticesDown = 0;
                for (uint i = 0; i < mCount; ++i)
                {
                    var o = i * 3;
                    if (mCurrent[o + 2] > 0f)
                    {
                        mCurrent[o] += 1.5f * (0.5f - (float)mRandom.NextDouble()) * FallSpeed * delta;
                        mCurrent[o + 1] += 1.5f * (0.5f - (float)mRandom.NextDouble()) * FallSpeed * delta;
                        mCurrent[o + 2] += 3.0f * (0.25f - (float)mRandom.NextDouble()) * FallSpeed * delta;
                    }
                    else
                    {
                        mCurrent[o + 2] = 0f;
                        ++mVerticesDown;
                    }
                }
            }

            if (mDirection > 0)
            {
                // 回升：向初始位置收敛；每帧重算归位数（同坠落阶段的累加坑）
                mVerticesUp = 0;
                for (uint i = 0; i < mCount; ++i)
                {
                    var o = i * 3;
                    var dx = Math.Abs(mCurrent[o] - mInitial[o]);
                    var dy = Math.Abs(mCurrent[o + 1] - mInitial[o + 1]);
                    var dz = Math.Abs(mCurrent[o + 2] - mInitial[o + 2]);
                    if (dx + dy + dz > 1f)
                    {
                        if (dx > 0f)
                            mCurrent[o] -= (mCurrent[o] - mInitial[o]) / dx * RiseSpeed * delta
                                * (0.85f + (float)mRandom.NextDouble());
                        if (dy > 0f)
                            mCurrent[o + 1] -= (mCurrent[o + 1] - mInitial[o + 1]) / dy * RiseSpeed * delta
                                * (0.85f + (float)mRandom.NextDouble());
                        if (dz > 0f)
                            mCurrent[o + 2] -= (mCurrent[o + 2] - mInitial[o + 2]) / dz * RiseSpeed * delta
                                * (1.0f + (float)mRandom.NextDouble());
                    }
                    else
                    {
                        ++mVerticesUp;
                    }
                }
            }

            if (mVerticesDown >= mCount)
            {
                if (mDelay <= 0f)
                {
                    mDirection = 1;
                    mVerticesDown = 0;
                    mDelay = 1.5f;
                }
                else
                {
                    mDelay -= delta;
                }
            }

            if (mVerticesUp >= mCount)
            {
                if (mDelay <= 0f)
                {
                    mDirection = -1;
                    mVerticesUp = 0;
                    mDelay = 1.0f;
                }
                else
                {
                    mDelay -= delta;
                }
            }

            // 托管镜像写回顶点缓冲（原地改写 + RequestUpdate 触发重上传）
            for (uint i = 0; i < mCount * 3; ++i)
                mPositions.SetValue(i, mCurrent[i]);
            mPositionAttr.RequestUpdate();
            mGeometry.RequestUpdate();

            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        float mLastTime = -1f;
    }
}

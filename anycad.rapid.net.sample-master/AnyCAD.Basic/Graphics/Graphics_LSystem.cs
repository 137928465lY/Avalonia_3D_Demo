using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnyCAD.Demo.Graphics
{
    class Graphics_LSystem : TestCase
    {
        public Graphics_LSystem()
        {
            Title = "L系统分形";
            Description = "L文法生成植物、龙形等分形并自动切换";
            Tags.Add("显示");
            Tags.Add("分形");
            Level = 1;
        }
        // L系统参数
        string mAxiom = "F";           // 起始符号
        Dictionary<char, string> mRules = new Dictionary<char, string>(); // 产生式规则
        int mIterations = 5;           // 迭代次数 (保留作说明，实际运行时使用 mCurrentIterations)
        float mAngle = 25.0f;          // 旋转角度 (保留作说明，实际运行时使用 mCurrentAngle)
        float mLength = 10.0f;         // 线段长度
        float mLengthScale = 0.5f;     // 长度缩放因子

        // 动画控制
        float mCurrentAngle = 25.0f;
        float mAngleMin = 15.0f;
        float mAngleMax = 45.0f;
        int mCurrentIterations = 5;

        List<Vector3> mVertices = new List<Vector3>();
        List<Vector3> mColors = new List<Vector3>();
        ColorLookupTable mColorTable = new ColorLookupTable();
        float mMaxHeight;

        BufferGeometry mGeometry;
        PrimitiveSceneNode mNode;
        IRenderView mRender;

        // 预设的L系统
        enum PresetType
        {
            Plant,       // 植物
            Sierpinski,  // 谢尔宾斯基
            Dragon,      // 龙形曲线
            Tree         // 树
        }
        PresetType[] mPresetList = { PresetType.Plant, PresetType.Sierpinski, PresetType.Dragon, PresetType.Tree };
        int mPresetIndex = 0;

        public override void Run(IRenderView render)
        {
            mRender = render;

            // 初始化颜色查找表
            mColorTable.SetMaxValue(100);
            mColorTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));

            // 设置L系统规则
            mPresetIndex = 0;
            SetupPreset(mPresetList[mPresetIndex]);

            RegenerateGeometry();

            // 创建材质
            var material = BasicMaterial.Create("lsystem");
            material.SetUniform("diffuse", ColorTable.Hex(0xFFFFFF));
            material.GetTemplate().SetVertexColors(true);

            mNode = new PrimitiveSceneNode(mGeometry, material);
            render.ShowSceneNode(mNode);

            ShowColorPalette();
            render.EnableAnimation(true);
        }

        void SetupPreset(PresetType preset)
        {
            mRules.Clear();
            switch (preset)
            {
                case PresetType.Plant:
                    mAxiom = "X";
                    mRules['X'] = "F+[[X]-X]-F[-FX]+X";
                    mRules['F'] = "FF";
                    mAngle = 25.0f;
                    mLength = 5.0f;
                    mLengthScale = 0.5f;
                    mCurrentAngle = 25.0f;
                    break;

                case PresetType.Sierpinski:
                    mAxiom = "F-G-G";
                    mRules['F'] = "F-G+F+G-F";
                    mRules['G'] = "GG";
                    mAngle = 120.0f;
                    mLength = 5.0f;
                    mLengthScale = 0.5f;
                    mCurrentAngle = 120.0f;
                    break;

                case PresetType.Dragon:
                    mAxiom = "FX";
                    mRules['X'] = "X+YF+";
                    mRules['Y'] = "-FX-Y";
                    mAngle = 90.0f;
                    mLength = 5.0f;
                    mLengthScale = 1.0f;
                    mCurrentAngle = 90.0f;
                    break;

                case PresetType.Tree:
                    mAxiom = "F";
                    mRules['F'] = "FF[+F][-F]";
                    mAngle = 30.0f;
                    mLength = 8.0f;
                    mLengthScale = 0.6f;
                    mCurrentAngle = 30.0f;
                    break;
            }
        }

        void RegenerateGeometry()
        {
            mVertices.Clear();
            mColors.Clear();
            mMaxHeight = 0;

            // 生成L系统字符串
            string result = GenerateLSystem(mAxiom, mCurrentIterations);

            // 解释字符串生成图形
            InterpretLSystem(result);

            CreateGeometry();

            if (mNode != null)
            {
                mNode.SetGeometry(mGeometry);
            }
        }

        string GenerateLSystem(string axiom, int iterations)
        {
            string current = axiom;
            for (int i = 0; i < iterations; i++)
            {
                StringBuilder sb = new StringBuilder();
                foreach (char c in current)
                {
                    if (mRules.ContainsKey(c))
                        sb.Append(mRules[c]);
                    else
                        sb.Append(c);
                }
                current = sb.ToString();
            }
            return current;
        }

        void InterpretLSystem(string lstring)
        {
            // 使用栈来保存位置和角度状态
            Stack<Tuple<Vector3, float, float>> stateStack = new Stack<Tuple<Vector3, float, float>>();

            Vector3 currentPos = new Vector3(0, -100, 0);
            float currentAngle = 90.0f; // 向上为90度
            float currentLength = mLength;

            foreach (char c in lstring)
            {
                switch (c)
                {
                    case 'F':
                    case 'G':
                        {
                            // 计算新位置
                            float rad = (float)(currentAngle * Math.PI / 180.0);
                            float dx = (float)Math.Cos(rad) * currentLength;
                            float dy = (float)Math.Sin(rad) * currentLength;
                            Vector3 newPos = currentPos + new Vector3(dx, dy, 0);

                            // 添加线段
                            mVertices.Add(currentPos);
                            mVertices.Add(newPos);

                            // 添加颜色
                            var clr = mColorTable.GetColor(currentPos.y + 100);
                            mColors.Add(new Vector3(clr.x, clr.y, clr.z));
                            mColors.Add(new Vector3(clr.x, clr.y, clr.z));

                            // 更新最大高度
                            if (newPos.y > mMaxHeight)
                                mMaxHeight = (float)newPos.y;

                            currentPos = newPos;
                        }
                        break;

                    case '+': // 右转
                        currentAngle += mCurrentAngle;
                        break;

                    case '-': // 左转
                        currentAngle -= mCurrentAngle;
                        break;

                    case '[': // 保存状态
                        stateStack.Push(new Tuple<Vector3, float, float>(currentPos, currentAngle, currentLength));
                        currentLength *= mLengthScale; // 长度缩小
                        break;

                    case ']': // 恢复状态
                        if (stateStack.Count > 0)
                        {
                            var state = stateStack.Pop();
                            currentPos = state.Item1;
                            currentAngle = state.Item2;
                            currentLength = state.Item3;
                        }
                        break;
                }
            }
        }

        void CreateGeometry()
        {
            Float32Buffer vb = new Float32Buffer(0);
            Float32Buffer cb = new Float32Buffer(0);

            for (int i = 0; i < mVertices.Count; i++)
            {
                vb.Append((float)mVertices[i].x, (float)mVertices[i].y, (float)mVertices[i].z);
            }

            for (int i = 0; i < mColors.Count; i++)
            {
                cb.Append((float)mColors[i].x, (float)mColors[i].y, (float)mColors[i].z);
            }

            mGeometry = GeometryBuilder.CreateLines(new Float32Array(vb), null, new Float32Array(cb));
        }

        void ShowColorPalette()
        {
            PaletteWidget pw = new PaletteWidget();
            pw.Update(mColorTable);
            mRender.ShowSceneNode(pw);
        }

        public override void Exit(IRenderView render)
        {
            render.EnableAnimation(false);
        }

        public override void Animation(IRenderView render, float time)
        {
            // 每个预设动画持续12秒（6个深度级别 x 2秒）
            float cycleTime = 12.0f * mPresetList.Length; // 总周期
            float localTime = time % cycleTime;
            int currentPresetIdx = (int)(localTime / 12.0f);

            // 切换预设
            if (currentPresetIdx != mPresetIndex)
            {
                mPresetIndex = currentPresetIdx;
                SetupPreset(mPresetList[mPresetIndex]);
                mCurrentIterations = 1;
            }

            // 当前预设内的深度和角度动画
            float presetLocalTime = localTime % 12.0f;

            // 深度动态变化：1-6
            int newIterations = (int)(presetLocalTime / 2) % 6 + 1;

            // 角度在最小和最大之间变化
            float t = (float)(Math.Sin(presetLocalTime * 0.5) * 0.5 + 0.5);
            float newAngle = mAngleMin + t * (mAngleMax - mAngleMin);

            if (newIterations != mCurrentIterations || Math.Abs(newAngle - mCurrentAngle) > 0.5f)
            {
                mCurrentIterations = newIterations;
                mCurrentAngle = newAngle;
                RegenerateGeometry();
            }

            render.RequestDraw();
        }
    }
}

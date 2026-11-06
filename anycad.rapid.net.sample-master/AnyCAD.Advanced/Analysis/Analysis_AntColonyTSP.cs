using AnyCAD.Foundation;
using AnyCAD.Simulate;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Analysis
{
    /// <summary>
    /// 蚁群算法（ACO）求解旅行商问题（TSP）
    /// 迁移自 Featured.ACO（WPF 版），去掉所有 GUI 依赖：
    /// - ACO 内核（Ant / PheroTable / AntColony / Config / 概率计算）逐行保留原逻辑
    /// - 原"开始"+"运行"两个按钮合并为 Run 内的一次完整求解
    /// - 原"逐步运行"动画与蚂蚁移动动画改为一次性求解后直接展示结果
    /// - 状态栏文字改为场景内文字节点
    /// 展示内容：随机城市（金色球）、全部候选路径（深灰细线）、
    /// 按信息素浓度着色的路径（Rainbow 色表 + 调色板图例）、最优路径（红色折线）
    /// </summary>
    class Analysis_AntColonyTSP : TestCase
    {
        public Analysis_AntColonyTSP()
        {
            Title = "蚁群算法路径优化";
            Description = "用蚁群算法求解旅行商问题：随机生成城市，迭代后按信息素浓度着色显示路径，并高亮最短闭合路径";
            Tags.Add("分析");
            Tags.Add("算法");
            Tags.Add("路径优化");
            Level = 3;
        }

        /// <summary>
        /// 城市
        /// </summary>
        class TspCity
        {
            public double X { get; set; }
            public double Y { get; set; }

            public TspCity(double x, double y)
            {
                X = x;
                Y = y;
            }

            public Vector3 GetPosition(double z)
            {
                return new Vector3((float)X, (float)Y, (float)z);
            }

            public void ShowCity(IRenderView view)
            {
                var node = PrimitiveSceneNode.Create(GeometryBuilder.AtomSphere(), MaterialStore.Gold);
                node.SetPickable(false);
                node.SetTransform(Matrix4d.makeTranslation(X, Y, 1.0) * Matrix4d.makeScale(0.9));
                view.ShowSceneNode(node);
            }
        }

        /// <summary>
        /// 城市地图：缓存两两距离
        /// </summary>
        class TspCityMap
        {
            readonly Dictionary<int, double> mTable = new Dictionary<int, double>();
            readonly List<TspCity> mCityList = new List<TspCity>();

            public int MaxDistance = 50;
            public List<TspCity> CityList { get { return mCityList; } }

            public TspCity GetCity(int idx)
            {
                return mCityList[idx];
            }

            public void Initialize()
            {
                mTable.Clear();

                for (int ii = 0; ii < mCityList.Count - 1; ii++)
                {
                    for (int jj = ii + 1; jj < mCityList.Count; jj++)
                    {
                        var idx = ii * mCityList.Count + jj;
                        var dx = mCityList[ii].X - mCityList[jj].X;
                        var dy = mCityList[ii].Y - mCityList[jj].Y;
                        mTable.Add(idx, Math.Sqrt(dx * dx + dy * dy) + 0.1);
                    }
                }
            }

            static void Swap<T>(ref T a, ref T b)
            {
                T temp = a;
                a = b;
                b = temp;
            }

            public double GetDistance(int from, int to)
            {
                if (to < from)
                    Swap(ref from, ref to);

                return mTable[from * mCityList.Count + to];
            }

            public double ComputeDistance(List<int> trajectory)
            {
                double dist = 0;
                for (int ii = 0; ii < trajectory.Count - 1; ++ii)
                {
                    dist += GetDistance(trajectory[ii], trajectory[ii + 1]);
                }
                return dist;
            }
        }

        /// <summary>
        /// 蚂蚁：记录走过的城市序列
        /// </summary>
        class TspAnt
        {
            public int TargetId { get { return Trajectory[Trajectory.Count - 1]; } }
            public List<int> Trajectory { get; set; } = new List<int>();
            public int Count { get { return Trajectory.Count; } }
            public double Fitness { get; set; } = 0;

            public TspAnt(int target)
            {
                Reset(target);
            }

            public void Reset(int target)
            {
                Fitness = 0.0;
                Trajectory.Clear();
                Trajectory.Add(target);
            }

            public void MoveTo(int target)
            {
                Trajectory.Add(target);
            }

            public bool Contains(int target)
            {
                return Trajectory.Contains(target);
            }
        }

        /// <summary>
        /// 算法参数
        /// </summary>
        class TspConfig
        {
            public double Alpha { get; set; } = 1.0;
            public double Beta { get; set; } = 5.0;
            public double RHO { get; set; } = 0.5;
            public double QVAL { get; set; } = 100;
            public int MaxCount { get; set; } = 10;
            public int TargetCount { get; set; } = 30;
            public int AntCount { get; set; } = 30;

            public double MaxTime
            {
                get { return MaxCount * TargetCount; }
            }

            public double InitPHER
            {
                get { return 1.0 / TargetCount; }
            }

            public ITspProbability Probability;

            public TspConfig(ITspProbability probability)
            {
                Probability = probability;
            }
        }

        interface ITspProbability
        {
            double Compute(int from, int to, TspPheroTable table);
        }

        /// <summary>
        /// 信息素表
        /// </summary>
        class TspPheroTable
        {
            readonly Dictionary<int, double> mTable = new Dictionary<int, double>();
            readonly TspConfig mConfig;
            readonly Random mRandom = new Random();

            public TspConfig Config { get { return mConfig; } }

            public TspPheroTable(TspConfig config)
            {
                mConfig = config;
            }

            public void Initialize()
            {
                mTable.Clear();

                for (int ii = 0; ii < mConfig.TargetCount - 1; ii++)
                {
                    for (int jj = ii + 1; jj < mConfig.TargetCount; jj++)
                    {
                        mTable.Add(ii * mConfig.TargetCount + jj, mConfig.InitPHER);
                    }
                }
            }

            static void Swap<T>(ref T a, ref T b)
            {
                T temp = a;
                a = b;
                b = temp;
            }

            public double GetPhero(int from, int to)
            {
                if (to < from)
                    Swap(ref from, ref to);

                return mTable[from * mConfig.TargetCount + to];
            }

            void SetPhero(int from, int to, double phero)
            {
                if (to < from)
                    Swap(ref from, ref to);

                mTable[from * mConfig.TargetCount + to] = phero;
            }

            /// <summary>
            /// 挥发 + 按路径质量累积信息素
            /// </summary>
            public void UpdateTrails(List<TspAnt> ants)
            {
                var cityCount = mConfig.TargetCount;

                for (int from = 0; from < cityCount - 1; from++)
                {
                    for (int to = from + 1; to < cityCount; to++)
                    {
                        var phero = GetPhero(from, to);
                        phero *= (1.0 - mConfig.RHO);
                        if (phero < 0.0)
                            phero = mConfig.InitPHER;

                        SetPhero(from, to, phero);
                    }
                }

                for (int aa = 0; aa < ants.Count; aa++)
                {
                    var ant = ants[aa];
                    for (int idx = 0; idx < ant.Trajectory.Count - 1; idx++)
                    {
                        int from = ant.Trajectory[idx];
                        int to = ant.Trajectory[idx + 1];

                        if (ant.Fitness <= 0.0)
                            continue;

                        var phero = GetPhero(from, to);
                        phero += (mConfig.QVAL / ant.Fitness);
                        SetPhero(from, to, phero);
                    }
                }

                for (int from = 0; from < cityCount - 1; from++)
                {
                    for (int to = from + 1; to < cityCount; to++)
                    {
                        SetPhero(from, to, GetPhero(from, to) * mConfig.RHO);
                    }
                }
            }

            /// <summary>
            /// 按概率选择下一座未被访问的城市
            /// </summary>
            public int SelectNextTarget(TspAnt ant)
            {
                int to;
                double sum = 0.0;
                int from = ant.TargetId;

                for (to = 0; to < mConfig.TargetCount; to++)
                {
                    if (from != to && !ant.Contains(to))
                        sum += mConfig.Probability.Compute(from, to, this);
                }

                do
                {
                    double p;
                    to++;

                    if (to >= mConfig.TargetCount)
                        to = 0;

                    if (!ant.Contains(to))
                    {
                        p = mConfig.Probability.Compute(from, to, this) / sum;
                        if (mRandom.NextDouble() < p)
                            break;
                    }
                } while (true);

                return to;
            }
        }

        /// <summary>
        /// 蚁群：管理蚂蚁与信息素
        /// </summary>
        class TspAntColony
        {
            readonly List<TspAnt> mAntList = new List<TspAnt>();
            readonly TspPheroTable mPheroTable;

            public TspPheroTable PheroTable { get { return mPheroTable; } }
            public List<TspAnt> Ants { get { return mAntList; } }
            public TspConfig Config { get { return mPheroTable.Config; } }

            public TspAntColony(TspConfig config)
            {
                mPheroTable = new TspPheroTable(config);
            }

            public void Initialize()
            {
                mAntList.Clear();

                var config = mPheroTable.Config;
                int to = 0;
                for (int ii = 0; ii < config.AntCount; ii++)
                {
                    if (to == config.TargetCount)
                        to = 0;

                    mAntList.Add(new TspAnt(to++));
                }

                mPheroTable.Initialize();
            }

            public void Reset()
            {
                var config = mPheroTable.Config;
                int to = 0;

                for (int ii = 0; ii < config.AntCount; ii++)
                {
                    if (to == config.TargetCount)
                        to = 0;

                    mAntList[ii].Reset(to++);
                }
            }

            public void UpdateTrails()
            {
                mPheroTable.UpdateTrails(Ants);
            }

            /// <summary>
            /// 所有蚂蚁各走一步，返回还在移动的蚂蚁数量
            /// </summary>
            public int Move()
            {
                var config = Config;
                int moving = 0;

                foreach (var ant in Ants)
                {
                    if (ant.Count < config.TargetCount)
                    {
                        var nextId = PheroTable.SelectNextTarget(ant);
                        ant.MoveTo(nextId);
                        moving++;
                    }
                }

                return moving;
            }
        }

        /// <summary>
        /// 距离启发因子：phero^Alpha * (1/distance)^Beta
        /// </summary>
        class TspCityProbability : ITspProbability
        {
            readonly TspCityMap mCityMap;

            public TspCityProbability(TspCityMap cityMap)
            {
                mCityMap = cityMap;
            }

            public double Compute(int from, int to, TspPheroTable table)
            {
                var config = table.Config;
                return Math.Pow(table.GetPhero(from, to), config.Alpha)
                    * Math.Pow(1.0 / mCityMap.GetDistance(from, to), config.Beta);
            }
        }

        /// <summary>
        /// 求解器：迭代状态机
        /// </summary>
        class TspSolver
        {
            protected TspConfig mConfig;
            protected TspCityMap mCityMap = new TspCityMap();
            protected TspAntColony mAntColony;

            protected double mFitness = 0;
            protected List<int> mBestPath = new List<int>();

            public TspSolver()
            {
                mConfig = new TspConfig(new TspCityProbability(mCityMap));
                mAntColony = new TspAntColony(mConfig);
            }

            public double Fitness { get { return mFitness; } }
            public List<int> Result { get { return mBestPath; } }
            public int SimulateCount { get; private set; } = 0;

            public TspCityMap CityMap { get { return mCityMap; } }
            public TspConfig Config { get { return mConfig; } }
            public TspPheroTable PheroTable { get { return mAntColony.PheroTable; } }

            public void Start()
            {
                SimulateCount = 0;
                mFitness = mCityMap.MaxDistance * mConfig.TargetCount;
                mAntColony.Initialize();
            }

            public void ComputeFitness()
            {
                var ants = mAntColony.Ants;
                var bestIndex = -1;
                for (int ii = 0; ii < ants.Count; ii++)
                {
                    var ant = ants[ii];
                    ant.Fitness = mCityMap.ComputeDistance(ant.Trajectory);
                    if (ant.Fitness < mFitness)
                    {
                        mFitness = ant.Fitness;
                        bestIndex = ii;
                    }
                }

                if (bestIndex > -1)
                {
                    mBestPath.Clear();
                    mBestPath.AddRange(ants[bestIndex].Trajectory);
                }
            }

            public EnumCode Next(int curTime)
            {
                EnumCode end = EnumCode.Continue;
                if (mAntColony.Move() == 0)
                {
                    ComputeFitness();
                    mAntColony.UpdateTrails();
                    end = EnumCode.NewGeneration;
                }

                return curTime < mConfig.MaxTime ? end : EnumCode.Finished;
            }

            /// <summary>
            /// 一次跑完所有迭代
            /// </summary>
            public void Compute()
            {
                Start();
                int curTime = 0;

                do
                {
                    var code = Next(curTime++);
                    if (code == EnumCode.Finished)
                        break;
                    else if (code == EnumCode.NewGeneration)
                        mAntColony.Reset();

                } while (true);

                SimulateCount = curTime;
            }
        }

        enum EnumCode
        {
            Continue,
            NewGeneration,
            Finished
        }

        // ---- 场景节点 ----
        PrimitiveSceneNode mMeshLines;
        SegmentsSceneNode mAllPathActors;
        SegmentsSceneNode mTrailsActors;
        ColorLookupTable mLookupTable = new ColorLookupTable();
        PaletteWidget mPaletteWidget = new PaletteWidget();

        TspSolver mSolver = new TspSolver();

        public override void Run(IRenderView render)
        {
            RandomCities(render);

            // 一次完整求解（等价于原示例的"开始" + "运行"两个按钮）
            mSolver.Compute();

            ShowTrails(render);
            ShowPath(render);

            var status = TextSceneNode.Create(
                string.Format("总路径：{0:0}   迭代次数：{1}", Math.Ceiling(mSolver.Fitness), mSolver.SimulateCount),
                ColorTable.Orange, 1.5f, true, 1.0f, 0.0f, 1.0f);
            status.SetTransform(Matrix4d.makeTranslation(2, -4, 3));
            status.SetPickable(false);
            render.ShowSceneNode(status);

            render.SetStandardView(EnumStandardView.Top, false);
            render.ZoomAll();
        }

        /// <summary>
        /// 随机生成城市，并画出全部候选路径
        /// </summary>
        void RandomCities(IRenderView view)
        {
            var cityMap = mSolver.CityMap;
            var cityList = cityMap.CityList;
            cityList.Clear();

            var targetCount = mSolver.Config.TargetCount;
            var count = (uint)(targetCount * targetCount);

            var random = new Random();
            for (int ii = 0; ii < targetCount; ii++)
            {
                int x = random.Next(0, cityMap.MaxDistance);
                int y = random.Next(0, cityMap.MaxDistance);
                var city = new TspCity(x, y);
                city.ShowCity(view);
                cityList.Add(city);
            }

            cityMap.Initialize();

            mAllPathActors = new SegmentsSceneNode(count, ColorTable.DarkGray, 1);
            mAllPathActors.SetPickable(false);

            var lineOffset = cityList.Count;
            for (int ii = 0; ii < cityList.Count; ii++)
            {
                var start = cityList[ii].GetPosition(-2);
                for (int jj = ii + 1; jj < cityList.Count; jj++)
                {
                    var idx = (uint)(ii * lineOffset + jj);
                    mAllPathActors.SetPositions(idx, start, cityList[jj].GetPosition(-2));
                }
            }

            mAllPathActors.Update();
            view.ShowSceneNode(mAllPathActors);

            // 信息素路径：初始全部收缩为原点
            mTrailsActors = new SegmentsSceneNode(count, ColorTable.LightGray, 1);
            mTrailsActors.SetPickable(false);

            for (int ii = 0; ii < targetCount; ii++)
            {
                for (int jj = ii + 1; jj < targetCount; jj++)
                {
                    mTrailsActors.SetPositions((uint)(ii * targetCount + jj), Vector3.Zero, Vector3.Zero);
                }
            }
            mTrailsActors.Update();
            mTrailsActors.SetVisible(false);
            view.ShowSceneNode(mTrailsActors);

            // 调色板图例
            mLookupTable.SetColorMap(ColorMapKeyword.Create(EnumSystemColorMap.Rainbow));
            mLookupTable.SetMinValue(-1);
            mLookupTable.SetMaxValue(4);

            mPaletteWidget.SetTitle("Phero");
            mPaletteWidget.SetTextColor(ColorTable.Black);
            mPaletteWidget.Update(mLookupTable);

            view.ShowSceneNode(mPaletteWidget);
        }

        /// <summary>
        /// 按信息素浓度着色显示已探明的路径
        /// </summary>
        void ShowTrails(IRenderView view)
        {
            var cityMap = mSolver.CityMap;
            var table = mSolver.PheroTable;
            var targetCount = mSolver.Config.TargetCount;

            for (int ii = 0; ii < targetCount; ii++)
            {
                var start = cityMap.GetCity(ii).GetPosition(-1);
                for (int jj = ii + 1; jj < targetCount; jj++)
                {
                    var val = table.GetPhero(ii, jj);
                    if (val < 0.1)
                        continue;

                    var color = mLookupTable.GetColor(val);
                    var idx = (uint)(ii * targetCount + jj);
                    mTrailsActors.SetColors(idx, color, color);
                    mTrailsActors.SetPositions(idx, start, cityMap.GetCity(jj).GetPosition(-2));
                }
            }

            mTrailsActors.Update();
            mTrailsActors.SetVisible(true);
            view.RequestDraw(EnumUpdateFlags.Scene);
        }

        /// <summary>
        /// 用红色折线显示最优路径
        /// </summary>
        void ShowPath(IRenderView view)
        {
            var cityMap = mSolver.CityMap;
            var bestPath = mSolver.Result;
            if (bestPath.Count < 2)
                return;

            var points = new Float32Buffer(0);
            points.Reserve((uint)bestPath.Count * 3);
            for (int ii = 0; ii < bestPath.Count; ii++)
            {
                var city = cityMap.GetCity(bestPath[ii]);
                points.Append((float)city.X, (float)city.Y, 0.0f);
            }

            var lines = GeometryBuilder.CreateLines(new Float32Array(points), null, null);
            lines.SetPrimitiveType(EnumPrimitiveType.LINE_STRIP);

            if (mMeshLines == null)
            {
                var material = LineMaterial.Create("path.line");
                material.SetColor(ColorTable.Red);
                material.SetLineWidth(3);
                mMeshLines = PrimitiveSceneNode.Create(lines, material);
                mMeshLines.SetPickable(false);
                view.ShowSceneNode(mMeshLines);
            }
            else
            {
                mMeshLines.SetGeometry(lines);
                mMeshLines.RequestUpdate();
            }
        }
    }
}

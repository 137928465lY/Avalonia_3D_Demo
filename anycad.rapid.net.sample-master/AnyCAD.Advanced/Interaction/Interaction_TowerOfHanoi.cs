using AnyCAD.Foundation;
using System;
using System.Collections.Generic;

namespace AnyCAD.Demo.Graphics
{
    /// <summary>
    /// 汉诺塔圆盘
    /// </summary>
    class HanoiPan
    {
        public ulong Id { get; set; } = 0;
        public double Height { get; set; } = 5;
        public double Size { get; set; } = 4;

        SceneNode mNode;
        Vector3 mBase = new Vector3(0, 0, 0);
        bool mLifted;

        public void Show(IRenderView view, MaterialInstance material)
        {
            mNode = BrepSceneNode.Create(CreateShape(), material, null);
            mNode.SetChildrenPickable(false);
            mNode.SetPickable(false);
            mNode.SetUserId(Id);
            view.ShowSceneNode(mNode);
        }

        /// <summary>
        /// 只有每根立柱最上方的圆盘可被拾取
        /// </summary>
        public void SetMoveable(bool moveable)
        {
            mNode?.SetPickable(moveable);
        }

        public void SetPosition(Vector3 position)
        {
            mBase = position;
            ApplyTransform();
        }

        /// <summary>
        /// 拿在手上时抬高，给出视觉反馈
        /// </summary>
        public void SetLifted(bool lifted)
        {
            mLifted = lifted;
            ApplyTransform();
        }

        void ApplyTransform()
        {
            if (mNode == null)
                return;

            float z = mBase.z + (mLifted ? 20.0f : 0.0f);
            mNode.SetTransform(Matrix4d.makeTranslation(mBase.x, mBase.y, z));
        }

        TopoShape CreateShape()
        {
            // 内孔半径固定 3（正好套住立柱），外半径 = Size
            return ShapeBuilder.MakeTube(new GPnt(), new GDir(0, 0, 1), 3, Size - 3, Height);
        }
    }

    /// <summary>
    /// 汉诺塔立柱
    /// </summary>
    class HanoiTower
    {
        readonly Stack<HanoiPan> mPans = new Stack<HanoiPan>();

        public ulong Id { get; set; } = 0;
        public float Position { get; set; } = 0;
        public float Height { get; set; } = 80;

        public void Show(IRenderView view, MaterialInstance material)
        {
            var arrow = ArrowWidget.Create(5.0f, Height, material);
            arrow.SetChildrenPickable(false);
            arrow.SetPosition(new Vector3(Position, 0, 0));
            arrow.SetUserId(Id);
            arrow.Update();

            view.ShowSceneNode(arrow);
        }

        public bool IsEmpty()
        {
            return mPans.Count == 0;
        }

        public HanoiPan GetTop()
        {
            return mPans.Count > 0 ? mPans.Peek() : null;
        }

        public void Add(HanoiPan pan)
        {
            if (mPans.Count > 0)
                mPans.Peek().SetMoveable(false);

            pan.SetLifted(false);
            pan.SetMoveable(true);
            pan.SetPosition(new Vector3(Position, 0, mPans.Count * (float)pan.Height));

            mPans.Push(pan);
        }

        /// <summary>
        /// 取走最上方的圆盘
        /// </summary>
        public HanoiPan Pop()
        {
            if (mPans.Count < 1)
                return null;

            var pan = mPans.Pop();
            if (mPans.Count > 0)
                mPans.Peek().SetMoveable(true);

            return pan;
        }

        public bool CanAdd(HanoiPan pan)
        {
            if (mPans.Count == 0)
                return true;

            return pan.Id < mPans.Peek().Id;
        }
    }

    /// <summary>
    /// 汉诺塔示例
    /// 迁移自 Featured.TowerOfHanoi（WPF 版），去掉所有 GUI 依赖：
    /// - 立柱 / 圆盘几何、拾取规则、移动判定与原示例一致
    /// - 弹窗（MessageBox）改为场景内的文字提示
    /// - 交互统一走 OnSelectionChanged：点圆盘拿起，点立柱放下
    /// 规则：一次只能移动每根立柱最上方的圆盘，且大盘不能压在小盘上，全部移到另一根立柱即获胜
    /// </summary>
    class Interaction_TowerOfHanoi : TestCase
    {
        public Interaction_TowerOfHanoi()
        {
            Title = "汉诺塔";
            Description = "点击立柱拾取最上方圆盘，点击另一根立柱放下；大盘不能压在小盘上，把整摞圆盘换到另一根立柱即获胜";
            Tags.Add("交互");
            Tags.Add("拾取");
            Tags.Add("动画");
            Level = 2;
        }

        readonly List<HanoiTower> mTowers = new List<HanoiTower>();
        ulong mHeldPanId = 0;
        bool mAllowPickNullBackup = true;
        TextSceneNode mStatus;
        bool mFinished;

        public override void Run(IRenderView render)
        {
            // 1. 三根立柱
            var towerMaterials = new[]
            {
                MeshPhongMaterial.Create("hanoi.tower.red"),
                MeshPhongMaterial.Create("hanoi.tower.green"),
                MeshPhongMaterial.Create("hanoi.tower.yellow"),
            };
            towerMaterials[0].SetColor(ColorTable.Red);
            towerMaterials[1].SetColor(ColorTable.Green);
            towerMaterials[2].SetColor(ColorTable.Yellow);

            var positions = new[] { -100.0f, 0.0f, 100.0f };
            for (int i = 0; i < 3; ++i)
            {
                var tower = new HanoiTower { Id = (ulong)(i + 1), Position = positions[i] };
                tower.Show(render, towerMaterials[i]);
                mTowers.Add(tower);
            }

            // 2. 五个圆盘：由大到小，依次对应 黄金/翡翠/紫铜/珍珠/白银
            var panMaterials = new[]
            {
                MakeMetal("hanoi.pan.silver", new Vector3(0.80f, 0.80f, 0.82f)),  // 白银
                MakeMetal("hanoi.pan.pearl", new Vector3(0.92f, 0.90f, 0.84f)),   // 珍珠
                MakeMetal("hanoi.pan.copper", new Vector3(0.72f, 0.45f, 0.20f)),  // 紫铜
                MakeMetal("hanoi.pan.emerald", new Vector3(0.13f, 0.55f, 0.35f)), // 翡翠
                MakeMetal("hanoi.pan.gold", new Vector3(0.85f, 0.65f, 0.13f)),    // 黄金
            };

            int count = panMaterials.Length;
            for (int ii = count; ii > 0; --ii)
            {
                var pan = new HanoiPan { Id = (ulong)ii + 100, Size = ii * 5 + 4 };
                pan.Show(render, panMaterials[ii - 1]);
                mTowers[0].Add(pan);
            }

            // 3. 不允许"点空白"产生空选择，与原示例的 BeforeSelectCallback 等价
            mAllowPickNullBackup = render.SelectionManager.GetAllowPickNull();
            render.SelectionManager.SetAllowPickNull(false);

            SetStatus(render, "点击圆盘拿起，再点击立柱放下", ColorTable.Black);

            render.SetStandardView(EnumStandardView.DefaultView, false);
            render.ZoomAll();
        }

        public override void OnSelectionChanged(IRenderView render, PickedResult result)
        {
            if (mFinished || result.IsEmpty())
                return;

            var id = result.GetItem().GetUserId();
            if (id < 100)
                MovePanToTower(render, id);
            else
                SetActivePan(render, id);
        }

        public override void Exit(IRenderView render)
        {
            render.SelectionManager.Clear();
            render.SelectionManager.SetAllowPickNull(mAllowPickNullBackup);
            base.Exit(render);
        }

        /// <summary>
        /// 拿起圆盘
        /// </summary>
        void SetActivePan(IRenderView render, ulong panId)
        {
            // 放下之前拿着的圆盘
            if (mHeldPanId != 0 && mHeldPanId != panId)
                FindPan(mHeldPanId)?.Item1.SetLifted(false);

            mHeldPanId = panId;

            var found = FindPan(panId);
            if (found != null)
                found.Item1.SetLifted(true);

            render.SelectionManager.Clear();
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        /// <summary>
        /// 把拿着的圆盘放到目标立柱
        /// </summary>
        void MovePanToTower(IRenderView render, ulong towerId)
        {
            var targetTower = FindTower(towerId);
            if (targetTower == null)
                return;

            if (mHeldPanId == 0)
            {
                SetStatus(render, "请先点击一个圆盘", ColorTable.Orange);
                render.RequestDraw(EnumUpdateFlags.Scene);
                return;
            }

            var found = FindPan(mHeldPanId);
            if (found == null)
                return;

            var pan = found.Item1;
            var sourceTower = found.Item2;
            if (sourceTower == targetTower)
                return;

            if (!targetTower.CanAdd(pan))
            {
                SetStatus(render, "大的圆盘不能放在小的上面哟！", ColorTable.Red);
                render.SelectionManager.Clear();
                render.RequestDraw(EnumUpdateFlags.Scene);
                return;
            }

            sourceTower.Pop();
            targetTower.Add(pan);
            mHeldPanId = 0;

            if (mTowers[0].IsEmpty() && (mTowers[1].IsEmpty() || mTowers[2].IsEmpty()))
            {
                mFinished = true;
                SetStatus(render, "你太棒啦！", ColorTable.Green);
            }
            else
            {
                SetStatus(render, "点击圆盘拿起，再点击立柱放下", ColorTable.Black);
            }

            render.SelectionManager.Clear();
            render.RequestDraw(EnumUpdateFlags.Scene);
        }

        Tuple<HanoiPan, HanoiTower> FindPan(ulong panId)
        {
            foreach (var tower in mTowers)
            {
                var top = tower.GetTop();
                if (top != null && top.Id == panId)
                    return new Tuple<HanoiPan, HanoiTower>(top, tower);
            }

            return null;
        }

        HanoiTower FindTower(ulong towerId)
        {
            foreach (var tower in mTowers)
            {
                if (tower.Id == towerId)
                    return tower;
            }

            return null;
        }

        /// <summary>
        /// 更新场景内的文字提示（替代原示例的 MessageBox）
        /// </summary>
        void SetStatus(IRenderView render, string text, Vector3 color)
        {
            if (mStatus != null)
                render.RemoveSceneNode(mStatus.GetUuid());

            mStatus = TextSceneNode.Create(text, color, 4.0f, true, 1.0f, 180.0f, 1.2f);
            mStatus.SetTransform(Matrix4d.makeTranslation(0, 0, 105));
            mStatus.SetPickable(false);
            render.ShowSceneNode(mStatus);
        }

        static MaterialInstance MakeMetal(string name, Vector3 color)
        {
            var material = MeshStandardMaterial.Create(name);
            material.SetColor(color);
            return material;
        }
    }
}

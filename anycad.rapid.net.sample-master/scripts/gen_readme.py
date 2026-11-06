# -*- coding: utf-8 -*-
"""根据 case_map.txt 与 showcase 目录，生成 README 的“程序示例”章节并原位替换。"""
import os
import re
import sys

REPO = r"D:\OpenSource\anycad.rapid.net.sample示例"
CASE_MAP = os.path.join(REPO, "case_map.txt")
SHOWCASE = os.path.join(REPO, "showcase")
README = os.path.join(REPO, "README.md")
SKIP_FILE = os.path.join(REPO, "scripts", "capture_skip.txt")


def load_skip_keys():
    """读取 capture_skip.txt，返回归一化键集合（小写、/ 转 .）。"""
    keys = set()
    if not os.path.exists(SKIP_FILE):
        return keys
    with open(SKIP_FILE, "r", encoding="utf-8-sig") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            keys.add(line.replace("\\", "/").replace("/", ".").lower())
    return keys

GROUP_UI = {
    "Graphics": "显示",
    "Geometry": "造型",
    "Analysis": "分析",
    "Interaction": "交互",
    "Simulation": "仿真",
    "Interop": "Interop",
    "Welcome": "Welcome",
}

MODULE_NAMES = {
    "Basic": ("基础功能", "AnyCAD.Basic"),
    "Adv": ("运动仿真与高级功能", "AnyCAD.Advanced"),
    "QS": ("高级造型", "AnyCAD.QuickSolid"),
}

MODULE_NOTES = {
    "Basic": "几何造型、分析、显示、交互等基础功能示例",
    "Adv": "运动仿真（碰撞、动力学、机器人）、CAE、点云、交互测量等高级功能示例",
    "QS": "快速建模与高级造型（放样、展开、船体、管件等）示例",
}


def rel_cs(path):
    p = path.replace("\\", "/")
    if p.startswith("AnyCAD."):
        return p
    prefix = REPO.replace("\\", "/") + "/"
    return p[len(prefix):] if p.startswith(prefix) else p


def main():
    rows = []
    with open(CASE_MAP, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            parts = line.split("|")
            if len(parts) < 5:
                continue
            module, group, name, fullname, cs = parts[0], parts[1], parts[2], parts[3], parts[4]
            rows.append(dict(module=module, group=group, name=name, cs=cs))

    skip_keys = load_skip_keys()
    before = len(rows)
    rows = [r for r in rows if f"{r['group']}.{r['name']}".lower() not in skip_keys]
    excluded = before - len(rows)

    # 校验截图存在
    missing = []
    for r in rows:
        img = os.path.join(SHOWCASE, f"{r['group'].lower()}.{r['name'].lower()}.png")
        r["img"] = f"showcase/{r['group'].lower()}.{r['name'].lower()}.png"
        if not os.path.exists(img):
            missing.append(r["img"])

    # 分模块统计
    modules = ["Basic", "Adv", "QS"]
    counts = {m: sum(1 for r in rows if r["module"] == m) for m in modules}
    total = len(rows)

    lines = []
    lines.append("## 2 程序示例\n")
    lines.append(f"本仓库共包含 **{total}** 个可运行示例，全部通过以下 App 的目录树切换查看：AnyCAD.WinForms.App / AnyCAD.WPF.App / AnyCAD.AvaloniaApp。")
    lines.append("下表列出了全部可正常运行并截图成功的示例：每个示例均给出对应的源码文件（相对链接）与自动截图生成的效果图。")
    lines.append(f"另有 **{excluded}** 个示例因原生崩溃或托管异常无法截图，未列入下表（清单见 [scripts/capture_skip.txt](scripts/capture_skip.txt)）。效果图存放于 [showcase](showcase) 目录，")
    lines.append("由 [scripts](scripts/README.md) 中的工具自动运行每个示例并截图生成（三维模型自动采用等轴测视角）。\n")
    lines.append("| 模块 | 工程 | 数量 | 说明 |")
    lines.append("| --- | --- | --- | --- |")
    for m in modules:
        nm, proj = MODULE_NAMES[m]
        lines.append(f"| {nm} | {proj} | {counts[m]} | {MODULE_NOTES[m]} |")
    lines.append("")

    # 每模块一个子章节，按分组（UI 名）分组展示
    for m in modules:
        nm, proj = MODULE_NAMES[m]
        lines.append(f"### 2.{modules.index(m)+1} {nm}（{proj}，共 {counts[m]} 个）\n")
        mrows = [r for r in rows if r["module"] == m]
        groups = []
        for r in mrows:
            if r["group"] not in groups:
                groups.append(r["group"])
        for g in groups:
            gname = GROUP_UI.get(g, g)
            grows = [r for r in mrows if r["group"] == g]
            lines.append(f"**{gname}（{len(grows)}）**\n")
            lines.append("| 示例 | 源码 | 效果图 |")
            lines.append("| --- | --- | --- |")
            for r in sorted(grows, key=lambda x: x["name"].lower()):
                cs = rel_cs(r["cs"])
                imgcell = (f"<img src=\"{r['img']}\" width=\"240\" alt=\"{r['name']}\"/>"
                           if os.path.exists(os.path.join(REPO, r["img"]))
                           else "⚠️ 截图失败")
                lines.append(
                    f"| {r['name']} | [{os.path.basename(cs)}]({cs}) | {imgcell} |"
                )
            lines.append("")

    if missing:
        lines.append(f"> ⚠️ 以下截图生成失败（共 {len(missing)} 个），待补拍：\n")
        for img in missing:
            lines.append(f"> - {img}")
        lines.append("")

    generated = "\n".join(lines)

    with open(README, "r", encoding="utf-8") as f:
        content = f.read()

    # 替换 "## 2 程序示例" 到 "## 3" 之前的内容
    start_m = re.search(r"^## 2 程序示例.*$", content, re.M)
    end_m = re.search(r"^## 3 ", content, re.M)
    if not start_m:
        print("ERROR: cannot find '## 2 程序示例' in README", file=sys.stderr)
        sys.exit(1)
    end_idx = end_m.start() if end_m else len(content)
    new_content = content[:start_m.start()] + generated + "\n" + content[end_idx:]

    with open(README, "w", encoding="utf-8") as f:
        f.write(new_content)

    print(f"OK: {total} rows (excluded {excluded} by capture_skip.txt), missing images: {len(missing)}")
    for x in missing:
        print("  MISSING: " + x)


if __name__ == "__main__":
    main()

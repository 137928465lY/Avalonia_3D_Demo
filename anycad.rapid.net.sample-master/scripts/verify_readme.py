# -*- coding: utf-8 -*-
"""校验 README 第 2 节：源码相对链接与效果图引用是否真实存在。"""
import os
import re

repo = r"D:\OpenSource\anycad.rapid.net.sample示例"
readme_path = os.path.join(repo, "README.md")
readme = open(readme_path, encoding="utf-8").read()
sec2 = readme.split("## 2 程序示例")[1].split("## 3 ")[0]

links = re.findall(r"\[[^\]]+\]\(([^)]+\.cs)\)", sec2)
imgs = re.findall(r'src="([^"]+\.png)"', sec2)
fail_markers = sec2.count("⚠️ 截图失败")

bad_l = [l for l in links if not os.path.exists(os.path.join(repo, l.replace("/", os.sep)))]
bad_i = [i for i in imgs if not os.path.exists(os.path.join(repo, i.replace("/", os.sep)))]

cases = set(re.findall(r"^\| ([A-Za-z0-9_]+) \| \[", sec2, re.M))
print("rows(links):", len(links), "bad:", len(bad_l))
for x in bad_l:
    print("  BAD LINK", x)
print("img refs:", len(imgs), "bad:", len(bad_i))
for x in bad_i:
    print("  BAD IMG", x)
print("unique case names in tables:", len(cases))
print("fail markers:", fail_markers)

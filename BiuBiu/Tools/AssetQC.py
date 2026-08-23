# -*- coding: utf-8 -*-
"""
AssetQC.py —— 素材需求文档第 8 章自动化 QC 脚本
检查项：#1 文件尺寸 / #2 透明背景 / #4 色板数量 / #5 拆件完整性 / #9 命名 / #10 落位与中间产物
附加：拼合预览图包围盒粗查（#6 程序粗查）、4x 目检拼图生成（#3 辅助）
用法：python Tools/AssetQC.py <批次号：batch1 / batch2 ...>
输出：逐项 PASS/FAIL 明细 + 退出码（0=全过，1=有失败项）
"""
import sys
import os
import re
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # 仓库根目录
ART = os.path.join(ROOT, "Assets", "Art")

# ---------------- 规格表：相对路径 -> (期望宽, 期望高, 类别) ----------------
# 类别 strict = 硬边像素（严格半透明检查）；glow = 发光/渐变特效（按设计允许半透明）；solid = 满幅实心
PARTS = ["Head", "Torso", "ArmL_Upper", "ArmR_Upper", "ArmL_Fore", "ArmR_Fore",
         "LegL_Thigh", "LegR_Thigh", "LegL_Shin", "LegR_Shin",
         "Head_Hurt", "Head_Dead"]
# v1.8：丧尸不含武器插槽件；角色专属附加件（画布有别于通用 32×48）
CHAR_EXTRA = {
    "ZombieNormal": [],
    "ZombieDoor": [("Prop_DoorBoard", 48, 48)],
    "ZombieThrower": [("Prop_WoodBundle", 32, 48)],
}

BATCH1 = {}
# 三种丧尸拆件（1.2~1.4，1.0 章命名表 12 件 + 角色专属附加件）
for char, extras in CHAR_EXTRA.items():
    for p in PARTS:
        BATCH1[f"Spine/{char}/Parts/{p}.png"] = (32, 48, "strict")
    for (p, w, h) in extras:
        BATCH1[f"Spine/{char}/Parts/{p}.png"] = (w, h, "strict")
# 掉落物（第 3 章）
BATCH1["Sprites/Items/ExpBlock_0.png"] = (16, 16, "strict")
BATCH1["Sprites/Items/ExpBlock_1.png"] = (16, 16, "strict")
BATCH1["Sprites/Items/HealthPotion.png"] = (16, 16, "strict")
BATCH1["Sprites/Items/WoodLog.png"] = (24, 8, "strict")
# 武器 icon（第 3/5 章共用）
BATCH1["UI/WeaponIcon_Stick.png"] = (16, 16, "strict")
BATCH1["UI/WeaponIcon_Fish.png"] = (16, 16, "strict")
# 战斗特效（第 4 章，批次 1 范围）
for i in range(6):
    BATCH1[f"VFX/Slash/Slash_{i}.png"] = (64, 64, "glow")
for i in range(3):
    BATCH1[f"VFX/Laser/Laser_{i}.png"] = (32, 32, "glow")
for i in range(4):
    BATCH1[f"VFX/LaserHit/LaserHit_{i}.png"] = (24, 24, "glow")
BATCH1["VFX/GunFlash/GunFlash_0.png"] = (48, 64, "gradexempt")  # 渐变受光层，v1.7 起 #4 豁免
for i in range(4):
    BATCH1[f"VFX/HitParticles/HitParticles_{i}.png"] = (16, 16, "glow")
for i in range(4):
    BATCH1[f"VFX/DeathParticles/DeathParticles_{i}.png"] = (16, 16, "glow")
for i in range(4):
    BATCH1[f"VFX/Blood/Blood_{i}.png"] = (32, 32, "strict")  # 血迹为不透明贴花
for i in range(3):
    BATCH1[f"VFX/Dust/Dust_{i}.png"] = (24, 24, "glow")
# 批次 3 内容提前交付（不计缺项，顺带 QC）
for i in range(3):
    BATCH1[f"VFX/BossProjectile/BossProjectile_{i}.png"] = (16, 16, "glow")
for i in range(2):
    BATCH1[f"VFX/SpellWarning/SpellWarning_{i}.png"] = (32, 32, "glow")
BATCH1["VFX/MagnetTrail/MagnetTrail_0.png"] = (8, 16, "glow")

# 拼合预览图（TuanjieAI 交付的站立拼合图，#6 程序粗查用）
PREVIEWS = {
    "Spine/ZombieNormal/ZombieNormal.png": (40, 48, 24, 32),   # 高 40~48 / 宽 24~32
    "Spine/ZombieDoor/ZombieDoor.png": (44, 52, 40, 48),       # 含门板：高 44~52 / 宽 40~48
    "Spine/ZombieThrower/ZombieThrower.png": (44, 52, 24, 32), # 高 44~52 / 宽 24~32
}

NAME_RE = re.compile(r"^[A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*\.png$")  # #9 PascalCase/下划线
FORBIDDEN_RE = re.compile(r"TJGenerators|Layer\d+", re.IGNORECASE)  # #10 中间产物

results = []  # (编号, 资产, 结果, 明细)


def add(qc_id, asset, ok, detail):
    results.append((qc_id, asset, "PASS" if ok else "FAIL", detail))


def check_batch(spec):
    """逐文件跑 #1/#2/#4/#9/#10"""
    for rel, (w, h, cls) in sorted(spec.items()):
        path = os.path.join(ART, rel)
        if not os.path.exists(path):
            add("#12", rel, False, "文件缺失")
            continue
        img = Image.open(path)
        # #1 文件尺寸
        add("#1", rel, img.size == (w, h), f"实际 {img.size[0]}x{img.size[1]}，要求 {w}x{h}")
        # #9 命名规范
        add("#9", rel, bool(NAME_RE.match(os.path.basename(rel))), "命名检查")
        # #2 透明背景（含 alpha 通道；strict 类额外查羽化半透明）
        has_alpha = img.mode in ("RGBA", "LA", "PA") or "transparency" in img.info
        if not has_alpha:
            add("#2", rel, False, f"无 Alpha 通道（mode={img.mode}）")
        elif cls == "strict":
            a = img.getchannel("A") if img.mode == "RGBA" else None
            if a is None:
                add("#2", rel, False, f"mode={img.mode} 非 RGBA")
            else:
                data = list(a.getdata())
                semi = sum(1 for v in data if 0 < v < 255)
                total = sum(1 for v in data if v > 0)
                pct = semi / total * 100 if total else 0
                # 硬边像素画允许极少量半透明像素（≤2%），羽化抠图会大面积出现
                add("#2", rel, pct <= 2.0, f"半透明像素 {semi}/{total}（{pct:.1f}%），阈值 2%")
        else:
            add("#2", rel, True, "发光/渐变类，允许设计性半透明")
        # #4 色板数量（单张去重颜色 ≤48；gradexempt=设计性渐变受光层，v1.7 豁免）
        if cls == "gradexempt":
            add("#4", rel, True, "渐变受光层豁免（v1.7）")
        else:
            if img.mode == "RGBA":
                colors = {c for c in img.getdata() if c[3] > 0}
            else:
                colors = set(img.getdata())
            add("#4", rel, len(colors) <= 48, f"去重颜色 {len(colors)}，上限 48")
        # #3 程序代理：平滑渐变步进检测（strict 类；真限色像素画应≈0）
        if cls == "strict" and img.mode == "RGBA":
            px = img.load()
            smooth = pairs = 0
            for y in range(img.height):
                for x in range(img.width - 1):
                    a, b = px[x, y], px[x + 1, y]
                    if a[3] > 0 and b[3] > 0:
                        pairs += 1
                        d = sum((p - q) ** 2 for p, q in zip(a[:3], b[:3])) ** 0.5
                        if 0.5 < d <= 12:
                            smooth += 1
            pct = smooth / pairs * 100 if pairs else 0
            add("#3", rel, pct <= 5.0, f"渐变步进 {pct:.1f}%，阈值 5%（批次0定调 Sunzi=0%）")


def check_char_palette_union(chars):
    """#4 同角色全拆件合计颜色 ≤48（v1.8：不含武器复用件）"""
    for char in chars:
        union = set()
        d = os.path.join(ART, "Spine", char, "Parts")
        for f in os.listdir(d):
            if not f.endswith(".png") or f.startswith("Prop_Weapon"):
                continue
            img = Image.open(os.path.join(d, f)).convert("RGBA")
            union |= {c for c in img.getdata() if c[3] > 0}
        add("#4", f"Spine/{char}/Parts（合计）", len(union) <= 48, f"全拆件合计 {len(union)} 色（不含武器件），上限 48")


def check_parts_completeness(chars):
    """#5 拆件完整性：与 1.0 章命名表 + 角色专属附加件逐一比对"""
    for char in chars:
        d = os.path.join(ART, "Spine", char, "Parts")
        actual = {f[:-4] for f in os.listdir(d) if f.endswith(".png")} if os.path.isdir(d) else set()
        expected = set(PARTS) | {p for (p, _, _) in CHAR_EXTRA.get(char, [])}
        missing = expected - actual
        extra = actual - expected
        ok = not missing
        detail = f"{len(actual & expected)}/{len(expected)} 齐"
        if missing:
            detail += f"；缺 {sorted(missing)}"
        if extra:
            detail += f"；多出 {sorted(extra)}"
        add("#5", f"Spine/{char}/Parts", ok, detail)


def check_forbidden():
    """#10 中间产物混入扫描（全 Art 目录）"""
    hits = []
    for dirpath, dirnames, filenames in os.walk(ART):
        if "Spine" + os.sep + "Runtime" in dirpath or "Spine Examples" in dirpath:
            continue  # 第三方 Spine 运行时不算
        for n in dirnames + filenames:
            if FORBIDDEN_RE.search(n):
                hits.append(os.path.relpath(os.path.join(dirpath, n), ART))
    add("#10", "Art/ 全目录", not hits, f"中间产物混入：{hits if hits else '无'}")


def check_previews():
    """#6 程序粗查：拼合图 alpha 包围盒 = 站立尺寸 ±4px"""
    for rel, (hmin, hmax, wmin, wmax) in PREVIEWS.items():
        path = os.path.join(ART, rel)
        if not os.path.exists(path):
            add("#6", rel, False, "拼合预览图缺失")
            continue
        img = Image.open(path).convert("RGBA")
        bbox = img.getbbox()  # 非 0 像素包围盒
        if bbox is None:
            add("#6", rel, False, "全透明")
            continue
        bw, bh = bbox[2] - bbox[0], bbox[3] - bbox[1]
        ok = (hmin - 4 <= bh <= hmax + 4) and (wmin - 4 <= bw <= wmax + 4)
        add("#6", rel, ok, f"包围盒 {bw}x{bh}，要求 宽{wmin}~{wmax}/高{hmin}~{hmax}（±4）")


def make_preview_sheet(spec, out_name, scale=4):
    """#3 辅助：生成放大目检拼图（最近邻放大）"""
    # 选代表性资产：每角色拼合图 + 全部小件首帧
    picks = ["Spine/ZombieNormal/ZombieNormal.png", "Spine/ZombieDoor/ZombieDoor.png",
             "Spine/ZombieThrower/ZombieThrower.png",
             "Spine/ZombieNormal/Parts/Head.png", "Spine/ZombieDoor/Parts/Head.png",
             "Spine/ZombieThrower/Parts/Head.png",
             "Sprites/Items/ExpBlock_0.png", "Sprites/Items/ExpBlock_1.png",
             "Sprites/Items/HealthPotion.png", "Sprites/Items/WoodLog.png",
             "UI/WeaponIcon_Stick.png", "UI/WeaponIcon_Fish.png",
             "VFX/Slash/Slash_0.png", "VFX/Slash/Slash_3.png",
             "VFX/Laser/Laser_0.png", "VFX/LaserHit/LaserHit_0.png",
             "VFX/GunFlash/GunFlash_0.png", "VFX/HitParticles/HitParticles_0.png",
             "VFX/DeathParticles/DeathParticles_0.png", "VFX/Blood/Blood_0.png",
             "VFX/Dust/Dust_0.png", "VFX/BossProjectile/BossProjectile_0.png",
             "VFX/SpellWarning/SpellWarning_0.png", "VFX/MagnetTrail/MagnetTrail_0.png"]
    cell, label_h, cols = 140, 18, 6
    rows = (len(picks) + cols - 1) // cols
    sheet = Image.new("RGB", (cell * cols, (cell + label_h) * rows), (40, 40, 48))
    from PIL import ImageDraw
    draw = ImageDraw.Draw(sheet)
    for idx, rel in enumerate(picks):
        path = os.path.join(ART, rel)
        r, c = divmod(idx, cols)
        x0, y0 = c * cell, r * (cell + label_h)
        if os.path.exists(path):
            img = Image.open(path).convert("RGBA")
            s = min((cell - 8) / img.width, (cell - 8) / img.height)
            img = img.resize((max(1, int(img.width * s)), max(1, int(img.height * s))), Image.NEAREST)
            sheet.paste(img, (x0 + (cell - img.width) // 2, y0 + (cell - img.height) // 2), img)
        draw.text((x0 + 4, y0 + cell + 2), os.path.basename(rel)[:22], fill=(220, 220, 220))
    out = os.path.join(ROOT, out_name)
    sheet.save(out)
    print(f"\n[目检图] 已生成 {out_name}（含 {len(picks)} 项代表帧，用于 #3 像素硬边目检）")


def main():
    batch = sys.argv[1] if len(sys.argv) > 1 else "batch1"
    spec = {"batch1": BATCH1}.get(batch)
    if spec is None:
        print(f"未知批次 {batch}")
        return 1
    chars = ["ZombieNormal", "ZombieDoor", "ZombieThrower"]
    check_batch(spec)
    check_parts_completeness(chars)
    check_char_palette_union(chars)
    check_forbidden()
    check_previews()
    make_preview_sheet(spec, f"qc_preview_{batch}.png")

    fails = [r for r in results if r[2] == "FAIL"]
    print(f"\n===== QC 结果（{batch}，共 {len(results)} 项检查）=====")
    cur = None
    for qc_id, asset, status, detail in results:
        if qc_id != cur:
            print(f"\n[{qc_id}]")
            cur = qc_id
        mark = "PASS" if status == "PASS" else "FAIL"
        print(f"  {mark}  {asset}  —— {detail}")
    print(f"\n===== 汇总：{len(results) - len(fails)} PASS / {len(fails)} FAIL =====")
    if fails:
        print("失败项：")
        for qc_id, asset, _, detail in fails:
            print(f"  {qc_id} {asset}: {detail}")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())

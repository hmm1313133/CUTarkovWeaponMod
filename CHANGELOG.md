# Changelog - CUTarkovWeaponMod

All notable changes to this project will be documented in this file.

## [1.1.0] - 2026-07-22

### 新增

- **夜视仪系统**（3件）
  - GPNVG-18 四目全景夜视仪（`gpnvg18`）- 全景视野，仅兼容 FAST MT / Galvion Calman
  - PVS-14 单目夜视仪（`pvs14`）- 兼容 FAST MT / Galvion Calman / 6B47
  - PVS-31A 双目夜视仪（`pvs31a`）- 仅兼容 FAST MT / Galvion Calman
  - 需先佩戴兼容头盔，装备后按 N 键开关，附带开关音效
  - 暗角遮挡修复（sortingOrder=-1），低温屏幕边缘提示不再被遮挡
  - 噪声纹理使用独立工作副本，避免原地修改损坏原始纹理

- **VSS "绞丝机" 特种狙击步枪**（`vss`）
  - 9x39 口径全自动消音狙击步枪，整体式消音器（响度 0.32）
  - 30 发弹匣，动物伤害 105，结构伤害 88，后坐力 2.8
  - 专用 VSS 弹匣（`vss_mag`，30 发，配方同 AKM 弹匣）
  - 专用 9x39mm SP-5 特种弹药（`939sp5`，亚音速钢芯弹）
  - 枪械/弹匣/子弹不在世界生成，仅可通过合成或控制台获取

- **武器维修套件**（`weaponrepairkit`）
  - 可使用 4 次，手持枪械右键使用即可将耐久回满
  - 重量 8.5u（随耐久消耗线性减少），价值 52
  - 世界生成：物资箱 7%、空投胶囊 12%、尸体 3%、崩溃舱 1%

- **夜视仪世界生成**
  - 物资箱 10%、空投胶囊 10%、空投舱 8%
  - PVS-14 权重 70% / GPNVG-18 权重 30%

- **夜视仪开关音效**（`trigger.wav`）

### 变更

- **枪械耐久消耗调整**：所有 14 把枪械耐久消耗提高 0.2
  - AKM: 0.10→0.30、M4A1: 0.12→0.32、UMP45: 0.13→0.33、RPD: 0.16→0.36
  - USP: 0.20→0.40、P90: 0.08→0.28、VSS: 0.30→0.50、MP153: 0.30→0.50
  - SKS: 0.40→0.60、DVL10: 0.40→0.60、Glock17: 0.50→0.70、AXMC: 0.50→0.70
  - MP133: 0.60→0.80、Deagle: 0.70→0.90

- **SFMP 背包容量调整**：6LBT-2670 容量从 14u 调整为 10u

- **README 更新**：新增夜视仪、VSS、维修套件章节，更新功能概览和世界生成表

### 修复

- **贴图文件名修复**（12项）
  - `2DayAssault.png` → `mysteryranch2day.png`（两日突击背包显示为瘀伤治疗包）
  - `Day Pack.png` → `daypack.png`、`Attack 2.png` → `ssoattack2.png`
  - `6B47.png` → `6b47.png`、`LK3F.png` → `lk3f.png`、`Partizan.png` → `partizan.png`
  - `Pilgrim.png` → `pilgrim.png`、`ReadyPack.png` → `readypack.png`
  - `trigge.wav` → `trigger.wav`（夜视仪开关音效不触发）
  - 新建透明占位 `6b516_down.png`（6B516 非头盔，down 用空白占位）
  - 新建 `6lbt2670.png`（从 `SFMP.png` 复制）
  - 新建 `deagle_magout.png`、`glock_magout.png`、`usp_magout.png`（无弹匣图标）
  - csproj 添加 `equipment/*.wav` 包含规则

- **Bug 修复**
  - 夜视仪噪声纹理原地修改损坏问题（创建独立 `_noiseWorkTex`）
  - USP 弹匣 tags 错误（`cangetwet` → `belttool`）
  - 7 种弹匣缺少悬停描述补丁（Deagle/Glock17/M4A1/P90/UMP45/RPD/USP）
  - 维修套件耐久归零不销毁问题（添加手动 `Destroy`）
  - USP 弹匣缺少 `ResizeColliderToSprite` 方法和调用
  - 退弹日志始终显示 0 发（先保存再清零）
  - 夜视仪路径回退值为空字符串（改为 `BepInEx.Paths.PluginPath`）
  - RecipePatch 日志配方数量错误（8→10 弹药，10→11 弹匣）

### 内部

- `KrokMpHelper.IsMultiplayer` 多人模式守卫（维修套件 useAction）
- 维修套件加入 `ConditionNamePatch` 耐久显示列表
- VSS 无弹匣贴图尺寸修正（153x41 → 100x30）
- VSS 弹匣贴图比例修正（去除多余旋转）

## [1.0.0] - 初始版本

- 13 把自定义枪械（AKM/M4A1/SKS/DVL10/AXMC/MP133/MP153/Deagle/Glock17/P90/UMP45/USP/RPD）
- 2 把近战武器（Red Rebel 冰镐 / M-2 战术剑）
- 12 件防弹背心、11 件插板胸挂、8 件战术胸挂
- 6 件防弹头盔、11 件背包、2 种防弹插板
- 9 种自定义弹药、10 个自定义弹匣
- 口径隔离系统、世界生成系统、合成配方系统

# Changelog - CUTarkovWeaponMod

All notable changes to this project will be documented in this file.

## [1.1.2] - 2026-07-23

### 优化

- **帧率优化 - 空 Harmony 补丁移除**：注释 71 个空 `[HarmonyPatch(typeof(PlayerCamera)...ItemHoverDescription)]` Postfix 补丁，消除每帧 83 次空调用开销（即使方法体只有 `return;`，注册的补丁仍每帧执行）
- **帧率优化 - 夜视仪系统**：
  - 缓存 NVG 引用和耗电率，每 30 帧刷新一次（替代每帧 3 次 GetComponent/查找）
  - 预创建 4 张噪声 Sprite 轮换替代每帧 `Texture2D.GetPixels()`/`SetPixels()`/`Apply()`，消除每帧 GC 数组分配
  - 移除 `_noiseWorkTex` 工作纹理字段
- **帧率优化 - 瞄准镜**：ScopeZoomPatch 先检查 `body.GetItem(body.handSlot)`，仅持 AXMC 时才调用 `HasWearable("autozoomgoggles")`
- **帧率优化 - 护甲耐久**：ArmorConditionPatch 添加快速路径 `if (__result <= 0f) return;`，耐久归零时跳过 `GetLimbWearables()` 遍历

### 变更

- **移除耐久百分比显示**：删除 `ConditionNamePatch.cs`，新增 `FullNameConditionPatch` 拦截 `Item.get_fullName` 移除游戏原生的 `(XX%)` 耐久后缀（游戏在 `fullName` 属性中始终追加 condition 百分比，非模组添加）
- **VSS 枪口火光禁用**：整体式消音器不应有枪口火光，`muzzleParticle.Stop()` + `emission.enabled=false` + `SetActive(false)`
- **cangetwet 标签清理**：移除 23 件防弹衣和 2 件近战武器（Red Rebel / M-2 战术剑）的 `cangetwet` tag

### 修复

- **MBSS 世界体积过小**：`RegisterWithCUCoreLib` 缺少 `customInfo.Icon` 赋值且未设置 `SpriteScale`。MBSS 图标 PPU=22.5（其他装备为6），需设置 `SpriteScale=3.75f` 补偿 3.75 倍尺寸差异
- **Pilgrim/SsoAttack2/6SH118 背包不显示衰减倒计时**：`EnsureRegisteredInItemTable` 中缺少 `rotSpeed`/`decayMinutes`/`decayInfo` 字段，游戏从模板读取而非实例，存档加载后不显示衰减倒计时

## [1.1.1] - 2026-07-22

### 修复

- **夜视仪电池丢失**：存档加载后 BatteryItem 组件丢失导致按 N 无反应，新增 `EnsureNVGBattery` 动态补上组件，电量判断改为 `condition <= 0f`
- **语言切换不生效**：刀、护甲、背包切换英文后仍显示中文，新增 `I18nRefreshPatch` Prefix 在悬停时刷新 `ItemInfo.fullName/description`；移除 18 个文件中缓存的 `marker.displayName`
- **背包缺少可撕裂属性**：11 个背包添加 `rippable` tag 和 `CraftingQuality`，amount 匹配各背包 `WearableHitDurabilityLossMultiplier`（LK3F=5, SH118=10 等）
- **背包衰减速度异常**：`decayMinutes` 未设置导致 UI 显示"30多分钟损坏"，现设置 `decayMinutes = (1/DecayRatePerSecond)/60`
- **背包 Container 组件丢失**：CUCoreLib 覆盖 ItemInfo 后 Container 配置丢失，在 `ConfigureSpawnedItem` 中重新确保 `maxWeight/maxWeightPerItem/encumberanceMult`
- **夜视仪噪声纹理损坏**：`_noiseImg.sprite.texture.SetPixels` 原地修改原始纹理数组，改用独立 `_noiseWorkTex` 工作副本
- **维修套件耐久归零不销毁**：添加手动 `Destroy(item.gameObject)`
- **USP 弹匣 tags 错误**：`cangetwet` -> `belttool`
- **7 种弹匣缺少悬停描述补丁**：Deagle/Glock17/M4A1/P90/UMP45/RPD/USP
- **退弹日志显示 0 发**：先保存 `roundsInMag` 再清零
- **夜视仪路径回退值为空字符串**：改为 `BepInEx.Paths.PluginPath`
- **RecipePatch 日志配方数量错误**：8->10 弹药，10->11 弹匣

### 变更

- **M-2 战术剑重量**：1.3u -> 0.8u
- **Red Rebel 冰镐重量**：1.1u -> 1.0u
- **SFMP 背包容量**：14u -> 10u
- **6B516 down 贴图**：从复制 6b516.png 改为 1x1 透明占位（6B516 非头盔）
- **武器维修套件分类**：从医疗分类改为 custom（不再显示为瘀伤治疗包）
- **VSS 无弹匣贴图**：153x41 修正为 100x30（与有弹匣一致）
- **VSS 弹匣贴图**：去除多余旋转
- **csproj**：添加 `equipment/*.wav` 包含规则

### 新增

- **武器维修套件世界生成**：物资箱 7%、空投胶囊 12%、尸体 3%、崩溃舱 1%
- **I18nRefreshPatch.cs**：语言切换后刷新自定义物品本地化文本

### 贴图文件修复（12项）

| 原文件名 | 新文件名 | 说明 |
|---------|---------|------|
| `2DayAssault.png` | `mysteryranch2day.png` | 两日突击背包显示为瘀伤治疗包 |
| `Day Pack.png` | `daypack.png` | 文件名空格 |
| `Attack 2.png` | `ssoattack2.png` | 文件名空格 |
| `6B47.png` | `6b47.png` | 大小写 |
| `LK3F.png` | `lk3f.png` | 大小写 |
| `Partizan.png` | `partizan.png` | 大小写 |
| `Pilgrim.png` | `pilgrim.png` | 大小写 |
| `ReadyPack.png` | `readypack.png` | 大小写 |
| `trigge.wav` | `trigger.wav` | 夜视仪开关音效不触发 |
| 新建 | `6b516_down.png` | 透明占位 |
| 新建 | `6lbt2670.png` | 从 SFMP.png 复制 |
| 新建 | `deagle/glock/usp_magout.png` | 无弹匣图标 |

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

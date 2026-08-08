# Changelog - CUTarkovWeaponMod

All notable changes to this project will be documented in this file.

## [1.2.1.0] - 2026-08-08

### 新增

- **Ops-Core FAST 护目罩**（`fastvisor`）：保护眼部免受弹片、化学液体等伤害，5% 减伤，30% 免疫眼部失明，重量 0.2u，价值 20，智力要求 8，耐久损耗 0.65，仅可安装在 FAST MT / TK Fast MT 头盔上
- **Ops-Core FAST 多重打击防弹面罩**（`fastvisor2`）：特殊高强度防弹面罩，6% 减伤，40% 免疫眼部失明，25% 免疫下颚缺失，重量 0.35u，价值 32，智力要求 8，耐久损耗 0.4，仅可安装在 FAST MT / TK Fast MT 头盔上
- **Rys-T 头部保护机制**（`RysTHeadProtectionPatch`）：45% 免疫下颚脱位，50% 免疫下颚缺失/毁容，75% 免疫眼部失明
- **FAST 面罩/护目罩保护机制**（`FastVisorHeadProtectionPatch` / `FastVisor2HeadProtectionPatch`）
- **头部受伤免疫辅助**（`HeadInjuryProtectionHelper`）：同一伤害事件内多次 RemoveEye 调用合并为一次免疫判定，使失明免疫百分比与介绍一致
- **药物副作用失明不受免疫**（`NeuralBoosterImmunitySuppression`）：NeuralBooster 第二次使用导致的双目失明不享受头盔/面罩免疫
- **EXFIL 头盔加入夜视仪兼容列表**：PVS-14 / GPNVG-18 / PVS-31A 现可在 EXFIL 头盔上佩戴

### 变更

- **卡壳概率公式重做**（`JamChancePatch`）：替换原版两段式公式为多段式线性映射——100%~80%：0%~0.5%；80%~60%：0.5%~2%；60%~50%：2%~10%；50%~20%：10%~30%；20%~0%：30%~60%。潮湿不再影响卡壳率
- **枪械伤害调整**：RPD 生物伤 87→100、方块伤 67→70；AA-12 单弹丸 44→41；P90 生物伤 45→50；格洛克17 生物伤 50→32
- **弹药配方调整**：.338 UCW 增加 2 钛棒 + 1 热源要求；5.7x28 SB193 废料管 2→4；7.62x51 BPZ 增加 2 废料板 + 1 废料管
- **武器维修套件重量**：8.5u → 4.5u
- **FAST 面罩/护目罩加入夜视仪刷新池**：PVS-14（70）/ FAST护目罩（40）/ GPNVG-18（30）/ FAST面罩（30）
- **摘盔掉落面罩**：摘下 FAST MT 系列头盔时，眼睛槽位的护目罩/防弹面罩自动掉落到地上
- **免疫数值整体下调 5%**：所有头盔/面罩的失明、下颚缺失、下颚脱位免疫各减 5%

### 修复

- **煮熟的方便面合成后立即销毁**：`Item.Start` 拦截中世界容器子物体生成的隐藏物品销毁，合成产物（父级为玩家身体）与控制台生成正常保留
- **食物箱刷煮方便面**：三层防护——战利池初始化过滤 + `RandomFromPool`/`AllItemsFromPool` 访问过滤 + `Item.Start` 世界容器销毁
- **失明免疫体感低于数值**：多路径（断肢、药物等）同一事件连续调用两次 `RemoveEye` 导致免疫被稀释，现合并为一次判定

### 文案

- **Rys-T / FAST 面罩介绍**：新增彩色免疫数值说明
- **方便面介绍**：新增"可用热水做成煮方便面"说明
- 移除面罩介绍中的减伤数值行

## [1.2.0.7] - 2026-08-03

### 新增

- **AA-12 自动霰弹枪**（`aa12`）：MPS Auto Assault-12 Gen 1，12g 口径，全自动，20 发弹鼓供弹，一次射出 8 发弹丸，散布 0.22，生物伤 44×8，方块伤 30，噪音 3.3，后坐力 6，重量 2.5u，价值 48
- **AA-12 弹鼓**（`aa12_mag`）：20 发容量，基于 `riflemagazine` 预制体，价值 25，重量 1.2u，可合成（5 废料板 + 1 废料管 + 3 弹匣基座 + 20ml 生化流体 + 切割 + 锤打）

### 变更

- **枪械价值调整**：批量更新 15 把枪械和 2 把近战武器价值（AXMC=59, VSS=50, RPD=55, AA12=48, DVL=46, AKM=45, M4A1=44, P90=42, UMP45=35, M2=33, MP153=37, MP133=27, 沙鹰=21, SKS=20, 格洛克=13, USP=14, 冰镐=40）
- **背包/弹挂衰减类型调整**：21 件背包和弹挂的 DecayType 改为 `NoDecayWhenNotWorn | NoDecayWhenStill`（仅穿戴且移动时衰减），与原版背包行为一致
- **RPD 弹鼓配方更新**：材料改为与 AA-12 弹鼓一致（5 废料板 + 1 废料管 + 3 弹匣基座 + 20ml 生化流体 + 切割 + 锤打）

### 修复

- **食物箱生成煮熟方便面**：`VanillaBlockPatch` 的 `Item.Start` 补丁现在始终销毁 `HiddenFromLootPoolIds` 中的隐藏物品，通过 `IsCraftingHiddenItem` 标志位区分合成路径，防止食物箱从 `Item.GlobalItems` 中错误生成隐藏的合成中间物品
- **AA-12 弹匣装填/卸下问题**：基础预制体从 `shotgun` 改为 `rifle`，原生继承 `feedType=Mag` 和 `firingMode=Auto`，解决无 UI、无法装填/卸下弹匣、弹匣按钮状态不更新等问题
- **AA-12 枪口/枪管位置调整**：barrel 和 muzzleParticle 同时偏移以保持弹道起点和火光效果一致

## [1.2.0.5] - 2026-07-31

### 新增

- **夜视仪键位设置**（`NvgKeybindPatch`）：NVG 开关键位不再硬编码为 N，改为从游戏 Settings → Input 读取，支持自定义改键。通过 Locale 注册 + UI 刷新确保设置界面显示 "Night Vision Toggle" 键位栏

### 变更

- **弹匣价值差异化**：不再统一按 2 计价，改为按容量差异化：7发=11、10发=17~20、12发=30、17发=25、25发=35、30发=25~30、50发=40、100发=50
- **弹匣默认空弹**：配置时 `rounds=0`，世界生成时才随机装弹（0~满弹），合成产出保持 0 发
- **子弹价值调整**：.338 UCW / .50 AE 铜弹基础价值从 1 改为 2
- **AKM 图标 pivot 微调**：0.30→0.35，手持位置更准确
- **MP133/MP153 贴图更新**

### 修复

- **语言切换修复**：`ItemI18nRegistry` 改用 `CaptureItemInfo` 替代 `Register`，确保自定义物品名称和描述在语言切换时正确刷新
- **Slickers 空白行清理**

## [1.2.0] - 2026-07-30

### 新增

- **食物系统**（11 种自定义食物）
  - 军用饼干（`crackers`）：不腐坏，一次吃完，+3 饱食/-1 水分/+0.5 心情
  - 黑麦面包块（`croutons`）：不腐坏，一次吃完，+6 饱食/-3 水分/+1 心情
  - 士力架能量棒（`slickers`）：4小时腐坏，2次吃，+7 饱食/-3 水分/+2 心情/+22 患病
  - 塔克肉干（`tarker`）：不腐坏，3次吃，+6 饱食/-2 水分/+1.3 心情
  - Alyonka 巧克力棒（`alyonka`）：2.5小时腐坏，5次吃，+5 饱食/-3 水分/+1.8 心情/+19 患病
  - 一包糖（`sugar`）：10小时腐坏，8次吃，+6 饱食/-4 水分/-0.2 心情/+2 患病
  - Iskra 单兵口粮（`iskra`）：不腐坏，3次吃，+23 饱食/+5 水分/+2 心情
  - MRE 个人即食口粮（`mre`）：不腐坏，3次吃，+20 饱食/+3 水分/+1.5 心情
  - 豌豆罐头（`peas`）：15小时腐坏，3次吃，+6 饱食/+4 水分/+0.2 心情
  - 方便面（`noodles`）：24小时腐坏，2次吃，+10 饱食/-5 水分/+0.2 心情
  - 煮熟的方便面（`cookednoodles`）：2小时腐坏，仅合成获取，2次吃，+13 饱食/+7 水分/+1.5 心情
- **糖水液体**（`sugarwater`）：通过合成获取（一包糖+100ml水），每100ml +3 饱食/+7 水分/+0.6 心情
- **TK Fast MT 头盔仿制品**（`tkfastmt`）：仅合成获取，数值与 bikehelmet 一致，兼容所有夜视仪
- **弹挂甲双向锁定**（`ArmoredRigWearPatch`）：先穿弹挂再穿弹挂甲也被阻止
- **防弹插板物资箱生成**：物资箱 10%（70%普通/30%高级）
- **尸体生成上限提升**：最多 2 种不同类型
- **商人售卖自定义物品**：每类型最多 1 件
- **合成配方翻译注入**：合成界面正确显示自定义物品名称和描述
- **WornSprite 修复**：NVG/耳机/TK Fast MT 卸下后拖拽恢复正常

### 变更

- **cangetwet 清理**：移除所有非枪械物品的 cangetwet
- **近战属性调整**：冰镐 hammering 50、战术剑 cutting 70
- **头盔减伤调整**：所有头盔减伤 +25 个百分点
- **头盔生成权重调整**：SSh-68=8、6B47=6、Caiman=5、Exfil/FAST MT/Ulach=3、Rys-T=2
- **手枪图标统一**：Glock17/USP PPU/Pivot 与 Deagle 一致
- **耳机续航 20 分钟**，无电音量 -13/-14dB
- **耳机降噪**（SoundCutoff ×0.4/×0.3）
- **冰镐攀爬体力消耗翻倍**
- **合成配方分类**：护甲修复->材料栏、弹匣->实用物品、食物->食物栏、ScavPack->实用物品
- **背包 category 改为 custom**
- **食物使用原版战利池生成**

### 修复

- **item.cont 绑定缺失**：30 件容器物品耐久条/衰减异常
- **item.battery 绑定缺失**：5 件电池物品无法卸下电池
- **世界生成链式反应**：IsSpawning 锁 + Item 组件检查
- **VSS 和所有自定义物品被原版战利池生成**
- **原版背包出现在合成列表**
- **玩家出生点生成物品**
- **夜视仪 AmbientLight 换层后失效**：重新查找
- **夜视仪 overlay 换层后消失**：自动重建
- **多人模式客户端无法穿戴夜视仪/耳机**：WearWearable Prefix 添加 KrokMP 守卫
- **食物多次使用耐久异常**：改用原版 destroyAtZeroCondition
- **医疗物品 PPU**：6 件物品图标过小

## [1.1.5] - 2026-07-25

### 新增

- **战术耳塞系统**（2件，新 `ear` 槽位）
  - Peltor TEP-300（`tep300`）：入耳式电子防护耳机，小型电池供电，满电 15 分钟
  - CENS ProFlex DX5（`proflextac`）：高端版本，更强降噪和听力增强
  - 有电：听力损伤降低 60%/70%，听距 +20%/+40%，环境音量 +5/+5.5dB，主动降噪（SoundCutoff ×0.4/×0.3）
  - 无电：听力损伤降低 50%/60%，听距 -60%/-75%，环境音量 -10/-11dB
  - 通用：减轻声波炮视觉/听觉影响（鱼眼上限、静音时间缩短）
  - 世界生成：物资箱 5%/3%、空投舱 5%/3%、空投胶囊 8%/5%、尸体 2%/1%、崩溃舱 1%/0.5%
- **防弹插板世界生成**：物资箱 10% 概率生成插板（70% 普通 / 30% 高级）
- **尸体生成上限提升**：从最多 1 种改为最多 2 种不同类型物品
- **商人售卖自定义物品**：每类型最多 1 件（枪/护甲/弹挂/头盔/夜视仪/耳机/背包/近战/维修套件）
- **弹挂甲反向锁定**：`ArmoredRigWearPatch` 阻止先穿弹挂再穿弹挂甲

### 变更

- **cangetwet 标签清理**：移除所有非枪械物品的 cangetwet（头盔 7 + 近战 2 + 夜视仪 3 + 维修套件 1），仅枪械保留
- **近战属性调整**：Red Rebel 冰镐 hammering 18 -> 50，M-2 战术剑 cutting 30 -> 70
- **头盔生成权重调整**：SSh-68=8、6B47=6、Calman=5、Ulach=4、Exfil=4、FAST MT=4、Rys-T=2
- **手枪图标统一**：Glock17 PPU 22->27、USP PPU 24->27/Pivot X 0.35->0.30，与 Deagle 一致
- **背包分类调整**：category 从 "container" 改为 "custom"，不再出现在合成列表
- **合成配方分类调整**：护甲/弹挂甲修复配方 -> 材料栏，弹匣合成配方 -> 实用物品栏
- **冰镐攀爬体力消耗**：StaminaPerJump 1.0 -> 2.0（翻倍）

### 修复

- **item.cont 绑定缺失**：30 件容器物品（11 背包 + 8 弹挂 + 11 弹挂甲）的 `item.cont` 为 null，导致耐久条不随重量变化、衰减时间显示异常
- **item.battery 绑定缺失**：5 件电池物品（3 夜视仪 + 2 耳机）的 `item.battery` 为 null，导致无法卸下电池
- **世界生成链式反应**：自定义物品的 Container.Awake 触发更多物品生成，添加 `IsSpawning` 锁和 `Item` 组件检查
- **VSS 被原版战利池生成**：加入 `HiddenFromLootPoolIds`
- **所有自定义物品被原版战利池生成**：`IsHiddenFromLoot` 新增 `WeaponItemIds` 检查
- **原版背包出现在合成列表**：`RecipePatch` 新增 `HiddenFromLootPoolIds` 配方移除
- **玩家出生点生成物品**：`Item.Start` 补丁添加 5 米距离检查
- **尸体物品过多**：从 8 个独立概率调用改为互斥单选（后改为最多 2 种）
- **generatingWorld 阻止加载存档生成**：移除检查，改用 `Item` 组件检查区分世界容器和物品容器

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

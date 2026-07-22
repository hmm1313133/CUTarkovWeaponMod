# Casualties: Unknown - Tarkov-Style Weapon Mod

> `未知伤亡（Casualties: Unknown）：塔科夫武器模组`
>
> **v1.1.2**

一个为 **Casualties: Unknown Demo** 开发的 BepInEx 模组，将《逃离塔科夫》中的自定义枪械、弹药、弹匣、近战武器、护甲装备、头盔、背包及完整武器系统引入游戏。

> **依赖：** 本模组依赖 [CUTarkovMedicalMod](https://github.com/hmm1313133/CUTarkovMedicalMod) 和 [CUCoreLib](https://github.com/hmm1313133/CUCoreLib)，必须同时安装。

## 更新日志

### v1.1.2

- **帧率优化**：注释 71 个空 Harmony Postfix 补丁（ItemHoverDescription），消除每帧 83 次空调用开销
- **夜视仪性能优化**：缓存 NVG 引用和耗电率（每 30 帧刷新），预创建 4 张噪声 Sprite 轮换替代每帧 GetPixels/SetPixels/Apply，消除 GC 数组分配
- **瞄准镜性能优化**：ScopeZoomPatch 先检查手持物品，仅持 AXMC 时才查询穿戴物品
- **护甲耐久性能优化**：ArmorConditionPatch 添加快速路径，耐久归零时跳过 GetLimbWearables 遍历
- **移除耐久百分比显示**：删除 ConditionNamePatch.cs，背包/弹挂/护甲/头盔名称不再显示 (XX%) 后缀
- **MBSS 世界体积修复**：RegisterWithCUCoreLib 缺少 customInfo.Icon 赋值，导致 CUCoreLib 使用默认尺寸，世界精灵过小
- **VSS 枪口火光禁用**：整体式消音器不应有枪口火光，禁用 muzzleParticle
- **背包衰减修复**：Pilgrim/SsoAttack2/6SH118 在 EnsureRegisteredInItemTable 中补充 rotSpeed/decayMinutes/decayInfo，修复存档加载后不显示衰减倒计时
- **cangetwet 标签清理**：移除 23 件防弹衣和 2 件近战武器的 cangetwet tag（护甲/近战不应被浸湿损坏）

### v1.1.1

- **夜视仪电池修复**：存档加载后 BatteryItem 组件丢失导致无法开启，现动态补上并用 condition 直接判断电量
- **语言切换修复**：新增 I18nRefreshPatch，悬停时刷新 ItemInfo 本地化文本；移除 18 个文件中缓存的 marker.displayName
- **背包可撕裂属性**：为 11 个背包添加 rippable tag，CraftingQuality amount 匹配各背包的 WearableHitDurabilityLossMultiplier（5~10）
- **背包衰减修复**：设置 decayMinutes 修复 UI 剩余时间显示错误；在 ConfigureSpawnedItem 中重新设置 rotSpeed/decayInfo/decayMinutes
- **背包 Container 修复**：在 ConfigureSpawnedItem 中确保 Container 组件正确配置，耐久条长度显示容量、颜色显示耐久
- **贴图修复**：12 项文件名不匹配/缺失（mysteryranch2day、daypack、ssoattack2、6b47、lk3f、partizan、pilgrim、readypack、trigger.wav 等）
- **重量调整**：M-2 战术剑 1.3u->0.8u，Red Rebel 冰镐 1.1u->1.0u
- **SFMP 背包容量**：14u->10u
- **6B516 down 贴图**：改为透明占位（6B516 非头盔）
- **Bug 修复**：夜视仪噪声纹理损坏、USP 弹匣 tags、7 个弹匣悬停补丁、维修套件耐久归零销毁、退弹日志、路径回退、RecipePatch 日志

### v1.1.0

- **夜视仪系统**：新增 3 件塔科夫风格夜视仪
  - GPNVG-18 四目全景夜视仪、PVS-14 单目夜视仪、PVS-31A 双目夜视仪
  - 需佩戴兼容头盔（FAST MT / Galvion Calman / 6B47）后按 N 键开关
  - GPNVG-18 / PVS-31A 不兼容 6B47 头盔，PVS-14 兼容全部三款
  - 开关音效、暗角遮挡修复（sortingOrder=-1）、噪声纹理独立工作副本
  - 世界生成：物资箱 10%、空投胶囊 10%、空投舱 8%（PVS-14 权重 70% / GPNVG-18 30%）
- **VSS "绞丝机" 特种狙击步枪**：新增 9x39 全自动消音狙击步枪
  - 整体式消音器（响度 0.32）、30 发弹匣、动物伤害 105、结构伤害 88
  - 专用 VSS 弹匣（30 发，配方同 AKM 弹匣）+ 9x39mm SP-5 特种弹药
  - 枪械/弹匣/子弹不在世界生成，仅可通过合成或控制台获取
- **武器维修套件**：新增消耗型修理工具
  - 可使用 4 次，每次将手持枪械耐久回满，消耗 1/4 套件耐久
  - 重量 8.5u（随耐久消耗减少），价值 52
  - 世界生成：物资箱 7%、空投胶囊 12%、尸体 3%、崩溃舱 1%
- **枪械耐久消耗调整**：所有 14 把枪械耐久消耗提高 0.2
- **SFMP 背包容量调整**：6LBT-2670 容量从 14u 调整为 10u
- **贴图修复**：修复 12 个贴图文件名不匹配/缺失问题
  - mysteryranch2day、daypack、ssoattack2、6b47、lk3f、partizan、pilgrim、readypack
  - 6b516_down（改为透明占位）、deagle/glock/usp magout、trigger.wav 音效文件
- **Bug 修复**：
  - 夜视仪噪声纹理原地修改损坏问题
  - USP 弹匣 tags 错误（cangetwet -> belttool）
  - 7 种弹匣缺少悬停描述补丁
  - 维修套件耐久归零不销毁问题
  - 退弹日志显示 0 发问题
  - 夜视仪路径回退值为空字符串

- **背包系统**：新增 11 件塔科夫风格背包
  - 小型背包（LK 3F、Ready Pack、Scav 背包、Day Pack、Berkut、Mystery Ranch 2日）
  - 中型背包（Partizan、Pilgrim、SSO Attack 2）
  - 大型背包（6SH118 突击背包、6LBT-2670 SFMP 医物包）
  - CUCoreLib 集成：WornSprite 穿戴外观 + Container 容器属性 + 重量减免
  - 6LBT-2670 SFMP：从原版 medkit 复制 tagRestriction，仅允许医疗物品放入
  - 6SH118/6LBT-2670：约7小时时间衰减损坏
  - 世界生成：物资箱 13%(1~2个)、空投胶囊 16%、空投舱 6%、尸体 3%
  - 加权随机（低价值=高权重）
- **头盔系统**：新增 6 件塔科夫风格防弹头盔
  - SSh-68 钢盔、6B47 Ratnik-BSh、Galvion 凯门鳄、Team Wendy EXFIL、Highcom ULACH IIIA、Rys-T
  - CUCoreLib 集成：WornSprite 穿戴外观（hat 槽位）
  - 世界生成：物资箱 17%、空投舱 20%、空投胶囊 17%(1~2个)、尸体 5%
  - 加权随机（低价值=高权重）
- **护甲/装备系统**：新增 30+ 件塔科夫风格护甲与胸挂装备
  - 防弹背心（PACA、MF-UN、DRD、THOR、Trooper、6B13、HPC、Gzhel-K、Redut-T5、Slick、HGrid、6B43）
  - 插板胸挂（MBSS、TV-115、TV-110、SP PC V2、MK4A、Siege-R、6B5-16、TT SK、AVS TE、LV-119、6B45）
  - 战术胸挂（IDEA、Bank Robber、Type 56、WT chest rig、LBCR、Commando、Umka、BlackRock）
  - 防弹插板（普通插板、高级插板），用于护甲修复
  - 护甲耐久系统：减伤公式 `1/(1+wearableArmor)`，耐久归零后不再提供减伤（ArmorConditionPatch 修复）
  - 多部位防护：Redut-T5 覆盖胸腔/背部/手臂/大腿（MultiWornSprites）
  - 双槽位锁定：插板胸挂同时占用 outertorso + bandolier 槽位（11 件支持）
  - 时间衰减：购物袋胸挂（IDEA）穿戴约4小时后损坏
  - 合成系统：7 件护甲可合成 + 23 件护甲可用插板修复
  - CUCoreLib 集成：WornSprite 穿戴外观 + Container 容器属性 + ItemRegistry 注册
  - 中/英双语翻译
- **枪械重量调整**：所有 13 把枪械重量降低约15%，最重 RPD 限制在 6.0u
- **世界生成概率调整**：
  - 物资箱护甲/弹挂 32% → 17%
  - 空投舱弹挂 24% → 25%
  - 尸体新增：7% 护甲/弹挂 + 5% 头盔 + 3% 背包
- **医疗背包过滤**：6LBT-2670 SFMP 从原版 medkit 预制体复制 tagRestriction，与原生医疗箱使用相同物品过滤逻辑
- **近战武器资产迁移**：近战武器图标从 `Framework/Assets/` 迁移到 `Framework/Assets/knife/` 子目录

### v1.0.3

- **CUCoreLib 硬依赖**：CUCoreLib 从软依赖升级为硬依赖，移除 Legacy 模式和工厂模式，简化集成层架构
- **KrokMP 多人联机兼容**：
  - 世界生成（物资箱/尸体/空投舱）仅主机执行，客户端通过 KrokMP 同步接收物品
  - 多人模式下卸弹/卸弹匣改为世界掉落（FreshItemDrop），避免物品进入主机背包
  - KrokMP 同步的枪械/弹匣/弹药正确显示自定义贴图（模板预创建时调用 ConfigureCustomItem）
  - 背包缩略图正确显示有/无弹匣差分（不传 icon 给 ItemRegistry，回退到 sr.sprite）
- **控制台补全修复**：`Command.argAutofill` 类型修正为 `Dictionary<int, List<string>>`，Legacy 模式下自定义物品出现在 spawn 补全
- **近战武器词条**：
  - Red Rebel 冰镐：切割 12 / 捶打 18 / backflip（背上竖立）/ cangetwet / tool
  - M-2 战术剑：切割 30 / 捶打 12 / backflip / cangetwet / tool
  - 悬停描述修复：不再覆盖游戏原生详细页面
- **弹匣调整**：所有弹匣价值统一为 2；UMP45 弹匣图标分辨率修正（48x120 -> 13x32）
- **移除空间音效同步**：因 KrokMP 同步机制限制，效果不理想，已移除

### v1.0.1

- 初始发布版本

## 功能概览

| 功能 | 说明 |
|------|------|
| 14 把自定义枪械 | 栓动狙击/半自动卡宾/全自动步枪/泵动霰弹枪/手枪/冲锋枪/轻机枪/消音狙击 |
| 2 把近战武器 | Red Rebel 冰镐（攀爬+切割+捶打）、M-2 战术剑（切割+捶打），附带挥砍音效 |
| 3 件夜视仪 | GPNVG-18/PVS-14/PVS-31A，需兼容头盔，N键开关，开关音效 |
| 武器维修套件 | 可用4次，修理手持枪械至满耐久，重量随耐久减少 |
| 12 件防弹背心 | PACA~6B43，含减伤系统、耐久修复、多部位防护 |
| 11 件插板胸挂 | MBSS~6B45，双槽位锁定（outertorso+bandolier），减伤+储物 |
| 8 件战术胸挂 | IDEA~BlackRock，仅储物，部分有时间衰减 |
| 6 件防弹头盔 | SSh-68~Rys-T，hat 槽位，减伤防护 |
| 11 件背包 | LK 3F~6SH118，back 槽位，容量 4.4~14.2u，重量减免 |
| 2 种防弹插板 | 普通/高级，用于护甲修复 |
| 10 种自定义弹药 | 每把枪对应专属口径弹药，通过口径系统强制隔离 |
| 11 个自定义弹匣 | 弹匣供弹枪械的专属弹匣，可合成、世界生成 |
| 口径隔离系统 | 三层 Harmony 补丁防止跨口径装弹 |
| 加权世界生成 | 枪械按类别权重生成（手枪35%/霰弹20%/冲锋枪17%/步枪13%/狙击10%/机枪5%） |
| 近战世界掉落 | 物资箱 13% 掉落近战武器（40%冰镐 / 60%M2战术剑） |
| 加权装备生成 | 护甲/胸挂/头盔/背包按价值反比加权随机，低价值=高权重 |
| 枪械音效 | 每把枪有独立的开火/拉栓/闭栓/装弹匣/卸弹匣音效 |
| 合成系统 | 弹药 + 弹匣可通过合成配方制作；7 件护甲可合成，23 件护甲可用插板修复 |
| 原版武器替换 | 封禁原版武器/弹药/弹匣，替换为自定义武器系统 |
| AXMC 瞄准镜 | AXMC 狙击步枪装备时扩展视野范围 |
| UMP45 消音器 | UMP45 内置消音器，噪音仅 0.2，禁用枪口火光 |
| 控制台 Spawn | 控制台 `spawn` 命令支持所有自定义物品 ID |
| 原版物品开关 | `spawn vanilla_on` / `spawn vanilla_off` 切换原版武器生成/合成/交易 |
| 多语言支持 | 中/英双语，通过医疗模组的 I18n 系统加载 |
| KrokMP 多人兼容 | 世界生成仅主机执行，卸弹/卸弹匣世界掉落，物品贴图正确同步 |

## 控制台命令

| 命令 | 说明 |
|------|------|
| `spawn [itemKey]` | 生成指定物品（支持 Tab 自动补全原生物品） |
| `spawn vanilla_on` | 启用原版武器/弹药/弹匣的世界生成、合成和商人出售 |
| `spawn vanilla_off` | 禁用原版武器/弹药/弹匣（默认状态） |

## 14 把枪械一览

| # | 枪械 | ItemKey | 口径 | 射击模式 | 供弹 | 弹匣 | 重量 | 生物伤 | 方块伤 | 噪音 | 特殊功能 |
|---|------|---------|------|---------|------|------|------|--------|--------|------|---------|
| 1 | **AXMC** | `axmc` | .338 LM | 栓动 | 弹匣 | 10 | 4.2 | 310 | 299 | 6.0 | 瞄准镜视野扩展 |
| 2 | **DVL-10** | `dvl10` | 7.62x51 | 栓动 | 弹匣 | 10 | 3.7 | 205 | 180 | 4.5 | - |
| 3 | **SKS** | `sks` | 7.62x39 | 半自动 | 直供 | 10 | 3.4 | 150 | 100 | 2.9 | - |
| 4 | **AKM** | `akm` | 7.62x39 | 全自动 | 弹匣 | 30 | 3.5 | 120 | 90 | 3.0 | - |
| 5 | **Desert Eagle** | `deagle` | .50 AE | 半自动 | 弹匣 | 7 | 1.7 | 110 | 60 | 5.5 | - |
| 6 | **M4A1** | `m4a1` | 5.56x45 | 全自动 | 弹匣 | 30 | 3.4 | 90 | 70 | 2.7 | - |
| 7 | **RPD** | `rpd` | 7.62x39 | 全自动 | 弹匣 | 100 | 6.0 | 87 | 67 | 3.0 | 弹链供弹 |
| 8 | **Glock 17** | `glock17` | 9x19 | 半自动 | 弹匣 | 17 | 1.3 | 50 | 20 | 2.0 | - |
| 9 | **P90** | `p90` | 5.7x28 | 全自动 | 弹匣 | 50 | 3.0 | 45 | 35 | 1.9 | - |
| 10 | **UMP 45** | `ump45` | .45 ACP | 全自动 | 弹匣 | 25 | 2.1 | 44 | 27 | 0.2 | 内置消音器 |
| 11 | **MP-153** | `mp153` | 12g | 半自动 | 直供 | 8 | 3.9 | 41 | 30 | 4.0 | 8弹丸 |
| 12 | **MP-133** | `mp133` | 12g | 泵动 | 直供 | 4 | 3.8 | 40 | 30 | 4.0 | 8弹丸 |
| 13 | **USP** | `usp` | .45 ACP | 半自动 | 弹匣 | 12 | 1.2 | 40 | 42 | 2.2 | - |
| 14 | **VSS** | `vss` | 9x39 | 全自动 | 弹匣 | 30 | 3.0 | 105 | 88 | 0.32 | 整体式消音器 |

## 2 把近战武器一览

| # | 武器 | ItemKey | 生物伤 | 方块伤 | 攻击距离 | 击退 | 智力 | 切割 | 捶打 | 特殊功能 |
|---|------|---------|--------|--------|---------|------|------|------|------|---------|
| 1 | **Red Rebel 冰镐** | `redrebel` | 37 | 35 | 4 | 270 | 5 | 12 | 18 | 攀爬效果、backflip |
| 2 | **M-2 战术剑** | `m2sword` | 87 | 31 | 7 | 320 | 5 | 30 | 12 | backflip |

## 10 种弹药一览

| # | 弹药 | ItemKey | 口径 | 对应枪械 |
|---|------|---------|------|---------|
| 1 | .338 LM UCW | `338ucw` | 338lm | AXMC |
| 2 | 7.62x51 BPZ | `76251bpz` | 762x51 | DVL-10 |
| 3 | 7.62x39 SP | `76239sp` | 762x39 | SKS / AKM / RPD |
| 4 | 12/70 8.5mm | `12g85` | 12g | MP-133 / MP-153 |
| 5 | .50 AE 铜弹 | `50copper` | 50ae | Desert Eagle |
| 6 | .45 ACP FMJ | `45fmj` | 45acp | UMP 45 / USP |
| 7 | 9x19 PSO | `919pso` | 9x19 | Glock 17 |
| 8 | 5.56x45 FMJ | `55645fmj` | 556x45 | M4A1 |
| 9 | 5.7x28 SB193 | `5728sb193` | 5728 | P90 |
| 10 | 9x39 SP-5 | `939sp5` | 9x39 | VSS |

## 11 个弹匣一览

| # | 弹匣 | ItemKey | 对应枪械 | 对应弹药 | 容量 | 价值 |
|---|------|---------|---------|---------|------|------|
| 1 | AXMC 弹匣 | `axmc_mag` | AXMC | 338ucw | 10 | 2 |
| 2 | DVL-10 弹匣 | `dvl10_mag` | DVL-10 | 76251bpz | 10 | 2 |
| 3 | AKM 弹匣 | `akm_mag` | AKM | 76239sp | 30 | 2 |
| 4 | 沙漠之鹰弹匣 | `deagle_mag` | Desert Eagle | 50copper | 7 | 2 |
| 5 | Glock 17 弹匣 | `glock17_mag` | Glock 17 | 919pso | 17 | 2 |
| 6 | M4A1 弹匣 | `m4a1_mag` | M4A1 | 55645fmj | 30 | 2 |
| 7 | P90 弹匣 | `p90_mag` | P90 | 5728sb193 | 50 | 2 |
| 8 | UMP 45 弹匣 | `ump45_mag` | UMP 45 | 45fmj | 25 | 2 |
| 9 | USP 弹匣 | `usp_mag` | USP | 45fmj | 12 | 2 |
| 10 | RPD 弹链盒 | `rpd_mag` | RPD | 76239sp | 100 | 2 |
| 11 | VSS 弹匣 | `vss_mag` | VSS | 939sp5 | 30 | 2 |

## 护甲装备一览

### 防弹背心（outertorso 槽位）

提供减伤防护，不提供容器。减伤公式：`实际伤害 = 原始伤害 / (1 + wearableArmor)`。

| # | 护甲 | ItemKey | 减伤 | 重量 | 价值 | 智力 | 特殊 |
|---|------|---------|------|------|------|------|------|
| 1 | PACA 软质背心 | `paca` | 30% | 1.3 | 17 | 4 | 衰减型 |
| 2 | MF-UNTAR | `mfun` | 30% | 1.3 | 17 | 4 | 衰减型 |
| 3 | DRD | `drd` | 30% | 1.3 | 17 | 4 | 衰减型 |
| 4 | THOR CRV | `thor` | 52.1% | 2.7 | 36 | 4 | - |
| 5 | Trooper | `trooper` | 54.5% | 3.6 | 44 | 4 | - |
| 6 | 6B13 突击甲 | `6b13` | 54.5% | 3.6 | 44 | 4 | - |
| 7 | HPC 插板背心 | `hpc` | 56.2% | 4.5 | 55 | 5 | - |
| 8 | Gzhel-K | `gzhel_k` | 58.7% | 5.2 | 58 | 5 | - |
| 9 | Redut-T5 | `redut_t5` | 56.2% | 6.9 | 67 | 5 | 多部位防护（胸/背/臂/腿） |
| 10 | Slick | `slick` | 65.1% | 4.0 | 66 | 5 | - |
| 11 | HGrid | `hgrid` | 65.1% | 3.5 | 62 | 5 | 极低耐久 |
| 12 | 6B43 屏障-Sh | `6b43` | 70.0% | 8.0 | 75 | 5 | 多部位防护（胸/背/臂） |

### 插板胸挂（outertorso + bandolier 双槽位）

同时提供减伤防护和容器储物功能，双槽位锁定防止与单独的弹挂叠加。

| # | 胸挂 | ItemKey | 减伤 | 重量 | 价值 | 智力 | 容量 | 特殊 |
|---|------|---------|------|------|------|------|------|------|
| 1 | MBSS | `mbss` | 40.7% | 1.5 | 35 | 5 | 2u | - |
| 2 | TV-115 | `tv115` | 40.7% | 1.5 | 35 | 5 | 2u | - |
| 3 | TV-110 | `tv110` | 45.0% | 2.0 | 40 | 5 | 2u | - |
| 4 | SP PC V2 | `sppcv2` | 45.0% | 2.0 | 40 | 5 | 2u | - |
| 5 | 6B5-16 | `6b516` | 45.0% | 2.5 | 42 | 5 | 2u | - |
| 6 | MK4A 突击型 | `mk4a` | 50.0% | 3.0 | 48 | 6 | 2u | - |
| 7 | Siege-R | `sieger` | 52.1% | 4.0 | 52 | 7 | 3u | - |
| 8 | TT SK | `ttsk` | 52.1% | 3.5 | 50 | 5 | 3u | - |
| 9 | AVS TE | `avste` | 52.1% | 3.5 | 50 | 5 | 3u | - |
| 10 | LV-119 | `lv119` | 52.1% | 3.5 | 50 | 5 | 3u | - |
| 11 | 6B45 医疗型 | `6b45` | 54.5% | 4.0 | 55 | 5 | 3u | - |

### 战术胸挂（bandolier 槽位）

不提供减伤防护，仅提供容器储物功能。部分型号有时间衰减。

| # | 胸挂 | ItemKey | 重量 | 价值 | 智力 | 容量 | 特殊 |
|---|------|---------|------|------|------|------|------|
| 1 | IDEA DIY | `idea` | 0.2 | 16 | 3 | 2u | 约4小时衰减损坏 |
| 2 | Bank Robber | `bankrobber` | 0.3 | 20 | 3 | 2u | 衰减型 |
| 3 | Type 56 | `type56` | 0.3 | 20 | 3 | 2u | 衰减型 |
| 4 | WT chest rig | `wtchestrig` | 0.4 | 24 | 3 | 2u | 衰减型 |
| 5 | LBCR | `lbcr` | 0.4 | 24 | 3 | 2u | 衰减型 |
| 6 | Commando | `commando` | 0.4 | 24 | 3 | 2u | 衰减型 |
| 7 | Umka | `umka` | 0.5 | 28 | 3 | 2u | 衰减型 |
| 8 | BlackRock | `blackrock` | 0.5 | 28 | 3 | 2u | 衰减型 |

### 防弹头盔（hat 槽位）

提供头部减伤防护。减伤公式同防弹背心。

| # | 头盔 | ItemKey | 减伤 | 重量 | 价值 | 智力 | 特殊 |
|---|------|---------|------|------|------|------|------|
| 1 | SSh-68 钢盔 | `ssh68` | 33.0% | 0.6 | 36 | 7 | - |
| 2 | 6B47 Ratnik-BSh | `6b47` | 35.0% | 0.4 | 38 | 7 | - |
| 3 | Galvion 凯门鳄 | `calman` | 35.0% | 0.35 | 40 | 7 | - |
| 4 | Team Wendy EXFIL | `exfil` | 40.0% | 0.65 | 46 | 7 | - |
| 5 | Highcom ULACH IIIA | `ulach` | 45.0% | 0.55 | 48 | 7 | - |
| 6 | Rys-T | `ryst` | 60.0% | 1.2 | 55 | 7 | 防弹面罩 |

### 背包（back 槽位）

提供容器储物功能，部分型号有重量减免和时间衰减。

| # | 背包 | ItemKey | 重量 | 价值 | 容量 | 重量减免 | 特殊 |
|---|------|---------|------|------|------|---------|------|
| 1 | LK 3F Transfer | `lk3f` | 0.6 | 15 | 4.4u | - | - |
| 2 | Vertx Ready Pack | `readypack` | 0.6 | 20 | 4.8u | - | - |
| 3 | Scav 背包 | `scavpack` | 0.9 | 25 | 4.8u | - | - |
| 4 | LBT-8005A Day Pack | `daypack` | 0.5 | 30 | 5.5u | - | - |
| 5 | Berkut BB-102 | `berkut` | 0.6 | 30 | 5.0u | - | - |
| 6 | Mystery Ranch 2日 | `mysteryranch2day` | 0.8 | 35 | 5.0u | - | - |
| 7 | Partizan | `partizan` | 0.4 | 35 | 5.5u | - | - |
| 8 | Pilgrim 旅行包 | `pilgrim` | 1.6 | 40 | 7.0u | - | - |
| 9 | SSO Attack 2 | `ssoattack2` | 1.5 | 42 | 7.2u | - | - |
| 10 | 6LBT-2670 SFMP | `6lbt2670` | 2.5 | 45 | 10.0u | 60% | 医疗专用、约7小时衰减 |
| 11 | 6SH118 突击背包 | `6sh118` | 3.0 | 50 | 14.2u | - | 约7小时衰减 |

### 防弹插板（utility 物品）

用于在合成台中修复防弹衣耐久，不可穿戴。

| # | 插板 | ItemKey | 重量 | 价值 | 智力 | 用途 |
|---|------|---------|------|------|------|------|
| 1 | 普通插板 | `cheapplate` | 0.5 | 10 | 2 | 修复低级护甲 |
| 2 | 高级插板 | `advancedplate` | 0.3 | 22 | 2 | 修复高级护甲 |

### 夜视仪（hat 槽位，需兼容头盔）

需先佩戴兼容头盔（FAST MT / Galvion Calman / 6B47），装备夜视仪后按 N 键开关。GPNVG-18 / PVS-31A 不兼容 6B47。

| # | 夜视仪 | ItemKey | 重量 | 价值 | 智力 | 兼容头盔 | 特殊 |
|---|------|---------|------|------|------|---------|------|
| 1 | GPNVG-18 四目全景 | `gpnvg18` | 0.9 | 60 | 8 | FAST MT / Calman | 全景视野 |
| 2 | PVS-14 单目 | `pvs14` | 0.7 | 40 | 6 | FAST MT / Calman / 6B47 | 单目视野 |
| 3 | PVS-31A 双目 | `pvs31a` | 0.8 | 50 | 7 | FAST MT / Calman | 双目视野 |

### 武器维修套件（消耗品）

可使用 4 次的手持枪械修理工具。手持需要修理的枪械，右键使用维修套件即可将枪械耐久回满，每次消耗 1/4 套件耐久。重量随耐久消耗线性减少。

| # | 物品 | ItemKey | 重量 | 价值 | 使用次数 | 特殊 |
|---|------|---------|------|------|---------|------|
| 1 | 武器维修套件 | `weaponrepairkit` | 8.5u | 52 | 4 次 | 重量随耐久减少 |

### 护甲修复配方

| 护甲等级 | 修复材料 | 示例护甲 |
|---------|---------|---------|
| 低级 | 2x 普通插板 | PACA、MF-UN、DRD、TV-115、MBSS |
| 中级 | 1x 普通 + 1x 高级 | MK4A、Thor、6B13 |
| 高级 | 2x 高级 | SPPCV2、6B45、TV-110、Trooper、Siege-R |
| 顶级 | 3x 高级 + 2x 普通 | AVS TE、TT SK、LV-119、Slick、HGrid |
| 最强 | 5x 高级 | 6B43 |
| 混合 | 4x 普通 + 2x 高级 | Redut-T5 |
| 混合 | 2x 高级 + 1x 普通 | Gzhel-K、HPC |

## 世界生成概率

### 枪械分类权重（触发后）

| 类别 | 权重 | 包含枪械 |
|------|------|---------|
| 手枪 | 35% | Deagle、Glock17、USP |
| SKS + 霰弹枪 | 20% | SKS、MP133、MP153 |
| 冲锋枪 | 17% | P90、UMP45 |
| 步枪 | 13% | AKM、M4A1 |
| 狙击枪 | 10% | AXMC、DVL-10 |
| 轻机枪 | 5% | RPD |

### 各位置触发概率

| 位置 | 枪械 | 弹匣 | 近战 | 护甲/弹挂 | 头盔 | 背包 | 夜视仪 | 维修套件 |
|------|------|------|------|---------|------|------|--------|---------|
| 物资箱 | 18.6% | 22% | 13%（40%冰镐 / 60%M2） | 17% | 17% | 13%(1~2个) | 10% | 7% |
| 空投舱 | 20% | - | - | 25% | 20% | 6% | 8% | - |
| 空投胶囊 | 29% | - | - | 32%（弹挂类） | 17%(1~2个) | 16% | 10% | 12% |
| 尸体旁 | 15% | 15% | - | 7% | 5% | 3% | - | 3% |
| 崩溃舱 | - | 62%（替换） | - | - | - | - | - | 1% |
| 医疗箱 | - | - | - | 20% | - | - | - | - |

### 装备加权随机规则

护甲/弹挂、头盔、背包均按价值反比加权随机，低价值=高权重：

- **护甲/弹挂**：物资箱和空投舱触发后 50/50 随机分为护甲类或弹挂类；空投胶囊固定弹挂类
- **头盔**：从 6 件中加权随机（SSh-68/6B47/Calman 权重4，EXFIL/ULACH 权重3，Rys-T 权重2）
- **背包**：从 11 件中加权随机（LK 3F 权重5，ReadyPack/ScavPack 权重4，DayPack/Berkut/MysteryRanch 权重3，Partizan/Pilgrim/SsoAttack2 权重2，6LBT-2670/6SH118 权重1）

### 弹匣子弹数规则

| 来源 | 子弹数 |
|------|--------|
| 世界生成（物资箱/空投舱/尸体/崩溃舱） | 0 ~ 满弹 随机 |
| 合成产出 | 0 发 |

## 构建与部署

### 前置条件

1. 安装 .NET SDK 8.0+
2. 安装游戏 Casualties: Unknown Demo
3. 克隆本仓库和 [CUTarkovMedicalMod](https://github.com/hmm1313133/CUTarkovMedicalMod) 到同一父目录
4. 安装 [CUCoreLib](https://github.com/hmm1313133/CUCoreLib) 到游戏 BepInEx/plugins 目录

### 配置游戏路径

在父目录创建 `vars.targets` 文件：

```xml
<Project>
  <PropertyGroup>
    <BaseGamePath>你的游戏路径/Casualties Unknown Demo</BaseGamePath>
    <BepInExDir>$(BaseGamePath)/BepInEx</BepInExDir>
    <GameAssembliesDir>$(BaseGamePath)/CasualtiesUnknown_Data/Managed</GameAssembliesDir>
    <PluginDirName>CUTarkovWeaponMod</PluginDirName>
  </PropertyGroup>
</Project>
```

### 构建

```bash
dotnet build CUTarkovWeaponMod.csproj
```

构建后 DLL 和资源会自动复制到 BepInEx plugins 目录。

## 项目结构

```
CUTarkovWeaponMod/
├── Plugin.cs                              # BepInEx 插件入口（含护甲双槽位补丁注册+衰减 Tick）
├── CUTarkovWeaponMod.csproj               # 项目文件
├── Lang/
│   ├── EN.json                           # 英文翻译
│   └── zh_CN.json                        # 中文翻译
├── Integration/
│   ├── WeaponCUCoreLibMode.cs            # CUCoreLib 集成（模板缓存+ItemRegistry 注册+护甲/头盔/背包 WornSprite）
│   └── WeaponItemSaveProvider.cs         # 武器存档保存/恢复（弹药数/弹匣状态）
└── Framework/
    ├── WeaponItemRegistration.cs          # 物品注册到医疗模组系统（枪械+护甲+头盔+背包+弹匣+弹药）
    ├── WeaponUpdateNotifier.cs            # 物品更新通知系统
    ├── MeleeSoundCache.cs                 # 近战挥砍音效预加载缓存
    ├── ConsoleAutofillPatch.cs            # 控制台命令自动补全注入
    ├── ScopeZoomPatch.cs                  # AXMC 瞄准镜视野扩展
    ├── ArmorConditionPatch.cs             # 护甲耐久归零后不再提供减伤
    ├── UnifiedHoverPatch.cs               # 物品悬停描述合并补丁（替代 83 个独立 Postfix）
    ├── [Key]ItemSystem.cs                 # 枪械/近战/护甲/头盔/背包物品系统（每件一个）
    ├── AmmoItemSystem.cs                  # 所有自定义弹药定义
    ├── GunMagPatch.cs                     # 枪<->弹匣映射 + 弹匣系统
    ├── CaliberPatch.cs                    # 口径注册表 + 装弹/退弹补丁
    ├── CustomSpawnPatch.cs                # 加权世界生成（枪械/弹匣/近战/护甲/头盔/背包）
    ├── VanillaBlockPatch.cs               # 封禁原版武器/弹药/弹匣
    ├── RecipePatch.cs                     # 弹药+弹匣+护甲合成/修复配方
    ├── RecipeSpawnPatch.cs                # 合成产出物品配置
    ├── RecipeSpritePatch.cs               # 合成配方自定义图标（弹药+弹匣+护甲+背包）
    ├── LocalePatch.cs                     # 物品名称注入游戏语言系统
    └── Assets/
        ├── *.png / *.webp                 # 枪械/弹药/弹匣图标
        ├── guns/[key]/[key]_*.wav         # 枪械音效（fire/open/close/magin/magout）
        ├── knife/*.png                    # 近战武器图标
        └── equipment/*.png                # 护甲/胸挂/插板/头盔/背包图标
```

## 技术要点

- **BepInDependency**: 硬依赖医疗模组 `com.yourname.cu.tarkovmedicalmod` 和 CUCoreLib `net.cucorelib`
- **I18n 集成**: 通过 `I18n.RegisterExternalLangDir()` 将武器翻译注入医疗模组的翻译系统
- **ConsoleSpawn 集成**: 通过 `ConsoleSpawnPatch.ExternalItemConfigurer` 回调注册物品配置
- **InternalsVisibleTo**: 医疗模组通过 `InternalsVisibleTo` 暴露 internal 成员给武器模组
- **CUCoreLib 集成**: 通过 `WeaponCUCoreLibMode` 注册武器到 ItemRegistry，预创建模板缓存绕过 ChooseTemplateId；护甲/头盔/背包注册 WornSprite/Container/MultiWornSprites
- **KrokMP 兼容**: 使用 `KrokMpHelper.ShouldSpawnLoot` 门控世界生成，`KrokMpHelper.IsMultiplayer` 控制卸弹行为
- **Harmony PatchAll**: 武器模组独立 PatchAll，与医疗模组互不干扰
- **MeleeSoundCache**: 近战音效在插件启动时一次性预加载，攻击时从内存直接取，避免 I/O 卡顿
- **护甲减伤系统**: 减伤公式 `伤害/(1+wearableArmor)`，`ArmorConditionPatch` 修复耐久归零后仍提供减伤的原版 bug
- **护甲双槽位**: 插板胸挂通过 `Body.GetWearableBySlotID` Postfix 补丁实现 outertorso + bandolier 双槽位锁定
- **护甲/背包时间衰减**: 衰减型装备通过 `Plugin.Update` 每帧调用 `TickDecay()` 按时间消耗耐久
- **医疗背包过滤**: 6LBT-2670 SFMP 从原版 medkit 预制体复制 `tagRestriction`，与原生医疗箱使用相同物品过滤逻辑
- **加权世界生成**: 装备/头盔/背包按价值反比加权随机，低价值物品更常见

# Casualties: Unknown - Tarkov-Style Weapon Mod

> `未知伤亡（Casualties: Unknown）：塔科夫武器模组`
>
> **v1.0.0.0**

一个为 **Casualties: Unknown Demo** 开发的 BepInEx 模组，将《逃离塔科夫》中的自定义枪械、弹药、弹匣、近战武器及完整武器系统引入游戏。

> **依赖：** 本模组依赖 [CUTarkovMedicalMod](https://github.com/hmm1313133/CUTarkovMedicalMod)，必须同时安装医疗模组。

## 功能概览

| 功能 | 说明 |
|------|------|
| 13 把自定义枪械 | 栓动狙击/半自动卡宾/全自动步枪/泵动霰弹枪/手枪/冲锋枪/轻机枪 |
| 2 把近战武器 | Red Rebel 冰镐（攀爬）、M-2 战术剑，附带挥砍音效 |
| 9 种自定义弹药 | 每把枪对应专属口径弹药，通过口径系统强制隔离 |
| 9 个自定义弹匣 | 弹匣供弹枪械的专属弹匣，可合成、世界生成 |
| 口径隔离系统 | 三层 Harmony 补丁防止跨口径装弹 |
| 加权世界生成 | 枪械按类别权重生成（手枪35%/霰弹20%/冲锋枪17%/步枪13%/狙击10%/机枪5%） |
| 近战世界掉落 | 物资箱 13% 掉落近战武器（40%冰镐 / 60%M2战术剑） |
| 容器掉落 | 物资箱 6.6% 枪械 + 10% 弹匣；尸体旁 3% 枪械/弹匣；空投舱 3% 枪械 |
| 枪械音效 | 每把枪有独立的开火/拉栓/闭栓/装弹匣/卸弹匣音效 |
| 合成系统 | 弹药 + 弹匣可通过合成配方制作 |
| 原版武器替换 | 封禁原版武器/弹药/弹匣，替换为自定义武器系统 |
| AXMC 瞄准镜 | AXMC 狙击步枪装备时扩展视野范围 |
| UMP45 消音器 | UMP45 内置消音器，噪音仅 0.2，禁用枪口火光 |
| 控制台 Spawn | 控制台 `spawn` 命令支持所有自定义物品 ID |
| 多语言支持 | 中/英双语，通过医疗模组的 I18n 系统加载 |

## 13 把枪械一览

| # | 枪械 | ItemKey | 口径 | 射击模式 | 供弹 | 弹匣 | 生物伤 | 方块伤 | 噪音 | 特殊功能 |
|---|------|---------|------|---------|------|------|--------|--------|------|---------|
| 1 | **AXMC** | `axmc` | .338 LM | 栓动 | 弹匣 | 10 | 310 | 299 | 6.0 | 瞄准镜视野扩展 |
| 2 | **DVL-10** | `dvl10` | 7.62x51 | 栓动 | 弹匣 | 10 | 205 | 180 | 4.5 | - |
| 3 | **SKS** | `sks` | 7.62x39 | 半自动 | 直供 | 10 | 150 | 100 | 2.9 | - |
| 4 | **AKM** | `akm` | 7.62x39 | 全自动 | 弹匣 | 30 | 120 | 90 | 3.0 | - |
| 5 | **Desert Eagle** | `deagle` | .50 AE | 半自动 | 弹匣 | 7 | 110 | 60 | 5.5 | - |
| 6 | **M4A1** | `m4a1` | 5.56x45 | 全自动 | 弹匣 | 30 | 90 | 70 | 2.7 | - |
| 7 | **RPD** | `rpd` | 7.62x39 | 全自动 | 弹匣 | 100 | 87 | 67 | 3.0 | 弹链供弹 |
| 8 | **Glock 17** | `glock17` | 9x19 | 半自动 | 弹匣 | 17 | 50 | 20 | 2.0 | - |
| 9 | **P90** | `p90` | 5.7x28 | 全自动 | 弹匣 | 50 | 45 | 35 | 1.9 | - |
| 10 | **UMP 45** | `ump45` | .45 ACP | 全自动 | 弹匣 | 25 | 44 | 27 | 0.2 | 内置消音器 |
| 11 | **MP-153** | `mp153` | 12g | 半自动 | 直供 | 8 | 41 | 30 | 4.0 | 8弹丸 |
| 12 | **MP-133** | `mp133` | 12g | 泵动 | 直供 | 4 | 40 | 30 | 4.0 | 8弹丸 |
| 13 | **USP** | `usp` | .45 ACP | 半自动 | 弹匣 | 12 | 40 | 42 | 2.2 | - |

## 2 把近战武器一览

| # | 武器 | ItemKey | 生物伤 | 方块伤 | 攻击距离 | 击退 | 智力 | 特殊功能 |
|---|------|---------|--------|--------|---------|------|------|---------|
| 1 | **Red Rebel 冰镐** | `redrebel` | 37 | 35 | 4 | 270 | 5 | 攀爬效果 |
| 2 | **M-2 战术剑** | `m2sword` | 87 | 31 | 7 | 320 | 5 | - |

## 9 种弹药一览

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

## 9 个弹匣一览

| # | 弹匣 | ItemKey | 对应枪械 | 对应弹药 | 容量 |
|---|------|---------|---------|---------|------|
| 1 | AXMC 弹匣 | `axmc_mag` | AXMC | 338ucw | 10 |
| 2 | DVL-10 弹匣 | `dvl10_mag` | DVL-10 | 76251bpz | 10 |
| 3 | AKM 弹匣 | `akm_mag` | AKM | 76239sp | 30 |
| 4 | 沙漠之鹰弹匣 | `deagle_mag` | Desert Eagle | 50copper | 7 |
| 5 | Glock 17 弹匣 | `glock17_mag` | Glock 17 | 919pso | 17 |
| 6 | M4A1 弹匣 | `m4a1_mag` | M4A1 | 55645fmj | 30 |
| 7 | P90 弹匣 | `p90_mag` | P90 | 5728sb193 | 50 |
| 8 | UMP 45 弹匣 | `ump45_mag` | UMP 45 | 45fmj | 25 |
| 9 | USP 弹匣 | `usp_mag` | USP | 45fmj | 12 |

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

| 位置 | 枪械 | 弹匣 | 近战 |
|------|------|------|------|
| 物资箱 | 6.6% | 10% | 13%（40%冰镐 / 60%M2） |
| 空投舱 | 3% | - | - |
| 尸体旁 | 3% | 3% | - |

## 构建与部署

### 前置条件

1. 安装 .NET SDK 8.0+
2. 安装游戏 Casualties: Unknown Demo
3. 克隆本仓库和 [CUTarkovMedicalMod](https://github.com/hmm1313133/CUTarkovMedicalMod) 到同一父目录

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
├── Plugin.cs                              # BepInEx 插件入口
├── CUTarkovWeaponMod.csproj               # 项目文件
├── Lang/
│   ├── EN.json                           # 英文翻译
│   └── zh_CN.json                        # 中文翻译
└── Framework/
    ├── WeaponItemRegistration.cs          # 物品注册到医疗模组系统
    ├── MeleeSoundCache.cs                 # 近战挥砍音效预加载缓存
    ├── ScopeZoomPatch.cs                  # AXMC 瞄准镜视野扩展
    ├── [Key]ItemSystem.cs                 # 枪械/近战物品系统（每把一个）
    ├── AmmoItemSystem.cs                  # 所有自定义弹药定义
    ├── GunMagPatch.cs                     # 枪<->弹匣映射 + 弹匣系统
    ├── CaliberPatch.cs                    # 口径注册表 + 装弹/退弹补丁
    ├── CustomSpawnPatch.cs                # 加权世界生成（枪械/弹匣/近战）
    ├── VanillaBlockPatch.cs               # 封禁原版武器/弹药/弹匣
    ├── RecipePatch.cs                     # 弹药 + 弹匣合成配方
    ├── RecipeSpawnPatch.cs                # 合成产出物品配置
    ├── RecipeSpritePatch.cs              # 合成配方自定义图标
    ├── LocalePatch.cs                     # 物品名称注入游戏语言系统
    └── Assets/
        ├── *.png / *.webp                 # 枪械/弹药/弹匣图标
        └── guns/[key]/[key]_*.wav         # 枪械音效（fire/open/close/magin/magout）
```

## 技术要点

- **BepInDependency**: 硬依赖医疗模组 `com.yourname.cu.tarkovmedicalmod`
- **I18n 集成**: 通过 `I18n.RegisterExternalLangDir()` 将武器翻译注入医疗模组的翻译系统
- **ConsoleSpawn 集成**: 通过 `ConsoleSpawnPatch.ExternalItemConfigurer` 回调注册物品配置
- **InternalsVisibleTo**: 医疗模组通过 `InternalsVisibleTo` 暴露 internal 成员给武器模组
- **Harmony PatchAll**: 武器模组独立 PatchAll，与医疗模组互不干扰
- **MeleeSoundCache**: 近战音效在插件启动时一次性预加载，攻击时从内存直接取，避免 I/O 卡顿

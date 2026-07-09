# Casualties: Unknown - Tarkov-Style Weapon Mod

> `未知伤亡（Casualties: Unknown）：塔科夫武器模组`
>
> **v0.1.0.0**

一个为 **Casualties: Unknown Demo** 开发的 BepInEx 模组，将《逃离塔科夫》中的 9 把自定义枪械、9 种弹药、7 个弹匣及其完整武器系统引入游戏。

> **依赖：** 本模组依赖 [CUTarkovMedicalMod](https://github.com/hmm1313133/CUTarkovMedicalMod)，必须同时安装医疗模组。

## 功能概览

| 功能 | 说明 |
|------|------|
| 9 把自定义枪械 | 栓动狙击/半自动卡宾/全自动步枪/泵动霰弹枪/手枪/冲锋枪/PDW |
| 9 种自定义弹药 | 每把枪对应专属口径弹药，通过口径系统强制隔离 |
| 7 个自定义弹匣 | 弹匣供弹枪械的专属弹匣，可合成、世界生成 |
| 口径隔离系统 | 三层 Harmony 补丁防止跨口径装弹（GunScript.LoadMag / AmmoScript.LoadRound / UnloadRound） |
| 世界战利品 | 枪械 + 弹匣 + 弹药在世界中刷新 |
| 容器掉落 | 物资箱/尸体/空投舱按概率掉落枪械或弹匣 |
| 枪械音效 | 每把枪有独立的开火/拉栓/换弹音效 |
| 合成系统 | 弹匣和弹药可通过合成配方制作 |
| 原版武器替换 | 封禁原版武器/弹药/弹匣，替换为自定义武器系统 |
| AXMC 瞄准镜 | AXMC 狙击步枪装备时扩展视野范围 |
| 控制台 Spawn | 控制台 `spawn` 命令支持所有自定义物品 ID |
| 多语言支持 | 中/英双语，通过医疗模组的 I18n 系统加载 |

## 9 把枪械一览

| # | 枪械 | ItemKey | 基础预制体 | 口径 | 射击模式 | 供弹 | 弹匣容量 | 生物伤害 | 方块伤害 | 弹丸数 | 特殊功能 |
|---|------|---------|-----------|------|---------|------|---------|---------|---------|--------|---------|
| 1 | **AXMC** | `axmc` | rifle | .338 LM | 栓动 | 弹匣 | 10 | 310 | 299 | 1 | 瞄准镜视野扩展 |
| 2 | **DVL-10** | `dvl10` | rifle | 7.62x51 | 栓动 | 弹匣 | 10 | 205 | 180 | 1 | - |
| 3 | **SKS** | `sks` | rifle | 7.62x39 | 半自动 | 直供 | 10 | 150 | 100 | 1 | - |
| 4 | **AKM** | `akm` | rifle | 7.62x39 | 全自动 | 弹匣 | 30 | 120 | 90 | 1 | - |
| 5 | **Desert Eagle** | `deagle` | pistol | .50 AE | 半自动 | 弹匣 | 7 | 110 | 60 | 1 | - |
| 6 | **M4A1** | `m4a1` | rifle | 5.56x45 | 全自动 | 弹匣 | 30 | 90 | 70 | 1 | - |
| 7 | **Glock 17** | `glock17` | pistol | 9x19 | 半自动 | 弹匣 | 17 | 50 | 20 | 1 | - |
| 8 | **P90** | `p90` | rifle | 5.7x28 | 全自动 | 弹匣 | 50 | 45 | 35 | 1 | - |
| 9 | **MP-133** | `mp133` | shotgun | 12g | 泵动 | 直供 | 4 | 40 | 30 | 8 | - |

## 9 种弹药一览

| # | 弹药 | ItemKey | 口径 | 对应枪械 | 基础预制体 |
|---|------|---------|------|---------|-----------|
| 1 | .338 LM UCW | `338ucw` | 338lm | AXMC | 556round |
| 2 | 7.62x51 BPZ | `76251bpz` | 762x51 | DVL-10 | 556round |
| 3 | 7.62x39 SP | `76239sp` | 762x39 | SKS / AKM | 556round |
| 4 | 12/70 8.5mm | `12g85` | 12g | MP-133 | 12gauge |
| 5 | .50 AE 铜弹 | `50copper` | 50ae | Desert Eagle | 556round |
| 6 | .45 ACP FMJ | `45fmj` | 45acp | - | 556round |
| 7 | 9x19 PSO | `919pso` | 9x19 | Glock 17 | 556round |
| 8 | 5.56x45 FMJ | `55645fmj` | 556x45 | M4A1 | 556round |
| 9 | 5.7x28 SB193 | `5728sb193` | 5728 | P90 | 556round |

## 7 个弹匣一览

| # | 弹匣 | ItemKey | 对应枪械 | 对应弹药 | 容量 | 基础预制体 |
|---|------|---------|---------|---------|------|-----------|
| 1 | AXMC 弹匣 | `axmc_mag` | AXMC | 338ucw | 10 | riflemagazine |
| 2 | DVL-10 弹匣 | `dvl10_mag` | DVL-10 | 76251bpz | 10 | riflemagazine |
| 3 | AKM 弹匣 | `akm_mag` | AKM | 76239sp | 30 | riflemagazine |
| 4 | 沙漠之鹰弹匣 | `deagle_mag` | Desert Eagle | 50copper | 7 | smallmagazine |
| 5 | Glock 17 弹匣 | `glock17_mag` | Glock 17 | 919pso | 17 | smallmagazine |
| 6 | M4A1 弹匣 | `m4a1_mag` | M4A1 | 55645fmj | 30 | riflemagazine |
| 7 | P90 弹匣 | `p90_mag` | P90 | 5728sb193 | 50 | riflemagazine |

## 构建与部署

### 前置条件

1. 安装 .NET SDK 9.0+
2. 安装游戏 Casualties: Unknown Demo
3. 克隆本仓库和 [CUTarkovMedicalMod](https://github.com/hmm1313133/CUTarkovMedicalMod) 到同一父目录

### 配置游戏路径

在父目录创建 `vars.targets` 文件（参考医疗模组的 `vars.targets`）：

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
├── CUTarkovWeaponMod.csproj               # 项目文件（引用医疗模组）
├── Lang/
│   ├── EN.json                           # 英文翻译
│   └── zh_CN.json                        # 中文翻译
└── Framework/
    ├── WeaponItemRegistration.cs          # 枪械注册到医疗模组系统
    ├── ScopeZoomPatch.cs                  # AXMC 瞄准镜视野扩展
    ├── AKMItemSystem.cs                   # AKM 突击步枪
    ├── AXMCItemSystem.cs                  # AXMC 狙击步枪
    ├── DVL10ItemSystem.cs                 # DVL-10 狙击步枪
    ├── M4A1ItemSystem.cs                  # M4A1 卡宾枪
    ├── P90ItemSystem.cs                   # P90 冲锋枪
    ├── MP133ItemSystem.cs                 # MP-133 霰弹枪
    ├── SKSItemSystem.cs                   # SKS 卡宾枪
    ├── DeagleItemSystem.cs                # 沙漠之鹰
    ├── Glock17ItemSystem.cs               # Glock 17 手枪
    ├── AmmoItemSystem.cs                  # 所有自定义弹药定义
    ├── GunMagPatch.cs                     # 枪<->弹匣映射、LoadMag/UnloadMag 补丁
    ├── CaliberPatch.cs                    # 口径隔离补丁
    ├── CustomSpawnPatch.cs                # 世界生成枪械/弹匣掉落
    ├── VanillaBlockPatch.cs               # 封禁原版武器/弹药/弹匣
    ├── RecipePatch.cs                     # 自定义合成配方
    ├── RecipeSpawnPatch.cs                # 合成产出物品配置
    ├── RecipeSpritePatch.cs              # 合成配方图标
    ├── LocalePatch.cs                     # 弹药名称/描述注入游戏语言系统
    └── Assets/
        ├── *.png / *.webp                 # 枪械/弹药/弹匣图标
        └── guns/[key]/[key]_*.wav         # 枪械音效
```

## 技术要点

- **BepInDependency**: 硬依赖医疗模组 `com.yourname.cu.tarkovmedicalmod`
- **I18n 集成**: 通过 `I18n.RegisterExternalLangDir()` 将枪械翻译注入医疗模组的翻译系统
- **ConsoleSpawn 集成**: 通过 `ConsoleSpawnPatch.ExternalItemConfigurer` 回调注册枪械物品配置
- **InternalsVisibleTo**: 医疗模组通过 `InternalsVisibleTo` 暴露 internal 成员给武器模组
- **Harmony PatchAll**: 武器模组独立 PatchAll，与医疗模组互不干扰

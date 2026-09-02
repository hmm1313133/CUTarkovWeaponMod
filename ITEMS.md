# 武器模组物品数据总表

> 自动生成：从 `Framework/*.cs` 的 ItemSystem 常量 + SuppressorSystem/AimSystem 效果链提取。字段值尽量保留原始单位；倍率字段为乘算值（1.0=不变）。
> 减伤率由 `wearableArmor` 按 `a/(1+a)` 换算。
> 注：枪械射速/弹药类型等若未在下方条目中出现，表示继承自其克隆的原版预制体（如 rifle / shotgun 基础）。
> 食物效果为使用一次的变化量。

## 枪械 / Guns

### MPS Auto Assault-12 Gen 1 12铅径自动霰弹枪【AA-12】 / MPS Auto Assault-12 Gen 1 12-Gauge Auto Shotgun [AA-12]
- ID: `aa12`
- 弹容量: 20；后坐力: 6；生物伤害: 41；方块伤害: 30；噪音: 3.3；每发弹丸数: 8；垂直散布: 0.22；每发耐久损耗: 0.556；燃气时间: 0.21；弹药类型:Shotgun

### AKM 7.62x39 突击步枪【AKM】 / AKM 7.62x39 Assault Rifle [AKM]
- ID: `akm`
- 弹容量: 30；后坐力: 4.5；生物伤害: 120；方块伤害: 90；噪音: 3；每发弹丸数: 1；垂直散布: 0.08；每发耐久损耗: 0.3；燃气时间: 0.1；FiringModeOverride: 全自动

### Accuracy International AXMC .338 LM 栓动式狙击步枪【AXMC】 / Accuracy International AXMC .338 LM Bolt-Action Sniper Rifle [AXMC]
- ID: `axmc`
- 弹容量: 10；后坐力: 18；生物伤害: 310；方块伤害: 299；噪音: 6；每发弹丸数: 1；垂直散布: 0.05；每发耐久损耗: 0.7；燃气时间: 0；FiringModeOverride: 栓动/泵动

### Magnum Research "沙漠之鹰"L6 .50 AE手枪【沙漠之鹰L6】 / Magnum Research "Desert Eagle" L6 .50 AE Pistol[Deagle]
- ID: `deagle`
- 弹容量: 7；后坐力: 20；生物伤害: 110；方块伤害: 60；噪音: 5.5；每发弹丸数: 1；垂直散布: 0.17；每发耐久损耗: 0.9；燃气时间: 0.1

### DVL-10 7.62x51 栓动式狙击步枪【DVL-10】 / DVL-10 7.62x51 Bolt-Action Sniper Rifle [DVL-10]
- ID: `dvl10`
- 弹容量: 10；后坐力: 12；生物伤害: 205；方块伤害: 180；噪音: 4.5；每发弹丸数: 1；垂直散布: 0.07；每发耐久损耗: 0.6；燃气时间: 0.15；FiringModeOverride: 栓动/泵动

### GLOCK 17 9x19手枪【Glock17】 / GLOCK 17 9x19 Pistol[Glock17]
- ID: `glock17`
- 弹容量: 17；后坐力: 5；生物伤害: 32；方块伤害: 20；噪音: 2；每发弹丸数: 1；垂直散布: 0.15；每发耐久损耗: 0.7；燃气时间: 0.1

### Miller Bros. Blades M-2 战术剑【M-2】 / Miller Bros. Blades M-2 Tactical Sword [M-2]
- ID: `m2sword`
- ConditionLossPerAttack: 0.001

### 柯尔特 M4A1 5.56x45 卡宾枪【M4A1】 / Colt M4A1 5.56x45 Carbine[M4A1]
- ID: `m4a1`
- 弹容量: 30；后坐力: 3.7；生物伤害: 90；方块伤害: 70；噪音: 2.7；每发弹丸数: 1；垂直散布: 0.08；每发耐久损耗: 0.32；燃气时间: 0.09；FiringModeOverride: 全自动

### MP-133 12铅径泵动式霰弹枪【MP-133】 / MP-133 12-Gauge Pump-Action Shotgun [MP-133]
- ID: `mp133`
- 弹容量: 4；后坐力: 14；生物伤害: 40；方块伤害: 30；噪音: 4；每发弹丸数: 8；垂直散布: 0.18；每发耐久损耗: 2.0；燃气时间: 0

### MP-153 12铅径半自动霰弹枪【MP 153】 / MP-153 12-Gauge Semi-Auto Shotgun [MP 153]
- ID: `mp153`
- 弹容量: 8；后坐力: 12；生物伤害: 41；方块伤害: 30；噪音: 4；每发弹丸数: 8；垂直散布: 0.2；每发耐久损耗: 1.0；燃气时间: 0.09；射击模式:1

### FN P90 5.7x28 冲锋枪【P90】 / FN P90 5.7x28 SMG[P90]
- ID: `p90`
- 弹容量: 50；后坐力: 3.1；生物伤害: 50；方块伤害: 35；噪音: 1.9；每发弹丸数: 1；垂直散布: 0.12；每发耐久损耗: 0.28；燃气时间: 0.08；FiringModeOverride: 全自动

### Red Rebel冰镐【Red Rebel】 / Red Rebel Ice Axe [Red Rebel]
- ID: `redrebel`
- ConditionLossPerAttack: 0.0010157895

### RPD 7.62x39 轻机枪【RPD】 / RPD 7.62x39 LMG [RPD]
- ID: `rpd`
- 弹容量: 100；后坐力: 4.8；生物伤害: 100；方块伤害: 70；噪音: 3；每发弹丸数: 1；垂直散布: 0.15；每发耐久损耗: 0.36；燃气时间: 0.1；FiringModeOverride: 全自动

### 西蒙诺夫 SKS 7.62x39 卡宾枪【SKS】 / Simonov SKS 7.62x39 Carbine [SKS]
- ID: `sks`
- 弹容量: 10；后坐力: 6；生物伤害: 150；方块伤害: 100；噪音: 2.9；每发弹丸数: 1；垂直散布: 0.1；每发耐久损耗: 0.6；燃气时间: 0.1

### HK UMP 45冲锋枪【UMP 45】 / HK UMP 45 SMG [UMP 45]
- ID: `ump45`
- 弹容量: 25；后坐力: 2.8；生物伤害: 44；方块伤害: 27；噪音: 2.2；每发弹丸数: 1；垂直散布: 0.12；每发耐久损耗: 0.33；燃气时间: 0.1；FiringModeOverride: 全自动

### HK USP .45 ACP手枪【USP】 / HK USP .45 ACP Pistol [USP]
- ID: `usp`
- 弹容量: 12；后坐力: 3.7；生物伤害: 40；方块伤害: 42；噪音: 2.2；每发弹丸数: 1；垂直散布: 0.15；每发耐久损耗: 0.4；燃气时间: 0.1

### VSS “绞丝机” 9x39 特种狙击步枪【VSS】 / VSS "Vintorez" 9x39 Special Sniper Rifle [VSS]
- ID: `vss`
- 弹容量: 30；后坐力: 2.8；生物伤害: 105；方块伤害: 88；噪音: 0.32；每发弹丸数: 1；垂直散布: 0.07；每发耐久损耗: 0.5；燃气时间: 0.08；FiringModeOverride: 全自动

## 弹药 / Ammo

### 7.62x51毫米 BPZ FMJ 步枪弹 / 7.62x51mm BPZ FMJ Rifle Round
- ID: `76251bpz`
- AmmoTypeEnum: GunScript.AmmoType.Rifle；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### 7.62x39毫米 SP 步枪弹 / 7.62x39mm SP Rifle Round
- ID: `76239sp`
- AmmoTypeEnum: GunScript.AmmoType.Rifle；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### 12/70 Magnum 8.5毫米鹿弹 / 12/70 Magnum 8.5mm Buckshot
- ID: `12g85`
- AmmoTypeEnum: GunScript.AmmoType.Shotgun；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### .338 Lapua Magnum UCW 步枪弹 / .338 Lapua Magnum UCW Rifle Round
- ID: `338ucw`
- AmmoTypeEnum: GunScript.AmmoType.Rifle；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### .50 AE 实心铜弹 / .50 AE Solid Copper
- ID: `50copper`
- AmmoTypeEnum: GunScript.AmmoType.Pistol；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### .45 ACP FMJ 子弹 / .45 ACP FMJ
- ID: `45fmj`
- AmmoTypeEnum: GunScript.AmmoType.Pistol；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### 9x19毫米 PSO gzh子弹 / 9x19mm PSO gzh
- ID: `919pso`
- AmmoTypeEnum: GunScript.AmmoType.Pistol；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### 5.56x45毫米 FMJ子弹 / 5.56x45mm FMJ
- ID: `55645fmj`
- AmmoTypeEnum: GunScript.AmmoType.Rifle；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### 5.7x28毫米 SB193子弹 / 5.7x28mm SB193
- ID: `5728sb193`
- AmmoTypeEnum: GunScript.AmmoType.Rifle；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

### 9x39毫米 SP-5弹 / 9x39mm SP-5
- ID: `939sp5`
- AmmoTypeEnum: GunScript.AmmoType.Rifle；弹药种类:Round；最大堆叠/弹容:1；初始数量:1

## 弹匣 / Magazines

### "Big Stick" Glock 9x19加长弹匣【Big Stick】 / "Big Stick" Glock 9x19 Extended Magazine[Big Stick]
- ID: `bigstick_mag`
- 最大弹容量: 33；弹药种类:Magazine；适配枪型:Pistol；最大堆叠/弹容:MaxRounds；初始数量:0

### SGMT Glock 9x19 50发弹鼓【G 50发】 / SGMT Glock 9x19 50-Round Drum[G 50-Round]
- ID: `g50_mag`
- 最大弹容量: 50；弹药种类:Magazine；适配枪型:Pistol；最大堆叠/弹容:MaxRounds；初始数量:0

### AXMC .338 LM 弹匣【10发】 / AXMC .338 LM Magazine [10-Round]
- ID: `axmc_mag`
- 最大弹容量: 10；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### DVL-10 弹匣【10发】 / DVL-10 Magazine [10-Round]
- ID: `dvl10_mag`
- 最大弹容量: 10；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### AKM 30发弹匣 / AKM 30-Round Magazine
- ID: `akm_mag`
- 最大弹容量: 30；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### 沙漠之鹰.50 AE手枪弹匣 / Desert Eagle Magazine [7-Round]
- ID: `deagle_mag`
- 最大弹容量: 7；弹药种类:Magazine；适配枪型:Pistol；最大堆叠/弹容:MaxRounds；初始数量:0

### GLOCK 17弹匣【17发】 / GLOCK 17 Magazine [17-Round]
- ID: `glock17_mag`
- 最大弹容量: 17；弹药种类:Magazine；适配枪型:Pistol；最大堆叠/弹容:MaxRounds；初始数量:0

### M4A1弹匣【30发】 / M4A1 Magazine [30-Round]
- ID: `m4a1_mag`
- 最大弹容量: 30；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### P90弹匣【50发】 / P90 Magazine [50-Round]
- ID: `p90_mag`
- 最大弹容量: 50；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### UMP 45弹匣【25发】 / UMP 45 Magazine [25-Round]
- ID: `ump45_mag`
- 最大弹容量: 25；弹药种类:Magazine；适配枪型:Pistol；最大堆叠/弹容:MaxRounds；初始数量:0

### RPD 100发弹链盒 / RPD 100-Round Belt Drum
- ID: `rpd_mag`
- 最大弹容量: 100；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### USP .45 ACP 弹匣【12发】 / USP .45 ACP Magazine [12-Round]
- ID: `usp_mag`
- 最大弹容量: 12；弹药种类:Magazine；适配枪型:Pistol；最大堆叠/弹容:MaxRounds；初始数量:0

### VSS弹匣【30发】 / VSS Magazine [30-Round]
- ID: `vss_mag`
- 最大弹容量: 30；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### AA-12 20发弹鼓 / AA-12 20-Round Drum Magazine
- ID: `aa12_mag`
- 最大弹容量: 20；弹药种类:Magazine；适配枪型:Shotgun；最大堆叠/弹容:MaxRounds；初始数量:0

### SureFire MAG5-60 5.56x45 STANAG 60发弹匣【MAG5-60】 / SureFire MAG5-60 5.56x45 STANAG 60-Round Magazine [MAG5-60]
- ID: `mag560`
- 最大弹容量: 60；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### ProMag SKS-A5 7.62x39 20发SKS弹匣【SKS-A5】 / ProMag SKS-A5 7.62x39 20-Round SKS Magazine [SKS-A5]
- ID: `sks_a5_mag`
- 最大弹容量: 20；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

### SKS 10发弹仓【SKS 10发弹仓】 / SKS 10-Round Integral Magazine [SKS 10-Round]
- ID: `sks_integral_mag`
- 重量: 0.3；价值: 15；识别智力: 4

### X Products 7.62x39 AK X-47 50发弹鼓【X-47 7.62】 / X Products 7.62x39 AK X-47 50-round Drum [X-47 7.62]
- ID: `x47mag`
- 最大弹容量: 50；弹药种类:Magazine；适配枪型:Rifle；最大堆叠/弹容:MaxRounds；初始数量:0

## 配件 / Attachments

### 枪口 / Muzzle

### SilencerCo AC-858 ASR .338 LM 膛口制退器【AC-858】 / SilencerCo AC-858 ASR .338 LM Muzzle Brake [AC-858]
- ID: `ac858`
- 后坐力倍率: 0.77；瞄准速度变化(秒): 0.05；重量: 0.4；价值: 50；识别智力: 6；效果: 后坐力倍率: 0.77f（-23.0%）；瞄准速度变化: +0.05s

### Zenit DTK-1 7.62x39 & 5.45x39 AK 膛口制退器【DTK-1】 / Zenit DTK-1 7.62x39 & 5.45x39 AK Muzzle Brake [DTK-1]
- ID: `dtk1`
- 后坐力倍率: 0.88；重量: 0.3；价值: 35；识别智力: 4；效果: 后坐力倍率: 0.88f（-12.0%）；瞄准速度变化: +0.1s

### Zenit DTK-4M 7.62x39 AKM 消音器【DTK-4M】 / Zenit DTK-4M 7.62x39 AKM Suppressor [DTK-4M]
- ID: `dtk4m`
- 后坐力倍率: 0.924；噪音倍率: 0.55；散布倍率: 1.02；瞄准速度变化(秒): 0.62；重量: 0.4；价值: 45；识别智力: 5；效果: 后坐力倍率: 0.924f（-7.6%）；散布倍率: 1.02f（2.0%）；噪音倍率: 0.55f（-45.0%）；瞄准速度变化: +0.62s

### Hexagon DTKP MK.2 7.62x39 消音器【DTKP】 / Hexagon DTKP MK.2 7.62x39 Suppressor [DTKP]
- ID: `dtkp`
- 后坐力倍率: 0.96；噪音倍率: 0.5；散布倍率: 1.045；瞄准速度变化(秒): 0.32；重量: 0.5；价值: 50；识别智力: 5；效果: 后坐力倍率: 0.96f（-4.0%）；散布倍率: 1.045f（4.5%）；噪音倍率: 0.5f（-50.0%）；瞄准速度变化: +0.32s

### DVL-10 7.62x51 500 毫米消音枪管枪口组合【DVL-10 silenced】 / DVL-10 7.62x51 500mm Suppressed Barrel & Muzzle [DVL-10 silenced]
- ID: `dvl10_silenced`
- 后坐力倍率: 0.85；散布倍率: 0.95；瞄准速度变化(秒): -0.25；重量: 1.2；价值: 80；识别智力: 7；效果: 后坐力倍率: 0.85f（-15.0%）；散布倍率: 0.95f（-5.0%）；瞄准速度变化: -0.25s

### Spike Tactical Dynacomp 7.62x39 AK 膛口制退器【Dynacomp】 / Spike Tactical Dynacomp 7.62x39 AK Muzzle Brake [Dynacomp]
- ID: `dynacomp`
- 后坐力倍率: 0.92；重量: 0.35；价值: 28；识别智力: 4；效果: 后坐力倍率: 0.92f（-8.0%）；瞄准速度变化: +0.05s

### Decelerator 3 Port 9x19补偿器【G 3 Port】 / Decelerator 3 Port 9x19 Compensator[G 3 Port]
- ID: `g3port`
- 瞄准速度变化(秒): 0.05；后坐力倍率: 0.90；重量: 0.1；价值: 40；识别智力: 5；效果: 瞄准速度变化: +0.05s

### Lone Wolf 9 9x19补偿器【LW 9】 / Lone Wolf 9 9x19 Compensator[LW 9]
- ID: `lw9`
- 瞄准速度变化(秒): 0.08；后坐力倍率: 0.88；重量: 0.1；价值: 45；识别智力: 5；效果: 瞄准速度变化: +0.08s

### SilencerCo Osprey 9 9x19毫米抑制器【Osprey 9】 / SilencerCo Osprey 9 9x19mm Suppressor[Osprey 9]
- ID: `osprey9`
- 瞄准速度变化(秒): 0.22；后坐力倍率: 0.93；噪音倍率: 0.40；耐久损耗倍率: 1.068；重量: 0.3；价值: 80；识别智力: 5；效果: 瞄准速度变化: +0.22s

### Sig SRD 9 9x19毫米声音抑制器【SRD 9】 / Sig SRD 9 9x19mm Sound Suppressor[SRD 9]
- ID: `srd9`
- 瞄准速度变化(秒): 0.42；后坐力倍率: 0.98；噪音倍率: 0.70；耐久损耗倍率: 1.008；重量: 0.3；价值: 75；识别智力: 5；效果: 瞄准速度变化: +0.42s

### CGS Hekate DT .338 LM 消音器【Hekate DT .338】 / CGS Hekate DT .338 LM Suppressor [Hekate DT .338]
- ID: `hekate_dt338`
- 后坐力倍率: 0.95；噪音倍率: 0.35；瞄准速度变化(秒): 0.35；重量: 0.6；价值: 60；识别智力: 6；效果: 后坐力倍率: 0.95f（-5.0%）；噪音倍率: 0.35f（-65.0%）；瞄准速度变化: +0.35s

### Hexagon AKM 7.62x39 消音器【Hexagon AKM】 / Hexagon AKM 7.62x39 Suppressor [Hexagon AKM]
- ID: `hexagonakm`
- 重量: 0.5；价值: 25；识别智力: 4；效果: 后坐力倍率: 0.985f（-1.5%）；散布倍率: 1.05f（5.0%）；噪音倍率: 0.35f（-65.0%）；耐久损耗倍率: 1.10f（10.0%）；瞄准速度变化: +0.5s

### Hexagon SKS 7.62x39 声音抑制器【Hexagon SKS】 / Hexagon SKS 7.62x39 Sound Suppressor [Hexagon SKS]
- ID: `hexagon_sks`
- 后坐力倍率: 0.987；噪音倍率: 0.35；散布倍率: 1.05；瞄准速度变化(秒): 0.62；重量: 0.5；价值: 40；识别智力: 5；效果: 后坐力倍率: 0.987f（-1.3%）；散布倍率: 1.05f（5.0%）；噪音倍率: 0.35f（-65.0%）；瞄准速度变化: +0.62s

### KAC QDSS NT-4 5.56x45 消音器 (FDE)【NT-4】 / KAC QDSS NT-4 5.56x45 Suppressor (FDE) [NT-4]
- ID: `nt4`
- 后坐力倍率: 0.94；噪音倍率: 0.5；散布倍率: 1.01；瞄准速度变化(秒): 1.0；ConditionLossMult: 1.09；重量: 0.6；价值: 45；识别智力: 5；效果: 后坐力倍率: 0.94f（-6.0%）；散布倍率: 1.01f（1.0%）；噪音倍率: 0.5f（-50.0%）；耐久损耗倍率: 1.09f（9.0%）；瞄准速度变化: +1s

### SilencerCo SAKER ASR 556 5.56x45 消音器【SAKER ASR 556】 / SilencerCo SAKER ASR 556 5.56x45 Suppressor [SAKER ASR 556]
- ID: `sakerasr556`
- 后坐力倍率: 0.915；噪音倍率: 0.52；散布倍率: 1.022；ConditionLossMult: 1.075；瞄准速度变化(秒): 0.65；重量: 0.65；价值: 48；识别智力: 5；效果: 后坐力倍率: 0.915f（-8.5%）；散布倍率: 1.022f（2.2%）；噪音倍率: 0.52f（-48.0%）；耐久损耗倍率: 1.075f（7.5%）；瞄准速度变化: +0.65s

### Noveske KX3 5.56x45 AR-15 消焰器【KX3】 / Noveske KX3 5.56x45 AR-15 Flash Hider [KX3]
- ID: `kx3`
- 后坐力倍率: 0.95；ConditionLossMult: 0.95；瞄准速度变化(秒): 0.15；重量: 0.3；价值: 30；识别智力: 4；效果: 后坐力倍率: 0.95f（-5.0%）；耐久损耗倍率: 0.95f（-5.0%）；瞄准速度变化: +0.15s

### Vendetta Precision VP-09 Interceptor 5.56x45 AR-15 膛口制退器【VP-09】 / Vendetta Precision VP-09 Interceptor 5.56x45 AR-15 Muzzle Brake [VP-09]
- ID: `vp09`
- 后坐力倍率: 0.925；瞄准速度变化(秒): 0.12；重量: 0.35；价值: 32；识别智力: 4；效果: 后坐力倍率: 0.925f（-7.5%）；瞄准速度变化: +0.12s

### FN P90 Attenuator 5.7x28消音器【Attenuator】 / FN P90 Attenuator 5.7x28 Suppressor[Attenuator]
- ID: `p90attenuator`
- 瞄准速度变化(秒): 0.3；后坐力倍率: 0.90；重量: 0.3；价值: 70；识别智力: 5；效果: 后坐力倍率: 0.90（-10%）；瞄准速度变化: +0.3s

### Rotor 43 7.62x39 消音器【Rotor43 7.62x39】 / Rotor 43 7.62x39 Suppressor [Rotor43 7.62x39]
- ID: `rotor43762`
- 后坐力倍率: 0.97；噪音倍率: 0.5；ConditionLossMult: 1.15；瞄准速度变化(秒): 0.9；重量: 0.55；价值: 42；识别智力: 5；效果: 后坐力倍率: 0.97f（-3.0%）；噪音倍率: 0.5f（-50.0%）；耐久损耗倍率: 1.15f（15.0%）；瞄准速度变化: +0.9s

### Rotor 43 5.56x45 消音器【Rotor43 556】 / Rotor 43 5.56x45 Suppressor [Rotor43 556]
- ID: `rotor43`
- 后坐力倍率: 0.975；噪音倍率: 0.6；ConditionLossMult: 1.12；瞄准速度变化(秒): 0.6；重量: 0.55；价值: 40；识别智力: 5；效果: 后坐力倍率: 0.975f（-2.5%）；噪音倍率: 0.6f（-40.0%）；耐久损耗倍率: 1.12f（12.0%）；瞄准速度变化: +0.6s

### SRVV 7.62x39 AK 膛口制退器【SRVV AKM】 / SRVV 7.62x39 AK Muzzle Brake [SRVV AKM]
- ID: `srvvakm`
- 后坐力倍率: 0.89；瞄准速度变化(秒): 0.12；重量: 0.3；价值: 40；识别智力: 5；效果: 后坐力倍率: 0.89f（-11.0%）；瞄准速度变化: +0.12s

### AI .338 LM 战术型枪口制退器【TMB 338LM】 / AI .338 LM Tactical Muzzle Brake [TMB 338LM]
- ID: `tmb338lm`
- 后坐力倍率: 0.795；瞄准速度变化(秒): 0.06；重量: 0.4；价值: 55；识别智力: 6；效果: 后坐力倍率: 0.795f（-20.5%）；瞄准速度变化: +0.06s

### AI .338 LM 战术声音抑制器【TSM .338LM】 / AI .338 LM Tactical Sound Suppressor [TSM .338LM]
- ID: `tsm338lm`
- 后坐力倍率: 0.945；噪音倍率: 0.47；瞄准速度变化(秒): 0.32；重量: 0.6；价值: 65；识别智力: 6；效果: 后坐力倍率: 0.945f（-5.5%）；噪音倍率: 0.47f（-53.0%）；瞄准速度变化: +0.32s

### B&T OEM .45 ACP UMP 消音器【UMP OEM】 / B&T OEM .45 ACP UMP Suppressor [UMP OEM]
- ID: `ump_oem`
- 瞄准速度变化(秒): 0.15；后坐力倍率: 0.93；噪音倍率: 0.40；重量: 0.35；价值: 85；识别智力: 6；效果: 后坐力倍率: 0.93f（-7.0%）；噪音倍率: 0.40f（-60.0%）；瞄准速度变化: +0.15s

### SKS Weapon Tuning 7.62x39 螺纹转换器【WT0032-1】 / SKS Weapon Tuning 7.62x39 Thread Adapter [WT0032-1]
- ID: `wt0032_1`
- 重量: 0.2；价值: 30；识别智力: 5

### 护木 / Handguards

### ADAR 2-15 AR-15 兼容木质枪托【2-15木制】 / ADAR 2-15 AR-15 Compatible Wooden Handguard [2-15 Wood]
- ID: `adarwood`
- ConditionLossMult: 1.01；重量: 0.5；价值: 30；识别智力: 4；效果: 耐久损耗倍率: 1.01f（1.0%）；瞄准速度变化: -0.25s

### TDI AKM-L 护木（电镀红）【AKM-L】 / TDI AKM-L Handguard (Anodized Red) [AKM-L]
- ID: `akml`
- ConditionLossMult: 0.97；重量: 0.45；价值: 40；识别智力: 4；效果: 耐久损耗倍率: 0.97f（-3.0%）；瞄准速度变化: -0.35s

### Zenit B-10M 导轨护木 + B-19 上导轨组合【B10M+B19】 / Zenit B-10M 导轨护木 + B-19 上导轨组合【B10M+B19】
- ID: `b10mb19`
- 后坐力倍率: 0.997；ConditionLossMult: 0.95；重量: 0.5；价值: 45；识别智力: 4；效果: 后坐力倍率: 0.997f（-0.3%）；耐久损耗倍率: 0.95f（-5.0%）；瞄准速度变化: -0.12s

### Hexagon AK 管状护木（Anodized Red）【Hexagon AK】 / Hexagon AK Tubular Handguard (Anodized Red) [Hexagon AK]
- ID: `hexagonak_hg`
- 后坐力倍率: 0.995；ConditionLossMult: 0.95；重量: 0.35；价值: 32；识别智力: 4；效果: 后坐力倍率: 0.995f（-0.5%）；耐久损耗倍率: 0.95f（-5.0%）；瞄准速度变化: -0.3s

### KAC RIS AR-15 护木【KAC RIS】 / KAC RIS AR-15 Handguard [KAC RIS]
- ID: `kacris`
- 重量: 0.4；价值: 36；识别智力: 4；效果: 瞄准速度变化: -0.15s

### War Sport LVOA-S AR-15 护木（黑色）【LVOA-S】 / War Sport LVOA-S AR-15 Handguard (Black) [LVOA-S]
- ID: `lvoa`
- ConditionLossMult: 0.985；重量: 0.42；价值: 45；识别智力: 4；效果: 耐久损耗倍率: 0.985f（-1.5%）；瞄准速度变化: -0.5s

### Magpul MOE AKM 护木 (FDE)【MOE AKM】 / Magpul MOE AKM Handguard (FDE) [MOE AKM]
- ID: `moeakm`
- 后坐力倍率: 0.99；ConditionLossMult: 0.97；重量: 0.4；价值: 30；识别智力: 4；效果: 后坐力倍率: 0.99f（-1.0%）；耐久损耗倍率: 0.97f（-3.0%）；瞄准速度变化: -0.2s

### Magpul MOE SL 卡宾枪长度 M-LOK AR15 护木【MOE SL】 / Magpul MOE SL M-LOK AR15 Handguard [MOE SL]
- ID: `moesl`
- 后坐力倍率: 0.997；重量: 0.4；价值: 35；识别智力: 4；效果: 后坐力倍率: 0.997f（-0.3%）；瞄准速度变化: -0.14s

### Geissele SMR MK16 13.5 英寸 AR-15 M-LOK 护木 (DDC)【SMR Mk.16 13.5】 / Geissele SMR MK16 13.5-inch AR-15 M-LOK Handguard (DDC) [SMR Mk.16 13.5]
- ID: `smrmk16`
- ConditionLossMult: 0.99；重量: 0.45；价值: 42；识别智力: 4；效果: 耐久损耗倍率: 0.99f（-1.0%）；瞄准速度变化: -0.52s

### Strike Industries Viper 卡宾枪规格 AR-15 M-LOK 护木 (FDE)【Viper】 / Strike Industries Viper Carbine-Length AR-15 M-LOK Handguard (FDE) [Viper]
- ID: `viper`
- 后坐力倍率: 0.997；ConditionLossMult: 0.988；重量: 0.4；价值: 38；识别智力: 4；效果: 后坐力倍率: 0.997f（-0.3%）；耐久损耗倍率: 0.988f（-1.2%）；瞄准速度变化: -0.1s

### CAF WASR-10/63 木制握把护木【WASR】 / CAF WASR-10/63 木制握把护木【WASR】
- ID: `wasr`
- 后坐力倍率: 0.977；ConditionLossMult: 1.005；重量: 0.6；价值: 42；识别智力: 4；效果: 后坐力倍率: 0.977f（-2.3%）；耐久损耗倍率: 1.005f（0.5%）；瞄准速度变化: -0.17s

### 枪管 / Barrels

### M4A1 加长枪管【加长枪管】 / M4A1 Long Barrel [Long Barrel]
- ID: `m4longbarrel`
- 后坐力倍率: 0.94；散布倍率: 0.90；DamageBonus: 10；重量: 0.5；价值: 45；识别智力: 5；效果: 瞄准速度变化: +0.6s

### 枪托 / Stocks

### Hera Arms CQR47 AKM/AK-74 一体式枪托【CQR47】 / Hera Arms CQR47 AKM/AK-74 Integrated Stock [CQR47]
- ID: `cqr47`
- 后坐力倍率: 0.75；散布倍率: 0.98；重量: 0.6；价值: 65；识别智力: 4；IconPixelsPerUnit: 11；效果: 后坐力倍率: 0.75f（-25.0%）；散布倍率: 0.98f（-2.0%）；瞄准速度变化: -1.2s

### Hexagon"烧火棍"AKM/AK-74 枪托（电镀红）【Kocherga】 / Hexagon AKM/AK-74 Stock (Anodized Red) [Kocherga]
- ID: `kocherga`
- 后坐力倍率: 0.83；散布倍率: 0.98；重量: 0.55；价值: 50；识别智力: 4；效果: 后坐力倍率: 0.83f（-17.0%）；散布倍率: 0.98f（-2.0%）；瞄准速度变化: -0.5s

### Strike Industries Viper Mod 1 AR-15 枪托【Viper Mod.1】 / Strike Industries Viper Mod 1 AR-15 Stock [Viper Mod.1]
- ID: `vipermod1`
- 后坐力倍率: 1.10；瞄准速度变化(秒): -0.85；重量: 0.3；价值: 30；识别智力: 4；效果: 后坐力倍率: 1.10f（10.0%）；瞄准速度变化: -0.85s

### Magpul CTR AR-15 卡宾枪托（黑色）【CTR】 / Magpul CTR AR-15 Carbine Stock (Black) [CTR]
- ID: `ctr`
- 后坐力倍率: 0.82；散布倍率: 0.975；瞄准速度变化(秒): -0.5；重量: 0.35；价值: 34；识别智力: 4；效果: 后坐力倍率: 0.82f（-18.0%）；散布倍率: 0.975f（-2.5%）；瞄准速度变化: -0.5s

### KRISS Defiance DS150 枪托 (FDE)【DS150 FDE】 / KRISS Defiance DS150 Stock (FDE) [DS150 FDE]
- ID: `ds150fde`
- 后坐力倍率: 0.835；散布倍率: 0.97；瞄准速度变化(秒): -0.44；重量: 0.36；价值: 36；识别智力: 4；效果: 后坐力倍率: 0.835f（-16.5%）；散布倍率: 0.97f（-3.0%）；瞄准速度变化: -0.44s

### Magpul ACS AR-15 卡宾枪托 (FDE)【ACS】 / Magpul ACS AR-15 Carbine Stock (FDE) [ACS]
- ID: `acs`
- 后坐力倍率: 0.75；瞄准速度变化(秒): -0.25；重量: 0.4；价值: 38；识别智力: 4；效果: 后坐力倍率: 0.75f（-25.0%）；瞄准速度变化: -0.25s

### 带托垫的 Magpul MOE AR-15 卡宾枪托 (叶绿色)【MOE FG】 / Magpul MOE AR-15 Carbine Stock with Pad (Olive Green) [MOE FG]
- ID: `moefg`
- 后坐力倍率: 0.80；散布倍率: 0.975；瞄准速度变化(秒): -0.45；重量: 0.33；价值: 35；识别智力: 4；效果: 后坐力倍率: 0.80f（-20.0%）；散布倍率: 0.975f（-2.5%）；瞄准速度变化: -0.45s

### 带托垫的 Magpul MOE AR-15 卡宾枪托 (FDE)【MOE FDE】 / Magpul MOE AR-15 Carbine Stock with Pad (FDE) [MOE FDE]
- ID: `moefde`
- 后坐力倍率: 0.80；散布倍率: 0.975；瞄准速度变化(秒): -0.45；重量: 0.33；价值: 35；识别智力: 4；效果: 后坐力倍率: 0.80f（-20.0%）；散布倍率: 0.975f（-2.5%）；瞄准速度变化: -0.45s

### 带托垫的 Magpul MOE AR-15 卡宾枪托（哑光灰）【MOE SG】 / Magpul MOE AR-15 Carbine Stock with Pad (Matte Gray) [MOE SG]
- ID: `moesg`
- 后坐力倍率: 0.80；散布倍率: 0.975；瞄准速度变化(秒): -0.45；重量: 0.33；价值: 35；识别智力: 4；效果: 后坐力倍率: 0.80f（-20.0%）；散布倍率: 0.975f（-2.5%）；瞄准速度变化: -0.45s

### ProMag Archangel OPFOR AAK7 AK 枪托【OPFOR AA47】 / ProMag Archangel OPFOR AAK7 AK Stock [OPFOR AA47]
- ID: `opforaa47`
- 后坐力倍率: 0.75；散布倍率: 0.97；重量: 0.6；价值: 55；识别智力: 4；效果: 后坐力倍率: 0.75f（-25.0%）；散布倍率: 0.97f（-3.0%）；瞄准速度变化: -0.7s

### SKS ATI Monte Carlo 枪托【SKS MC】 / SKS ATI Monte Carlo Stock [SKS MC]
- ID: `sks_mc`
- 后坐力倍率: 0.90；瞄准速度变化(秒): -0.45；重量: 0.7；价值: 50；识别智力: 6；效果: 后坐力倍率: 0.90f（-10.0%）；瞄准速度变化: -0.45s

### SKS 7.62x39 卡宾枪 Tapco INTRAFUSE 套件组【Tapco intrafuse】 / SKS 7.62x39 Carbine Tapco INTRAFUSE Kit [Tapco intrafuse]
- ID: `tapco_intrafuse`
- KnockBackMultWithStock: 0.95；KnockBackMultNoStock: 1.26；ConditionLossMult: 0.98；AimTimeDeltaWithStock: -0.44；AimTimeDeltaNoStock: 0.6；重量: 0.9；价值: 60；识别智力: 6；效果: 后坐力倍率: 有后托 0.95（-5%）/ 无后托 1.26（+26%）；耐久损耗倍率: 0.98f（-2.0000000000000018%）

### SKS 7.62x39 卡宾枪 UAS 套件组【UAS SKS】 / SKS 7.62x39 Carbine UAS Kit [UAS SKS]
- ID: `uas_sks`
- 后坐力倍率: 0.70；ConditionLossMult: 0.93；瞄准速度变化(秒): -1.0；重量: 0.8；价值: 55；识别智力: 6；效果: 后坐力倍率: 0.70f（-30.0%）；耐久损耗倍率: 0.93f（-7.0%）；瞄准速度变化: -1s

### AKM/AK-74 Magpul Zhukov-S 枪托【Zhukov-S】 / AKM/AK-74 Magpul Zhukov-S Stock [Zhukov-S]
- ID: `zhukovs`
- 后坐力倍率: 0.81；散布倍率: 0.94；重量: 0.55；价值: 60；识别智力: 4；IconPixelsPerUnit: 11；效果: 后坐力倍率: 0.81f（-19.0%）；散布倍率: 0.94f（-6.0%）；瞄准速度变化: -1s

### 后握把 / Pistol Grips

### AK Custom Arms AGS-74 PRO + Sniper Kit 手枪式握把【AGS-74】 / AK Custom Arms AGS-74 PRO + Sniper Kit Pistol Grip [AGS-74]
- ID: `ags74`
- 后坐力倍率: 0.97；散布倍率: 0.97；重量: 0.3；价值: 32；识别智力: 4；效果: 后坐力倍率: 0.97f（-3.0%）；散布倍率: 0.97f（-3.0%）；瞄准速度变化: -0.3s

### AMXC 橡胶握把垫【握把垫】 / AMXC Rubber Grip Pad [Grip Pad]
- ID: `axmc_grip`
- 瞄准速度变化(秒): -0.65；重量: 0.1；价值: 20；识别智力: 4；效果: 瞄准速度变化: -0.65s

### Tactical Dynamics AR-15 镂空手枪式握把【TD120001】 / Tactical Dynamics AR-15 Skeletonized Pistol Grip [TD120001]
- ID: `td120001`
- 后坐力倍率: 0.985；瞄准速度变化(秒): -0.18；重量: 0.25；价值: 30；识别智力: 4；效果: 后坐力倍率: 0.985f（-1.5%）；瞄准速度变化: -0.18s

### Stark AR AR-15 手枪式握把 (FDE)【Stark AR RG】 / Stark AR AR-15 Pistol Grip (FDE) [Stark AR RG]
- ID: `starkarrg`
- 后坐力倍率: 0.98；瞄准速度变化(秒): -0.2；重量: 0.28；价值: 32；识别智力: 4；效果: 后坐力倍率: 0.98f（-2.0%）；瞄准速度变化: -0.2s

### Magpul MIAD AR-15 手枪式握把 (FDE)【MIAD手枪式】 / Magpul MIAD AR-15 Pistol Grip (FDE) [MIAD Pistol Grip]
- ID: `miad`
- 后坐力倍率: 0.99；瞄准速度变化(秒): -0.12；重量: 0.26；价值: 30；识别智力: 4；效果: 后坐力倍率: 0.99f（-1.0%）；瞄准速度变化: -0.12s

### F1 Firearms 镂空 2 型 AR-15 手枪式握把（缠线版本）【F1 St2 PC】 / F1 Firearms Skeletonized Type 2 AR-15 Pistol Grip (Wrapped) [F1 St2 PC]
- ID: `f1st2pc`
- 后坐力倍率: 0.98；散布倍率: 0.99；瞄准速度变化(秒): -0.65；重量: 0.22；价值: 34；识别智力: 4；效果: 后坐力倍率: 0.98f（-2.0%）；散布倍率: 0.99f（-1.0%）；瞄准速度变化: -0.65s

### HK Ergo PSG-1 样式 AR-15 手枪式握把【Ergo】 / HK Ergo PSG-1 Style AR-15 Pistol Grip [Ergo]
- ID: `ergo`
- 后坐力倍率: 0.974；散布倍率: 0.98；瞄准速度变化(秒): -0.3；重量: 0.3；价值: 36；识别智力: 4；效果: 后坐力倍率: 0.974f（-2.6%）；散布倍率: 0.98f（-2.0%）；瞄准速度变化: -0.3s

### KGB MG-47 AK 手枪式握把（电镀红）【MG-47】 / KGB MG-47 AK Pistol Grip (Anodized Red) [MG-47]
- ID: `mg47`
- 后坐力倍率: 0.98；重量: 0.3；价值: 28；识别智力: 4；效果: 后坐力倍率: 0.98f（-2.0%）

### Zenit RK-3 AK 手枪式握把【RK-3】 / Zenit RK-3 AK Pistol Grip [RK-3]
- ID: `rk3`
- 后坐力倍率: 0.975；散布倍率: 0.99；重量: 0.3；价值: 28；识别智力: 4；效果: 后坐力倍率: 0.975f（-2.5%）；散布倍率: 0.99f（-1.0%）；瞄准速度变化: -0.5s

### 前握把 / Foregrips

### Magpul AFG 战术握把（黑色）【AFG】 / Magpul AFG Tactical Grip (Black) [AFG]
- ID: `afg`
- 后坐力倍率: 0.98；重量: 0.16；价值: 34；识别智力: 4；效果: 后坐力倍率: 0.98f（-2.0%）；瞄准速度变化: -0.1s

### Zenit RK-1 B-25U 基座前握把【B-25U RK-1】 / Zenit RK-1 B-25U Base Foregrip [B-25U RK-1]
- ID: `b25ur1`
- 后坐力倍率: 0.963；重量: 0.22；价值: 40；识别智力: 4；效果: 后坐力倍率: 0.963f（-3.7%）；瞄准速度变化: -0.15s

### Strike Industries Cobra 战术前握把【Cobra】 / Strike Industries Cobra Tactical Foregrip [Cobra]
- ID: `cobra`
- 后坐力倍率: 0.995；重量: 0.14；价值: 33；识别智力: 4；效果: 后坐力倍率: 0.995f（-0.5%）；瞄准速度变化: -0.27s

### RTM Pillau P-2 战术前握把（红色）【P-2】 / RTM Pillau P-2 Tactical Foregrip (Red) [P-2]
- ID: `p2`
- 后坐力倍率: 0.99；重量: 0.12；价值: 36；识别智力: 4；效果: 后坐力倍率: 0.99f（-1.0%）；瞄准速度变化: -0.08s

### Zenit RK-0 前握把【RK-0】 / Zenit RK-0 Foregrip [RK-0]
- ID: `rk0`
- 后坐力倍率: 0.983；重量: 0.15；价值: 28；识别智力: 4；效果: 后坐力倍率: 0.983f（-1.7%）；瞄准速度变化: -0.08s

### Zenit RK-2 前握把【RK-2】 / Zenit RK-2 Foregrip [RK-2]
- ID: `rk2`
- 后坐力倍率: 0.955；重量: 0.16；价值: 32；识别智力: 4；效果: 后坐力倍率: 0.955f（-4.5%）；瞄准速度变化: +0.1s

### STARK SE-5 Express 握把【SE-5】 / STARK SE-5 Express Grip [SE-5]
- ID: `se5`
- 后坐力倍率: 0.99；重量: 0.18；价值: 30；识别智力: 4；效果: 后坐力倍率: 0.99f（-1.0%）；瞄准速度变化: -0.22s

### Fortis Shift 战术前握把【Shift】 / Fortis Shift Tactical Foregrip [Shift]
- ID: `shift`
- 后坐力倍率: 0.98；重量: 0.2；价值: 35；识别智力: 4；效果: 后坐力倍率: 0.98f（-2.0%）；瞄准速度变化: -0.12s

### 瞄准镜 / Sights

### Aimpoint ACRO P-1反射式瞄具【ACRO P-1】 / Aimpoint ACRO P-1 Reflex Sight[ACRO P-1]
- ID: `acrop1`
- IconSubPath: "guns/common/acro p-1.png"；散布倍率: 0.90；重量: 0.2；价值: 60；识别智力: 5

### Leupold DeltaPoint反射式瞄具【DP】 / Leupold DeltaPoint Reflex Sight[DP]
- ID: `dp`
- IconSubPath: "guns/common/DP.png"；散布倍率: 0.88；重量: 0.2；价值: 65；识别智力: 5

### EOTech 553 全息瞄具【553】 / EOTech 553 Holographic Sight [553]
- ID: `eotech553`
- IconSubPath: "guns/common/eotech553.png"；散布倍率: 0.84；DrainPerSecond: 1f / 10800；重量: 0.35；价值: 70；识别智力: 5；效果: 散布倍率: 0.84f（-16.0%）；瞄准速度变化: +0.2s

### EOTech HHS-1 复合瞄具【HHS-1】 / EOTech HHS-1 Composite Sight [HHS-1]
- ID: `hhs1`
- IconSubPath: "guns/common/hhs1.png"；散布倍率: 0.75；DrainPerSecond: 1f / 10800；重量: 0.4；价值: 85；识别智力: 5；效果: 散布倍率: 0.75f（-25.0%）；瞄准速度变化: +0.4s

### Monstrum 紧凑战术棱镜式瞄准镜 2x32【Monstr. 2x32】 / Monstrum Compact Tactical Prism Scope 2x32 [Monstr. 2x32]
- ID: `monstr2x32`
- IconSubPath: "guns/common/monstr2x32.png"；散布倍率: 0.85；重量: 0.35；价值: 55；识别智力: 5；效果: 瞄准速度变化: +0.2s

### Walther MRS 反射式瞄具【MRS】 / Walther MRS Reflex Sight [MRS]
- ID: `mrs`
- IconSubPath: "guns/common/mrs.png"；散布倍率: 0.90；DrainPerSecond: 1f / 7200；重量: 0.3；价值: 60；识别智力: 5；效果: 散布倍率: 0.90f（-10.0%）；瞄准速度变化: +0.1s

### Schmidt & Bender PM II 1-8x24 30 毫米步枪瞄准镜【PM II 1-8x24】 / Schmidt & Bender PM II 1-8x24 30mm Rifle Scope [PM II 1-8x24]
- ID: `pm2`
- IconSubPath: "guns/common/pm2.png"；散布倍率: 0.65；重量: 0.6；价值: 130；识别智力: 5；效果: 瞄准速度变化: +0.7s

### Vortex Razor HD Gen.2 1-6x24 30 毫米步枪瞄准镜【Razor HD Gen.2】 / Vortex Razor HD Gen.2 1-6x24 30mm Rifle Scope [Razor HD Gen.2]
- ID: `razorhd`
- IconSubPath: "guns/common/razorhd.png"；散布倍率: 0.65；重量: 0.55；价值: 110；识别智力: 5；效果: 瞄准速度变化: +0.5s

### ELCAN SpecterDR 1x/4x 瞄准镜 FDE【SpecterDR】 / ELCAN SpecterDR 1x/4x Scope FDE [SpecterDR]
- ID: `specterdr`
- IconSubPath: "guns/common/specterdr.png"；散布倍率: 0.75；重量: 0.45；价值: 95；识别智力: 5；效果: 散布倍率: 0.75f（-25.0%）；瞄准速度变化: +0.5s

### Trijicon ACOG TA01NSN 4x32 瞄准镜（黄褐色）【TA01NSN】 / Trijicon ACOG TA01NSN 4x32 Scope (Tan) [TA01NSN]
- ID: `ta01nsn`
- IconSubPath: "guns/common/ta01nsn.png"；散布倍率: 0.72；重量: 0.45；价值: 75；识别智力: 5；效果: 瞄准速度变化: +0.3s

### 战术设备 / Tactical Devices

### Olight Baldr Pro 战术手电激光组合【BaldrPro】 / Olight Baldr Pro Tactical Flashlight & Laser [BaldrPro]
- ID: `baldrpro`
- IconSubPath: "guns/common/baldrpro.png"；重量: 0.18；价值: 45；识别智力: 6；激光每秒耗电: 1f / 3600；手电每秒耗电: 1f / 2100；双开每秒耗电: 1f / 1800；激光距离: 14；激光宽度: 0.035；效果: 瞄准速度变化: +0.18s

### Zenit Klesch-2U 战术手电【Klesch-2U】 / Zenit Klesch-2U Tactical Flashlight [Klesch-2U]
- ID: `klesch2u`
- IconSubPath: "guns/common/2u.png"；重量: 0.15；价值: 30；识别智力: 6；DrainPerSecond: 1f / 660；效果: 瞄准速度变化: +0.25s

### LAS/TAC 2 战术手电【LAS/TAC 2】 / LAS/TAC 2 Tactical Flashlight [LAS/TAC 2]
- ID: `lastac2`
- IconSubPath: "guns/common/lastac2.png"；重量: 0.15；价值: 25；识别智力: 6；HighLightDrainPerSecond: 1f / 1200；LowLightDrainPerSecond: 1f / 2400；效果: 瞄准速度变化: +0.15s

### NcSTAR Tactical LAM模块 蓝色激光【TBL】 / NcSTAR Tactical LAM Blue Laser [TBL]
- ID: `tbl`
- IconSubPath: "guns/common/TBL.png"；重量: 0.12；价值: 35；识别智力: 6；DrainPerSecond: 1f / 4800；激光距离: 14；激光宽度: 0.09；效果: 瞄准速度变化: +0.1s

### 格洛克配件 / Glock Parts

### Glock 9x19 Lone Wolf AlphaWolf螺纹枪管【AW螺纹】 / Glock 9x19 Lone Wolf AlphaWolf Threaded Barrel[AW Thread]
- ID: `awlw`
- 重量: 0.2；价值: 50；识别智力: 5

### Polymer80 PS9 Glock套筒【PS9】 / Polymer80 PS9 Glock Slide[PS9]
- ID: `ps9`
- 瞄准速度变化(秒): -0.02；后坐力倍率: 0.995；重量: 0.2；价值: 45；识别智力: 5；效果: 瞄准速度变化: -0.02s

### UM Tactical UM3瞄具基座【UM3】 / UM Tactical UM3 Sight Mount[UM3]
- ID: `um3`
- 瞄准速度变化(秒): 0.1；重量: 0.15；价值: 55；识别智力: 5；效果: 瞄准速度变化: +0.1s

### Glock 9x19 Viper Cut套筒【Glock Viper Cut】 / Glock 9x19 Viper Cut Slide[Glock Viper Cut]
- ID: `vipercut`
- 瞄准速度变化(秒): -0.05；后坐力倍率: 0.98；耐久损耗倍率: 0.95；重量: 0.2；价值: 60；识别智力: 5；效果: 瞄准速度变化: -0.05s

### 其他配件 / Other Attachments

### SKS Leapers UTG PRO MTU017 机匣基座【MTU017】 / SKS Leapers UTG PRO MTU017 Receiver Base [MTU017]
- ID: `mtu017`
- 重量: 0.3；价值: 50；识别智力: 5

### FAB Defense PDC AK 导轨防尘盖【PDC】 / FAB Defense PDC AK Rail Dust Cover [PDC]
- ID: `pdc`
- ConditionLossMult: 0.995；重量: 0.25；价值: 45；识别智力: 4；效果: 耐久损耗倍率: 0.995f（-0.5%）

## 护甲/胸挂/背包 / Armor & Rigs & Backpacks

### 6B45 防弹胸挂 (医疗型)【6B45】 / 6B45 Armored Rig (Medical) [6B45]
- ID: `6b45`
- 护甲系数: 1.2371；重量: 4.4；被击中耐久损耗倍率: 0.19；保温值: 0.11；价值: 64；识别智力: 5；容器容量: 4；单物最大重量: 1.7；负重减免: 0.40；穿戴视觉偏移: 5；减伤率:55.3%

### 6B5-16 Zh-86 Uley 防弹胸挂（卡其色）【6B5-16】 / 6B5-16 Zh-86 Uley Armored Rig (Khaki) [6B5-16]
- ID: `6b516`
- 护甲系数: 0.4837；重量: 4；被击中耐久损耗倍率: 0.4；保温值: 0.16；价值: 26；识别智力: 5；容器容量: 1.3；单物最大重量: 1；负重减免: 0.60；穿戴视觉偏移: 5；减伤率:32.6%

### Crye Precision AVS 插板胸挂（Tagilla 版）【AVS TE】 / Crye Precision AVS Plate Carrier (Tagilla Edition) [AVS TE]
- ID: `avste`
- 护甲系数: 1.8653；重量: 4.8；被击中耐久损耗倍率: 0.24；保温值: 0.08；价值: 69；识别智力: 5；容器容量: 2；单物最大重量: 0.75；负重减免: 0.70；穿戴视觉偏移: 5；减伤率:65.1%

### Spiritus Systems Bank Robber 胸挂（高原复合迷彩）【Bank Robber】 / Spiritus Systems Bank Robber Chest Rig (Alpine Multicam) [Bank Robber]
- ID: `bankrobber`
- WearSlotId: "bandolier"；重量: 0.6；保温值: 0.02；价值: 24；识别智力: 3；容器容量: 2.5；单物最大重量: 1；负重减免: 0.54；穿戴视觉偏移: 6；DecayRatePerSecond: 1.0f / 21600.0

### WARTECH 金雕 BB-102 背包（A-TACSFG迷彩）【Berkut】 / WARTECH Berkut BB-102 Backpack (A-TACS FG) [Berkut]
- ID: `berkut`
- WearSlotId: "back"；重量: 0.6；保温值: 0.02；价值: 30；识别智力: 3；容器容量: 5；单物最大重量: 4；负重减免: 0.45；被击中耐久损耗倍率: 0；可撕裂属性: 6；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 21600.0

### BlackRock胸挂【BlackRock】 / BlackRock Chest Rig [BlackRock]
- ID: `blackrock`
- WearSlotId: "bandolier"；重量: 2；保温值: 0.04；价值: 41；识别智力: 4；容器容量: 4.7；单物最大重量: 1.8；负重减免: 0.35；穿戴视觉偏移: 6；DecayRatePerSecond: 1.0f / 25200.0

### Blackhawk! Commando胸挂（Desert Tan）【Commando】 / Blackhawk! Commando Chest Rig (Desert Tan) [Commando]
- ID: `commando`
- WearSlotId: "bandolier"；重量: 1.5；保温值: 0.02；价值: 34；识别智力: 5；容器容量: 4；单物最大重量: 1.2；负重减免: 0.45；穿戴视觉偏移: 6；DecayRatePerSecond: 1.0f / 27000.0

### LBT-8005A Day Pack 背包（黑系复合迷彩）【Day Pack】 / LBT-8005A Day Pack Backpack (Black Multicam) [Day Pack]
- ID: `daypack`
- WearSlotId: "back"；重量: 0.5；保温值: 0.02；价值: 30；识别智力: 3；容器容量: 5.5；单物最大重量: 3；负重减免: 0.44；被击中耐久损耗倍率: 0；可撕裂属性: 7；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 21600.0

### DRD防弹衣【DRD】 / DRD Body Armor [DRD]
- ID: `drd`
- WearSlotId: "outertorso"；护甲系数: 0.5576；重量: 1.5；被击中耐久损耗倍率: 0.27；保温值: 0.07；价值: 24；识别智力: 4；穿戴视觉偏移: 5；DecayRatePerSecond: 1.0f / 19800.0；减伤率:35.8%

### BNTI Gzhel-K（彩瓷-K）防弹衣【GZHEL-K】 / BNTI Gzhel-K Body Armor [GZHEL-K]
- ID: `gzhel_k`
- WearSlotId: "outertorso"；护甲系数: 1.4155；重量: 4.7；被击中耐久损耗倍率: 0.2；保温值: 0.08；价值: 53；识别智力: 5；穿戴视觉偏移: 5；减伤率:58.6%

### 5.11 Hexgrid 插板背心【HGrid】 / 5.11 Hexgrid Plate Carrier [HGrid]
- ID: `hgrid`
- WearSlotId: "outertorso"；护甲系数: 1.8653；重量: 2；被击中耐久损耗倍率: 0.35；保温值: 0.08；价值: 64；识别智力: 5；穿戴视觉偏移: 5；减伤率:65.1%

### Hexatac HPC 插板背心（黑系复合迷彩）【HPC】 / Hexatac HPC Plate Carrier (Black Multicam) [HPC]
- ID: `hpc`
- WearSlotId: "outertorso"；护甲系数: 1.6808；重量: 2.1；被击中耐久损耗倍率: 0.23；保温值: 0.04；价值: 50；识别智力: 4；穿戴视觉偏移: 5；减伤率:62.7%

### IDEA DIY胸挂【IDEA】 / IDEA DIY Chest Rig [IDEA]
- ID: `idea`
- WearSlotId: "bandolier"；重量: 0.2；保温值: 0.02；价值: 16；识别智力: 3；容器容量: 2；单物最大重量: 1；负重减免: 0.60；穿戴视觉偏移: 6；DecayRatePerSecond: 1.0f / 14400.0

### LBT-1961A 承重胸挂（MAS 灰色）【LBCR】 / LBT-1961A Load-Bearing Chest Rig (MAS Grey) [LBCR]
- ID: `lbcr`
- WearSlotId: "bandolier"；重量: 1.6；保温值: 0.02；价值: 36；识别智力: 5；容器容量: 4.2；单物最大重量: 2；负重减免: 0.40；穿戴视觉偏移: 6；DecayRatePerSecond: 1.0f / 27000.0

### 6LBT-2670 小型野战医物包【SFMP】 / 6LBT-2670 Small Field Medical Pack [SFMP]
- ID: `6lbt2670`
- WearSlotId: "back"；重量: 2.5；保温值: 0.02；价值: 45；识别智力: 3；容器容量: 10.0；单物最大重量: 3.0；负重减免: 0.40；被击中耐久损耗倍率: 0；可撕裂属性: 6；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 25200.0

### Spiritus Systems LV-119 插板胸挂 (黑色军团 V1)【LV-119】 / Spiritus Systems LV-119 Plate Carrier (Black Legion V1) [LV-119]
- ID: `lv119`
- 护甲系数: 1.8653；重量: 4.8；被击中耐久损耗倍率: 0.2；保温值: 0.11；价值: 74；识别智力: 5；容器容量: 4.4；单物最大重量: 2；负重减免: 0.34；穿戴视觉偏移: 5；减伤率:65.1%

### Eagle Allied Industries MBSS 插板胸挂（狼棕色）【MBSS】 / Eagle Allied Industries MBSS Plate Carrier (Coyote Brown) [MBSS]
- ID: `mbss`
- 护甲系数: 1.0；重量: 1.5；被击中耐久损耗倍率: 0.25；保温值: 0.1；价值: 35；识别智力: 5；容器容量: 2；单物最大重量: 1；穿戴视觉偏移: 5；减伤率:50.0%

### MF-UNTAR防弹背心【MF-UN】 / MF-UNTAR Bulletproof Vest [MF-UN]
- ID: `mfun`
- WearSlotId: "outertorso"；护甲系数: 0.5974；重量: 1.6；被击中耐久损耗倍率: 0.25；保温值: 0.07；价值: 24；识别智力: 4；穿戴视觉偏移: 5；DecayRatePerSecond: 1.0f / 19800.0；减伤率:37.4%

### CQC 鱼鹰 MK4A 防弹胸挂（突击型，多地形迷彩）【MK4A突击型】 / CQC Osprey MK4A Plate Carrier (Assault, Multicam) [MK4A Assault]
- ID: `mk4a`
- 护甲系数: 0.7857；重量: 3.7；被击中耐久损耗倍率: 0.15；保温值: 0.13；价值: 55；识别智力: 6；容器容量: 3.5；单物最大重量: 2；负重减免: 0.35；穿戴视觉偏移: 5；减伤率:44.0%

### Mystery Ranch 2 日突击包 (黑色)【2 Day Assault】 / Mystery Ranch 2 Day Assault Pack (Black) [2 Day Assault]
- ID: `mysteryranch2day`
- WearSlotId: "back"；重量: 0.8；保温值: 0.02；价值: 35；识别智力: 3；容器容量: 5；单物最大重量: 4.2；负重减免: 0.35；被击中耐久损耗倍率: 0；可撕裂属性: 7；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 21600.0

### PACA 软质防弹背心【PACA】 / PACA Soft Armor Vest [PACA]
- ID: `paca`
- WearSlotId: "outertorso"；护甲系数: 0.3231；重量: 1.3；被击中耐久损耗倍率: 0.3；保温值: 0.07；价值: 17；识别智力: 4；穿戴视觉偏移: 5；DecayRatePerSecond: 1.0f / 18000.0；减伤率:24.4%

### Partizan的包【Partizan】 / Partizan's Pack [Partizan]
- ID: `partizan`
- WearSlotId: "back"；重量: 0.4；保温值: 0.02；价值: 35；识别智力: 3；容器容量: 5.5；单物最大重量: 3；负重减免: 0.64；被击中耐久损耗倍率: 0；可撕裂属性: 6；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 21600.0

### Pilgrim旅行包【Pilgrim】 / Pilgrim Travel Backpack [Pilgrim]
- ID: `pilgrim`
- WearSlotId: "back"；重量: 1.6；保温值: 0.02；价值: 40；识别智力: 3；容器容量: 7；单物最大重量: 4.5；负重减免: 0.40；被击中耐久损耗倍率: 0；可撕裂属性: 8；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 21600.0

### Vertx Ready Pack背包（红色）【ReadyPack】 / Vertx Ready Pack Backpack (Red) [ReadyPack]
- ID: `readypack`
- WearSlotId: "back"；重量: 0.6；保温值: 0.02；价值: 20；识别智力: 2；容器容量: 4.8；单物最大重量: 2.2；负重减免: 0.50；被击中耐久损耗倍率: 0；可撕裂属性: 5；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 16920.0

### FORT Redut-T5（堡垒-T5）防弹衣（烟雾迷彩）【Redut-T5】 / FORT Redut-T5 Body Armor (Smoke Camo) [Redut-T5]
- ID: `redut_t5`
- WearSlotId: "outertorso"；护甲系数: 1.2831；重量: 5；被击中耐久损耗倍率: 0.15；保温值: 0.17；价值: 67；识别智力: 5；穿戴视觉偏移: 5；减伤率:56.2%

### Rys-T 防弹头盔（黑色）【Rys-T】 / Rys-T Ballistic Helmet (Black) [Rys-T]
- ID: `ryst`
- WearSlotId: "hat"；护甲系数: 3.381；重量: 1.2；被击中耐久损耗倍率: 0.33；保温值: 0.2；价值: 55；识别智力: 7；穿戴视觉偏移: 8；减伤率:77.2%

### Scav背包【Scavpack】 / Scav Backpack [Scavpack]
- ID: `scavpack`
- WearSlotId: "back"；重量: 0.9；保温值: 0.02；价值: 25；识别智力: 3；容器容量: 4.8；单物最大重量: 3；负重减免: 0.50；被击中耐久损耗倍率: 0；可撕裂属性: 5；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 18000.0

### 6SH118突击背包【6SH118】 / 6SH118 Assault Backpack [6SH118]
- ID: `6sh118`
- WearSlotId: "back"；重量: 3.0；保温值: 0.02；价值: 50；识别智力: 3；容器容量: 14.2；单物最大重量: 8.5；负重减免: 0.30；被击中耐久损耗倍率: 0；可撕裂属性: 10；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 31680.0

### First Spear Siege-R Optimized M.A.S.S. 插板胸挂 (黑色军团)【Siege-R】 / First Spear Siege-R Optimized M.A.S.S. Plate Carrier (Black Legion) [Siege-R]
- ID: `sieger`
- 护甲系数: 1.1978；重量: 5.2；被击中耐久损耗倍率: 0.13；保温值: 0.13；价值: 66；识别智力: 7；容器容量: 5.5；单物最大重量: 2；负重减免: 0.3；穿戴视觉偏移: 5；减伤率:54.5%

### 6B13 突击甲（丛林迷彩）【6B13】 / 6B13 Assault Armor (Jungle Camo) [6B13]
- ID: `6b13`
- WearSlotId: "outertorso"；护甲系数: 1.1978；重量: 3；被击中耐久损耗倍率: 0.21；保温值: 0.11；价值: 44；识别智力: 4；穿戴视觉偏移: 5；减伤率:54.5%

### 6B43 屏障-Sh 防弹衣（数码丛林迷彩）【6B43】 / 6B43 Zabralo-Sh Body Armor (Digital Jungle Camo) [6B43]
- ID: `6b43`
- WearSlotId: "outertorso"；护甲系数: 2.3333；重量: 6；被击中耐久损耗倍率: 0.14；保温值: 0.22；价值: 75；识别智力: 5；穿戴视觉偏移: 5；减伤率:70.0%

### LBT 6094A Slick 插板背心（黄褐色）【Slick】 / LBT 6094A Slick Plate Carrier (Coyote Tan) [Slick]
- ID: `slick`
- WearSlotId: "outertorso"；护甲系数: 1.8653；重量: 4.6；被击中耐久损耗倍率: 0.21；保温值: 0.08；价值: 66；识别智力: 5；穿戴视觉偏移: 5；减伤率:65.1%

### Stich Profi V2 插板胸挂（黑色）【SP PC V2】 / Stich Profi V2 Plate Carrier (Black) [SP PC V2]
- ID: `sppcv2`
- 护甲系数: 0.9531；重量: 2.7；被击中耐久损耗倍率: 0.17；保温值: 0.11；价值: 44；识别智力: 5；容器容量: 3；单物最大重量: 2；负重减免: 0.35；穿戴视觉偏移: 5；减伤率:48.8%

### SSO Attack 2 突击背包（卡其色）【Attack 2】 / SSO Attack 2 Assault Backpack (Khaki) [Attack 2]
- ID: `ssoattack2`
- WearSlotId: "back"；重量: 1.5；保温值: 0.02；价值: 42；识别智力: 3；容器容量: 7.2；单物最大重量: 5.5；负重减免: 0.34；被击中耐久损耗倍率: 0；可撕裂属性: 8；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 28080.0

### NFM THOR 隐蔽型强化防弹背心【THOR CRV】 / NFM THOR Concealable Enhanced Body Armor [THOR CRV]
- ID: `thor`
- WearSlotId: "outertorso"；护甲系数: 1.0877；重量: 2.7；被击中耐久损耗倍率: 0.15；保温值: 0.1；价值: 36；识别智力: 4；穿戴视觉偏移: 5；减伤率:52.1%

### HighCom Trooper TFO 防弹背心（复合迷彩）【Trooper】 / HighCom Trooper TFO Body Armor (Multicam) [Trooper]
- ID: `trooper`
- WearSlotId: "outertorso"；护甲系数: 1.1978；重量: 3.2；被击中耐久损耗倍率: 0.17；保温值: 0.06；价值: 37；识别智力: 4；穿戴视觉偏移: 5；减伤率:54.5%

### Tasmanian Tiger SK 插板胸挂（黑系复合迷彩）【TT SK】 / Tasmanian Tiger SK Plate Carrier (Black Multicam) [TT SK]
- ID: `ttsk`
- 护甲系数: 1.8653；重量: 3.5；被击中耐久损耗倍率: 0.21；保温值: 0.08；价值: 70；识别智力: 5；容器容量: 2.5；单物最大重量: 1；负重减免: 0.5；穿戴视觉偏移: 5；减伤率:65.1%

### Wartech TV-110 插板胸挂（灰褐色）【TV-110】 / Wartech TV-110 Plate Carrier (Taupe) [TV-110]
- ID: `tv110`
- 护甲系数: 1.0202；重量: 3；被击中耐久损耗倍率: 0.17；保温值: 0.1；价值: 45；识别智力: 5；容器容量: 3.2；单物最大重量: 2；负重减免: 0.4；穿戴视觉偏移: 5；减伤率:50.5%

### Wartech TV-115 插板胸挂（橄榄绿）【TV-115】 / Wartech TV-115 Plate Carrier (Olive Drab) [TV-115]
- ID: `tv115`
- 护甲系数: 0.5576；重量: 1.3；被击中耐久损耗倍率: 0.2；保温值: 0.1；价值: 37；识别智力: 5；容器容量: 2.2；单物最大重量: 1；负重减免: 0.36；穿戴视觉偏移: 5；减伤率:35.8%

### 56式胸挂【TYPE 56】 / Type 56 Chest Rig [TYPE 56]
- ID: `type56`
- WearSlotId: "bandolier"；重量: 0.5；保温值: 0.03；价值: 27；识别智力: 4；容器容量: 3.5；单物最大重量: 1；负重减免: 0.54；穿戴视觉偏移: 6；DecayRatePerSecond: 1.0f / 25200.0

### Umka М33-SET1 猎人背心【Umka】 / Umka M33-SET1 Hunter Vest [Umka]
- ID: `umka`
- WearSlotId: "bandolier"；重量: 1.7；保温值: 0.14；价值: 31；识别智力: 4；容器容量: 4；单物最大重量: 1.5；负重减免: 0.55；穿戴视觉偏移: 6；DecayRatePerSecond: 1.0f / 21600.0

### WARTECH TV-109 + TV-106 胸挂（A-TACS橄榄绿迷彩）【WT chest rig】 / WARTECH TV-109 + TV-106 Chest Rig (A-TACS Olive Drab) [WT chest rig]
- ID: `wtchestrig`
- WearSlotId: "bandolier"；重量: 0.9；保温值: 0.02；价值: 27；识别智力: 4；容器容量: 3.5；单物最大重量: 1.5；负重减免: 0.55；穿戴视觉偏移: 6；DecayRatePerSecond: 1.0f / 23400.0

## 头盔/耳机/夜视 / Helmets & Headsets

### 6B47 Ratnik-BSh 头盔（数码迷彩盔罩）【6B47】 / 6B47 Ratnik-BSh Helmet (Digital Camo Cover) [6B47]
- ID: `6b47`
- WearSlotId: "hat"；护甲系数: 1.334；重量: 0.4；被击中耐久损耗倍率: 0.4；保温值: 0.11；价值: 38；识别智力: 7；穿戴视觉偏移: 8；减伤率:57.2%

### Galvion 凯门鳄 复合防弹头盔（高原复合迷彩）【Calman】 / Galvion Caiman Composite Ballistic Helmet (Alpine Multicam) [Calman]
- ID: `calman`
- WearSlotId: "hat"；护甲系数: 1.334；重量: 0.35；被击中耐久损耗倍率: 0.39；保温值: 0.1；价值: 40；识别智力: 7；穿戴视觉偏移: 8；减伤率:57.2%

### Team Wendy EXFIL 防弹头盔（狼棕色）【EXFIL】 / Team Wendy EXFIL Ballistic Helmet (Coyote Brown) [EXFIL]
- ID: `exfil`
- WearSlotId: "hat"；护甲系数: 1.59；重量: 0.65；被击中耐久损耗倍率: 0.36；保温值: 0.1；价值: 46；识别智力: 7；穿戴视觉偏移: 8；减伤率:61.4%

### Ops-Core FAST MT 超级高切头盔（黑色）【Fast MT】 / Ops-Core FAST MT Super High Cut Helmet (Black) [Fast MT]
- ID: `fastmt`
- WearSlotId: "hat"；护甲系数: 1.596；重量: 0.5；被击中耐久损耗倍率: 0.39；保温值: 0.1；价值: 44；识别智力: 7；穿戴视觉偏移: 8；减伤率:61.5%

### Ops-Core FAST头盔多重打击防弹面罩【FAST面罩】 / Ops-Core FAST Multi-Hit Ballistic Face Shield [FAST Shield]
- ID: `fastvisor2`
- 重量: 0.35；价值: 32；识别智力: 8；被击中耐久损耗倍率: 0.4；护甲系数: 0.0638；保温值: 0；WearSlotId: "eyes"；DesiredWearLimb: "Head"；穿戴视觉偏移: 6；减伤率:6.0%

### Ops-Core FAST护目罩【F护目罩】 / Ops-Core FAST Visor [F-Visor]
- ID: `fastvisor`
- 重量: 0.2；价值: 20；识别智力: 8；护甲系数: 0.0526；被击中耐久损耗倍率: 0.65；保温值: 0；WearSlotId: "eyes"；DesiredWearLimb: "Head"；穿戴视觉偏移: 6；减伤率:5.0%

### GPNVG-18全景夜视镜【GPNVG-18】 / GPNVG-18 Ground Panoramic Night Vision Goggle [GPNVG-18]
- ID: `gpnvg18`
- WearSlotId: "eyes"；重量: 0.44；价值: 35；识别智力: 7；穿戴视觉偏移: 6

### LolKek 3F Transfer 旅行背包【LK 3F】 / LolKek 3F Transfer Travel Backpack [LK 3F]
- ID: `lk3f`
- WearSlotId: "back"；重量: 0.6；保温值: 0.02；价值: 15；识别智力: 2；容器容量: 4.4；单物最大重量: 2；负重减免: 0.55；被击中耐久损耗倍率: 0；可撕裂属性: 5；穿戴视觉偏移: 4；DecayRatePerSecond: 1.0f / 14400.0

### CENS ProFlex DX5 战术耳塞 / CENS ProFlex DX5
- ID: `proflextac`
- WearSlotId: "ear"；DesiredWearLimb: "Head"；重量: 0.05；价值: 36；穿戴视觉偏移: 1；识别智力: 7

### AN/PVS-14单筒夜视仪【PVS-14】 / AN/PVS-14 Monocular Night Vision Device [PVS-14]
- ID: `pvs14`
- WearSlotId: "eyes"；重量: 0.3；价值: 30；识别智力: 7；穿戴视觉偏移: 6

### L3Harris PVS-31A夜视仪【PVS-31A】 / L3Harris PVS-31A Night Vision Goggle [PVS-31A]
- ID: `pvs31a`
- WearSlotId: "eyes"；重量: 0.24；价值: 55；识别智力: 9；穿戴视觉偏移: 6

### SSh-68头盔（1968钢盔）【SSh-68】 / SSh-68 Steel Helmet (1968) [SSh-68]
- ID: `ssh68`
- WearSlotId: "hat"；护甲系数: 1.247；重量: 0.6；被击中耐久损耗倍率: 0.45；保温值: 0.11；价值: 36；识别智力: 7；穿戴视觉偏移: 8；减伤率:55.5%

### Peltor TEP-300 战术耳塞 / Peltor TEP-300
- ID: `tep300`
- WearSlotId: "ear"；DesiredWearLimb: "Head"；重量: 0.05；价值: 20；穿戴视觉偏移: 1；识别智力: 7

### TK Fast MT 头盔 / TK Fast MT Helmet
- ID: `tkfastmt`
- 重量: 0.8；价值: 15；识别智力: 6；护甲系数: 1；被击中耐久损耗倍率: 0.8；保温值: 0.08；WearSlotId: "hat"；穿戴视觉偏移: 8；减伤率:50.0%

### Highcom Striker ULACH IIIA 头盔（黑色）【ULACH】 / Highcom Striker ULACH IIIA Helmet (Black) [ULACH]
- ID: `ulach`
- WearSlotId: "hat"；护甲系数: 1.898；重量: 0.55；被击中耐久损耗倍率: 0.35；保温值: 0.1；价值: 48；识别智力: 7；穿戴视觉偏移: 8；减伤率:65.5%

## 食物 / Food

### Alyonka巧克力棒 / Alyonka Chocolate Bar
- ID: `alyonka`
- 重量: 0.18；价值: 7；识别智力: 4；腐烂时间(分钟): 150；每次使用耐久消耗: 0.2；饱食+5f, 体重+0.13f；水分-3f；心情+1.8f；患病+19f

### 煮熟的方便面 / Cooked Noodles
- ID: `cookednoodles`
- 重量: 0.7；价值: 0；识别智力: 4；腐烂时间(分钟): 120；每次使用耐久消耗: 0.5；饱食+13f, 体重+0.14f；水分+7f；心情+1.5f

### 军用饼干 / Military Crackers
- ID: `crackers`
- 重量: 0.2；价值: 2；识别智力: 4；饱食+3f, 体重+0.08f；水分-1f；心情+0.5f

### 黑麦面包块 / Rye Croutons
- ID: `croutons`
- 重量: 0.2；价值: 3；识别智力: 4；饱食+6f, 体重+0.08f；水分-3f；心情+1f

### Iskra 单兵口粮 / Iskra Field Ration
- ID: `iskra`
- 重量: 0.9；价值: 27；识别智力: 4；每次使用耐久消耗: 0.334；饱食+23f, 体重+0.25f；水分+5f；心情+2f

### MRE 个人即食口粮 / MRE Field Ration
- ID: `mre`
- 重量: 1；价值: 23；识别智力: 4；每次使用耐久消耗: 0.334；饱食+20f, 体重+0.2f；水分+3f；心情+1.5f

### 方便面 / Instant Noodles
- ID: `noodles`
- 重量: 0.3；价值: 6；识别智力: 4；腐烂时间(分钟): 1440；每次使用耐久消耗: 0.5；饱食+10f, 体重+0.1f；水分-5f；心情+0.2f

### 豌豆罐头 / Canned Peas
- ID: `peas`
- 重量: 0.8；价值: 8；识别智力: 4；腐烂时间(分钟): 900；每次使用耐久消耗: 0.334；饱食+6f, 体重+0.02f；水分+4f；心情+0.2f

### 士力架能量棒 / Slickers Energy Bar
- ID: `slickers`
- 重量: 0.15；价值: 5；识别智力: 4；腐烂时间(分钟): 240；每次使用耐久消耗: 0.5；饱食+7f, 体重+0.15f；水分-3f；心情+2f；患病+22f

### 一包糖 / Pack of Sugar
- ID: `sugar`
- 重量: 0.5；价值: 13；识别智力: 4；腐烂时间(分钟): 600；每次使用耐久消耗: 0.125；饱食+6f, 体重+0.12f；水分-4f；心情-0.2f；患病+2f

### 塔克肉干 / Tarker Beef Jerky
- ID: `tarker`
- 重量: 0.15；价值: 8；识别智力: 4；每次使用耐久消耗: 0.334；饱食+6f, 体重+0.12f；水分-2f；心情+1.3f

## 工具 / Tools

### Leatherman 多功能工具钳【工具钳】 / Leatherman Multi-Tool [Multi-Tool]
- ID: `leatherman`
- 重量: 0.5；价值: 20；识别智力: 5

### 武器维修套件【Weapon repair Kit】 / Weapon Repair Kit [Weapon repair Kit]
- ID: `weaponrepairkit`
- 重量: 4.5；价值: 52；DurabilityPerUse: 0.25

## 钥匙卡 / Keycards

### Terragroup-Blue Area钥匙卡【Blue Area】 / Terragroup Blue Area Keycard [Blue Area]
- ID: `bluearea_keycard`
- 重量: 0.05；价值: 6000；识别智力: 5

### Terragroup-武器室房卡【武器室】 / Terragroup Weapon Room Card [Weapon Room]
- ID: `weaponroom_keycard`
- 重量: 0.05；价值: 5000；识别智力: 5

## 世界刷新与掉落 / World Spawn & Drops

### 自定义世界生成（CustomSpawnPatch）

- 物资箱（Container）：18.6% 枪械 + 22% 弹匣 + 13% 近战 + 17% 护甲/胸挂 + 17% 头盔 + 13% 背包(1~2) + 10% 夜视仪
- 空投舱（LifePod）：20% 枪械 + 25% 弹挂 + 20% 头盔 + 6% 背包 + 8% 夜视仪
- 空投胶囊（DropCapsule）：29% 枪械 + 32% 弹挂类 + 17% 头盔(1~2) + 16% 背包 + 10% 夜视仪
- 医疗箱（medcrate）：20% 护甲（破坏时触发）
- 尸体（CorpseScript）：15% 枪械 + 15% 弹匣 + 7% 护甲/弹挂 + 5% 头盔 + 3% 背包
- 崩溃舱（CollapsedPod）：62% 弹匣
- 枪械随机权重：手枪 35% / SKS+霰弹 20% / 冲锋枪 17% / 步枪 13% / 狙击 10% / 轻机枪 5%
- 近战（物资箱）：冰镐 40% / M-2 60%
- 护甲刷新：弹挂类 40% / 防弹衣 35% / 弹挂甲 25%，按价值反比加权
- 头盔刷新：按价值反比加权
- 背包刷新：按价值反比加权

### 武器室物资箱掉落

- 小武器物资箱：1~2 个随机配件（WeaponPartIds 池）
- 大型武器箱：2~4 个随机配件 + 1 把随机枪（WeaponGunIds 池）
- 配件耐久：50%~100% 随机
- 测试附件生成器（TestAttachmentSpawner）：每次游戏会话在玩家脚边生成全部 WeaponPartIds 一次

### WeaponGunIds 枪械池

- `mp133` MP-133 12铅径泵动式霰弹枪【MP-133】
- `mp153` MP-153 12铅径半自动霰弹枪【MP 153】
- `sks` 西蒙诺夫 SKS 7.62x39 卡宾枪【SKS】
- `axmc` Accuracy International AXMC .338 LM 栓动式狙击步枪【AXMC】
- `dvl10` DVL-10 7.62x51 栓动式狙击步枪【DVL-10】
- `akm` AKM 7.62x39 突击步枪【AKM】
- `deagle` Magnum Research "沙漠之鹰"L6 .50 AE手枪【沙漠之鹰L6】
- `glock17` GLOCK 17 9x19手枪【Glock17】
- `m4a1` 柯尔特 M4A1 5.56x45 卡宾枪【M4A1】
- `p90` FN P90 5.7x28 冲锋枪【P90】
- `ump45` HK UMP 45冲锋枪【UMP 45】
- `rpd` RPD 7.62x39 轻机枪【RPD】
- `usp` HK USP .45 ACP手枪【USP】
- `vss` VSS “绞丝机” 9x39 特种狙击步枪【VSS】
- `aa12` MPS Auto Assault-12 Gen 1 12铅径自动霰弹枪【AA-12】

### WeaponPartIds 配件池

- `moeakm` Magpul MOE AKM 护木 (FDE)【MOE AKM】
- `hexagonak_hg` Hexagon AK 管状护木（Anodized Red）【Hexagon AK】
- `b10mb19` b10mb19
- `wasr` wasr
- `akml` TDI AKM-L 护木（电镀红）【AKM-L】
- `moesl` Magpul MOE SL 卡宾枪长度 M-LOK AR15 护木【MOE SL】
- `viper` Strike Industries Viper 卡宾枪规格 AR-15 M-LOK 护木 (FDE)【Viper】
- `kacris` KAC RIS AR-15 护木【KAC RIS】
- `smrmk16` Geissele SMR MK16 13.5 英寸 AR-15 M-LOK 护木 (DDC)【SMR Mk.16 13.5】
- `adarwood` ADAR 2-15 AR-15 兼容木质枪托【2-15木制】
- `lvoa` War Sport LVOA-S AR-15 护木（黑色）【LVOA-S】
- `hexagon_sks` Hexagon SKS 7.62x39 声音抑制器【Hexagon SKS】
- `tapco_intrafuse` SKS 7.62x39 卡宾枪 Tapco INTRAFUSE 套件组【Tapco intrafuse】
- `uas_sks` SKS 7.62x39 卡宾枪 UAS 套件组【UAS SKS】
- `sks_mc` SKS ATI Monte Carlo 枪托【SKS MC】
- `mtu017` SKS Leapers UTG PRO MTU017 机匣基座【MTU017】
- `rk3` Zenit RK-3 AK 手枪式握把【RK-3】
- `mg47` KGB MG-47 AK 手枪式握把（电镀红）【MG-47】
- `ags74` AK Custom Arms AGS-74 PRO + Sniper Kit 手枪式握把【AGS-74】
- `td120001` Tactical Dynamics AR-15 镂空手枪式握把【TD120001】
- `starkarrg` Stark AR AR-15 手枪式握把 (FDE)【Stark AR RG】
- `miad` Magpul MIAD AR-15 手枪式握把 (FDE)【MIAD手枪式】
- `f1st2pc` F1 Firearms 镂空 2 型 AR-15 手枪式握把（缠线版本）【F1 St2 PC】
- `ergo` HK Ergo PSG-1 样式 AR-15 手枪式握把【Ergo】
- `shift` shift
- `se5` se5
- `rk0` rk0
- `rk2` rk2
- `b25ur1` b25ur1
- `cobra` cobra
- `p2` p2
- `afg` afg
- `axmc_grip` AMXC 橡胶握把垫【握把垫】
- `opforaa47` ProMag Archangel OPFOR AAK7 AK 枪托【OPFOR AA47】
- `kocherga` Hexagon"烧火棍"AKM/AK-74 枪托（电镀红）【Kocherga】
- `zhukovs` AKM/AK-74 Magpul Zhukov-S 枪托【Zhukov-S】
- `cqr47` Hera Arms CQR47 AKM/AK-74 一体式枪托【CQR47】
- `vipermod1` Strike Industries Viper Mod 1 AR-15 枪托【Viper Mod.1】
- `ctr` Magpul CTR AR-15 卡宾枪托（黑色）【CTR】
- `ds150fde` KRISS Defiance DS150 枪托 (FDE)【DS150 FDE】
- `acs` Magpul ACS AR-15 卡宾枪托 (FDE)【ACS】
- `moefg` 带托垫的 Magpul MOE AR-15 卡宾枪托 (叶绿色)【MOE FG】
- `moefde` 带托垫的 Magpul MOE AR-15 卡宾枪托 (FDE)【MOE FDE】
- `moesg` 带托垫的 Magpul MOE AR-15 卡宾枪托（哑光灰）【MOE SG】
- `mrs` Walther MRS 反射式瞄具【MRS】
- `eotech553` EOTech 553 全息瞄具【553】
- `hhs1` EOTech HHS-1 复合瞄具【HHS-1】
- `specterdr` ELCAN SpecterDR 1x/4x 瞄准镜 FDE【SpecterDR】
- `monstr2x32` Monstrum 紧凑战术棱镜式瞄准镜 2x32【Monstr. 2x32】
- `ta01nsn` Trijicon ACOG TA01NSN 4x32 瞄准镜（黄褐色）【TA01NSN】
- `razorhd` Vortex Razor HD Gen.2 1-6x24 30 毫米步枪瞄准镜【Razor HD Gen.2】
- `pm2` Schmidt & Bender PM II 1-8x24 30 毫米步枪瞄准镜【PM II 1-8x24】
- `dp` Leupold DeltaPoint反射式瞄具【DP】
- `acrop1` Aimpoint ACRO P-1反射式瞄具【ACRO P-1】
- `hexagonakm` Hexagon AKM 7.62x39 消音器【Hexagon AKM】
- `dynacomp` Spike Tactical Dynacomp 7.62x39 AK 膛口制退器【Dynacomp】
- `dtk1` Zenit DTK-1 7.62x39 & 5.45x39 AK 膛口制退器【DTK-1】
- `dtk4m` Zenit DTK-4M 7.62x39 AKM 消音器【DTK-4M】
- `dtkp` Hexagon DTKP MK.2 7.62x39 消音器【DTKP】
- `rotor43` Rotor 43 5.56x45 消音器【Rotor43 556】
- `nt4` KAC QDSS NT-4 5.56x45 消音器 (FDE)【NT-4】
- `sakerasr556` SilencerCo SAKER ASR 556 5.56x45 消音器【SAKER ASR 556】
- `kx3` Noveske KX3 5.56x45 AR-15 消焰器【KX3】
- `vp09` Vendetta Precision VP-09 Interceptor 5.56x45 AR-15 膛口制退器【VP-09】
- `rotor43762` Rotor 43 7.62x39 消音器【Rotor43 7.62x39】
- `p90attenuator` FN P90 Attenuator 5.7x28消音器【Attenuator】
- `ump_oem` B&T OEM .45 ACP UMP 消音器【UMP OEM】
- `dvl10_silenced` DVL-10 7.62x51 500 毫米消音枪管枪口组合【DVL-10 silenced】
- `ac858` SilencerCo AC-858 ASR .338 LM 膛口制退器【AC-858】
- `hekate_dt338` CGS Hekate DT .338 LM 消音器【Hekate DT .338】
- `tmb338lm` AI .338 LM 战术型枪口制退器【TMB 338LM】
- `tsm338lm` AI .338 LM 战术声音抑制器【TSM .338LM】
- `srvvakm` SRVV 7.62x39 AK 膛口制退器【SRVV AKM】
- `wt0032_1` SKS Weapon Tuning 7.62x39 螺纹转换器【WT0032-1】
- `lastac2` LAS/TAC 2 战术手电【LAS/TAC 2】
- `klesch2u` Zenit Klesch-2U 战术手电【Klesch-2U】
- `baldrpro` Olight Baldr Pro 战术手电激光组合【BaldrPro】
- `tbl` NcSTAR Tactical LAM模块 蓝色激光【TBL】
- `vipercut` Glock 9x19 Viper Cut套筒【Glock Viper Cut】
- `ps9` Polymer80 PS9 Glock套筒【PS9】
- `um3` UM Tactical UM3瞄具基座【UM3】
- `awlw` Glock 9x19 Lone Wolf AlphaWolf螺纹枪管【AW螺纹】
- `g3port` Decelerator 3 Port 9x19补偿器【G 3 Port】
- `lw9` Lone Wolf 9 9x19补偿器【LW 9】
- `osprey9` SilencerCo Osprey 9 9x19毫米抑制器【Osprey 9】
- `srd9` Sig SRD 9 9x19毫米声音抑制器【SRD 9】
- `leatherman` Leatherman 多功能工具钳【工具钳】
- `weaponrepairkit` 武器维修套件【Weapon repair Kit】

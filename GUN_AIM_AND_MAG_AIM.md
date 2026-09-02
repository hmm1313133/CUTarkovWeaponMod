# 枪械腰射精度 / 瞄准速度 与 弹匣瞄准速度影响

> 腰射精度用「腰射散布」表示：数值越小越准（0=收缩最小，0.8=最宽）。
> 瞄准速度用「基础瞄准恢复时间」表示：秒数越短 = 开镜越快。
> 弹匣影响为对瞄准时间的加减秒数：正 = 减慢，负 = 加快。
> 已按最新平衡调整更新（AKM/M4A1 基础瞄准时间 1.6s；AKM/M4A1 弹匣不再影响瞄准速度）。

## 1. 枪械腰射精度与瞄准速度

| 枪械 | ID | 腰射散布 | 基础瞄准时间 |
|---|---:|---:|---:|
| AA-12 | aa12 | 0.40 | 2.0s |
| AKM | akm | 0.50 | 1.6s |
| AXMC | axmc | 0.75 | 2.0s |
| 沙漠之鹰 | deagle | 0.25 | 0.8s |
| DVL-10 | dvl10 | 0.65 | 1.3s |
| Glock 17 | glock17 | 0.35 | 0.5s |
| M4A1 | m4a1 | 0.45 | 1.6s |
| MP-133 | mp133 | 0.37 | 1.6s |
| MP-153 | mp153 | 0.38 | 1.6s |
| P90 | p90 | 0.36 | 1.0s |
| RPD | rpd | 0.66 | 2.5s |
| SKS | sks | 0.42 | 1.6s |
| UMP 45 | ump45 | 0.40 | 1.1s |
| USP | usp | 0.20 | 0.5s |
| VSS | vss | 0.42 | 1.2s |

## 2. 弹匣对瞄准速度的影响

| 弹匣 | ID | 对瞄准时间影响 |
|---|---:|---:|
| AXMC 10发 | axmc_mag | 0s |
| DVL-10 10发 | dvl10_mag | 0s |
| AKM 30发 | akm_mag | 0s |
| 沙漠之鹰 7发 | deagle_mag | 0s |
| Glock 17 17发 | glock17_mag | 0s |
| M4A1 30发 | m4a1_mag | 0s |
| SureFire MAG5-60 60发 | mag560 | +0.65s |
| P90 50发 | p90_mag | 0s |
| UMP 45 25发 | ump45_mag | 0s |
| RPD 100发 | rpd_mag | 0s |
| USP 12发 | usp_mag | 0s |
| VSS 30发 | vss_mag | 0s |
| AA-12 20发弹鼓 | aa12_mag | 0s |
| Glock Big Stick 33发 | bigstick_mag | +0.2s |
| Glock G50 50发弹鼓 | g50_mag | +1.0s |
| X-47 50发弹鼓 | x47mag | +0.5s |
| SKS-A5 20发 | sks_a5_mag | 0s |
| SKS 10发弹仓 | sks_integral_mag | 0s |

## 备注
- 实际瞄准时间 = 基础瞄准时间 + 配件影响 + 弹匣影响，最低 0.3s。
- 腰射散布由 `AimSystem.HipFireSpreadMap` 按枪械单独指定。
- 准星间距与当前实际精准度正相关：当前散布 0 → 最小间距，0.8 → 最大间距。

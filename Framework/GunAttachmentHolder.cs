using System.Collections.Generic;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 枪械配件状态组件。挂在枪械上，记录已安装的配件（按安装顺序，可多个）。
/// 状态通过 WeaponItemSaveProvider 存档（CUCoreLib IItemSaveProvider）。
///
/// 卸下策略：右键枪械时卸下最后安装的配件（LIFO），连续右键可逐个卸完。
/// </summary>
public class GunAttachmentHolder : MonoBehaviour
{
    /// <summary>已安装的配件 ID 列表（按安装顺序，末尾=最近安装）。</summary>
    public List<string> attachmentIds = new();

    /// <summary>当前已装弹匣的 ID（多弹匣支持：卸载时生成正确的弹匣）。空=默认弹匣。</summary>
    public string currentMagId = "";

    /// <summary>LAS/TAC 2 战术手电电量（0~1）。安装时从物品 condition 读取；运行时消耗；卸下时恢复。</summary>
    public float lasTacCharge = 1f;

    /// <summary>Klesch-2U 战术手电电量（0~1）。安装时从物品 condition 读取；运行时消耗；卸下时恢复。</summary>
    public float kleschCharge = 1f;

    /// <summary>Baldr Pro 战术手电激光组合电量（0~1）。安装时从物品 condition 读取；运行时消耗；卸下时恢复。</summary>
    public float baldrCharge = 1f;

    /// <summary>TBL 战术激光指示器电量（0~1）。安装时从物品 condition 读取；运行时消耗；卸下时恢复。</summary>
    public float tblCharge = 1f;

    /// <summary>MRS 反射式瞄具电量（0~1）。安装时从物品 condition 读取；运行时消耗；卸下时恢复。</summary>
    public float mrsCharge = 1f;

    /// <summary>EOTech 553 全息瞄具电量（0~1）。安装时从物品 condition 读取；运行时消耗；卸下时恢复。</summary>
    public float eotechCharge = 1f;

    /// <summary>EOTech HHS-1 复合瞄具电量（0~1）。安装时从物品 condition 读取；运行时消耗；卸下时恢复。</summary>
    public float hhs1Charge = 1f;

    /// <summary>记录安装时无电池的带电配件 ID（卸下时保持无电池，不凭空生成电池）。</summary>
    public System.Collections.Generic.HashSet<string> noBatteryAttachments = new(System.StringComparer.OrdinalIgnoreCase);
}

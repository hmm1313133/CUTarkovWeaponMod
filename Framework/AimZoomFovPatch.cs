using HarmonyLib;
using UnityEngine;

namespace CUTarkovWeaponMod.Framework;

/// <summary>
/// 倍镜视野变远幅度补丁。
///
/// 游戏原版 HandleCameraPosition 里：
///   vector2 = 鼠标偏移 × (zoomTime > 0 ? 5f : 1f)
///   transform.position = 玩家中心 + vector2 × 4
/// 即 zoomTime > 0 时视野变远幅度固定 ×5。
///
/// 本补丁完全替换 HandleCameraPosition，把固定 ×5 改为可调幅度，
/// 用倍镜倍率代表视野变远幅度：
///   AXMC 6x、HHS-1 3x、SpecterDR 4x
/// </summary>
[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.HandleCameraPosition))]
public static class AimZoomFovPatch
{
    // 缓存 speed 字段访问器（每帧 HandleCameraPosition 都会调用，避免 Traverse/FieldInfo.GetValue 反射分配）
    // FieldRefAccess 编译为直接字段读写，比 FieldInfo.GetValue 快一个数量级。
    private static readonly AccessTools.FieldRef<PlayerCamera, float> SpeedRef =
        AccessTools.FieldRefAccess<PlayerCamera, float>("speed");

    [HarmonyPrefix]
    public static bool Prefix(PlayerCamera __instance)
    {
        try
        {
            var body = __instance.body;
            if (body == null || __instance.zoomTime <= 0f)
                return true; // 非倍镜状态，走原逻辑

            // 计算当前倍镜的视野变远幅度
            float zoomFactor = GetZoomFactor(body);
            if (zoomFactor <= 1f)
                return true; // 无倍镜或 1x，走原逻辑

            // 复制原逻辑，但把 ×5 改为 ×zoomFactor
            Vector3 vector = Vector2.zero;
            Vector2 vector2 = (new Vector2(
                Mathf.Clamp(Input.mousePosition.x, 0f, Screen.width) / (float)Screen.width,
                Mathf.Clamp(Input.mousePosition.y, 0f, Screen.height) / (float)Screen.height)
                - Vector2.one * 0.5f) * zoomFactor;

            float num = SpeedRef != null ? SpeedRef(__instance) : 1f;
            if (!body.conscious || (bool)MinigameBase.main.currentMinigame
                || __instance.woundView.activeSelf || __instance.craftingPanel.activeSelf)
            {
                vector2 = Vector2.zero;
            }
            if ((bool)__instance.following)
            {
                vector = __instance.following.position;
            }
            else
            {
                Limb[] limbs = body.limbs;
                foreach (Limb limb in limbs)
                {
                    vector += limb.transform.position;
                }
                vector /= (float)body.limbs.Length;
                if (body.standing)
                {
                    vector.x = body.transform.position.x;
                }
            }
            if (__instance.isFreecam)
            {
                vector = ConsoleScript.instance.freecamPos;
                vector2 = Vector2.zero;
                num /= 4f;
            }
            __instance.shaker.Update();
            vector += (Vector3)__instance.shaker.pos;
            __instance.transform.position = Vector2.Lerp(
                __instance.transform.position, vector + (Vector3)vector2 * 4f, num * Time.deltaTime);

            float num2 = (float)WorldGeneration.world.height * 0.5f - Camera.main.orthographicSize;
            float num3 = (float)WorldGeneration.world.width * 0.5f - Camera.main.orthographicSize * Camera.main.aspect;
            Vector2 vector3 = __instance.transform.position;
            vector3.y = Mathf.Clamp(vector3.y, 0f - num2, num2);
            vector3.x = Mathf.Clamp(vector3.x, 0f - num3, num3);
            __instance.transform.position = vector3;
            __instance.transform.position = new Vector3(__instance.transform.position.x, __instance.transform.position.y, -3f);
            __instance.transform.eulerAngles = new Vector3(0f, 0f, __instance.shaker.pos.y);

            return false; // 已替换
        }
        catch
        {
            return true; // 出错走原逻辑
        }
    }

    /// <summary>获取当前手持枪械倍镜的视野变远幅度（倍率）。</summary>
    private static float GetZoomFactor(Body body)
    {
        var handItem = body.GetItem(body.handSlot);
        if (handItem == null || handItem.GetComponent<GunScript>() == null) return 1f;

        // 检查各倍镜及其放大状态
        // 以 SpecterDR 4x→实际6x 为基准，整体 ×1.5 重新标定：
        //   HHS-1 3x→4.5x、SpecterDR 4x→6x
        // 注意：AXMC 原厂不再自动放大（改为可更换瞄准镜），放大由 ScopeZoomPatch
        // （装瞄准镜才激活视野扩展）和具体倍镜控制器（HHS-1/SpecterDR 等）处理。

        var hhs = handItem.GetComponent<Hhs1Controller>();
        if (hhs != null && hhs.IsZoomed)
            return 4.5f; // HHS-1 4.5x

        var spec = handItem.GetComponent<SpecterDrController>();
        if (spec != null && spec.IsZoomed)
            return 6f;   // SpecterDR 6x

        var monstr = handItem.GetComponent<Monstr2x32Controller>();
        if (monstr != null && monstr.IsZoomed)
            return 3f;   // Monstr 2x32 2x（以 SpecterDR 6x 为基准：2x=3 幅度）

        var ta01 = handItem.GetComponent<Ta01nsnController>();
        if (ta01 != null && ta01.IsZoomed)
            return 6f;   // TA01NSN 4x（×1.5 = 6）

        var razor = handItem.GetComponent<RazorHdController>();
        if (razor != null && razor.Mode > 0)
            return razor.Mode switch { 1 => 4.5f, 2 => 9f, _ => 1f };   // Razor HD 3x/6x（×1.5 = 4.5/9）

        var pm2 = handItem.GetComponent<Pm2Controller>();
        if (pm2 != null && pm2.Mode > 0)
            return pm2.Mode switch { 1 => 4.5f, 2 => 9f, 3 => 12f, _ => 1f };   // PM II 3x/6x/8x（×1.5 = 4.5/9/12）

        return 1f;
    }
}

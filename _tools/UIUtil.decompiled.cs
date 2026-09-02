using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIUtil : MonoBehaviour
{
	public static bool IsPointerOverUIElement()
	{
		return IsPointerOverUIElement(GetEventSystemRaycastResults());
	}

	public static bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
	{
		int num = LayerMask.NameToLayer("UI");
		for (int i = 0; i < eventSystemRaysastResults.Count; i++)
		{
			if (eventSystemRaysastResults[i].gameObject.layer == num)
			{
				return true;
			}
		}
		return false;
	}

	public static List<RaycastResult> GetEventSystemRaycastResults(Vector2? pos = null)
	{
		Vector2 valueOrDefault = pos.GetValueOrDefault();
		if (!pos.HasValue)
		{
			valueOrDefault = Input.mousePosition;
			pos = valueOrDefault;
		}
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = pos.Value;
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, list);
		return list;
	}

	public static (string, string) GetUITooltip(List<RaycastResult> UICasts)
	{
		foreach (RaycastResult UICast in UICasts)
		{
			if (UICast.gameObject.TryGetComponent<UITooltip>(out var component))
			{
				return (component.tipName, component.tipDesc);
			}
		}
		return ("", "");
	}

	public static bool IsPointerOverContextMenu()
	{
		return IsPointerOverUIElementContext(GetEventSystemRaycastResultsContext());
	}

	private static bool IsPointerOverUIElementContext(List<RaycastResult> eventSystemRaysastResults)
	{
		int num = LayerMask.NameToLayer("UI");
		for (int i = 0; i < eventSystemRaysastResults.Count; i++)
		{
			RaycastResult raycastResult = eventSystemRaysastResults[i];
			if (raycastResult.gameObject.layer == num && raycastResult.gameObject.CompareTag("ContextMenu"))
			{
				return true;
			}
		}
		return false;
	}

	private static List<RaycastResult> GetEventSystemRaycastResultsContext()
	{
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = Input.mousePosition;
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, list);
		return list;
	}
}

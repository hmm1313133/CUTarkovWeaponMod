namespace UnityEngine.U2D;

[DisallowMultipleComponent]
[AddComponentMenu("")]
[RequireComponent(typeof(Camera))]
[HelpURL("https://docs.unity3d.com/Packages/com.unity.2d.pixel-perfect@latest/index.html?subfolder=/manual/index.html%23properties")]
public class PixelPerfectCamera : MonoBehaviour, IPixelPerfectCamera
{
	[SerializeField]
	private int m_AssetsPPU = 100;

	[SerializeField]
	private int m_RefResolutionX = 320;

	[SerializeField]
	private int m_RefResolutionY = 180;

	[SerializeField]
	private bool m_UpscaleRT;

	[SerializeField]
	private bool m_PixelSnapping;

	[SerializeField]
	private bool m_CropFrameX;

	[SerializeField]
	private bool m_CropFrameY;

	[SerializeField]
	private bool m_StretchFill;

	private Camera m_Camera;

	private PixelPerfectCameraInternal m_Internal;

	private bool m_CinemachineCompatibilityMode;

	public int assetsPPU
	{
		get
		{
			return m_AssetsPPU;
		}
		set
		{
			m_AssetsPPU = ((value <= 0) ? 1 : value);
		}
	}

	public int refResolutionX
	{
		get
		{
			return m_RefResolutionX;
		}
		set
		{
			m_RefResolutionX = ((value <= 0) ? 1 : value);
		}
	}

	public int refResolutionY
	{
		get
		{
			return m_RefResolutionY;
		}
		set
		{
			m_RefResolutionY = ((value <= 0) ? 1 : value);
		}
	}

	public bool upscaleRT
	{
		get
		{
			return m_UpscaleRT;
		}
		set
		{
			m_UpscaleRT = value;
		}
	}

	public bool pixelSnapping
	{
		get
		{
			return m_PixelSnapping;
		}
		set
		{
			m_PixelSnapping = value;
		}
	}

	public bool cropFrameX
	{
		get
		{
			return m_CropFrameX;
		}
		set
		{
			m_CropFrameX = value;
		}
	}

	public bool cropFrameY
	{
		get
		{
			return m_CropFrameY;
		}
		set
		{
			m_CropFrameY = value;
		}
	}

	public bool stretchFill
	{
		get
		{
			return m_StretchFill;
		}
		set
		{
			m_StretchFill = value;
		}
	}

	public int pixelRatio
	{
		get
		{
			if (m_CinemachineCompatibilityMode)
			{
				if (m_UpscaleRT)
				{
					return m_Internal.zoom * m_Internal.cinemachineVCamZoom;
				}
				return m_Internal.cinemachineVCamZoom;
			}
			return m_Internal.zoom;
		}
	}

	public Vector3 RoundToPixel(Vector3 position)
	{
		float unitsPerPixel = m_Internal.unitsPerPixel;
		if (unitsPerPixel == 0f)
		{
			return position;
		}
		Vector3 result = default(Vector3);
		result.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
		result.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
		result.z = Mathf.Round(position.z / unitsPerPixel) * unitsPerPixel;
		return result;
	}

	public float CorrectCinemachineOrthoSize(float targetOrthoSize)
	{
		m_CinemachineCompatibilityMode = true;
		if (m_Internal == null)
		{
			return targetOrthoSize;
		}
		return m_Internal.CorrectCinemachineOrthoSize(targetOrthoSize);
	}

	private void PixelSnap()
	{
		Vector3 position = m_Camera.transform.position;
		Vector3 vector = RoundToPixel(position) - position;
		vector.z = 0f - vector.z;
		Matrix4x4 matrix4x = Matrix4x4.TRS(-vector, Quaternion.identity, new Vector3(1f, 1f, -1f));
		m_Camera.worldToCameraMatrix = matrix4x * m_Camera.transform.worldToLocalMatrix;
	}

	private void Awake()
	{
		m_Camera = GetComponent<Camera>();
		m_Internal = new PixelPerfectCameraInternal(this);
		m_Internal.originalOrthoSize = m_Camera.orthographicSize;
		m_Internal.hasPostProcessLayer = GetComponent("PostProcessLayer") != null;
		if (m_Camera.targetTexture != null)
		{
			Debug.LogWarning("Render to texture is not supported by Pixel Perfect Camera.", m_Camera);
		}
	}

	private void LateUpdate()
	{
		m_Internal.CalculateCameraProperties(Screen.width, Screen.height);
		m_Camera.forceIntoRenderTexture = m_Internal.hasPostProcessLayer || m_Internal.useOffscreenRT;
	}

	private void OnPreCull()
	{
		PixelSnap();
		if (m_Internal.pixelRect != Rect.zero)
		{
			m_Camera.pixelRect = m_Internal.pixelRect;
		}
		else
		{
			m_Camera.rect = new Rect(0f, 0f, 1f, 1f);
		}
		if (!m_CinemachineCompatibilityMode)
		{
			m_Camera.orthographicSize = m_Internal.orthoSize;
		}
	}

	private void OnPreRender()
	{
		if (m_Internal.cropFrameXOrY)
		{
			GL.Clear(clearDepth: false, clearColor: true, Color.black);
		}
		PixelPerfectRendering.pixelSnapSpacing = m_Internal.unitsPerPixel;
	}

	private void OnPostRender()
	{
		PixelPerfectRendering.pixelSnapSpacing = 0f;
		if (m_Internal.useOffscreenRT)
		{
			RenderTexture activeTexture = m_Camera.activeTexture;
			if (activeTexture != null)
			{
				activeTexture.filterMode = (m_Internal.useStretchFill ? FilterMode.Bilinear : FilterMode.Point);
			}
			m_Camera.pixelRect = m_Internal.CalculatePostRenderPixelRect(m_Camera.aspect, Screen.width, Screen.height);
		}
	}

	private void OnEnable()
	{
		m_CinemachineCompatibilityMode = false;
	}

	internal void OnDisable()
	{
		m_Camera.rect = new Rect(0f, 0f, 1f, 1f);
		m_Camera.orthographicSize = m_Internal.originalOrthoSize;
		m_Camera.forceIntoRenderTexture = m_Internal.hasPostProcessLayer;
		m_Camera.ResetAspect();
		m_Camera.ResetWorldToCameraMatrix();
	}
}

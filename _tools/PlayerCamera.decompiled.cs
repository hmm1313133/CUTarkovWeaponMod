using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerCamera : MonoBehaviour
{
	public enum SpeedType
	{
		Normal,
		Fast,
		SuperFast,
		UnconsciousFast,
		DyingFast,
		Slowmo,
		Paused
	}

	public enum UISoundType
	{
		Click,
		MiniClick,
		Close,
		HealthPanelOn,
		InventoryOpen,
		InventoryClose,
		Deny,
		Time
	}

	public Transform following;

	public bool isFreecam;

	public Body body;

	private float speed = 10f;

	public GameObject woundView;

	public GameObject mainView;

	public static PlayerCamera main;

	public Item focusedItem;

	public Limb selectedLimb;

	public TextMeshProUGUI timescaleText;

	public TextMeshProUGUI holdingText;

	public TextMeshProUGUI holdingDescriptionText;

	public TextMeshProUGUI holdingSecondaryText;

	public SpeedType curTimeScale;

	public Image holdingImage;

	public Image[] speedImages;

	public Button[] pickupButtons;

	public List<Container> selectedContainers;

	public List<GameObject> containerUnloadButtons;

	public List<GameObject> containerLoadButtons;

	public List<GameObject> unwearButtons;

	public RectTransform depthLine;

	public RectTransform radDepthLine;

	public TextMeshProUGUI statusText;

	public Gradient itemConditionGradient;

	public Volume volume;

	public bool[] showInfection;

	public SpriteRenderer hoverSquare;

	public SpriteRenderer placeSquare;

	public Sprite defaultHoverSquareSprite;

	public Canvas mainCanvas;

	public Image consciousnessFade;

	public Image sleepFill;

	public Image consciousnessVignette;

	public TextMeshProUGUI consciousnessText;

	public ECGVisualizer consciousnessECG;

	public float timeHasBeenBlack;

	public float blackAmount;

	private bool wasUnconscious;

	public Material mainScreenPass;

	public Material damagePass;

	public Material bonusPass;

	public Material blurPass;

	private float dirMultSmooth;

	public float zoomTime;

	public Texture2D[] cursors;

	private int cursor;

	public Image woundButton;

	public Sprite woundButtonNormal;

	public Sprite woundButtonFlash;

	public Sprite woundButtonScreen;

	public bool didDeathScreen;

	public Image endScreen;

	public Sprite[] deathScreenSprites;

	public AudioSource windSource;

	private float windvol;

	private float windpitch;

	public Button combineButton;

	public float painSmooth;

	public float hungerSmooth;

	public float thirstSmooth;

	public float sickSmooth;

	public float waterSmooth;

	public float dropletSmooth;

	public float coldSmooth;

	public float dirtSmooth;

	public float sadSmooth;

	public float satSmooth;

	public float oxySmooth;

	public Image waitImage;

	public Image waitBackImage;

	public float timeHeldThrow;

	public SpriteRenderer crouchGuide;

	public Shaker shaker = new Shaker();

	private float timeSinceRadUpdate;

	private float painbeatTime;

	public GameObject depthMeter;

	public GameObject bonusUseButton;

	public RectTransform radialMenu;

	public Image dragImage;

	public Image radialCircle;

	public Item dragItem;

	public bool radialOpen;

	public GameObject containerMenu;

	public GameObject contPopulate;

	public GameObject wearPopulate;

	public Container currentContainer;

	public TextMeshProUGUI containerText;

	public TextMeshProUGUI radialText;

	public Image containerFill;

	public Image containerIcon;

	public SpriteRenderer playerDragIndicator;

	public Image[] radialEdgeImages;

	public Vector2 clickPos;

	public float threatMusicTime;

	public float lastAttackCool;

	public float bonusAbber;

	public RawImage brainFlash;

	public TextMeshProUGUI weightText;

	public TraderScript currentTrader;

	public TraderScript lastTrader;

	public GameObject tradeGiveObject;

	public GameObject liquidDrainObject;

	public AudioSource underWaterSource;

	public int currentThreatTheme;

	public Canvas worldCanvas;

	public LineRenderer throwRenderer;

	public float dropletAmount;

	public GameObject backgroundSnow;

	private float alertTime;

	public Image alertBack;

	public TextMeshProUGUI alertText;

	public GameObject gunMenu;

	public Image gunRackImage;

	public Image gunSafeImage;

	public Sprite gunRackedSprite;

	public Sprite gunNormalSprite;

	public Sprite gunSafeSprite;

	public Sprite gunUnsafeSprite;

	public Button gunMagButton;

	public Button tradeHaggleButton;

	public Button tradeThreatenButton;

	public Button tradeSleepButton;

	public Sprite craftQuestionMark;

	public Image gunBulletImage;

	public Sprite[] gunBulletSprites;

	[HideInInspector]
	public float bonusActionBarTime;

	public AudioClip[] uiSounds;

	private bool previousRadialOpen;

	public GameObject tradeMenu;

	public Transform traderInventory;

	public Transform playerInventory;

	public TextMeshProUGUI charTalkText;

	public TextMeshProUGUI traderTalkText;

	public TextMeshProUGUI traderRepText;

	public TextMeshProUGUI traderValueText;

	public FilledImagePP traderRepBar;

	public Gradient traderRepGradient;

	public RectTransform gunCrosshair;

	public RectTransform deathStats;

	public int caloriesConsumed;

	public Sprite[] lastStandImages;

	public GameObject lastStandPanel;

	public Image handSwapImage;

	public Sprite[] handSwapSprites;

	public GameObject craftingPanel;

	public GameObject craftButton;

	public int selectedRecipe;

	public string recipeFilter;

	public TMP_InputField recipeFilterField;

	public RectTransform recipeListContent;

	private List<GameObject> recipeObjects;

	private List<GameObject> recipeItemPreviewObjects;

	public GameObject searchClearButton;

	public ScrollRect recipeScrollRect;

	public GameObject recipesBackToTopButton;

	private Coroutine recipeScrollCoroutine;

	private Recipes.RecipeCategory? selectedRecipeCategory;

	public Image pinRecipeImage;

	public TextMeshProUGUI pinRecipeText;

	private float pinnedRecipeUpdateTime;

	public Image playerLiquidRagdollBar;

	public Image[] recipeCategorySelectImages;

	public Sprite uiNano;

	public Sprite darkenedUiNano;

	public Image traderHagglebutton;

	public GameObject traderMoveButon;

	public TextMeshProUGUI traderNameText;

	public Image selectedItemOverlay;

	public GameObject LOSHole;

	private bool lastHeartBeating;

	private float screenFlipSmooth;

	public float globalMuteTime;

	private Item recipeItemFilter;

	private float traderPlayerInventoryScootAmount;

	public bool wantsJumpInput;

	public static bool alwaysExpandDescriptions;

	public static float inventoryUseLeniency;

	public static bool autoJump;

	public static bool clearerRecipes;

	public static int lineOfSightRes;

	public static float screenShakeMult;

	public bool selfDestructActive;

	private Coroutine finishCoroutine;

	public static float brightness;

	public float irradiateIntensity { get; private set; }

	public static float uiScale => main.mainCanvas.transform.localScale.y;

	public int? pinnedRecipe { get; private set; }

	public float muteTimeVolume => Mathf.Clamp((0f - globalMuteTime) * 15f, -80f, 0f);

	public void PlayUISound(UISoundType sound, float pitch = 1f)
	{
		Sound.Play(uiSounds[(int)sound], Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, pitch, noReverb: true, ignoreMixer: true);
	}

	public static void PlayUISound(string sound, float pitch = 1f)
	{
		Sound.Play(sound, Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, pitch, noReverb: true, ignoreMixer: true);
	}

	public void PlayBackpackSound()
	{
		Sound.Play("backpack" + UnityEngine.Random.Range(1, 3), Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, UnityEngine.Random.Range(0.8f, 1.2f), noReverb: true, ignoreMixer: true);
	}

	public void SwitchHands()
	{
		PlayUISound(UISoundType.Click);
		body.handSlot = ((body.handSlot == 0) ? 1 : 0);
		handSwapImage.sprite = handSwapSprites[body.handSlot];
		if (body.handSlot == 1)
		{
			DoAlert(Locale.GetOther("handswitchwarning"));
		}
	}

	public void SetRecipeCategory(int index)
	{
		if (index == 0)
		{
			selectedRecipeCategory = null;
		}
		else
		{
			selectedRecipeCategory = (Recipes.RecipeCategory)(index - 1);
		}
		for (int i = 0; i < recipeCategorySelectImages.Length; i++)
		{
			if (index == i)
			{
				recipeCategorySelectImages[i].sprite = darkenedUiNano;
			}
			else
			{
				recipeCategorySelectImages[i].sprite = uiNano;
			}
		}
		PlayUISound(UISoundType.MiniClick);
		RefreshRecipeList();
	}

	public void GunRack()
	{
		if (body.HoldingItem(body.handSlot) && body.GetItem(body.handSlot).TryGetComponent<GunScript>(out var component))
		{
			component.TryRack();
		}
	}

	public void GunEjectMag()
	{
		if (body.HoldingItem(body.handSlot) && body.GetItem(body.handSlot).TryGetComponent<GunScript>(out var component))
		{
			component.UnloadMag();
		}
	}

	public void GunToggleSafety()
	{
		if (body.HoldingItem(body.handSlot) && body.GetItem(body.handSlot).TryGetComponent<GunScript>(out var component))
		{
			component.ToggleSafety();
		}
	}

	public void OpenCraftScreen()
	{
		if (!woundView.activeSelf)
		{
			PlayUISound(craftingPanel.activeSelf ? UISoundType.Close : UISoundType.Click);
			craftingPanel.SetActive(!craftingPanel.activeSelf);
			radialOpen = false;
			if (craftingPanel.activeSelf)
			{
				craftingPanel.transform.localScale = Vector3.one * 0.1f;
				RefreshRecipeList();
			}
		}
	}

	public void SeeRecipesWithItem(Item item)
	{
		recipeItemFilter = item;
		RefreshRecipeList();
	}

	public void StartSelfDestruct()
	{
		if (!selfDestructActive)
		{
			StartCoroutine(SelfDestructSequence());
		}
	}

	private IEnumerator SelfDestructSequence()
	{
		selfDestructActive = true;
		SetTimeScale(SpeedType.Normal, switchSound: false, force: true);
		body.eyeScareTime = 10f;
		body.talker.Talk(Locale.GetCharacter("selfdestruct"), null, force: false, resetTalkTimer: true);
		Sound.Play("selfdestruct1", Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, 1f, noReverb: true, ignoreMixer: true);
		yield return new WaitForSeconds(3f);
		body.eyeCloseTime = 10f;
		yield return new WaitForSeconds(1.15f);
		Utils.Create("BloodExplosion", body.limbs[0].transform.position, 0f);
		Utils.Create("Special/ExplosionParticle", body.limbs[0].transform.position, 0f);
		Sound.Play("selfdestruct2", Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, 1f, noReverb: true, ignoreMixer: true);
		yield return new WaitForSeconds(0.02f);
		body.brainHealth = 0f;
		body.consciousness = 0f;
		body.limbs[0].muscleHealth = 0f;
		body.limbs[0].skinHealth = 0f;
		body.limbs[0].bleedAmount = 100f;
		body.Disfigure();
		body.RemoveEye();
		body.RemoveEye();
		body.DoGoreSound();
		selfDestructActive = false;
	}

	public void RefreshRecipeList()
	{
		Color color = Color.white;
		Color color2 = new Color(0.3f, 0.3f, 0.3f);
		if (clearerRecipes)
		{
			color = new Color(0.5f, 1f, 0.5f);
			color2 = new Color(0.5f, 0f, 0f);
		}
		if (recipeObjects != null)
		{
			foreach (GameObject recipeObject in recipeObjects)
			{
				UnityEngine.Object.Destroy(recipeObject);
			}
		}
		recipeObjects = new List<GameObject>();
		new List<(Recipe, bool)>();
		List<(Recipe rec, bool)> source = (from x in Recipes.GetVisibleRecipes(recipeItemFilter)
			orderby x.INT
			select x into rec
			select (rec: rec, rec.GetItemsForRecipe() != null)).ToList();
		List<(Recipe, bool)> list = source.Where<(Recipe, bool)>(((Recipe, bool) x) => x.Item2).ToList();
		List<(Recipe, bool)> collection = source.Where<(Recipe, bool)>(((Recipe, bool) x) => !x.Item2).ToList();
		list.AddRange(collection);
		if (selectedRecipeCategory.HasValue && string.IsNullOrEmpty(recipeFilter) && !recipeItemFilter)
		{
			list = list.Where(((Recipe, bool) r) => r.Item1.category == selectedRecipeCategory.Value).ToList();
		}
		if (!string.IsNullOrEmpty(recipeFilter) && !recipeItemFilter)
		{
			list = list.Where(((Recipe, bool) r) => r.Item1.simpleName.Contains(recipeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			GameObject gameObject = Utils.Create("Special/RecipeBox", recipeListContent);
			gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(-9f, -i * 64);
			gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = list[i].Item1.fullName;
			gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = (list[i].Item2 ? color : color2);
			gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = list[i].Item1.INT.ToString();
			gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = RecipeINTToColor(list[i].Item1.INT);
			(Sprite, Color) resultSprite = list[i].Item1.resultSprite;
			gameObject.transform.GetChild(2).GetComponent<Image>().sprite = resultSprite.Item1;
			gameObject.transform.GetChild(2).GetComponent<Image>().color = resultSprite.Item2;
			gameObject.transform.GetChild(2).GetComponent<RectTransform>().sizeDelta = ImageSizeDelta(resultSprite.Item1.texture, 3f, 42f);
			gameObject.GetComponent<Image>().color = (list[i].Item2 ? color : color2);
			int toIndex = list[i].Item1.index;
			gameObject.GetComponent<Button>().onClick.AddListener(delegate
			{
				SelectRecipe(toIndex);
			});
			UITooltip uITooltip = gameObject.AddComponent<UITooltip>();
			uITooltip.skipLocale = true;
			uITooltip.tipName = list[i].Item1.simpleName;
			uITooltip.tipDesc = list[i].Item1.description;
			recipeObjects.Add(gameObject);
			num++;
		}
		recipeListContent.sizeDelta = new Vector2(1f, (float)num * 64f);
		recipeItemFilter = null;
		RefreshCurrentlySelectedRecipe();
	}

	public Color RecipeINTToColor(int INT)
	{
		int num = body.skills.INT - INT;
		if (num >= 0)
		{
			return Color.green;
		}
		return num switch
		{
			-1 => Color.yellow, 
			-2 => new Color(1f, 0.5f, 0f), 
			_ => Color.red, 
		};
	}

	public string IngredientTextForRecipe(Recipe recipe, List<Item> its = null)
	{
		string text = "";
		string text2 = "<sprite index=23>";
		string text3 = "<sprite index=24>";
		if (its == null)
		{
			its = recipe.GetItemsForRecipeThorough();
		}
		for (int i = 0; i < recipe.items.Count; i++)
		{
			RecipeItem recit = recipe.items[i];
			text += "<color=#FFFFFF>";
			text += (its[i] ? text2 : text3);
			text = (recit.specific ? ((!recit.isLiquid) ? (text + Locale.GetItem(recit.specificId) + "\n") : (text + Locale.GetOther(recit.specificId) + "\n")) : ((!recit.isLiquid) ? (text + Locale.GetOther((recit.quality.id == "hammering" || recit.quality.id == "cutting") ? "craftanytool" : "craftanyitem") + "\n") : (text + Locale.GetOther("craftanyliquid") + "\n")));
			text += "<color=#666666>";
			if (recit.isLiquid)
			{
				if (!recit.specific)
				{
					text = text + Locale.GetOther("craftliquidquality").Replace("<1>", recit.quality.amount.ToString("0.#")).Replace("<2>", recit.quality.LocaleName) + "\n";
				}
				else if (recit.minimumCondition > 0f)
				{
					text = text + Locale.GetOther("craftml").Replace("<>", recit.minimumCondition.ToString("0.#")) + "\n";
				}
				continue;
			}
			if (!recit.specific)
			{
				text += Locale.GetOther("craftitemquality").Replace("<1>", recit.quality.amount.ToString("0.#")).Replace("<2>", recit.quality.LocaleName);
				KeyValuePair<CraftingQuality, string> keyValuePair = Recipes.QualityExamples.FirstOrDefault((KeyValuePair<CraftingQuality, string> x) => x.Key.id == recit.quality.id && x.Key.amount == recit.quality.amount);
				if (keyValuePair.Value != null)
				{
					text += Locale.GetOther("craftexample").Replace("<>", Locale.GetItem(keyValuePair.Value));
				}
				text += "\n";
			}
			if (recit.minimumCondition > 0f)
			{
				text = text + Locale.GetOther("craftcondition").Replace("<>", (recit.minimumCondition * 100f).ToString("0.#")) + "\n";
			}
		}
		return text;
	}

	public void RefreshCurrentlySelectedRecipe()
	{
		Recipe recipe = Recipes.recipes[selectedRecipe];
		craftingPanel.transform.GetChild(5).GetComponent<TextMeshProUGUI>().text = recipe.fullName;
		(Sprite, Color) resultSprite = recipe.resultSprite;
		craftingPanel.transform.GetChild(6).GetComponent<Image>().sprite = resultSprite.Item1;
		craftingPanel.transform.GetChild(6).GetComponent<Image>().color = resultSprite.Item2;
		craftingPanel.transform.GetChild(6).GetComponent<RectTransform>().sizeDelta = ImageSizeDelta(resultSprite.Item1.texture, 12f, 150f);
		TextMeshProUGUI component = craftingPanel.transform.GetChild(8).GetComponent<TextMeshProUGUI>();
		List<Item> itemsForRecipeThorough = recipe.GetItemsForRecipeThorough();
		component.text = IngredientTextForRecipe(recipe, itemsForRecipeThorough);
		if (recipeItemPreviewObjects != null)
		{
			foreach (GameObject recipeItemPreviewObject in recipeItemPreviewObjects)
			{
				UnityEngine.Object.Destroy(recipeItemPreviewObject);
			}
			recipeItemPreviewObjects.Clear();
		}
		if (recipeItemPreviewObjects == null)
		{
			recipeItemPreviewObjects = new List<GameObject>();
		}
		component.ForceMeshUpdate();
		TMP_TextInfo textInfo = component.textInfo;
		int num = 0;
		float num2 = 9999f;
		bool flag = false;
		for (int i = 0; i < textInfo.characterCount; i++)
		{
			TMP_CharacterInfo tMP_CharacterInfo = textInfo.characterInfo[i];
			if (!tMP_CharacterInfo.isVisible || tMP_CharacterInfo.elementType != TMP_TextElementType.Sprite || (tMP_CharacterInfo.spriteIndex != 23 && tMP_CharacterInfo.spriteIndex != 24))
			{
				continue;
			}
			if (num >= recipe.items.Count)
			{
				break;
			}
			_ = recipe.items[num];
			Item item = itemsForRecipeThorough[num];
			if ((bool)item)
			{
				GameObject gameObject = Utils.Create("Special/RecipeItemPreview", component.transform);
				Vector2 anchoredPosition = new Vector2(-20f, tMP_CharacterInfo.topLeft.y - 20f);
				if (num2 - anchoredPosition.y < 32f)
				{
					anchoredPosition.x += 20f * (flag ? 0f : (-1f));
					flag = !flag;
				}
				else
				{
					flag = false;
				}
				num2 = anchoredPosition.y;
				gameObject.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
				Image component2 = gameObject.transform.GetChild(0).GetComponent<Image>();
				component2.sprite = item.GetComponent<SpriteRenderer>().sprite;
				component2.rectTransform.sizeDelta = ImageSizeDelta(component2.sprite.texture, 4f, 20f);
				recipeItemPreviewObjects.Add(gameObject);
			}
			num++;
		}
		string text = "";
		text = ((!recipe.result.isLiquid) ? (text + Locale.GetOther("craftinfocondition") + (recipe.result.resultCondition * 100f).ToString("0.#") + "%\n") : (text + Locale.GetOther("craftinfovolume") + recipe.result.resultCondition.ToString("0.#") + "mL\n"));
		text = text + Locale.GetOther("craftinfoamount") + recipe.result.amount + "\n";
		text = text + "<color=#" + ColorUtility.ToHtmlStringRGB(RecipeINTToColor(recipe.INT)) + ">";
		text = text + Locale.GetOther("craftinfoint").Replace("<1>", recipe.INT.ToString()).Replace("<2>", body.skills.INT.ToString()) + "\n";
		if (body.skills.INT - recipe.INT == -1)
		{
			text += Locale.GetOther("craftminorfail");
		}
		else if (body.skills.INT - recipe.INT == -2)
		{
			text += Locale.GetOther("craftmajorfail");
		}
		else if (body.skills.INT - recipe.INT == -3)
		{
			text += Locale.GetOther("craftcriticalfail");
		}
		craftingPanel.transform.GetChild(10).GetComponent<TextMeshProUGUI>().text = text;
		craftingPanel.transform.GetChild(11).GetComponent<TextMeshProUGUI>().enabled = !recipe.hasMadeBefore;
	}

	public void TryCraft()
	{
		Recipes.recipes[selectedRecipe].TryMake();
		RefreshRecipeList();
	}

	public void UpdateRecipeFilter()
	{
		recipeFilter = recipeFilterField.text;
		RefreshRecipeList();
	}

	public void ClearRecipeFilter()
	{
		recipeFilter = "";
		recipeFilterField.SetTextWithoutNotify("");
		recipeItemFilter = null;
		RefreshRecipeList();
	}

	public void SelectRecipe(int i)
	{
		selectedRecipe = i;
		PlayUISound(UISoundType.MiniClick);
		RefreshCurrentlySelectedRecipe();
	}

	public void RecipeScrollBackToTop()
	{
		recipeScrollCoroutine = StartCoroutine(RecipeSmoothScrollToTop());
	}

	private IEnumerator RecipeSmoothScrollToTop()
	{
		float elapsed = 0f;
		float startPos = recipeScrollRect.verticalNormalizedPosition;
		while (elapsed < 0.3f)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / 0.3f;
			float verticalNormalizedPosition = Mathf.Lerp(startPos, 1f, Mathf.SmoothStep(0f, 1f, t));
			recipeScrollRect.verticalNormalizedPosition = verticalNormalizedPosition;
			yield return null;
		}
		recipeScrollRect.verticalNormalizedPosition = 1f;
		recipeScrollCoroutine = null;
	}

	public void PinRecipe()
	{
		if (pinnedRecipe.HasValue && pinnedRecipe.Value == selectedRecipe)
		{
			pinnedRecipe = null;
		}
		else
		{
			pinnedRecipe = selectedRecipe;
		}
		PlayUISound(UISoundType.MiniClick);
	}

	public IEnumerator LastStandSequence()
	{
		SetTimeScale(SpeedType.Normal);
		float initialVol = AudioListener.volume;
		AudioListener.volume = 0f;
		float timer = 0f;
		lastStandPanel.SetActive(value: true);
		Image img = lastStandPanel.GetComponent<Image>();
		img.color = Color.black;
		float blackTime = 0f;
		bool playedDrone = false;
		Sound.Play("laststandheartbeat", Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, 1f, noReverb: true, ignoreMixer: true);
		while (timer < 6.7f)
		{
			AudioListener.volume = Mathf.MoveTowards(AudioListener.volume, initialVol, initialVol * Time.unscaledDeltaTime * 0.25f);
			body.bloodOxygen = 100f;
			if (timer > 2f)
			{
				if (!playedDrone)
				{
					playedDrone = true;
					Sound.Play("laststanddrone", Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, 1f, noReverb: true, ignoreMixer: true);
				}
				blackTime += Time.unscaledDeltaTime * 0.25f;
				img.color = new Color(blackTime * UnityEngine.Random.Range(0.75f, 1f), blackTime * UnityEngine.Random.Range(0.75f, 1f), blackTime * UnityEngine.Random.Range(0.75f, 1f));
				int num = (int)(Time.unscaledTime * 3f) % lastStandImages.Length;
				img.sprite = lastStandImages[num];
			}
			timer += Time.unscaledDeltaTime;
			yield return null;
		}
		float alpha = 1f;
		while (true)
		{
			alpha -= Time.unscaledDeltaTime * 0.25f;
			img.color = new Color(1f, 1f, 1f, alpha);
			if (alpha <= 0f)
			{
				break;
			}
			yield return null;
		}
		lastStandPanel.SetActive(value: false);
	}

	public void SetDroplets(Color color)
	{
		blurPass.SetColor("_DropletColor", color);
		blurPass.SetFloat("_DropletNoise", UnityEngine.Random.Range(-9999f, 9999f));
		dropletAmount = 1f;
	}

	public IEnumerator FlashBrain()
	{
		SetTimeScale(SpeedType.Normal);
		yield return new WaitForEndOfFrame();
		Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
		Color clr = new Color(UnityEngine.Random.Range(0.5f, 1f), UnityEngine.Random.Range(0.5f, 1f), UnityEngine.Random.Range(0.5f, 1f));
		brainFlash.color = clr;
		brainFlash.texture = screenshot;
		brainFlash.enabled = true;
		Sound.Play("lobotomy", Vector2.zero, twoDimensional: true, pitchShift: false, null, 0.65f, 1f, noReverb: true, ignoreMixer: true);
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.75f, 2.5f));
		while (brainFlash.color.a > 0f)
		{
			brainFlash.color = new Color(clr.r, clr.g, clr.b, brainFlash.color.a - Time.deltaTime * 0.14f);
			yield return null;
		}
		brainFlash.enabled = false;
		UnityEngine.Object.Destroy(screenshot);
	}

	public IEnumerator FlashBrief()
	{
		SetTimeScale(SpeedType.Normal);
		yield return new WaitForEndOfFrame();
		Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
		Color clr = new Color(UnityEngine.Random.Range(0.9f, 1f), UnityEngine.Random.Range(0.9f, 1f), UnityEngine.Random.Range(0.9f, 1f));
		brainFlash.color = clr;
		brainFlash.texture = screenshot;
		brainFlash.enabled = true;
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.2f, 0.5f));
		while (brainFlash.color.a > 0f)
		{
			brainFlash.color = new Color(clr.r, clr.g, clr.b, brainFlash.color.a - Time.deltaTime * 0.25f);
			yield return null;
		}
		brainFlash.enabled = false;
		UnityEngine.Object.Destroy(screenshot);
	}

	public void SetIrradiateIntensity(float rad)
	{
		irradiateIntensity = Mathf.Max(irradiateIntensity, rad);
		if (rad >= irradiateIntensity)
		{
			timeSinceRadUpdate = 0f;
		}
	}

	public void ToMainMenu()
	{
		SceneManager.LoadScene("PreGen");
	}

	public string AddComaIfTrue(string text, bool bl)
	{
		if (!bl)
		{
			return text;
		}
		return text + ", ";
	}

	public static string ConditionToColorCode(float cond)
	{
		return "<color=#" + ColorUtility.ToHtmlStringRGB(main.itemConditionGradient.Evaluate(cond)) + ">";
	}

	public static Color ConditionToColor(float cond)
	{
		return main.itemConditionGradient.Evaluate(Mathf.Clamp01(cond));
	}

	public static string EndColor()
	{
		return "</color>";
	}

	public void AutoTimeScaleSet(int sc)
	{
		SetTimeScale((SpeedType)sc);
	}

	public void SetTimeScale(SpeedType speed, bool switchSound = true, bool force = false)
	{
		if ((PauseHandler.paused || selfDestructActive || didDeathScreen) && !force)
		{
			return;
		}
		curTimeScale = speed;
		switch (curTimeScale)
		{
		case SpeedType.Normal:
			Time.timeScale = 1f;
			if (switchSound)
			{
				PlayUISound(UISoundType.Time, 0.8f);
			}
			break;
		case SpeedType.Fast:
			Time.timeScale = 5f;
			if (switchSound)
			{
				PlayUISound(UISoundType.Time, 1.1f);
			}
			break;
		case SpeedType.SuperFast:
			Time.timeScale = 20f;
			if (switchSound)
			{
				PlayUISound(UISoundType.Time, 1.5f);
			}
			break;
		case SpeedType.UnconsciousFast:
			Time.timeScale = 25f;
			break;
		case SpeedType.DyingFast:
			Time.timeScale = 3.5f;
			break;
		case SpeedType.Slowmo:
			Time.timeScale = 0.16f;
			break;
		case SpeedType.Paused:
			Time.timeScale = 0f;
			break;
		}
	}

	private void Awake()
	{
		main = this;
		WoundView.view = woundView.GetComponent<WoundView>();
	}

	public void SetUnchippedUI(bool unchipped)
	{
		depthMeter.SetActive(!WorldGeneration.unchipped);
		foreach (GameObject item in WoundView.view.disableWhenUnchipped)
		{
			item.SetActive(!unchipped);
		}
	}

	private void Start()
	{
		showInfection = new bool[body.limbs.Length];
		satSmooth = 1f;
		GetComponent<PixelPerfectCamera>().enabled = Settings.Get<SettingBool>("pixelate").value;
		if (!SaveSystem.loadedRun)
		{
			WoundView.view.SetCharDetails(UnityEngine.Random.Range(130, 161), UnityEngine.Random.Range(10, 26), UnityEngine.Random.Range(1, 100000), UnityEngine.Random.Range(0, 10));
		}
		UITooltip uITooltip = craftingPanel.transform.GetChild(8).gameObject.AddComponent<UITooltip>();
		uITooltip.skipLocale = true;
		uITooltip.tipName = Locale.GetOther("craftingredients");
		uITooltip.tipDesc = Locale.GetOther("craftingredientsdsc").Replace("<>", KeyBinds.GetBindName("favourite"));
		SetTimeScale(SpeedType.Normal, switchSound: false);
		ConsoleScript.instance.RegisterPlayerDetails();
	}

	public void ApplyWoundItem(Item item)
	{
		if (selectedLimb.dismembered)
		{
			return;
		}
		if (!body.aboveMedicalCutoff && !item.Stats.ignoreDepression)
		{
			DoAlert(Locale.GetOther("alerttoounhappy"));
		}
		else if (body.conscious && item.Stats.ActuallyUsableOnLimb(item))
		{
			WaterContainerItem component;
			if (item.Stats.usableOnLimb)
			{
				item.Stats.useLimbAction(selectedLimb, item);
			}
			else if (item.TryGetComponent<WaterContainerItem>(out component))
			{
				component.ApplyToLimb(selectedLimb);
			}
		}
	}

	public void WoundSpecialAction()
	{
		if (body.allowUseItem)
		{
			if (body.conscious)
			{
				if (selectedLimb.TryGetComponent<TourniquetScript>(out var component))
				{
					component.TakeOff();
				}
				else if (selectedLimb.hasShrapnel)
				{
					MinigameBase.main.StartMinigame(new ShrapnelMinigame(selectedLimb), null);
				}
				if (selectedLimb.TryGetComponent<SplintLimb>(out var component2))
				{
					component2.TakeOff();
				}
				else if (selectedLimb.dislocated)
				{
					MinigameBase.main.StartMinigame(new DislocationMinigame(selectedLimb), null);
				}
			}
		}
		else
		{
			UseFailUnhappiness();
		}
	}

	public void DoBodyWorkout(int index)
	{
		body.StartCoroutine(body.DoWorkout((Body.WorkoutType)index));
		ToggleWoundView();
	}

	public void WoundViewButton()
	{
		ToggleWoundView();
	}

	public void ToggleWoundView(bool sound = true)
	{
		if (!woundView.activeSelf && !body.alive)
		{
			return;
		}
		if (!woundView.activeSelf)
		{
			WoundView.view.workoutList.SetActive(value: false);
		}
		if ((radialOpen && !woundView.activeSelf) || (!woundView.activeSelf && !body.conscious))
		{
			radialOpen = false;
			return;
		}
		if (craftingPanel.activeSelf)
		{
			OpenCraftScreen();
		}
		if (sound)
		{
			PlayUISound(woundView.activeSelf ? UISoundType.Close : UISoundType.HealthPanelOn);
		}
		woundView.SetActive(!woundView.activeSelf);
		woundView.GetComponent<WoundView>().UpdateView();
		woundView.GetComponent<WoundView>().timeOpen = 0f;
		mainView.SetActive(!mainView.activeSelf);
	}

	public void HandleInput()
	{
		if ((bool)ConsoleScript.instance && ConsoleScript.instance.active)
		{
			return;
		}
		body.targetLookPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		if (Input.GetKeyDown(KeyBinds.GetBind("pause")))
		{
			if (PauseHandler.paused)
			{
				PauseHandler.TogglePause();
			}
			else if ((bool)MinigameBase.main.currentMinigame && MinigameBase.main.currentMinigame.CanExit())
			{
				MinigameBase.main.EndMinigame();
			}
			else if (woundView.activeSelf)
			{
				ToggleWoundView();
			}
			else if (craftingPanel.activeSelf)
			{
				OpenCraftScreen();
			}
			else if (tradeMenu.activeSelf)
			{
				ToggleTradeMenu();
			}
			else if (radialOpen)
			{
				radialOpen = false;
			}
			else if (!ScrollableText.textExists)
			{
				PauseHandler.TogglePause();
			}
		}
		if (PauseHandler.paused)
		{
			return;
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("speed1")))
		{
			SetTimeScale(SpeedType.Normal);
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("speed2")))
		{
			SetTimeScale(SpeedType.Fast);
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("speed3")))
		{
			SetTimeScale(SpeedType.SuperFast);
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("craft")) && (!craftingPanel.activeSelf || !recipeFilterField.isFocused))
		{
			OpenCraftScreen();
		}
		body.moveDir.x = 0f;
		body.moveDir.y = 0f;
		if (Input.GetKeyDown(KeyBinds.GetBind("iteminteract")) && craftingPanel.activeSelf)
		{
			OpenCraftScreen();
		}
		if (craftingPanel.activeSelf || tradeMenu.activeSelf)
		{
			return;
		}
		if ((bool)MinigameBase.main.currentMinigame)
		{
			if (Input.GetKeyDown(KeyBinds.GetBind("iteminteract")) && MinigameBase.main.currentMinigame.CanExit())
			{
				MinigameBase.main.EndMinigame();
			}
			return;
		}
		body.moveDir.x = (Input.GetKey(KeyBinds.GetBind("right")) ? 1f : 0f) - (Input.GetKey(KeyBinds.GetBind("left")) ? 1f : 0f);
		body.moveDir.y = (Input.GetKey(KeyBinds.GetBind("up")) ? 1f : 0f) - (Input.GetKey(KeyBinds.GetBind("down")) ? 1f : 0f);
		if (Input.GetKeyDown(KeyBinds.GetBind("right")) || Input.GetKeyDown(KeyBinds.GetBind("left")))
		{
			SetTimeScale(SpeedType.Normal, switchSound: false);
		}
		if (Application.isEditor && Input.GetKeyDown(KeyCode.Alpha5))
		{
			SetTimeScale(SpeedType.Slowmo);
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("switchhands")))
		{
			body.SwitchHands();
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("toggleinventory")) && !PauseHandler.paused)
		{
			radialOpen = !radialOpen;
		}
		body.crouching = (!body.reversedControls && Input.GetKey(KeyBinds.GetBind("down"))) || (body.reversedControls && Input.GetKey(KeyBinds.GetBind("up")));
		int handSlot = body.handSlot;
		if (Input.GetKeyUp(KeyBinds.GetBind("throw")) && body.HoldingItem(handSlot) && Time.timeScale <= 5f)
		{
			if (timeHeldThrow >= 0.15f)
			{
				body.ThrowItem(timeHeldThrow * 2f);
			}
			else
			{
				body.DropItem(handSlot);
			}
		}
		if (Input.GetKey(KeyBinds.GetBind("throw")) && body.HoldingItem(handSlot))
		{
			timeHeldThrow += Time.deltaTime * 0.8f;
		}
		else
		{
			timeHeldThrow = 0f;
		}
		if (Input.GetKey(KeyBinds.GetBind("ragdoll")) && Time.timeScale <= 5f)
		{
			body.Ragdoll();
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("jump")) || (autoJump && Input.GetKey(KeyBinds.GetBind("jump"))))
		{
			wantsJumpInput = true;
		}
		if (Input.GetKeyUp(KeyBinds.GetBind("jump")))
		{
			wantsJumpInput = false;
			body.endedJump = true;
		}
		if (wantsJumpInput)
		{
			body.Jump();
			body.endedJump = false;
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("woundview")))
		{
			ToggleWoundView();
		}
		if (Input.GetKeyDown(KeyBinds.GetBind("bark")))
		{
			body.GetComponent<PantSound>().Bark();
		}
		HandleAttacks();
	}

	public static Vector2 ImageSizeDelta(Texture tex, float pixelSize, float maxSize)
	{
		Vector2 result = (Vector3)new Vector2((float)tex.width * pixelSize, (float)tex.height * pixelSize);
		if (result.y > maxSize)
		{
			float num = maxSize / result.y;
			result *= num;
		}
		if (result.x > maxSize)
		{
			float num2 = maxSize / result.x;
			result *= num2;
		}
		return result;
	}

	public void LifePodShake()
	{
		shaker.Shake(400f);
	}

	public static string TimeToString(TimeSpan span)
	{
		string text = "";
		if (span.Days > 0)
		{
			return Locale.GetOther("longtime");
		}
		if (span.Hours > 0)
		{
			return span.Hours + Locale.GetOther("hours");
		}
		if (span.Minutes > 0)
		{
			return span.Minutes + Locale.GetOther("minutes");
		}
		return span.Seconds + Locale.GetOther("seconds");
	}

	public static (string, string) ItemHoverDescription(Item item)
	{
		if (!item)
		{
			return ("", "");
		}
		string fullName = item.fullName;
		string description = item.Stats.description;
		description += "\n\n";
		if (!item.Stats.rec.recognizable)
		{
			string category = item.Stats.category;
			switch (category)
			{
			case "medical":
			case "drug":
			case "tool":
			case "container":
			case "utility":
			case "water":
			case "food":
				fullName = Locale.GetOther("unknown" + category);
				break;
			default:
				fullName = Locale.GetOther("unknownobject");
				break;
			}
			description = Locale.GetOther("unknownobjectdsc") + "\n\n";
			description = description + "<color=#fc037b>" + Locale.GetOther("unknownobjectwarning").Replace("<>", item.Stats.rec.min.ToString() ?? "");
			return (fullName, description);
		}
		if (item.TryGetComponent<BatteryItem>(out var component))
		{
			description = description + component.GetBatteryInfo() + "\n";
		}
		if (item.TryGetComponent<WaterContainerItem>(out var component2))
		{
			description = description + "<color=#" + ColorUtility.ToHtmlStringRGB((component2.CurrentTotal > 0f) ? component2.AverageColor() : Color.white) + "><sprite index=21 tint=1>";
			description = description + Mathf.RoundToInt(component2.CurrentTotal) + "/" + Mathf.RoundToInt(component2.Capacity) + "mL\n";
			if (component2.stack.Count > 0)
			{
				List<string> list = new List<string>();
				foreach (LiquidStack item2 in component2.stack)
				{
					if (Liquids.Registry.TryGetValue(item2.liquidId, out var value))
					{
						string text = (value.localeFromItem ? Locale.GetItem(item2.liquidId) : Locale.GetOther(value.localeName));
						string text2 = Mathf.RoundToInt(item2.amount) + "mL";
						string text3 = "<color=#" + ColorUtility.ToHtmlStringRGB(value.color) + ">" + text + " (" + text2 + ")<color=#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(value.color, Color.white, 0.5f)) + ">";
						if (Input.GetKey(KeyBinds.GetBind("expanddesc")) || alwaysExpandDescriptions)
						{
							text3 = text3 + "\n<alpha=#88>" + Locale.GetOther((value.localeFromItem ? item2.liquidId : value.localeName) + "dsc") + "<alpha=#FF>\n";
							if (value.qualities.Count > 0)
							{
								text3 = text3 + "<sprite index=26 tint=1>" + Locale.GetOther("itemqualities");
								List<string> list2 = new List<string>();
								foreach (CraftingQuality scaledQuality in value.GetScaledQualities(item2.amount))
								{
									list2.Add(scaledQuality.LocaleName + $" ({scaledQuality.amount:0.#})");
								}
								text3 = text3 + string.Join(", ", list2) + "\n";
							}
						}
						list.Add(text3);
					}
					else
					{
						list.Add(item2.liquidId + " " + Mathf.RoundToInt(item2.amount) + "mL");
					}
				}
				description = ((!Input.GetKey(KeyBinds.GetBind("expanddesc")) && !alwaysExpandDescriptions) ? (description + string.Join(", ", list)) : (description + string.Join("\n", list)));
				description += "\n";
			}
		}
		description = description + "<color=#ffffff><sprite index=0 tint=1>" + Locale.GetOther("weight") + ": " + item.totalWeight.ToString("0.##") + "u\n";
		if (Input.GetKey(KeyBinds.GetBind("expanddesc")) || alwaysExpandDescriptions)
		{
			if (item.Stats.rotSpeed > 0f)
			{
				TimeSpan span = item.TimeUntilDecayed();
				if (span.TotalSeconds > 0.0)
				{
					string newValue = TimeToString(span);
					description = description + "<color=#a2dec2><sprite index=5 tint=1>" + Locale.GetOther("decaysin").Replace("<>", newValue);
					ItemInfo.DecayType decayInfo = (ItemInfo.DecayType)item.Stats.decayInfo;
					bool flag = decayInfo.HasFlag(ItemInfo.DecayType.NoDecayWithoutContainerItem);
					bool flag2 = decayInfo.HasFlag(ItemInfo.DecayType.NoDecayWhenStill);
					bool flag3 = decayInfo.HasFlag(ItemInfo.DecayType.NoDecayWhenNotWorn);
					if (flag || flag2 || flag3)
					{
						description = description + " (" + Locale.GetOther("decayonlyif");
						List<string> list3 = new List<string>();
						if (flag3)
						{
							list3.Add(Locale.GetOther("decayonlyworn"));
						}
						if (flag2)
						{
							list3.Add(Locale.GetOther("decayonlymoving"));
						}
						if (flag)
						{
							list3.Add(Locale.GetOther("decayonlyhasitems"));
						}
						description = description + string.Join(", ", list3) + ")";
					}
					description += "\n";
				}
			}
			if (item.Stats.onlyHoldInHands)
			{
				description = description + "<color=#ff8787><sprite index=6 tint=1>" + Locale.GetOther("itemonlyinhands") + "\n";
			}
			if (item.TryGetComponent<Container>(out var component3))
			{
				description = description + "<color=#cf99bf><sprite index=4 tint=1>" + Locale.GetOther("containermaxweight") + " - " + component3.GetHoldingWeight().ToString("0.##") + "/" + component3.maxWeight.ToString("0.##") + "u\n<sprite index=10 tint=1>" + Locale.GetOther("containermaxweightperitem") + " - " + component3.maxWeightPerItem.ToString("0.##") + "u\n<sprite index=0 tint=1>" + Locale.GetOther("containerencumberance") + ": " + (1f - component3.encumberanceMult) * 100f + "%\n";
			}
			if (item.Stats.wearable)
			{
				description = description + "<color=#91d1ff><sprite index=7 tint=1>" + Locale.GetOther("itemwearable") + " \"" + item.Stats.wearSlotId + "\"\n";
				if (item.Stats.wearableArmor > 0f)
				{
					description = description + "<color=#91d1ff><sprite index=13 tint=1>" + Locale.GetOther("itemwearableprotection") + " " + Mathf.RoundToInt(item.Stats.wearableArmor * 100f) + "%\n";
				}
				if (item.Stats.wearableIsolation > 0f)
				{
					description = description + "<color=#91d1ff><sprite index=14 tint=1>" + Locale.GetOther("itemwearableisolation") + " " + item.Stats.wearableIsolation * 100f + "%\n";
				}
			}
			if (item.Stats.usable || item.Stats.ActuallyUsableOnLimb(item))
			{
				description = description + "<color=#a3a3a3><sprite index=11 tint=1>" + Locale.GetOther("itemusable");
				if (item.Stats.usable)
				{
					description = description + "<color=#a6ffaa><sprite index=6 tint=1>" + Locale.GetOther(item.Stats.usableWithLMB ? "itemusablehand" : "itemusableinventory");
				}
				if (item.Stats.usable && item.Stats.ActuallyUsableOnLimb(item))
				{
					description += "<color=#ffffff>/";
				}
				if (item.Stats.ActuallyUsableOnLimb(item))
				{
					description = description + "<color=#fffb91><sprite index=12 tint=1>" + Locale.GetOther("itemusablewound");
				}
				description += "\n";
			}
			if (item.TryGetComponent<AmmoScript>(out var component4) && component4.itemType == AmmoScript.AmmoItemType.Magazine)
			{
				description = description + "<color=#ff8fb0><sprite index=8 tint=1>" + component4.rounds + "/" + component4.maxRounds + " " + Locale.GetOther("magazinerounds") + "\n";
			}
			if (item.Stats.qualities.Count > 0)
			{
				description = description + "<color=#ffc814><sprite index=26 tint=1>" + Locale.GetOther("itemqualities");
				List<string> list4 = new List<string>();
				foreach (CraftingQuality quality in item.Stats.qualities)
				{
					list4.Add(quality.LocaleName + $" ({quality.amount:0.#})");
				}
				description = description + string.Join(", ", list4) + "\n";
			}
			if (item.Stats.GetValue(item) > 0)
			{
				description = (item.GetComponent<BoughtItem>() ? (description + "<color=#ff2b2b><sprite index=19 tint=1>" + Locale.GetOther("tradingblocked") + $" ({TimeSpan.FromSeconds(item.GetComponent<BoughtItem>().time):m\\:ss})\n") : (description + "<color=#f6ff73><sprite index=9 tint=1>" + Locale.GetOther("itemvalue") + item.Stats.GetValue(item) + "\n"));
			}
		}
		else
		{
			if (item.TryGetComponent<Container>(out var component5))
			{
				description = description + "<color=#cf99bf><sprite index=4 tint=1>" + component5.GetHoldingWeight().ToString("0.##") + "/" + component5.maxWeight.ToString("0.##") + "u\n";
			}
			description = description + "<color=#a2e8af><sprite index=2 tint=1><i>" + Locale.GetOther("shifttoexpand") + "</i>";
		}
		if (item.favourited)
		{
			description = description + "\n\n<sprite index=22><color=#EFE27F>" + Locale.GetOther("favouriteditem");
		}
		return (fullName, description);
	}

	private void Update()
	{
		cursor = 0;
		List<RaycastResult> eventSystemRaycastResults = UIUtil.GetEventSystemRaycastResults();
		UsableObject usableObject = null;
		HandleVariables();
		HandleInput();
		HandleWorldUI();
		HandleDeathScreen();
		HandleUnconsciousScreen();
		HandleLimbInfectionShows();
		HandleDepthLine();
		HandleTimescaleIcons();
		HandleCurrentlyHeldIcon();
		HandleBlockPlaceGhost();
		HandleRadiationOverlay();
		HandleMouseOver(eventSystemRaycastResults, ref usableObject);
		HandleDragging(eventSystemRaycastResults, ref usableObject);
		HandleCameraPosition();
		HandleWoundView();
		HandleTradeMenu();
		HandleRadialMenu();
		if (!currentTrader && tradeMenu.activeSelf)
		{
			ToggleTradeMenu();
		}
		HandleTraderOnscreenText();
		HandleAmbientSound();
		HandleCursorIcon();
	}

	private void HandleDragging(List<RaycastResult> uiCasts, ref UsableObject usableObject)
	{
		if (Input.GetKeyDown(KeyBinds.GetBind("iteminteract")) && !MinigameBase.main.currentMinigame && !PauseHandler.paused)
		{
			HandleStartDragging(uiCasts, ref usableObject);
		}
		if (Input.GetKeyUp(KeyBinds.GetBind("iteminteract")))
		{
			HandleReleaseDragging(uiCasts);
		}
		HandleWhileDragging(uiCasts);
	}

	private void HandleStartDragging(List<RaycastResult> uiCasts, ref UsableObject usableObject)
	{
		if (!body.conscious)
		{
			return;
		}
		dragItem = null;
		clickPos = Input.mousePosition;
		if (!body.allowUseItem)
		{
			UseFailUnhappiness();
			return;
		}
		TryPickupFromUI(uiCasts);
		if (!dragItem)
		{
			TryPickupFromWorld(usableObject);
		}
		if ((bool)dragItem)
		{
			dragImage.enabled = true;
			dragImage.transform.localScale = Vector2.one * 1.75f;
			dragImage.sprite = dragItem.GetComponent<SpriteRenderer>().sprite;
			dragImage.rectTransform.sizeDelta = ImageSizeDelta(dragImage.sprite.texture, 5f, 128f);
			PlayUISound(UISoundType.MiniClick);
		}
	}

	private void TryPickupFromUI(List<RaycastResult> uiCasts)
	{
		if ((bool)dragItem)
		{
			return;
		}
		foreach (RaycastResult uiCast in uiCasts)
		{
			if (uiCast.gameObject.TryGetComponent<ItemLabel>(out var component))
			{
				dragItem = component.refItem;
				break;
			}
			if (uiCast.gameObject.TryGetComponent<InvButton>(out var component2) && component2.Overlaps(uiCasts))
			{
				dragItem = component2.GetItem();
				uiCast.gameObject.transform.localScale = Vector3.one * 0.6f;
				break;
			}
		}
	}

	private void TryPickupFromWorld(UsableObject usableObject)
	{
		Collider2D collider2D = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Item"));
		if (!collider2D)
		{
			collider2D = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Body", "Limb"));
		}
		if (!collider2D)
		{
			collider2D = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Descriptor", "Ground"));
		}
		if ((bool)collider2D)
		{
			if (collider2D.TryGetComponent<Item>(out var component))
			{
				dragItem = component;
			}
			else if (collider2D.CompareTag("Player"))
			{
				radialOpen = true;
			}
			else if ((bool)usableObject && Vector2.Distance(body.transform.position, usableObject.transform.position) < 10f * usableObject.rangeMultiplier)
			{
				usableObject.gameObject.SendMessage("OnUse");
				PlayUISound(UISoundType.Click);
			}
		}
	}

	private void HandleReleaseDragging(List<RaycastResult> uiCasts)
	{
		dragImage.enabled = false;
		if (!dragItem)
		{
			if (!UIUtil.IsPointerOverUIElement(uiCasts))
			{
				radialOpen = false;
			}
			return;
		}
		if (!body.DoPickupCheck(dragItem))
		{
			dragItem = null;
			return;
		}
		if (!TryPerformUIActions(uiCasts))
		{
			TryPerformWorldActions();
		}
		if (dragItem.TryGetComponent<Container>(out var component) && Vector2.Distance(Input.mousePosition, clickPos) < 10f)
		{
			radialOpen = true;
			OpenContainer(component);
			if (tradeMenu.activeSelf)
			{
				ToggleTradeMenu();
			}
			if (woundView.activeSelf)
			{
				ToggleWoundView();
			}
		}
		if ((bool)currentContainer)
		{
			RepopulateContainer();
		}
		UpdateWearables();
		PlayUISound(UISoundType.Click);
		dragItem = null;
	}

	private bool TryPerformUIActions(List<RaycastResult> uiCasts)
	{
		foreach (RaycastResult uiCast in uiCasts)
		{
			if (TryPerformInventoryAction(uiCast, uiCasts))
			{
				return true;
			}
			if (TryPerformRadialAction(uiCast))
			{
				return true;
			}
			if (TryPerformSpecialUIAction(uiCast))
			{
				return true;
			}
		}
		return false;
	}

	public void StartLiquidTransfer(WaterContainerItem wat1, WaterContainerItem wat2)
	{
		GameObject obj = Utils.Create("Special/LiquidTransfer", mainCanvas.transform);
		obj.GetComponent<LiquidTransfer>().transferTo = wat1;
		obj.GetComponent<LiquidTransfer>().transferFrom = wat2;
	}

	private bool TryPerformInventoryAction(RaycastResult hit, List<RaycastResult> uiCasts)
	{
		if (!hit.gameObject.TryGetComponent<InvButton>(out var component) || !component.Overlaps(uiCasts))
		{
			return false;
		}
		if (component.GetItem() == dragItem)
		{
			return true;
		}
		Item item = component.GetItem();
		if ((bool)item && (bool)item.battery)
		{
			if (dragItem.Stats.HasTag("tool"))
			{
				item.battery.UnloadBattery();
				return true;
			}
			if (dragItem.Stats.HasTag("battery"))
			{
				item.battery.LoadBattery(dragItem);
				return true;
			}
		}
		if ((bool)item && item.TryGetComponent<Container>(out var component2))
		{
			if (!Input.GetKey(KeyBinds.GetBind("expanddesc")) || !(item != dragItem))
			{
				if (component2.CanHoldItem(dragItem))
				{
					hit.gameObject.transform.localScale = Vector3.one * 1.5f;
				}
				else
				{
					hit.gameObject.transform.localScale = Vector3.one * 0.8f;
				}
				component2.UnloadItem(dragItem);
				component2.LoadItem(dragItem);
				PlayBackpackSound();
				return true;
			}
			List<Item> list = new List<Item>();
			foreach (Transform item2 in dragItem.transform)
			{
				if (item2.TryGetComponent<Item>(out var component3))
				{
					list.Add(component3);
				}
			}
			bool flag = false;
			foreach (Item item3 in list)
			{
				if (component2.CanHoldItem(item3))
				{
					dragItem.container.UnloadItem(item3);
					component2.LoadItem(item3);
					flag = true;
				}
			}
			if (flag)
			{
				PlayBackpackSound();
				return true;
			}
		}
		if ((bool)item && body.CanCombine(item, dragItem))
		{
			body.CombineItems(item, dragItem);
			return true;
		}
		if (component.isBody)
		{
			if (dragItem.Stats.wearable && !dragItem.Stats.wearableCanBeHeld)
			{
				DoAlert(Locale.GetOther("alertpickupwearable"));
				return true;
			}
			if (body.HoldingItem(component.slot) && body.HoldingItem(dragItem))
			{
				body.SwapSlots(component.slot, body.SlotOf(dragItem));
				hit.gameObject.transform.localScale = Vector3.one * 1.5f;
			}
			else
			{
				if (body.HoldingItem(dragItem))
				{
					body.DropItem(dragItem);
				}
				if (body.HoldingItem(component.slot))
				{
					body.DropItem(component.slot);
				}
				body.PickUpItem(dragItem, component.slot);
				hit.gameObject.transform.localScale = Vector3.one * 1.5f;
			}
			return true;
		}
		return false;
	}

	private bool TryPerformRadialAction(RaycastResult hit)
	{
		if (hit.gameObject.CompareTag("RadialCenter") && radialMenu.localScale.x > inventoryUseLeniency && Vector2.Distance(Input.mousePosition, radialCircle.transform.position) / uiScale < radialCircle.rectTransform.sizeDelta.x * 0.5f)
		{
			if (dragItem.Stats.wearable)
			{
				body.WearWearable(dragItem);
			}
			if (dragItem.Stats.usable)
			{
				body.UseItem(dragItem);
			}
			return true;
		}
		return false;
	}

	private bool TryPerformSpecialUIAction(RaycastResult hit)
	{
		if ((bool)hit.gameObject.GetComponent<WoundViewLimb>())
		{
			ApplyWoundItem(dragItem);
			return true;
		}
		if (hit.gameObject.CompareTag("TradeThing") && (bool)currentTrader)
		{
			currentTrader.GiveItem(dragItem);
			return true;
		}
		if (hit.gameObject.CompareTag("ContainerBack") && (bool)currentContainer)
		{
			currentContainer.UnloadItem(dragItem);
			currentContainer.LoadItem(dragItem);
			PlayBackpackSound();
			return true;
		}
		if (hit.gameObject == craftButton)
		{
			if (!craftingPanel.activeSelf)
			{
				OpenCraftScreen();
			}
			SeeRecipesWithItem(dragItem);
			return true;
		}
		return false;
	}

	private void TryPerformWorldActions()
	{
		if ((bool)dragItem.transform.parent && dragItem.transform.parent.TryGetComponent<Container>(out var component))
		{
			component.UnloadItem(dragItem);
			PlayBackpackSound();
		}
		if (body.HoldingItem(dragItem) && !woundView.activeSelf && Vector2.Distance(Input.mousePosition, clickPos) > 10f)
		{
			body.DropItem(dragItem);
		}
		if (dragItem.Stats.wearable && (bool)body.GetWearable(dragItem.id) && Vector2.Distance(Input.mousePosition, clickPos) > 10f)
		{
			body.DropWearable(dragItem);
		}
		Collider2D collider2D = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Item"));
		if ((bool)collider2D && collider2D.TryGetComponent<Container>(out var component2) && !dragItem.GetComponent<Container>())
		{
			PlayBackpackSound();
			component2.UnloadItem(dragItem);
			component2.LoadItem(dragItem);
		}
	}

	private void HandleWhileDragging(List<RaycastResult> uiCasts)
	{
		dragImage.rectTransform.position = Input.mousePosition;
		dragImage.transform.localScale = Vector2.Lerp(dragImage.transform.localScale, Vector2.one, Time.unscaledDeltaTime * 10f);
		radialText.text = Locale.GetOther("inventory");
		radialCircle.color = new Color(0f, 0f, 0f, 0.66f);
		if (uiCasts.Count > 0)
		{
			cursor = 3;
		}
		foreach (RaycastResult uiCast in uiCasts)
		{
			if ((bool)uiCast.gameObject.GetComponent<Button>())
			{
				cursor = 4;
			}
			if (uiCast.gameObject == liquidDrainObject && (bool)dragItem && !FluidManager.main.HasLiquid(WorldGeneration.world.WorldToBlockPos(dragItem.transform.position)) && body.DoPickupCheck(dragItem, noAlerts: true))
			{
				WaterContainerItem component = dragItem.GetComponent<WaterContainerItem>();
				component.Drain(component.CalculateDrain(0.2f * Time.deltaTime * component.Capacity));
			}
			if (uiCast.gameObject.TryGetComponent<InvButton>(out var component2) && component2.Overlaps(uiCasts))
			{
				cursor = 1;
				component2.hovered = dragItem;
				component2.miniHovered = true;
				if ((bool)dragItem && (bool)dragItem.container && (bool)component2.GetItem() && (bool)component2.GetItem().container && Input.GetKey(KeyBinds.GetBind("expanddesc")))
				{
					cursor = 5;
				}
				if (Input.GetKeyDown(KeyBinds.GetBind("favourite")) && (bool)component2.GetItem())
				{
					component2.GetItem().favourited = !component2.GetItem().favourited;
					PlayUISound(UISoundType.MiniClick);
				}
				break;
			}
			if (uiCast.gameObject.TryGetComponent<ItemLabel>(out var component3))
			{
				cursor = 1;
				(string, string) tuple = ItemHoverDescription(component3.refItem);
				component3.tip.tipDesc = tuple.Item2;
				component3.tip.tipName = tuple.Item1;
				component3.hovered = true;
			}
		}
		Image[] array = radialEdgeImages;
		foreach (Image image in array)
		{
			float a = ((radialOpen && (bool)dragItem && !tradeMenu.activeSelf && radialMenu.localScale.x > 0.9f) ? (1f - Mathf.Abs(image.transform.position.x - Input.mousePosition.x) / uiScale * 0.0055f) : 0f);
			image.color = new Color(1f, 1f, 1f, a);
		}
		if ((bool)dragItem)
		{
			if (cursor != 5)
			{
				cursor = 1;
			}
			UpdateRadialWhileDragging(uiCasts);
			if ((bool)Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Body", "Limb")))
			{
				radialOpen = true;
			}
			if ((Mathf.Abs(radialMenu.transform.position.x - Input.mousePosition.x) > 600f * uiScale && !tradeMenu.activeSelf) || body.consciousness < 20f)
			{
				radialOpen = false;
			}
		}
	}

	private void UpdateRadialWhileDragging(List<RaycastResult> uiCasts)
	{
		bool flag = false;
		foreach (RaycastResult uiCast in uiCasts)
		{
			if (uiCast.gameObject.CompareTag("RadialCenter") && radialMenu.localScale.x > inventoryUseLeniency && Vector2.Distance(Input.mousePosition, radialCircle.transform.position) / uiScale < radialCircle.rectTransform.sizeDelta.x * 0.5f)
			{
				flag = true;
			}
		}
		if (dragItem.Stats.usable)
		{
			radialText.text = Locale.GetOther("dragheretouse");
			if (flag)
			{
				radialCircle.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
			}
		}
		else if (dragItem.Stats.wearable)
		{
			radialText.text = Locale.GetOther("dragheretowear");
			if (flag)
			{
				radialCircle.color = new Color(0.25f, 0.25f, 0.5f, 0.9f);
			}
		}
	}

	private void HandleVariables()
	{
		zoomTime -= Time.unscaledDeltaTime;
		threatMusicTime -= Time.deltaTime;
		pinnedRecipeUpdateTime -= Time.unscaledDeltaTime;
	}

	private void HandleAttacks()
	{
		if (woundView.activeSelf || UIUtil.IsPointerOverUIElement() || (bool)MinigameBase.main.currentMinigame || (bool)LiquidTransfer.instance)
		{
			return;
		}
		Item item = body.GetItem(body.handSlot);
		if (Input.GetKey(KeyBinds.GetBind("attack")) && (!item || !item.Stats.usableWithLMB || item.Stats.autoAttack))
		{
			body.UseItemInHand();
		}
		else if (Input.GetKeyDown(KeyBinds.GetBind("attack")) && (!item || !item.Stats.usableWithLMB || !item.Stats.autoAttack))
		{
			if (body.allowUseItem)
			{
				body.UseItemInHand();
				return;
			}
			body.talker.Talk(Locale.GetCharacter("refuse"));
			UseFailUnhappiness();
		}
	}

	private void HandleWoundView()
	{
		if (!woundView.activeSelf)
		{
			woundView.transform.localScale = new Vector3(1f, 0.01f, 1f);
		}
		else
		{
			woundView.transform.localScale = Vector3.Lerp(woundView.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 20f);
		}
		if (lastHeartBeating && !WorldGeneration.unchipped && body.inCardiacArrest)
		{
			Sound.Play("flatline", Vector2.zero, twoDimensional: true, pitchShift: false, null, 0.8f, 1f, noReverb: true, ignoreMixer: true);
		}
		lastHeartBeating = !body.inCardiacArrest;
	}

	private void HandleRadialMenu()
	{
		if (containerMenu.activeSelf)
		{
			containerMenu.transform.localScale = Vector3.Lerp(containerMenu.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 8f);
		}
		if ((bool)currentContainer)
		{
			UpdateContainerInfoText();
		}
		if (craftingPanel.activeSelf)
		{
			radialOpen = false;
		}
		if (radialOpen)
		{
			radialMenu.localScale = Vector3.Lerp(radialMenu.localScale, Vector3.one, Time.unscaledDeltaTime * 8f);
			if (!radialMenu.gameObject.activeSelf)
			{
				radialMenu.gameObject.SetActive(value: true);
				UpdateWearables();
			}
			playerDragIndicator.enabled = false;
			playerDragIndicator.color = Color.clear;
			WaterContainerItem component = null;
			if ((bool)dragItem)
			{
				dragItem.TryGetComponent<WaterContainerItem>(out component);
			}
			liquidDrainObject.SetActive((bool)dragItem && (bool)component && component.CurrentTotal > 0f && !tradeMenu.activeSelf);
			if (liquidDrainObject.activeSelf)
			{
				liquidDrainObject.transform.GetChild(1).GetComponent<Image>().fillAmount = component.CurrentTotal / component.Capacity;
			}
			weightText.text = $"<alpha=#FF>{body.totalEncumberance:0.##}<alpha=#66>/<alpha=#88>{body.maxEncumberance:0.##}<alpha=#66>u";
			return;
		}
		radialMenu.localScale = Vector3.Lerp(radialMenu.localScale, Vector3.one * 0.01f, Time.unscaledDeltaTime * 16f);
		if (radialMenu.localScale.x < 0.05f)
		{
			radialMenu.gameObject.SetActive(value: false);
			if ((bool)currentContainer || containerMenu.activeSelf)
			{
				CloseContainer();
			}
		}
		playerDragIndicator.enabled = true;
		float a = playerDragIndicator.color.a;
		a = Mathf.Lerp(a, ((bool)dragItem && body.standing) ? (1f - Vector2.Distance(body.transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition)) * 0.07f) : 0f, Time.unscaledDeltaTime * 6f);
		playerDragIndicator.color = new Color(1f, 1f, 1f, a);
		playerDragIndicator.transform.position = body.transform.position;
	}

	private void HandleTradeMenu()
	{
		radialMenu.transform.position = Vector3.Lerp(Camera.main.WorldToScreenPoint(body.transform.position), new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f), 0.5f);
		if (tradeMenu.activeSelf)
		{
			radialMenu.anchoredPosition = Vector3.right * (571f - traderPlayerInventoryScootAmount);
			radialOpen = true;
			if ((bool)currentTrader)
			{
				tradeThreatenButton.interactable = currentTrader.reputation >= 30f;
				tradeHaggleButton.interactable = currentTrader.haggleAmount < 10f;
				tradeSleepButton.interactable = body.canTakeNap;
			}
		}
	}

	private void HandleCameraPosition()
	{
		Vector3 vector = Vector2.zero;
		Vector2 vector2 = (new Vector2(Mathf.Clamp(Input.mousePosition.x, 0f, Screen.width) / (float)Screen.width, Mathf.Clamp(Input.mousePosition.y, 0f, Screen.height) / (float)Screen.height) - Vector2.one * 0.5f) * ((zoomTime > 0f) ? 5f : 1f);
		float num = speed;
		if (!body.conscious || (bool)MinigameBase.main.currentMinigame || woundView.activeSelf || craftingPanel.activeSelf)
		{
			vector2 = Vector2.zero;
		}
		if ((bool)following)
		{
			vector = following.position;
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
		if (isFreecam)
		{
			vector = ConsoleScript.instance.freecamPos;
			vector2 = Vector2.zero;
			num /= 4f;
		}
		shaker.Update();
		vector += (Vector3)shaker.pos;
		base.transform.position = Vector2.Lerp(base.transform.position, vector + (Vector3)vector2 * 4f, num * Time.deltaTime);
		float num2 = (float)WorldGeneration.world.height * 0.5f - Camera.main.orthographicSize;
		float num3 = (float)WorldGeneration.world.width * 0.5f - Camera.main.orthographicSize * Camera.main.aspect;
		Vector2 vector3 = base.transform.position;
		vector3.y = Mathf.Clamp(vector3.y, 0f - num2, num2);
		vector3.x = Mathf.Clamp(vector3.x, 0f - num3, num3);
		base.transform.position = vector3;
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y, -3f);
		base.transform.eulerAngles = new Vector3(0f, 0f, shaker.pos.y);
	}

	private void HandleMouseOver(List<RaycastResult> uiCasts, ref UsableObject usableObject)
	{
		hoverSquare.enabled = false;
		hoverSquare.drawMode = SpriteDrawMode.Simple;
		hoverSquare.size = Vector2.one;
		if (uiCasts.Count == 0 || radialOpen)
		{
			HandleWorldHover(ref usableObject);
		}
	}

	private void HandleWorldHover(ref UsableObject usableObject)
	{
		Collider2D collider2D = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Item", "Ground", "Descriptor", "Limb"));
		if (!collider2D)
		{
			collider2D = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), LayerMask.GetMask("Body"));
		}
		if (!body.conscious)
		{
			return;
		}
		if ((bool)collider2D)
		{
			Item component;
			BuildingEntity component2;
			if (collider2D.CompareTag("Player"))
			{
				HandlePlayerHover();
			}
			else if (collider2D.TryGetComponent<Item>(out component))
			{
				HandleItemHover(component);
			}
			else if (collider2D.TryGetComponent<BuildingEntity>(out component2))
			{
				HandleBuildingHover(component2, ref usableObject);
			}
			else
			{
				HandleBlockHover(collider2D);
			}
		}
		else
		{
			HandleFluidHover();
		}
	}

	private void HandlePlayerHover()
	{
		GlobalDark.main.SetTooltip((Locale.GetOther("you"), Locale.GetOther("youdescription")));
		cursor = 4;
	}

	private void HandleItemHover(Item item)
	{
		cursor = 1;
		(string, string) tooltip = ItemHoverDescription(item);
		GlobalDark.main.SetTooltip(tooltip);
		hoverSquare.transform.position = item.transform.position;
		hoverSquare.sprite = item.GetComponent<SpriteRenderer>().sprite;
		hoverSquare.transform.rotation = item.transform.rotation;
		hoverSquare.transform.localScale = item.transform.localScale;
		hoverSquare.flipX = false;
		hoverSquare.enabled = true;
	}

	private void HandleBuildingHover(BuildingEntity build, ref UsableObject usableObject)
	{
		cursor = 2;
		(string, string) tooltip = (build.fullNameDisplay, build.description);
		hoverSquare.transform.position = build.transform.position;
		hoverSquare.sprite = (build.TryGetComponent<SpriteRenderer>(out var component) ? component.sprite : null);
		hoverSquare.drawMode = (component ? component.drawMode : SpriteDrawMode.Simple);
		hoverSquare.size = (component ? component.size : Vector2.one);
		hoverSquare.flipX = (bool)component && component.flipX;
		hoverSquare.transform.rotation = build.transform.rotation;
		hoverSquare.transform.localScale = build.transform.localScale;
		hoverSquare.enabled = true;
		if (build.TryGetComponent<UsableObject>(out usableObject) && Vector2.Distance(body.transform.position, usableObject.transform.position) < 10f * usableObject.rangeMultiplier)
		{
			ref string item = ref tooltip.Item2;
			item = item + "\n\n<color=#FFFFFF><sprite index=20>" + usableObject.toggleString;
			cursor = 4;
		}
		GlobalDark.main.SetTooltip(tooltip);
	}

	private void HandleBlockHover(Collider2D hitcol)
	{
		cursor = 2;
		BlockInfo blockInfo = WorldGeneration.world.GetBlockInfo(WorldGeneration.world.GetBlock(Camera.main.ScreenToWorldPoint(Input.mousePosition)));
		BlockDamage blockDamage = WorldGeneration.world.GetBlockDamage(WorldGeneration.world.WorldToBlockPos(Camera.main.ScreenToWorldPoint(Input.mousePosition)));
		hoverSquare.transform.position = WorldGeneration.world.BlockToWorldPos(WorldGeneration.world.WorldToBlockPos(Camera.main.ScreenToWorldPoint(Input.mousePosition)));
		hoverSquare.enabled = true;
		hoverSquare.sprite = defaultHoverSquareSprite;
		hoverSquare.transform.rotation = Quaternion.identity;
		hoverSquare.transform.localScale = Vector3.one;
		(string, string) tooltip = (blockInfo.name, "");
		if (blockDamage != null)
		{
			tooltip.Item2 = Mathf.RoundToInt((blockInfo.health - blockDamage.damage) / blockInfo.health * 100f) + "%";
		}
		GlobalDark.main.SetTooltip(tooltip);
	}

	private void HandleFluidHover()
	{
		Vector2Int pos = WorldGeneration.world.WorldToBlockPos(Camera.main.ScreenToWorldPoint(Input.mousePosition));
		if (FluidManager.main.WaterInfo(pos).type > 0)
		{
			GlobalDark.main.SetTooltip((FluidManager.main.LiquidName(pos).Item1, FluidManager.main.LiquidName(pos).Item2 + "\n\n<i>" + Locale.GetOther("liquiddescription")));
		}
	}

	private void HandleBlockPlaceGhost()
	{
		placeSquare.enabled = false;
		if (body.HoldingItem(body.handSlot) && body.GetItem(body.handSlot).Stats.HasTag("placeable"))
		{
			placeSquare.enabled = true;
			Vector2 vector = body.transform.position + (body.targetLookPos - body.transform.position).normalized * 5f;
			RaycastHit2D raycastHit2D = Physics2D.Linecast(body.transform.position, vector, LayerMask.GetMask("Ground"));
			if ((bool)raycastHit2D)
			{
				vector = raycastHit2D.point + raycastHit2D.normal * 0.2f;
			}
			placeSquare.transform.position = WorldGeneration.world.BlockToWorldPos(WorldGeneration.world.WorldToBlockPos(vector));
			placeSquare.color = ((!body.canPlaceBlock) ? Color.red : new Color(0.2188857f, 1f, 0f, 32f / 85f));
		}
	}

	private void HandleCurrentlyHeldIcon()
	{
		if (body.HoldingItem(body.handSlot))
		{
			holdingText.text = (body.GetItem(body.handSlot).Stats.rec.recognizable ? body.GetItem(body.handSlot).fullName : Locale.GetOther("unknownobject"));
			Texture2D texture = body.GetItem(body.handSlot).GetComponent<SpriteRenderer>().sprite.texture;
			holdingImage.enabled = true;
			holdingImage.sprite = body.GetItem(body.handSlot).GetComponent<SpriteRenderer>().sprite;
			holdingImage.rectTransform.sizeDelta = ImageSizeDelta(texture, 3f, 77f);
		}
		else
		{
			holdingText.text = Locale.GetOther("unarmed");
			holdingImage.enabled = false;
		}
		if (body.HoldingItem(1 - ((body.handSlot > 1) ? 1 : body.handSlot)))
		{
			holdingSecondaryText.text = (body.GetItem(1 - body.handSlot).Stats.rec.recognizable ? body.GetItem(1 - body.handSlot).fullName : Locale.GetOther("unknownobject"));
		}
		else
		{
			holdingSecondaryText.text = Locale.GetOther("unarmed");
		}
	}

	private void HandleTimescaleIcons()
	{
		timescaleText.text = "x" + Mathf.RoundToInt(Time.timeScale);
		for (int i = 0; i < speedImages.Length; i++)
		{
			if (i != (int)curTimeScale)
			{
				speedImages[i].color = new Color(0.25f, 0.25f, 0.25f);
			}
			else
			{
				speedImages[i].color = new Color(1f, 1f, 1f, 1f);
			}
		}
	}

	private void HandleDepthLine()
	{
		depthLine.anchoredPosition = new Vector2(0f, (0f - WorldGeneration.world.PlayerLayerDepthUnits() / (float)WorldGeneration.world.height) * depthLine.parent.GetComponent<RectTransform>().rect.size.y);
		depthLine.GetComponentInChildren<TextMeshProUGUI>().text = WorldGeneration.world.TotalDepthString();
		if (RadiationLine.line.active)
		{
			radDepthLine.gameObject.SetActive(value: true);
			radDepthLine.anchoredPosition = new Vector2(0f, (0f - WorldGeneration.world.RadlineLayerDepthUnits() / (float)WorldGeneration.world.height) * depthLine.parent.GetComponent<RectTransform>().rect.size.y);
			radDepthLine.GetComponentInChildren<TextMeshProUGUI>().text = WorldGeneration.world.RadlineTotalDepthString();
		}
		else
		{
			radDepthLine.gameObject.SetActive(value: false);
		}
	}

	private void HandleLimbInfectionShows()
	{
		for (int i = 0; i < showInfection.Length; i++)
		{
			if (!body.limbs[i].infected)
			{
				showInfection[i] = false;
			}
			if (body.limbs[i].infectionAmount > 25f)
			{
				showInfection[i] = true;
			}
		}
	}

	public void OnBecameConscious()
	{
		wasUnconscious = false;
		SetTimeScale(SpeedType.Normal, switchSound: false);
	}

	public void OnBecameUnconscious()
	{
		wasUnconscious = true;
		if (woundView.activeSelf)
		{
			ToggleWoundView(sound: false);
		}
	}

	private void HandleUnconsciousScreen()
	{
		float unconsciousBlack = GetUnconsciousBlack();
		bool brainDying = body.brainDying;
		Color color = Color.white;
		float fillAmount = (body.alive ? (body.energy * 0.01f) : 0f);
		if (body.consciousness < 20f && (body.alive || !didDeathScreen))
		{
			if (!wasUnconscious)
			{
				OnBecameUnconscious();
			}
			blackAmount = Mathf.MoveTowards(blackAmount, body.alive ? unconsciousBlack : 1f, Time.unscaledDeltaTime * 2f);
			if (brainDying)
			{
				consciousnessText.text += "!";
				fillAmount = body.brainHealth * 0.01f;
				color = Color.red;
			}
			if (blackAmount >= unconsciousBlack && body.lastTimeStepVelocity.magnitude < 5f)
			{
				if (brainDying)
				{
					SetTimeScale(SpeedType.DyingFast, switchSound: false);
				}
				else
				{
					SetTimeScale(SpeedType.UnconsciousFast, switchSound: false);
				}
			}
			else
			{
				SetTimeScale(SpeedType.Normal, switchSound: false);
			}
		}
		else
		{
			if (wasUnconscious)
			{
				OnBecameConscious();
			}
			blackAmount = Mathf.MoveTowards(blackAmount, body.alive ? Mathf.Clamp01((0.7f - body.consciousness * 0.01f) * 2.1f) : 0.25f, Time.deltaTime * (body.alive ? 0.8f : 0.045f));
			if (blackAmount > unconsciousBlack)
			{
				blackAmount = unconsciousBlack;
			}
		}
		if (blackAmount >= unconsciousBlack && body.consciousness < 20f)
		{
			timeHasBeenBlack = Mathf.MoveTowards(timeHasBeenBlack, 4f, Time.unscaledDeltaTime);
		}
		else
		{
			timeHasBeenBlack = Mathf.MoveTowards(timeHasBeenBlack, 0f, Time.unscaledDeltaTime * 1.5f);
		}
		if (WorldGeneration.unchipped)
		{
			fillAmount = 0f;
		}
		sleepFill.fillAmount = fillAmount;
		sleepFill.color = new Color(color.r, color.g, color.b, (timeHasBeenBlack - 1f) * 0.3334f);
		consciousnessECG.GetComponent<RawImage>().color = sleepFill.color;
		consciousnessECG.active = sleepFill.color.a > 0f;
		consciousnessText.color = sleepFill.color;
		consciousnessVignette.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(consciousnessVignette.color.a, (body.conscious && body.totalHappiness > -90f) ? 0f : (didDeathScreen ? 0.75f : 1f), Time.deltaTime * 0.4f));
		consciousnessFade.color = new Color(0f, 0f, 0f, blackAmount);
	}

	public static float GetUnconsciousBlack()
	{
		if (!WorldGeneration.world.unchippedMode)
		{
			return 0.965f;
		}
		return 1f;
	}

	public IEnumerator EndSequence(int type)
	{
		SetTimeScale(SpeedType.Normal, switchSound: false, force: true);
		if (type == 0)
		{
			PlayerPrefs.SetInt("deathcount", PlayerPrefs.GetInt("deathcount") + 1);
			endScreen.gameObject.SetActive(value: true);
			if (body.inWater)
			{
				endScreen.sprite = deathScreenSprites[2];
			}
			else if (body.totalBleedSpeed > 0.02f)
			{
				endScreen.sprite = deathScreenSprites[1];
			}
			else
			{
				endScreen.sprite = deathScreenSprites[0];
			}
			int num = Mathf.RoundToInt(body.AverageHappiness() * 0.1f);
			deathStats.GetChild(0).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreenstatus");
			deathStats.GetChild(1).GetComponent<TextMeshProUGUI>().text = "2XXX-" + DateTime.Now.ToString("MM-dd-HH-mm-ss");
			deathStats.GetChild(2).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreenunresolved");
			deathStats.GetChild(3).GetComponent<TextMeshProUGUI>().text = "#" + WoundView.specimenId + "-SAW-01";
			deathStats.GetChild(4).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreenphysical");
			deathStats.GetChild(5).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreendeceased");
			deathStats.GetChild(6).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreenmental");
			deathStats.GetChild(7).GetComponent<TextMeshProUGUI>().text = (Locale.GetOther("moodrange" + (body.mindWipe ? "wiped" : ((object)num))) + (body.succesfullyRolledLastStand ? Locale.GetOther("moodrangeyethopeful") : "")).ToUpper();
			deathStats.GetChild(8).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreendepth");
			deathStats.GetChild(9).GetComponent<TextMeshProUGUI>().text = (int)WorldGeneration.world.PlayerTotalDepthMeters() + Locale.GetOther("endscreendepthres") + TimeSpan.FromSeconds(WorldGeneration.TotalRunTime()).ToString("hh\\:mm\\:ss");
			deathStats.GetChild(10).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreencalories");
			deathStats.GetChild(11).GetComponent<TextMeshProUGUI>().text = $"{caloriesConsumed}cal";
			deathStats.GetChild(12).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreencasualties");
			deathStats.GetChild(13).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("endscreenunknown") + " + " + PlayerPrefs.GetInt("deathcount");
			yield return new WaitForSecondsRealtime(3.5f);
			Vector2 origPos2 = new Vector2(-336f, -1000f);
			Vector2 newPos2 = new Vector2(-336f, -934f);
			Sound.Play("receiptPrint", Vector2.zero, twoDimensional: true, pitchShift: false, null, 0.5f, 1f, noReverb: true, ignoreMixer: true);
			float timer2 = 1f;
			while (timer2 > 0f)
			{
				timer2 -= Time.unscaledDeltaTime * 1.5f;
				deathStats.anchoredPosition = Vector2.LerpUnclamped(origPos2, newPos2, 1f - timer2);
				yield return null;
			}
			deathStats.anchoredPosition = newPos2;
			bool clicked = false;
			deathStats.GetComponent<Button>().onClick.AddListener(delegate
			{
				clicked = true;
			});
			while (!clicked)
			{
				yield return null;
			}
			Sound.Play("receiptRip", Vector2.zero, twoDimensional: true, pitchShift: false, null, 0.5f, 1f, noReverb: true, ignoreMixer: true);
			timer2 = 1f;
			origPos2 = new Vector2(-336f, -900f);
			newPos2 = new Vector2(-400f, 0f);
			while (timer2 > 0f)
			{
				timer2 -= Time.unscaledDeltaTime * 1.8f;
				deathStats.anchoredPosition = Vector2.LerpUnclamped(origPos2, newPos2, Utils.OutQuart(1f - timer2));
				yield return null;
			}
			deathStats.anchoredPosition = newPos2;
		}
		endScreen.gameObject.SetActive(value: true);
		yield return null;
	}

	public void CopyEndPaperToClipboard()
	{
		string text = "";
		for (int i = 0; i < 14; i++)
		{
			if (deathStats.GetChild(i).TryGetComponent<TextMeshProUGUI>(out var component))
			{
				text += component.text;
				if (i < 13)
				{
					text += "\n";
				}
			}
		}
		GUIUtility.systemCopyBuffer = text;
	}

	private void HandleDeathScreen()
	{
		if (!body.alive && blackAmount >= 1f && !didDeathScreen)
		{
			didDeathScreen = true;
			finishCoroutine = StartCoroutine(EndSequence(0));
		}
		if (didDeathScreen && body.alive)
		{
			if (finishCoroutine != null)
			{
				StopCoroutine(finishCoroutine);
			}
			endScreen.gameObject.SetActive(value: false);
			if ((bool)PauseHandler.main)
			{
				PauseHandler.main.ResetQuitPressed();
			}
			didDeathScreen = false;
			deathStats.anchoredPosition = new Vector2(-336f, -1000f);
		}
	}

	private void HandleWorldUI()
	{
		Vector2 vector = body.transform.position + (Vector3)body.visualBodyOffset + Vector3.up * (body.col.size.y * 0.5f + body.col.edgeRadius) + (Vector3)body.col.offset;
		vector.x = body.limbs[1].transform.position.x;
		crouchGuide.transform.position = vector;
		crouchGuide.color = Color.Lerp(crouchGuide.color, (body.crouching && body.crouchAmount < 0.97f) ? new Color(1f, 1f, 1f, 0.5f) : Color.clear, Time.deltaTime * 8f);
		if (body.attackCooldown <= 0f)
		{
			lastAttackCool = 0f;
		}
		if (body.liquidDrinkTime > 0f)
		{
			waitImage.fillAmount = body.liquidDrinkTime * 0.5f;
		}
		else if (lastAttackCool > 0f)
		{
			waitImage.fillAmount = body.attackCooldown / lastAttackCool;
		}
		else
		{
			waitImage.fillAmount = timeHeldThrow * 2f;
		}
		waitImage.fillAmount = Mathf.Max(waitImage.fillAmount, bonusActionBarTime);
		waitBackImage.enabled = waitImage.fillAmount > 0f;
		bool flag = (body.liquidRagdollBar < 1f || body.liquidSlipTime > 0f) && body.standing;
		float a = playerLiquidRagdollBar.color.a;
		a = Mathf.MoveTowards(a, flag ? 1f : 0f, Time.deltaTime * 1.5f);
		playerLiquidRagdollBar.color = new Color(1f, 1f - body.liquidSlipTime, 1f - body.liquidSlipTime, a);
		playerLiquidRagdollBar.fillAmount = body.liquidRagdollBar;
	}

	private void HandleRadiationOverlay()
	{
		if (timeSinceRadUpdate > 0.1f)
		{
			irradiateIntensity -= Time.unscaledDeltaTime * ((irradiateIntensity > 1f) ? 2f : 0.5f);
		}
		timeSinceRadUpdate += Time.unscaledDeltaTime;
	}

	private void HandleAmbientSound()
	{
		globalMuteTime = Mathf.MoveTowards(globalMuteTime, 0f, Time.unscaledDeltaTime);
		windvol = Mathf.Lerp(windvol, (body.rb.velocity.magnitude - body.jumpSpeed * 1.4f) * 0.08f, Time.deltaTime * 15f);
		windpitch = Mathf.Lerp(windpitch, 0.8f + body.rb.velocity.magnitude / (body.jumpSpeed * 1.4f) * 0.3f, Time.deltaTime * 15f);
		windSource.volume = windvol;
		windSource.pitch = windpitch;
		underWaterSource.volume = Mathf.Lerp(underWaterSource.volume, body.inWater ? 1f : 0f, Time.deltaTime * 5f);
		WorldGeneration.world.soundMixerGroup.audioMixer.SetFloat("GlobalPitch", Mathf.Lerp(Time.timeScale, 1f, 0.66f));
		float num = 1f - Mathf.Max(body.hearingLoss * 0.01f, 1f - Mathf.Clamp(body.consciousness * 0.01f, 0.3f, 1f), 1f - body.brainHealth * 0.01f);
		num *= num;
		if (body.inWater)
		{
			num = Mathf.Clamp(num, 0f, 0.1f);
		}
		WorldGeneration.world.soundMixerGroup.audioMixer.SetFloat("SoundCutoff", Mathf.Lerp(50f, 22000f, num));
		WorldGeneration.world.soundMixerGroup.audioMixer.SetFloat("Volume", muteTimeVolume);
	}

	private void HandleTraderOnscreenText()
	{
		if ((bool)currentTrader)
		{
			if (string.IsNullOrEmpty(currentTrader.talker.text.text))
			{
				traderTalkText.color = Color.Lerp(traderTalkText.color, Color.clear, Time.deltaTime * 0.6f);
				return;
			}
			traderTalkText.text = currentTrader.talker.text.text;
			traderTalkText.color = currentTrader.talker.text.color;
		}
	}

	private void HandleCursorIcon()
	{
		if (Input.GetKey(KeyBinds.GetBind("iteminteract")) || Input.GetKey(KeyBinds.GetBind("attack")))
		{
			cursor += 6;
		}
		Cursor.SetCursor(cursors[cursor], Vector2.zero, CursorMode.Auto);
	}

	public void TraderToggleCategory(TraderScript.TraderItemPreference cat)
	{
		if ((bool)currentTrader)
		{
			if (currentTrader.collapsedCategories.Contains(cat))
			{
				currentTrader.collapsedCategories.Remove(cat);
			}
			else
			{
				currentTrader.collapsedCategories.Add(cat);
			}
			RefreshTraderInventories();
		}
	}

	public void RefreshTraderInventories()
	{
		if (!tradeMenu.activeSelf)
		{
			return;
		}
		ClearTraderInventories();
		TraderScript.TraderItemPreference traderItemPreference = TraderScript.TraderItemPreference.WantsKeep;
		float num = 0f;
		for (int i = 0; i < currentTrader.items.Count; i++)
		{
			TraderItem item = currentTrader.items[i];
			if (item.preference != traderItemPreference || i == 0)
			{
				traderItemPreference = item.preference;
				GameObject obj = Utils.Create("Special/TraderInvSplit", traderInventory);
				obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f - num);
				obj.GetComponentInChildren<TextMeshProUGUI>().text = Locale.GetOther(traderItemPreference.ToString().ToLower());
				obj.GetComponent<UITooltip>().localeName = Locale.GetOther(traderItemPreference.ToString().ToLower() + "tip");
				obj.transform.GetChild(1).localEulerAngles = new Vector3(0f, 0f, currentTrader.collapsedCategories.Contains(traderItemPreference) ? (-90f) : 0f);
				obj.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate
				{
					TraderToggleCategory(item.preference);
				});
				num += 50f;
			}
			if (!currentTrader.collapsedCategories.Contains(traderItemPreference))
			{
				GameObject gameObject = Utils.Create("Special/TraderItemPanel", traderInventory);
				gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f - num);
				GameObject obj2 = Resources.Load(item.id) as GameObject;
				Sprite sprite = obj2.GetComponent<SpriteRenderer>().sprite;
				if (obj2.TryGetComponent<WaterContainerItem>(out var component) && (bool)component.fillSprite && Item.GetItem(item.id) is LiquidItemInfo { defaultContents: not null } liquidItemInfo && liquidItemInfo.defaultContents.Count > 0)
				{
					gameObject.transform.GetChild(0).GetComponent<Image>().enabled = true;
					gameObject.transform.GetChild(0).GetComponent<Image>().sprite = component.fillSprite;
					gameObject.transform.GetChild(0).GetComponent<Image>().color = Liquids.Registry[liquidItemInfo.defaultContents[0].liquidId].color;
					gameObject.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = ImageSizeDelta(component.fillSprite.texture, 8f, 64f);
				}
				else
				{
					gameObject.transform.GetChild(0).GetComponent<Image>().enabled = false;
				}
				gameObject.transform.GetChild(1).GetComponent<Image>().sprite = sprite;
				gameObject.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = ImageSizeDelta(sprite.texture, 8f, 64f);
				gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = Item.GetItem(item.id).fullName;
				gameObject.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = Item.GetItem(item.id).description;
				gameObject.transform.GetChild(3).GetComponent<UITooltip>().skipLocale = true;
				gameObject.transform.GetChild(3).GetComponent<UITooltip>().tipName = Item.GetItem(item.id).fullName;
				gameObject.transform.GetChild(3).GetComponent<UITooltip>().tipDesc = Item.GetItem(item.id).description;
				gameObject.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = string.Format("{0}{1}", Locale.GetOther("costs"), currentTrader.ItemPrice(item));
				if (currentTrader.ItemPrice(item) == 0)
				{
					gameObject.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = Locale.GetOther("free");
				}
				gameObject.transform.GetChild(5).GetComponent<TextMeshProUGUI>().text = string.Format("{0}{1:0.##}u", Locale.GetOther("weighs"), Item.GetItem(item.id).weight);
				gameObject.transform.GetChild(6).GetComponent<Button>().onClick.AddListener(delegate
				{
					currentTrader.TryPurchase(item);
				});
				gameObject.transform.GetChild(7).GetComponent<Image>().color = TraderScript.PrefToColor(item.preference);
				num += 120f;
			}
		}
		traderInventory.GetComponent<RectTransform>().sizeDelta = new Vector2(traderInventory.GetComponent<RectTransform>().sizeDelta.x, num);
	}

	public void TraderTryHaggle()
	{
		if ((bool)currentTrader)
		{
			currentTrader.TryHaggle();
		}
	}

	public void TraderTryHug()
	{
		if ((bool)currentTrader)
		{
			currentTrader.TryHug();
		}
	}

	public void TraderTryThreaten()
	{
		if ((bool)currentTrader)
		{
			currentTrader.Threaten();
		}
	}

	public void TraderTryRest()
	{
		if ((bool)currentTrader)
		{
			currentTrader.DoRest();
		}
	}

	public void TraderTryMove()
	{
		if ((bool)currentTrader && currentTrader.AskToMove())
		{
			traderMoveButon.SetActive(value: false);
		}
	}

	public void ClearTraderInventories()
	{
		foreach (Transform item in traderInventory)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}

	public void ToggleTradeMenu()
	{
		if (!tradeMenu.activeSelf && !currentTrader)
		{
			return;
		}
		tradeMenu.SetActive(!tradeMenu.activeSelf);
		if (tradeMenu.activeSelf)
		{
			RefreshTraderInventories();
			UpdateTradeTexts();
			if (woundView.activeSelf)
			{
				ToggleWoundView();
			}
			PlayUISound(UISoundType.Click);
			if (currentTrader.character == 2)
			{
				traderHagglebutton.sprite = Resources.Load<Sprite>("Sprites/dunehaggle");
				traderHagglebutton.transform.parent.GetComponent<UITooltip>().skipLocale = true;
				traderHagglebutton.transform.parent.GetComponent<UITooltip>().tipName = Locale.GetOther("haggledune");
				traderHagglebutton.transform.parent.GetComponent<UITooltip>().tipDesc = Locale.GetOther("haggledunedsc");
			}
			else
			{
				traderHagglebutton.sprite = Resources.Load<Sprite>("Sprites/haggleIcon");
				traderHagglebutton.transform.parent.GetComponent<UITooltip>().skipLocale = true;
				traderHagglebutton.transform.parent.GetComponent<UITooltip>().tipName = Locale.GetOther("haggle");
				traderHagglebutton.transform.parent.GetComponent<UITooltip>().tipDesc = Locale.GetOther("haggledsc");
			}
			traderMoveButon.SetActive(currentTrader.farEnoughToMove && !currentTrader.didMove);
			string text = CharacterIndexToLocaleName();
			traderNameText.text = Locale.GetOther(text);
			traderNameText.GetComponent<UITooltip>().skipLocale = true;
			traderNameText.GetComponent<UITooltip>().tipName = Locale.GetOther(text);
			traderNameText.GetComponent<UITooltip>().tipDesc = Locale.GetOther(text + "traderdsc");
		}
		else
		{
			ClearTraderInventories();
			PlayUISound(UISoundType.Close);
		}
	}

	public string CharacterIndexToLocaleName()
	{
		string result = "";
		switch (currentTrader.character)
		{
		case 0:
			result = "experiment";
			break;
		case 1:
			result = "milky";
			break;
		case 2:
			result = "dune";
			break;
		case 3:
			result = "shelly";
			break;
		}
		return result;
	}

	public IEnumerator DoAlertDelayed(string text, bool important, float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		DoAlert(text, important);
	}

	public void UpdateTradeTexts()
	{
		if ((bool)currentTrader)
		{
			traderRepText.text = "<sprite index=18>" + Math.Clamp((int)currentTrader.reputation, 0, 200);
			traderValueText.text = "<sprite index=9>" + currentTrader.valueGiven + " <color=#333333><size=70%>(" + currentTrader.totalValueGiven + "/" + TraderScript.MAX_VALUE_GIVEN + ")";
			traderRepBar.fillAmount = currentTrader.reputation / 200f;
			traderRepBar.GetComponent<Image>().color = traderRepGradient.Evaluate(currentTrader.reputation / 200f);
		}
	}

	public void DoAlert(string text, bool important = false)
	{
		alertTime = 6f;
		alertText.text = text;
		alertBack.rectTransform.anchoredPosition = new Vector2(0f, important ? 0f : (-400f));
	}

	private void FixedUpdate()
	{
		if ((bool)dragItem && dragItem.rb.simulated && Vector2.Distance(dragItem.transform.position, body.transform.position) < 10f)
		{
			dragItem.rb.velocity *= 0.85f;
			dragItem.rb.angularVelocity *= 0.85f;
		}
	}

	public void UpdateWearables()
	{
		List<Item> allWearables = body.GetAllWearables();
		foreach (GameObject unwearButton in unwearButtons)
		{
			UnityEngine.Object.Destroy(unwearButton);
		}
		unwearButtons.Clear();
		for (int i = 0; i < allWearables.Count; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load("GraphicWearDropButton"), wearPopulate.transform) as GameObject;
			gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2((float)i * 110f - ((float)allWearables.Count - 1f) * 55f, 0f);
			gameObject.GetComponent<InvButton>().refItem = allWearables[i];
			gameObject.GetComponent<InvButton>().UpdateRefs();
			gameObject.GetComponent<InvButton>().UpdateGraphic();
			unwearButtons.Add(gameObject);
		}
	}

	public void OpenContainer(Container cont)
	{
		PlayBackpackSound();
		if ((bool)currentContainer)
		{
			ClearContainer();
		}
		currentContainer = cont;
		containerMenu.SetActive(value: true);
		containerMenu.transform.localScale = Vector3.one * 0.01f;
		RepopulateContainer();
	}

	public void ClearContainer()
	{
		foreach (GameObject containerUnloadButton in containerUnloadButtons)
		{
			UnityEngine.Object.Destroy(containerUnloadButton);
		}
		containerUnloadButtons.Clear();
	}

	public void UpdateContainerInfoText()
	{
		if ((bool)currentContainer)
		{
			containerText.text = $"{currentContainer.GetHoldingWeight():0.##}/{currentContainer.maxWeight:0.##}u";
			containerFill.fillAmount = currentContainer.GetHoldingWeight() / currentContainer.maxWeight;
			containerIcon.sprite = currentContainer.GetComponent<SpriteRenderer>().sprite;
			containerIcon.rectTransform.sizeDelta = ImageSizeDelta(containerIcon.sprite.texture, 4f, 36f);
		}
	}

	public void RepopulateContainer()
	{
		ClearContainer();
		if ((bool)currentContainer)
		{
			UpdateContainerInfoText();
			for (int i = 0; i < currentContainer.transform.childCount; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load("ContainerItem"), contPopulate.transform) as GameObject;
				RectTransform component = gameObject.GetComponent<RectTransform>();
				Item component2 = currentContainer.transform.GetChild(i).GetComponent<Item>();
				component.anchoredPosition = new Vector2(y: -34.5f - (float)(int)((float)i / 3f) * 66f, x: 34.5f + (float)(i % 3) * 66f);
				gameObject.GetComponent<InvButton>().refItem = component2;
				gameObject.GetComponent<InvButton>().UpdateRefs();
				gameObject.GetComponent<InvButton>().UpdateGraphic();
				containerUnloadButtons.Add(gameObject);
			}
		}
	}

	public void CloseContainer()
	{
		ClearContainer();
		containerMenu.SetActive(value: false);
		currentContainer = null;
	}

	public void UseFailUnhappiness()
	{
		PlayUISound(UISoundType.Deny);
		body.talker.Talk(Locale.GetCharacter("refuse"));
	}

	private void LateUpdate()
	{
		craftingPanel.transform.localScale = Vector3.Lerp(craftingPanel.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 8f);
		if (craftingPanel.activeSelf)
		{
			searchClearButton.SetActive(!string.IsNullOrEmpty(recipeFilter));
			recipesBackToTopButton.SetActive(recipeScrollRect.verticalNormalizedPosition < 0.999f && recipeScrollRect.content.rect.height > recipeScrollRect.viewport.rect.height && recipeScrollCoroutine == null);
			pinRecipeImage.color = ((pinnedRecipe.HasValue && pinnedRecipe.Value == selectedRecipe) ? Color.green : Color.white);
		}
		if (pinnedRecipeUpdateTime < 0f)
		{
			pinnedRecipeUpdateTime = 0.5f;
			if (pinnedRecipe.HasValue)
			{
				pinRecipeText.text = Recipes.recipes[pinnedRecipe.Value].fullName + ":\n" + IngredientTextForRecipe(Recipes.recipes[pinnedRecipe.Value]);
			}
			else
			{
				pinRecipeText.text = "";
			}
		}
		if (previousRadialOpen != radialOpen)
		{
			PlayUISound(radialOpen ? UISoundType.InventoryOpen : UISoundType.InventoryClose);
			previousRadialOpen = radialOpen;
		}
		worldCanvas.transform.position = body.transform.position;
		HandleHealthPanelButtonSprite();
		HandleAlertPopupOpacity();
		HandleThrowLine();
		HandleGunMenu();
		HandleOnscreenCharacterTalkText();
		HandleScreenShaders();
		HandleLineOfSight();
		Item item = body.GetItem(body.handSlot);
		if ((bool)item && item.Stats.usableWithLMB && !GlobalDark.main.tooltipRect.gameObject.activeSelf)
		{
			selectedItemOverlay.sprite = item.GetComponent<SpriteRenderer>().sprite;
			selectedItemOverlay.rectTransform.sizeDelta = ImageSizeDelta(selectedItemOverlay.sprite.texture, 2f, 64f);
			selectedItemOverlay.rectTransform.anchoredPosition = Input.mousePosition / uiScale + (Vector3.right * 32f + Vector3.down * 32f);
			selectedItemOverlay.color = new Color(1f, 1f, 1f, 1f);
		}
		else
		{
			selectedItemOverlay.color = Color.clear;
		}
		currentTrader = null;
	}

	public void HandleLineOfSight()
	{
		if (!WorldGeneration.lineOfSightEnabled)
		{
			return;
		}
		List<Vector2> list = new List<Vector2>();
		float num = lineOfSightRes;
		float num2 = 50f;
		Vector2 vector = body.transform.position;
		for (int i = 0; (float)i < num; i++)
		{
			float f = (float)i / num * MathF.PI * 2f;
			Vector2 vector2 = new Vector2(Mathf.Cos(f), Mathf.Sin(f));
			Vector2 startWorld = vector;
			Vector2 vector3 = (Vector2)body.transform.position + vector2 * num2;
			Vector2 vector4 = vector3;
			if (WorldGeneration.world.Linecast(startWorld, vector3, out var hitEntryPosition, out var _))
			{
				vector4 = hitEntryPosition;
			}
			Vector2 vector5 = vector4;
			if (list.Count > 0)
			{
				if (list[list.Count - 1] == vector5)
				{
					continue;
				}
			}
			list.Add(vector5);
		}
		LOSHole.GetComponent<VisionMask>().UpdateMask(vector, list);
	}

	private void HandleScreenShaders()
	{
		dirMultSmooth = Mathf.Lerp(dirMultSmooth, body.isRight ? 1f : (-1f), Time.deltaTime * 8f);
		painSmooth = Mathf.Lerp(painSmooth, body.averagePain, Time.deltaTime * 6f);
		painbeatTime += Time.unscaledDeltaTime * 7f * painSmooth * 0.01f;
		satSmooth = Mathf.MoveTowards(satSmooth, 1f + (body.onHardStimulants ? 3f : 0f), Time.deltaTime * 0.05f);
		float num = Mathf.Max(0f, painSmooth + Mathf.Sin(painbeatTime) * (painSmooth * 0.15f));
		float b = 0f;
		float num2 = 0f;
		if (body.TryGetComponent<Painkillers>(out var component))
		{
			b = component.actualOpiateReception * 0.015f;
			if (component.actualOpiateReception < 0f)
			{
				num2 = (0f - component.actualOpiateReception) * 0.05f;
				if (component.actualOpiateReception < -30f)
				{
					num2 += 0f - (component.actualOpiateReception + 30f);
				}
			}
		}
		float num3 = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
		float b2 = Mathf.Clamp01((25f - body.hunger) / 25f);
		float b3 = Mathf.Clamp01((25f - body.thirst) / 25f);
		float num4 = (100f - body.brainHealth) * 0.03f + bonusAbber;
		float num5 = Mathf.Clamp01((body.averagePain - 50f) * 0.0025f);
		float num6 = (100f - body.brainHealth) * 0.01f / 0.7f;
		dropletAmount = Mathf.MoveTowards(dropletAmount, 0f, Time.deltaTime / 25f);
		hungerSmooth = Mathf.Lerp(hungerSmooth, b2, Time.deltaTime);
		thirstSmooth = Mathf.Lerp(thirstSmooth, b3, Time.deltaTime);
		sickSmooth = Mathf.Lerp(sickSmooth, body.sicknessAmount * 0.01f, Time.deltaTime);
		waterSmooth = Mathf.Lerp(waterSmooth, body.inWater ? 0.75f : 0f, Time.deltaTime * 4f);
		dropletSmooth = Mathf.Lerp(dropletSmooth, dropletAmount, Time.deltaTime * 4f);
		coldSmooth = Mathf.Lerp(coldSmooth, Mathf.Clamp01((34f - body.temperature) * 0.17f), Time.deltaTime);
		dirtSmooth = Mathf.Lerp(dirtSmooth, body.dirtyness * 0.01f, Time.deltaTime * 5f);
		sadSmooth = Mathf.Lerp(sadSmooth, body.totalHappiness, Time.deltaTime * 0.5f);
		oxySmooth = Mathf.Lerp(oxySmooth, Mathf.Max((3.5f - body.bloodOxygen * 0.035f) * num3 + Mathf.Max(120f - body.bloodPressure, 0f) / 30f, b), Time.deltaTime);
		mainScreenPass.SetFloat("_Pain", num * 0.01f);
		mainScreenPass.SetFloat("_LowOxyAmt", oxySmooth);
		mainScreenPass.SetFloat("_BloodAmount", ((bool)body.mindWipe && body.mindWipe.active) ? 0f : (body.bloodVolume * 0.01f));
		mainScreenPass.SetFloat("_Stamina", Mathf.Clamp01(body.stamina * 0.01f + 0.3f) - (((bool)body.mindWipe && body.mindWipe.active) ? 0.7f : 0f) - thirstSmooth);
		mainScreenPass.SetFloat("_Consciousness", 1f - WorldGeneration.world.fogAmount);
		mainScreenPass.SetFloat("_FrostAmount", coldSmooth);
		mainScreenPass.SetFloat("_OverheatAmount", Mathf.Clamp01((body.temperature - 38.5f) * 0.25f));
		mainScreenPass.SetFloat("_BadFocusAmount", body.harmer.blackAmount + body.vomiter.badLookAmount);
		mainScreenPass.SetFloat("_InsanityFX", Mathf.Clamp01(sadSmooth * -0.01f));
		mainScreenPass.SetFloat("_BrightMult", (WorldGeneration.world.biomeDepth == 0) ? Mathf.Clamp01(WorldGeneration.world.timeSinceFinishedGeneration * 0.16f - 0.2f) : 1f);
		mainScreenPass.SetVector("_PlayerPos", body.transform.position);
		mainScreenPass.SetFloat("_Dirtyness", dirtSmooth);
		mainScreenPass.SetFloat("_SaturationMult", satSmooth);
		mainScreenPass.SetFloat("_Brightness", brightness);
		blurPass.SetFloat("_BlurIntensity", Mathf.Clamp01(1f - body.consciousness * 0.01f) * 1.5f + (body.bothEyesGone ? 2f : 0f) + waterSmooth * 2f);
		blurPass.SetFloat("_HungerIntensity", hungerSmooth);
		blurPass.SetFloat("_DropletAmount", dropletSmooth);
		blurPass.SetFloat("_SicknessAmount", sickSmooth);
		blurPass.SetFloat("_Insanity", Mathf.Clamp01((sadSmooth * -0.01f).Remap(0.25f, 0.85f, 0f, 1f)));
		bonusAbber = Mathf.MoveTowards(bonusAbber, 0f, Time.deltaTime);
		bonusPass.SetFloat("_Intensity", num2 + num4);
		bonusPass.SetFloat("_Distort", 0f - num5 - Mathf.Max(1f - (body.bloodOxygen * 0.01f - 0.3f) / 0.5f, b) * 0.1f);
		bonusPass.SetFloat("_FlipScreen", body.reversedControls ? 1f : 0f);
		float num7 = 0f;
		if (body.eyeGone)
		{
			num7 = 1f;
		}
		if (body.bothEyesGone || (bool)body.GetWearable("blindfold"))
		{
			num7 = 2f;
		}
		num7 += body.strokeAmount * 0.7f / 100f;
		damagePass.SetFloat("_EyeDamage", num7);
		damagePass.SetFloat("_BrainDamage", num6 * num6);
		damagePass.SetFloat("_Bloodloss", 0.4f - body.bloodVolume * 0.004f + (1f - body.consciousness * 0.01f) * 0.1f + hungerSmooth * 0.1f + thirstSmooth * 0.1f);
		damagePass.SetVector("_Pos", body.transform.position);
		damagePass.SetFloat("_DirectionMult", dirMultSmooth);
		damagePass.SetFloat("_RadAmount", WorldGeneration.unchipped ? 0f : Mathf.Clamp01(irradiateIntensity));
		WorldGeneration.world.fogMat.SetVector("_PlayerPos", body.transform.position);
	}

	private void HandleHealthPanelButtonSprite()
	{
		if (woundView.activeSelf)
		{
			woundButton.sprite = woundButtonScreen;
		}
		woundButton.sprite = ((!woundView.activeSelf && body.shouldCheckHealthPanel && Mathf.PingPong(Time.unscaledTime, 0.3f) > 0.15f && !WorldGeneration.unchipped) ? woundButtonFlash : woundButtonNormal);
	}

	private void HandleAlertPopupOpacity()
	{
		alertTime -= Time.unscaledDeltaTime;
		float num = 1f;
		num = ((!(alertTime > 5f)) ? (alertTime * 0.4f) : ((6f - alertTime) * 2f));
		alertBack.color = new Color(1f, 1f, 1f, num);
		alertText.color = new Color(1f, 1f, 1f, num);
	}

	private void HandleOnscreenCharacterTalkText()
	{
		if (string.IsNullOrEmpty(body.talker.text.text))
		{
			charTalkText.color = Color.Lerp(charTalkText.color, Color.gray, Time.deltaTime * 0.6f);
		}
		else
		{
			charTalkText.text = body.talker.text.text;
			charTalkText.color = body.talker.text.color;
		}
		charTalkText.enabled = woundView.activeSelf || tradeMenu.activeSelf;
	}

	private void HandleThrowLine()
	{
		if (timeHeldThrow >= 0.15f && body.HoldingItem(body.handSlot))
		{
			throwRenderer.enabled = true;
			Vector2 vector = body.ThrowVelocity(body.GetItem(body.handSlot), Mathf.Clamp01(timeHeldThrow * 2f));
			Vector2 vector2 = body.slots[body.handSlot].transform.position;
			for (int i = 0; i < throwRenderer.positionCount; i++)
			{
				vector2 += vector * Time.fixedDeltaTime;
				vector += Physics2D.gravity * Time.fixedDeltaTime;
				throwRenderer.SetPosition(i, vector2);
			}
		}
		else
		{
			throwRenderer.enabled = false;
		}
	}

	private void HandleGunMenu()
	{
		if (body.HoldingItem(body.handSlot) && body.GetItem(body.handSlot).Stats.HasTag("gun"))
		{
			gunMenu.SetActive(!radialOpen);
			GunScript component = body.GetItem(body.handSlot).GetComponent<GunScript>();
			gunRackImage.sprite = (component.racked ? gunRackedSprite : gunNormalSprite);
			gunMagButton.interactable = component.feedType != GunScript.FeedType.Direct && component.hasMag;
			gunSafeImage.sprite = (component.safe ? gunSafeSprite : gunUnsafeSprite);
			gunBulletImage.sprite = gunBulletSprites[(int)(WorldGeneration.unchipped ? GunScript.RoundInChamber.None : component.roundInChamber)];
			gunCrosshair.gameObject.SetActive(!component.safe);
			float num = Vector2.Distance(component.barrel.transform.position, body.targetLookPos);
			if (!body.isRight)
			{
				num *= -1f;
			}
			Vector2 vector = component.barrel.transform.position + component.transform.right * num;
			gunCrosshair.position = Camera.main.WorldToScreenPoint(vector);
		}
		else
		{
			gunMenu.SetActive(value: false);
			gunCrosshair.gameObject.SetActive(value: false);
		}
	}
}

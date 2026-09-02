using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[JsonObject(MemberSerialization.OptIn)]
public class Body : MonoBehaviour
{
	public enum SleepQuality
	{
		Bad,
		Mediocre,
		Okay,
		Good
	}

	public enum WorkoutType
	{
		Pushups,
		Squats,
		Plank
	}

	public enum LimbNum
	{
		Head = 0,
		UpTorso = 1,
		DownTorso = 2,
		ArmF = 3,
		ArmB = 6,
		LegF = 9,
		LegB = 12
	}

	public Limb[] limbs;

	public Limb[] legLimbs;

	public Limb baseLimb;

	public Animator bodyAnimator;

	public AnimationClip idleClip;

	public Animator armsAnimator;

	public Rigidbody2D rb;

	public bool standing;

	private float baseMass;

	[HideInInspector]
	public BoxCollider2D col;

	public bool isRight;

	[JsonProperty]
	public float maxSpeed;

	public Vector3 targetLookPos;

	public AnimationCurve staminaStrength;

	public Vomiter vomiter;

	[HideInInspector]
	public float idleTime;

	private bool bothLegsAmputated;

	public const float interactionRange = 10f;

	public float stimulantMultiplier;

	[JsonProperty]
	public float jumpSpeed;

	[JsonProperty]
	public float temporarySlowdown;

	private float jumpCooldown;

	public bool grounded = true;

	public Vector2 moveDir;

	[JsonProperty]
	public float moveForce;

	public float liquidSlipTime;

	public float liquidRagdollBar;

	[JsonProperty]
	public float slowdownAmount;

	public Vector2 lastTimeStepVelocity;

	private Vector2 origColSize;

	private bool slidingLeft;

	private bool slidingRight;

	private float timeSinceSlidLeft;

	private float timeSinceSlidRight;

	public float wallSlideSlowdown;

	public float timeSlidfor;

	private bool lastJumpedOnRightWall;

	private bool firstWallJump;

	[Header("Body stats")]
	[JsonProperty]
	public float bloodOxygen = 100f;

	[JsonProperty]
	public float bloodVolume = 100f;

	[JsonProperty]
	public float heartRate = 70f;

	[JsonProperty]
	public float respiratoryRate = 100f;

	[JsonProperty]
	public float bloodPressure = 120f;

	[JsonProperty]
	public float bloodVesselSize = 1f;

	[JsonProperty]
	public float fibrillationProgress;

	[JsonProperty]
	public bool fibrillationForced;

	[JsonProperty]
	public float bloodViscosity;

	[JsonProperty]
	[HideInInspector]
	public float heartRatePressureOffset;

	public string bloodPressureReadout = "120/80";

	[JsonProperty]
	public float adrenaline;

	[JsonProperty]
	public float curAdrenaline;

	[JsonProperty]
	public float happiness;

	public float opiateHappiness;

	public float antidepressantHappiness;

	[JsonProperty]
	public float weightOffset;

	[JsonProperty]
	public float hunger = 100f;

	[JsonProperty]
	public float thirst = 100f;

	[JsonProperty]
	public float stamina = 100f;

	[JsonProperty]
	public float energy = 100f;

	[JsonProperty]
	public float brainHealth = 100f;

	[JsonProperty]
	public float consciousness = 100f;

	[JsonProperty]
	public float shock;

	public bool sleeping;

	[JsonProperty]
	public float temperature;

	public float clothingTemperature;

	public float averagePain;

	public float totalBleedSpeed;

	public InventorySlot[] slots;

	public bool breathing = true;

	public float eatTime;

	public float attackCooldown;

	public float crouchAmount;

	public bool crouching;

	[JsonProperty]
	public float sicknessAmount;

	private AudioSource slideSource;

	public Talker talker;

	public float eyeCloseTime;

	public float eyeScareTime;

	public float eyePanicTime;

	public AnimationCurve weightMovementCurve;

	public AnimationCurve temperatureMovementCurve;

	public AnimationCurve foodMovementCurve;

	private float secondCheckTime;

	public static float consciousnessRiseRate = 3f;

	public static float consciousnessFallRate = 12f;

	[JsonProperty]
	public float desensitizedMult = 1f;

	[JsonProperty]
	public int corpsesSeen;

	public float soundCooldown;

	public float bonusRot;

	public float accelRot;

	public float attackRot;

	private float timeRagdolled;

	private float timeSinceGrounded;

	private BlockInfo standingOn;

	[JsonProperty]
	public float septicShock;

	public SelfHarmer harmer;

	private float lastHeadAngle;

	[JsonProperty]
	public bool disfigured;

	[JsonProperty]
	public bool eyeGone;

	[JsonProperty]
	public bool bothEyesGone;

	public Vector2 visualBodyOffset;

	public float overrideLookTime;

	public Vector2 overrideLookPos;

	private float torsoLookSmooth;

	private float extraCrouchSmooth;

	public int charType;

	public float armOffset;

	public ParticleSystem wallSlideParticle;

	public float standLerpTime;

	public float totalEncumberance;

	public float overEncumberance;

	private float halfSecondCheckTime;

	public float limpAnimatorSpeed;

	[JsonProperty]
	public float radiationSickness;

	public float minuteCheckTime;

	public float halfMinuteCheckTime;

	public float maxEncumberance = 11f;

	public float fallShakeCooldown;

	[JsonProperty]
	public float caffeinated;

	public int handSlot;

	[JsonProperty]
	public float hearingLoss;

	[JsonProperty]
	public float internalBleeding;

	[JsonProperty]
	public float hemothorax;

	public bool forceWalk;

	[JsonProperty]
	public float painShock;

	public float limbBloodUpdateTimer;

	[JsonProperty]
	public float traumaAmount;

	public AnimationCurve hungerLimbHeal;

	private float burpTimer = -100f;

	private bool bodyLerpFromRagdoll;

	[HideInInspector]
	public int overdoseIndex;

	public AudioClip[] impactSmall;

	public AudioClip[] impactMedium;

	public AudioClip[] impactLarge;

	private float crawlTime;

	public bool reversedControls;

	public Gradient furColors;

	public bool endedJump;

	public AnimationCurve depressionChanceCurve;

	[JsonProperty]
	public float wetness;

	public bool specialCrying;

	public float tempCheckTime;

	public bool inWater;

	private Color lastLiquidColor;

	public float liquidDrinkTime;

	public LiquidAffect bodyAffect;

	private float wetShakeTime;

	public float dogShakeIntensity;

	public float brainShakeIntensity;

	public float miscShakeIntensity;

	public bool hasScubaGear;

	public MindwipeScript mindWipe;

	public SleepQuality curSleep;

	[JsonProperty]
	public float badSleepAmount;

	[JsonProperty]
	public float goodSleepTime;

	private bool slippery;

	[JsonProperty]
	public float snowAmount;

	[JsonProperty]
	public float immunity;

	public AnimationCurve immunityInfectionSpeed;

	[JsonProperty]
	public float antibioticImmunityTime;

	public float curImmunityMult;

	public Transform tail;

	public float[] lastHappiness = new float[10];

	[JsonProperty]
	public bool triedRollingLastStand;

	[JsonProperty]
	public bool succesfullyRolledLastStand;

	public AnimationCurve lastLastChanceHappiness;

	[JsonProperty]
	public float lastStandTime;

	[JsonProperty]
	public float dirtyness;

	public float dirtReduceTime;

	[JsonProperty]
	public float brainGrowSickness;

	private float fallScreamCooldown;

	[JsonProperty]
	public bool usedNeuralBooster;

	public SleepQuality? forcedSleepQuality;

	private bool movingAllowed = true;

	public AnimationCurve clawDamageCurve;

	[JsonProperty]
	public float clawHealth = 100f;

	[JsonProperty]
	public float clawRegrowTime;

	public float heartProg;

	public float randomFibrillationVariation;

	private bool didThump;

	public float tempDiffFromNormal;

	[JsonProperty]
	public Skills skills;

	public Climbable currentClimbable;

	public float climbableProgress;

	public float climbVelocity;

	public bool onHardStimulants;

	public bool usingSleepingBag;

	public AnimationCurve heartCurveNormal;

	public AnimationCurve heartCurveArrythmia;

	[HideInInspector]
	public int defibShockedFrames;

	public AnimationCurve thirstBloodPressureCurve;

	[JsonProperty]
	public bool hasPulmonaryEmbolism;

	[JsonProperty]
	public float strokeAmount;

	[JsonProperty]
	public float bloodPressureChangeFromMedicine;

	[JsonProperty]
	public float venomTotal;

	[JsonProperty]
	public float venomCurrent;

	public float horrifiedLevel;

	public float focusedLevel;

	public Light2D radGlow;

	private Vector2 groundNormalDir;

	private CollisionDetectionMode2D currentLimbCollisionMode = CollisionDetectionMode2D.Continuous;

	public static bool censorPain;

	public static bool censorMood;

	public float bloodRegenSpeed => 0.035f * Mathf.Max(hunger * 0.01f, 0f) * WorldGeneration.GetRunSettingFloat("healingrate");

	public float actualStaminaStrength => staminaStrength.Evaluate(stamina * 0.01f);

	public bool canTakeNap
	{
		get
		{
			if ((!(energy < 35f) || !(averagePain < 31f) || !(sicknessAmount < 80f)) && !GetComponent<SleepingPills>())
			{
				return WorldGeneration.GetRunSettingBool("nosleeprestrictions");
			}
			return true;
		}
	}

	public bool shouldCheckHealthPanel => isDying;

	public float legSpeedMult
	{
		get
		{
			float num = 0f;
			for (int i = 0; i < legLimbs.Length; i++)
			{
				num += staminaStrength.Evaluate(legLimbs[i].totalForce);
			}
			return Mathf.Min(num / (float)legLimbs.Length, (limbs[1].dislocated || limbs[0].broken) ? 0.7f : 1f, staminaStrength.Evaluate(consciousness * 0.01f), actualStaminaStrength, 1f - temporarySlowdown, currentWeightMovementMult, currentTemperatureMovementMult, foodMovementCurve.Evaluate(hunger)) * (1f - overEncumberance * overEncumberance) * ((thirst > 125f) ? 0.9f : 1f) * (1f + stimulantMultiplier);
		}
	}

	public float actualMaxSpeed
	{
		get
		{
			float num = (bodyLerpFromRagdoll ? standLerpTime : 1f);
			return Mathf.Min(legSpeedMult * maxSpeed * (forceWalk ? 0.5f : 1f) * num * num, Mathf.Lerp(maxSpeed, maxSpeed * 0.5f, crouchAmount) * (1f + stimulantMultiplier));
		}
	}

	public float actualWallSlideSlowdown => wallSlideSlowdown * (1f / (1f + timeSlidfor * 0.35f)) * actualStaminaStrength;

	public float actualJumpSpeed => legSpeedMult * jumpSpeed * Mathf.Clamp01(1.45f - liquidSlipTime);

	public float actualMoveForce => moveForce * legSpeedMult * (forceWalk ? 0.5f : 1f) * (movingAllowed ? 1f : 0f) * (slippery ? 0.2f : 1f) * Mathf.Clamp01(totalHappiness * 0.01f + 1.2f) * Mathf.Clamp01(1f - liquidSlipTime);

	private bool canWalljumpLeft => timeSinceSlidLeft < 0.21f;

	private bool canWalljumpRight => timeSinceSlidRight < 0.21f;

	public bool fibrillationRising
	{
		get
		{
			if (!(bloodOxygen < 60f) && !(bloodPressure < 88f) && !(heartRate > 200f) && !fibrillationForced && !(bloodViscosity > 80f))
			{
				return temperature < 28.5f;
			}
			return true;
		}
	}

	public bool brainDying
	{
		get
		{
			if (bloodPressure < 10f)
			{
				return consciousness < 5f;
			}
			return false;
		}
	}

	public float bloodVolumePercentage => 0.5f + bloodVolume / 200f;

	public bool inCardiacArrest => heartRate < 20f;

	public float totalHappiness => Mathf.Clamp(happiness - ((happiness < -50f) ? (totalBleedSpeed * 15f) : 0f) - averagePain * 0.1f - sicknessAmount * 0.1f - (1f - Mathf.Clamp01(hunger * 0.01f + 0.6f)) * 18f - (1f - Mathf.Clamp01(Mathf.Min(thirst, 100f) * 0.01f + 0.6f)) * 18f - radiationSickness * 0.1f - hearingLoss * 0.2f - (100f - Mathf.Min(bloodVolume, 100f)) * 0.2f - traumaAmount * 0.525f - wetness * 0.05f + opiateHappiness + antidepressantHappiness, -100f, 100f) * (mindWipe ? 0f : 1f) * (1f - horrifiedLevel * 0.005f);

	public bool alive => brainHealth > 0f;

	public bool conscious
	{
		get
		{
			if (alive)
			{
				return consciousness > 30f;
			}
			return false;
		}
	}

	public bool aboveMedicalCutoff => totalHappiness > -75f;

	public bool allowUseItem
	{
		get
		{
			if (!(totalHappiness > -20f))
			{
				return UnityEngine.Random.value >= depressionChanceCurve.Evaluate(totalHappiness);
			}
			return true;
		}
	}

	public float currentWeightMovementMult { get; private set; }

	public float currentTemperatureMovementMult { get; private set; }

	public bool peekingBehind
	{
		get
		{
			if (isRight)
			{
				Vector2 obj = ((overrideLookTime > 0f) ? overrideLookPos : ((Vector2)targetLookPos));
				if (obj.x < base.transform.position.x)
				{
					return true;
				}
			}
			if (!isRight)
			{
				Vector2 obj2 = ((overrideLookTime > 0f) ? overrideLookPos : ((Vector2)targetLookPos));
				return obj2.x > base.transform.position.x;
			}
			return false;
		}
	}

	public float BaseHungerRate => Time.deltaTime / 23f * WorldGeneration.GetRunSettingFloat("metabolismrate");

	public float hungerLimbHealCurrent { get; private set; }

	public bool bothHandsUnusable
	{
		get
		{
			if (limbs[5].totalForce < 0.08f)
			{
				return limbs[8].totalForce < 0.08f;
			}
			return false;
		}
	}

	public float baseTemperatureLerpRate => 0.003f;

	public bool canPlaceBlock
	{
		get
		{
			if (!(rb.velocity.magnitude <= 2f) || !grounded)
			{
				return currentClimbable;
			}
			return true;
		}
	}

	public float clawGrowthRate => 0.25f * staminaStrength.Evaluate(hunger * 0.01f) * ((clawRegrowTime > 0f) ? 3f : 1f) * (sleeping ? 1.4f : 1f);

	public bool isDying
	{
		get
		{
			if (!(totalBleedSpeed > 0.134f) && (!(totalBleedSpeed > 0.02f) || !(bloodVolume < 40f)) && breathing && !(hunger < 10f) && !(thirst < 10f) && (!(thirst > 175f) || !(brainHealth < 50f)) && !(septicShock > 75f) && !(temperature > 41f) && !(temperature < 29f) && !(radiationSickness > 60f) && (!(fibrillationProgress > 1f) || !fibrillationRising) && !(bloodPressure < 80f) && !(bloodPressure > 170f) && !(bloodOxygen < 65f) && !hasPulmonaryEmbolism)
			{
				return strokeAmount > 50f;
			}
			return true;
		}
	}

	public bool isCriticallyDying
	{
		get
		{
			if ((!(totalBleedSpeed > 0.02f) || !(bloodVolume < 30f)) && !(bloodOxygen < 50f) && (!(hunger < 0f) || !(limbs[0].muscleHealth < 25f)) && !(septicShock > 82.5f) && !(temperature < 27f) && !(temperature > 41.5f) && !(fibrillationProgress > 60f) && !inCardiacArrest)
			{
				return bloodPressure < 70f;
			}
			return true;
		}
	}

	public bool exercising { get; private set; }

	public float bleedClottingSpeed { get; private set; }

	public float bleedingSpeedMultiplier { get; private set; }

	public float thirstBloodPressure { get; private set; }

	public string respiratoryRateReadout { get; private set; }

	public float bloodToLiters(float amount)
	{
		return amount * 0.025f;
	}

	public float bloodToLitersBody(float amount)
	{
		return 2.5f + amount * 0.025f;
	}

	public float BaseThirstRate(float tempDiff)
	{
		return Time.deltaTime / (17f - tempDiff * 0.5f) * ((!(thirst > 100f)) ? 1f : ((thirst > 175f) ? 2.5f : 2f)) * WorldGeneration.GetRunSettingFloat("metabolismrate");
	}

	public IEnumerator DoWorkout(WorkoutType type)
	{
		if (exercising || rb.velocity.magnitude > 1f || !standing || attackCooldown > 0f || (bool)currentClimbable)
		{
			yield break;
		}
		exercising = true;
		bodyAnimator.SetBool("exercising", value: true);
		switch (type)
		{
		case WorkoutType.Pushups:
			bodyAnimator.Play("ExperimentPushups");
			armsAnimator.Play("ArmsPushups");
			bodyAnimator.SetFloat("workoutSpeed", 1f + skills.STRFrom10 * 0.07f);
			armsAnimator.SetFloat("workoutSpeed", 1f + skills.STRFrom10 * 0.07f);
			break;
		case WorkoutType.Squats:
			bodyAnimator.Play("ExperimentSquats");
			armsAnimator.Play("ArmsSquats");
			bodyAnimator.SetFloat("workoutSpeed", 1.25f + skills.RESFrom10 * 0.07f);
			armsAnimator.SetFloat("workoutSpeed", 1.25f + skills.RESFrom10 * 0.07f);
			break;
		case WorkoutType.Plank:
			bodyAnimator.Play("ExperimentPlank");
			armsAnimator.Play("ArmsPlank");
			bodyAnimator.SetFloat("workoutSpeed", 1f);
			armsAnimator.SetFloat("workoutSpeed", 1f);
			break;
		}
		while (true)
		{
			Limb[] array = limbs;
			foreach (Limb limb in array)
			{
				if (!limb.dismembered && (limb.broken || limb.dislocated))
				{
					limb.pain += Time.deltaTime * 2.5f * limb.brokenPainMultiplier;
				}
			}
			switch (type)
			{
			case WorkoutType.Pushups:
				skills.AddExp(0, Time.deltaTime * 0.5f);
				stamina -= Time.deltaTime * 1.2f;
				eyeCloseTime = 0.5f;
				break;
			case WorkoutType.Squats:
				skills.AddExp(1, Time.deltaTime * 0.4f);
				stamina -= Time.deltaTime * 0.9f;
				break;
			case WorkoutType.Plank:
				skills.AddExp(0, Time.deltaTime * 0.25f);
				skills.AddExp(1, Time.deltaTime * 0.25f);
				stamina -= Time.deltaTime * 1.05f;
				break;
			}
			temperature += Time.deltaTime * 0.015f;
			if (rb.velocity.magnitude > 1f || !standing || attackCooldown > 0f)
			{
				break;
			}
			yield return null;
		}
		bodyAnimator.SetBool("exercising", value: false);
		armsAnimator.StopPlayback();
		armsAnimator.Play("Grounded");
		exercising = false;
	}

	public static SleepQuality BumpUpSleepQuality(SleepQuality qual)
	{
		return qual switch
		{
			SleepQuality.Bad => SleepQuality.Mediocre, 
			SleepQuality.Mediocre => SleepQuality.Okay, 
			SleepQuality.Okay => SleepQuality.Good, 
			SleepQuality.Good => SleepQuality.Good, 
			_ => SleepQuality.Okay, 
		};
	}

	public void StartClimbing(Climbable climbable)
	{
		if (!climbable || !(jumpCooldown <= 0f) || !(Time.time - climbable.lastStartedClimbing > 1.25f) || (bool)currentClimbable)
		{
			return;
		}
		ClimbableGrabInfo grabInfo = climbable.GetGrabInfo(base.transform.position);
		if (grabInfo.distanceToPlayer < 1f)
		{
			currentClimbable = climbable;
			climbableProgress = grabInfo.pathDistance;
			climbVelocity = lastTimeStepVelocity.y;
			climbable.lastStartedClimbing = Time.time;
			jumpCooldown = 0.5f;
			if (climbable.climbSounds != null && climbable.climbSounds.Length != 0)
			{
				Sound.Play(climbable.climbSounds.PickRandom(), Vector2.zero, twoDimensional: true);
			}
		}
	}

	public void StopClimbing()
	{
		if ((bool)currentClimbable)
		{
			climbableProgress = 0f;
			currentClimbable = null;
		}
	}

	public void AutoPickUpItem(Item item)
	{
		if (item.Stats.HasTag("noautopickup"))
		{
			return;
		}
		if (!item.Stats.wearable)
		{
			int? num = FirstEmptySlot();
			if (!num.HasValue)
			{
				foreach (Item surfaceInventoryItem in GetSurfaceInventoryItems())
				{
					if ((bool)surfaceInventoryItem.container && surfaceInventoryItem.container.CanHoldItem(item))
					{
						surfaceInventoryItem.container.LoadItem(item);
						break;
					}
				}
				return;
			}
			PickUpItem(item, num.Value, force: true);
		}
		else
		{
			Item wearableBySlotID = GetWearableBySlotID(item.Stats.wearSlotId);
			if ((bool)wearableBySlotID)
			{
				DropItem(wearableBySlotID);
			}
			WearWearable(item);
			PlayerCamera.main.UpdateWearables();
		}
	}

	public List<Item> GetAllItemsThorough()
	{
		List<Item> list = new List<Item>();
		for (int i = 0; i < slots.Length; i++)
		{
			Item item = GetItem(i);
			if (!item)
			{
				continue;
			}
			list.Add(item);
			if (!item.GetComponent<Container>())
			{
				continue;
			}
			foreach (Transform item2 in item.transform)
			{
				if (item2.TryGetComponent<Item>(out var component))
				{
					list.Add(component);
				}
			}
		}
		foreach (Item allWearable in GetAllWearables())
		{
			list.Add(allWearable);
			if (!allWearable.GetComponent<Container>())
			{
				continue;
			}
			foreach (Transform item3 in allWearable.transform)
			{
				if (item3.TryGetComponent<Item>(out var component2))
				{
					list.Add(component2);
				}
			}
		}
		return list;
	}

	public List<Item> GetSurfaceInventoryItems()
	{
		List<Item> list = new List<Item>();
		for (int i = 0; i < slots.Length; i++)
		{
			Item item = GetItem(i);
			if ((bool)item)
			{
				list.Add(item);
			}
		}
		foreach (Item allWearable in GetAllWearables())
		{
			list.Add(allWearable);
		}
		return list;
	}

	public bool FindByTagThorough(string tag, out Item it)
	{
		it = null;
		foreach (Item item in GetAllItemsThorough())
		{
			if (item.Stats.HasTag(tag))
			{
				it = item;
				return true;
			}
		}
		return false;
	}

	public bool FindByIdThorough(string id, out Item it)
	{
		it = null;
		foreach (Item item in GetAllItemsThorough())
		{
			if (item.id == id)
			{
				it = item;
				return true;
			}
		}
		return false;
	}

	public bool FindByIdSurface(string id, out Item it)
	{
		it = null;
		foreach (Item surfaceInventoryItem in GetSurfaceInventoryItems())
		{
			if (surfaceInventoryItem.id == id)
			{
				it = surfaceInventoryItem;
				return true;
			}
		}
		return false;
	}

	public float AverageHappiness()
	{
		float num = 0f;
		float[] array = lastHappiness;
		foreach (float num2 in array)
		{
			num += num2;
		}
		return num / (float)lastHappiness.Length;
	}

	public void TryStartFibrillation(bool forced = false)
	{
		if (fibrillationProgress <= 0f)
		{
			fibrillationProgress = 0.1f;
		}
		if (forced)
		{
			fibrillationForced = true;
		}
	}

	public void HandleCirculation(Painkillers painkillers)
	{
		bloodOxygen += Time.deltaTime * (respiratoryRate * 0.01f * ((breathing && (!inWater || hasScubaGear)) ? 1f : 0f) - 0.5f);
		if (bloodVolumePercentage < 0.6f && bloodOxygen > bloodVolumePercentage / 0.6f * 100f)
		{
			bloodOxygen = Mathf.MoveTowards(bloodOxygen, bloodVolumePercentage / 0.6f * 100f, Time.deltaTime * 0.75f);
		}
		bloodOxygen -= Time.deltaTime * (100f - stamina) / 100f * 0.35f;
		float num = 100f - Mathf.Abs(Mathf.MoveTowards(bloodViscosity, 0f, 40f)) * 0.4f;
		if (bloodOxygen > num)
		{
			bloodOxygen = Mathf.MoveTowards(bloodOxygen, num, Time.deltaTime * 0.75f);
		}
		if (bloodOxygen > 100f - hemothorax * 0.3f)
		{
			bloodOxygen = Mathf.MoveTowards(bloodOxygen, 100f - hemothorax * 0.3f, Time.deltaTime * 0.8f);
		}
		bloodOxygen = Mathf.Clamp(bloodOxygen, 0f, 100f);
		bloodViscosity = Mathf.MoveTowards(bloodViscosity, 0f, Time.deltaTime * 0.05f);
		bloodViscosity = Mathf.Clamp(bloodViscosity, -100f, 100f);
		float num2 = Mathf.Max(0f, bloodViscosity);
		bloodVolume = Mathf.MoveTowards(bloodVolume, 100f, Time.deltaTime * bloodRegenSpeed);
		if (bloodVolume > 200f)
		{
			bloodVolume = 200f;
		}
		if (bloodVolume < -100f)
		{
			bloodVolume = -100f;
		}
		float num3 = 100f;
		if (bloodPressure > 145f)
		{
			num3 -= bloodPressure - 145f;
		}
		if (bloodPressure < 20f)
		{
			num3 = 0f;
		}
		if ((bool)painkillers)
		{
			num3 -= painkillers.actualOpiateReception;
		}
		num3 -= fibrillationProgress * 0.35f;
		num3 -= hemothorax * 0.5f;
		num3 += tempDiffFromNormal * 3f;
		num3 += (100f - stamina) * 0.4f;
		num3 += adrenaline * 0.2f;
		num3 += averagePain * 0.25f;
		num3 -= Mathf.Max(0f, (strokeAmount - 50f) * 2f);
		if (temperature < 28f && !conscious)
		{
			num3 -= 50f;
		}
		if (limbs[1].broken)
		{
			num3 -= 0.6f;
		}
		respiratoryRate = Mathf.MoveTowards(respiratoryRate, num3, Time.deltaTime * ((respiratoryRate > 10f) ? 8f : 1f));
		respiratoryRate = Mathf.Clamp(respiratoryRate, 0f, 100f);
		if (fibrillationProgress > 0f)
		{
			if (fibrillationRising)
			{
				fibrillationProgress += Time.deltaTime * WorldGeneration.GetRunSettingFloat("fibrillationrate");
				if (heartRate > 280f)
				{
					fibrillationProgress += Time.deltaTime * 3f * WorldGeneration.GetRunSettingFloat("fibrillationrate");
				}
			}
			else
			{
				fibrillationProgress -= Time.deltaTime * 0.75f;
				if (fibrillationProgress < 0f)
				{
					fibrillationProgress = 0f;
				}
			}
		}
		else
		{
			fibrillationForced = false;
		}
		if (bloodOxygen < 50f || bloodPressure < 78f || heartRate > 200f || num2 > 95f || temperature < 28f)
		{
			TryStartFibrillation();
		}
		if (fibrillationProgress >= 100f)
		{
			fibrillationForced = false;
			heartRate = 0f;
		}
		fibrillationProgress = Mathf.Clamp(fibrillationProgress, 0f, 100f);
		float num4 = 120f;
		num4 -= (100f - bloodVolume) / 4f;
		num4 += (100f - stamina) * 0.2f;
		num4 += curAdrenaline * 0.2f;
		num4 -= septicShock * 0.4f;
		num4 += tempDiffFromNormal * 2f;
		num4 += weightOffset * 0.333f;
		if (thirst < 0f)
		{
			num4 += thirst;
		}
		if (hunger < 40f)
		{
			num4 += (hunger - 40f) * 0.25f;
		}
		if (bloodPressureChangeFromMedicine > 0f)
		{
			num4 *= 0.75f;
		}
		if (bloodPressureChangeFromMedicine < 0f)
		{
			num4 *= 1.25f;
		}
		if ((bool)painkillers)
		{
			num4 -= painkillers.actualOpiateReception * 0.4f;
		}
		if (!inCardiacArrest)
		{
			float num5 = 70f;
			num5 += averagePain;
			num5 += (100f - stamina) * 0.6f;
			num5 += curAdrenaline * 0.55f;
			num5 -= num2 * 0.3f;
			num5 += tempDiffFromNormal * 0.5f;
			if ((bool)painkillers)
			{
				num5 -= painkillers.actualOpiateReception / 5f;
			}
			if (bloodPressure < num4 - 5f)
			{
				heartRatePressureOffset += Time.deltaTime * 1.5f;
			}
			if (bloodPressure > num4 + 5f)
			{
				heartRatePressureOffset -= Time.deltaTime * 1.5f;
			}
			heartRatePressureOffset = Mathf.Clamp(heartRatePressureOffset, -30f, 80f);
			num5 += heartRatePressureOffset;
			num5 += fibrillationProgress;
			if (fibrillationProgress > 75f)
			{
				num5 += (fibrillationProgress - 75f) * 4f;
			}
			if (fibrillationProgress > 95f)
			{
				num5 += (fibrillationProgress - 95f) * 30f;
			}
			heartRate = Mathf.Lerp(heartRate, num5, Time.deltaTime * 0.15f);
		}
		else
		{
			heartRate = 0f;
		}
		if (bloodPressure > num4 + 10f)
		{
			bloodVesselSize += Time.deltaTime * 0.0036f;
		}
		else if (bloodPressure < num4 - 10f)
		{
			bloodVesselSize -= Time.deltaTime * 0.0036f;
		}
		else
		{
			bloodVesselSize = Mathf.MoveTowards(bloodVesselSize, 1f, Time.deltaTime * 0.0036f);
		}
		if (stamina < 50f || curAdrenaline > 30f)
		{
			bloodVesselSize -= Time.deltaTime * 0.005f;
		}
		bloodVesselSize = Mathf.Clamp(bloodVesselSize, 0.85f, 1.15f);
		float num6 = Mathf.Clamp(heartRate, 0f, 215f) - 70f;
		num6 = ((!(num6 > 0f)) ? (num6 / 70f) : (num6 / 200f));
		float num7 = 1f + (bloodVolumePercentage - 1f) * 1.1f;
		float num8 = 1f - fibrillationProgress / 260f;
		float num9 = 1f + bloodViscosity / 200f;
		float num10 = 1f - septicShock * 0.00525f;
		float num11 = 1f;
		if ((bool)painkillers)
		{
			num11 = Mathf.Clamp(num11 - painkillers.actualOpiateReception / 400f, 0.75f, 1.25f);
		}
		float num12 = 1f - tempDiffFromNormal / 40f;
		float num13 = 1f + weightOffset / 180f;
		float num14 = 120f * (1f + num6) * num7 * num8 * num9 * thirstBloodPressure * num10 * num11 * num12 * num13 / bloodVesselSize;
		if (bloodPressureChangeFromMedicine > 0f)
		{
			num14 *= 0.75f;
		}
		if (bloodPressureChangeFromMedicine < 0f)
		{
			num14 *= 1.25f;
		}
		bloodPressure = Mathf.Lerp(bloodPressure, num14, Time.deltaTime * 0.25f);
		bloodPressure = Mathf.Clamp(bloodPressure, 0f, 250f);
		if (bloodPressure > 145f)
		{
			float num15 = Mathf.Min((bloodPressure - 120f) * 0.5f, 50f) * Mathf.Clamp01(energy / 33f);
			if (limbs[0].pain < num15)
			{
				limbs[0].pain = Mathf.MoveTowards(limbs[0].pain, num15, Time.deltaTime * 2.5f);
			}
		}
		if (bloodOxygen < 80f && !brainDying)
		{
			brainHealth -= Time.deltaTime * (80f - bloodOxygen) / 600f;
		}
		if (brainDying)
		{
			brainHealth -= Time.deltaTime * 1.5f;
		}
		heartProg += Time.unscaledDeltaTime * heartRate / 60f;
		if (heartProg > 1f)
		{
			if (heartProg > 1.2f)
			{
				heartProg = 1.2f;
			}
			heartProg -= 1f;
			didThump = false;
			if (fibrillationProgress > 40f)
			{
				float num16 = (fibrillationProgress - 40f) / 150f;
				randomFibrillationVariation = 1f + UnityEngine.Random.Range(0f - num16, num16);
			}
			else
			{
				randomFibrillationVariation = 1f;
			}
		}
		if (heartProg > 0.3f && !didThump)
		{
			didThump = true;
			string clip = "heartthump";
			float volume = 1f - (fibrillationProgress - 50f) / 80f;
			if (isCriticallyDying)
			{
				clip = "heartthump-heavy";
				if (PlayerCamera.main.woundView.activeSelf && !WorldGeneration.unchipped)
				{
					clip = "heartthump-heavy-monitor";
				}
			}
			if (PlayerCamera.main.woundView.activeSelf || isCriticallyDying)
			{
				Sound.Play(clip, Vector2.zero, twoDimensional: true, pitchShift: false, null, volume, 1f, noReverb: true, ignoreMixer: true);
			}
		}
		defibShockedFrames--;
		if (defibShockedFrames < 0)
		{
			defibShockedFrames = 0;
		}
		bloodPressureReadout = Mathf.RoundToInt(bloodPressure) + "/" + Mathf.RoundToInt(bloodPressure * 0.66f);
		respiratoryRateReadout = Mathf.RoundToInt(respiratoryRate * 0.25f) + "/m";
	}

	public float GetECGHeight(float offset)
	{
		offset *= heartRate / 60f;
		float num = Mathf.Lerp(heartCurveNormal.Evaluate(heartProg - offset), heartCurveArrythmia.Evaluate(heartProg - offset), fibrillationProgress / 90f) * randomFibrillationVariation;
		if (fibrillationProgress > 75f)
		{
			num *= 1f - (fibrillationProgress - 75f) / 25f;
		}
		if (heartRate <= 0f)
		{
			num = 0f;
		}
		if (defibShockedFrames > 0)
		{
			num = ((UnityEngine.Random.value > 0.5f) ? 1f : (-1f));
		}
		return num;
	}

	public void TryLastStand()
	{
		float num = lastLastChanceHappiness.Evaluate(lastHappiness[9]);
		triedRollingLastStand = true;
		if (UnityEngine.Random.value < num)
		{
			brainHealth = UnityEngine.Random.Range(75f, 90f);
			hunger = Mathf.Lerp(hunger, 100f, 0.5f);
			thirst = Mathf.Lerp(thirst, 100f, 0.5f);
			weightOffset = Mathf.Lerp(weightOffset, 0f, 0.15f);
			sicknessAmount = Mathf.Lerp(sicknessAmount, 0f, 0.3f);
			bloodVolume = Mathf.Max(bloodVolume, 50f);
			heartRate = 120f;
			fibrillationProgress = 0f;
			bloodPressure = 135f;
			bloodVesselSize = 1f;
			bloodOxygen = 100f;
			bloodViscosity = 0f;
			strokeAmount = 0f;
			hasPulmonaryEmbolism = false;
			heartRatePressureOffset = 0f;
			respiratoryRate = 100f;
			septicShock *= 0.4f;
			lastStandTime = 300f;
			happiness = 10f;
			venomCurrent = 0f;
			venomTotal = 0f;
			energy = 100f;
			antibioticImmunityTime = 120f;
			caffeinated = 200f;
			hemothorax *= 0.5f;
			temperature = 37f;
			radiationSickness *= 0.2f;
			internalBleeding *= 0.05f;
			clawHealth = Mathf.Max(clawHealth, 80f);
			Limb[] array = limbs;
			foreach (Limb obj in array)
			{
				obj.muscleHealth = Mathf.Lerp(obj.muscleHealth, 100f, 0.3f);
				obj.infectionAmount *= 0.05f;
				obj.bleedAmount *= 0.05f;
			}
			if (TryGetComponent<Painkillers>(out var component))
			{
				component.opiateAmount = 0f;
				component.opiateTolerance = 0f;
				component.opiateReception = 0f;
				component.actualOpiateReception = 0f;
			}
			CoUtils.instance.CancelAll();
			Sound.Play("observerlaugh", Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, 1f, noReverb: true, ignoreMixer: true);
			if (TryGetComponent<SleepingPills>(out var component2))
			{
				UnityEngine.Object.Destroy(component2);
			}
			if (TryGetComponent<Antidepressants>(out var component3))
			{
				UnityEngine.Object.Destroy(component3);
			}
			PlayerCamera.main.StartCoroutine(PlayerCamera.main.LastStandSequence());
			succesfullyRolledLastStand = true;
			if ((bool)Observer.main)
			{
				Observer.main.RolledLastStand();
			}
			if (WorldGeneration.GetRunSettingBool("infinitelaststand"))
			{
				triedRollingLastStand = false;
			}
		}
	}

	public float SleepQualityToRegen(SleepQuality q)
	{
		return q switch
		{
			SleepQuality.Bad => 0.7f, 
			SleepQuality.Mediocre => 0.85f, 
			SleepQuality.Okay => 1f, 
			SleepQuality.Good => 1.25f, 
			_ => 1f, 
		};
	}

	private void Awake()
	{
		isRight = true;
		float num = 0f;
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<BoxCollider2D>();
		Limb[] array = limbs;
		foreach (Limb limb in array)
		{
			Limb[] array2 = limbs;
			foreach (Limb limb2 in array2)
			{
				Physics2D.IgnoreCollision(limb.GetComponent<Collider2D>(), limb2.GetComponent<Collider2D>(), ignore: true);
			}
			num += limb.baseMass;
		}
		baseMass = num;
		origColSize = col.size;
		talker = GetComponent<Talker>();
		skills = new Skills();
		skills.Setup(charType);
		slideSource = base.gameObject.AddComponent<AudioSource>();
		slideSource.spatialBlend = 1f;
		slideSource.clip = Resources.Load<AudioClip>("Sounds/slide");
		slideSource.playOnAwake = false;
		slideSource.outputAudioMixerGroup = WorldGeneration.world.soundMixerGroup;
		slideSource.loop = false;
		slideSource.dopplerLevel = 0f;
		vomiter = GetComponent<Vomiter>();
		currentWeightMovementMult = 1f;
		temperature = 37f;
		harmer = GetComponent<SelfHarmer>();
		liquidRagdollBar = 1f;
	}

	public IEnumerator LastHappinessUpdater()
	{
		while (true)
		{
			float[] array = lastHappiness.ToArray();
			for (int i = 0; i < lastHappiness.Length; i++)
			{
				array[(i + 1) % array.Length] = lastHappiness[i];
			}
			lastHappiness = array;
			lastHappiness[0] = totalHappiness;
			yield return new WaitForSeconds(60f);
		}
	}

	public void DoFurTuft()
	{
		GameObject obj = UnityEngine.Object.Instantiate(Resources.Load("Special/FurExplode"), base.transform.position, Quaternion.identity) as GameObject;
		ParticleSystem.MainModule main = obj.GetComponent<ParticleSystem>().main;
		main.startColor = new ParticleSystem.MinMaxGradient(furColors)
		{
			mode = ParticleSystemGradientMode.RandomColor
		};
		UnityEngine.Object.Destroy(obj, 30f);
	}

	public void SwitchHands()
	{
		if (slots[0].canPickUp && slots[1].canPickUp)
		{
			Item item = GetItem(0);
			Item item2 = GetItem(1);
			DropItem(0);
			DropItem(1);
			if ((bool)item)
			{
				PickUpItem(item, 1, force: true);
			}
			if ((bool)item2)
			{
				PickUpItem(item2, 0, force: true);
			}
			if ((bool)item || (bool)item2)
			{
				Sound.Play("switch", base.transform.position);
			}
		}
	}

	public void RemoveEye()
	{
		if (WorldGeneration.GetRunSettingBool("disfigurement"))
		{
			if (!eyeGone)
			{
				eyeGone = true;
				limbs[0].pain = 100f;
				limbs[0].bleedAmount += 20f;
				traumaAmount += 35f;
			}
			else if (!bothEyesGone)
			{
				bothEyesGone = true;
				limbs[0].pain = 100f;
				limbs[0].bleedAmount += 20f;
				traumaAmount += 35f;
			}
		}
	}

	public void PromptTalk()
	{
		if ((bool)talker)
		{
			talker.PromptTalk();
		}
	}

	public void FootStep(float vol = 1f)
	{
		vol *= 1f - crouchAmount * 0.5f;
		CreateCloudMini(base.transform.position + Vector3.down * origColSize.y * 0.5f);
		if (bodyAffect.wasWater)
		{
			Sound.Play(WorldGeneration.world.RandomStepSound("Water"), base.transform.position, twoDimensional: false, pitchShift: true, null, vol);
		}
		else if (standingOn != null)
		{
			Sound.Play(WorldGeneration.world.RandomStepSound(standingOn.stepsound), base.transform.position, twoDimensional: false, pitchShift: true, null, vol);
		}
		else
		{
			Sound.Play("BSFootstep" + UnityEngine.Random.Range(1, 5), base.transform.position, twoDimensional: false, pitchShift: true, null, vol);
		}
	}

	public void SwitchDir()
	{
		if (!standing)
		{
			return;
		}
		isRight = !isRight;
		base.transform.localScale = new Vector3(0f - base.transform.localScale.x, 1f, 1f);
		Limb[] array = limbs;
		foreach (Limb limb in array)
		{
			if ((bool)limb.GetComponent<HingeJoint2D>())
			{
				HingeJoint2D component = limb.GetComponent<HingeJoint2D>();
				if (component.useLimits)
				{
					JointAngleLimits2D limits = component.limits;
					float min = component.limits.min;
					limits.min = 0f - component.limits.max;
					limits.max = 0f - min;
					component.limits = limits;
				}
			}
		}
	}

	public void Disfigure()
	{
		if (!disfigured && WorldGeneration.GetRunSettingBool("disfigurement"))
		{
			disfigured = true;
			Sound.Play("gore", base.transform.position);
			Sound.Play("disfigure", base.transform.position, twoDimensional: true);
			GetComponentInChildren<FacialExpression>().disfiguredIndex = UnityEngine.Random.Range(0, GetComponentInChildren<FacialExpression>().disfiguredHead.Length);
			limbs[0].bleedAmount += 5f;
			limbs[0].skinHealth -= 50f;
			limbs[0].pain = 100f;
			traumaAmount += 50f;
			eyeScareTime = 20f;
			consciousness = 30f;
			shock = 100f;
		}
	}

	public void CombineLiquids(WaterContainerItem wat1, WaterContainerItem wat2, float amt)
	{
		float amount = Mathf.Min(wat1.SpaceLeft, amt);
		List<LiquidStack> list = new List<LiquidStack>();
		List<float> list2 = wat2.CalculateDrain(amount);
		for (int i = 0; i < list2.Count; i++)
		{
			list.Add(new LiquidStack(wat2.stack[i].liquidId, list2[i]));
		}
		wat2.Drain(list2);
		foreach (LiquidStack item in list)
		{
			wat1.AddLiquid(item.liquidId, item.amount);
		}
		if (wat1.GetComponent<Item>().TryGetParentContainer(out var cont) && (cont.AboveHoldingWeight() || wat1.GetComponent<Item>().totalWeight > cont.maxWeightPerItem))
		{
			cont.UnloadItem(wat1.GetComponent<Item>());
		}
		Sound.Play("waterpour", base.transform.position);
	}

	public void CombineItems(Item it1, Item it2)
	{
		if (!CanCombine(it1, it2))
		{
			return;
		}
		if ((bool)it1.GetComponent<GunScript>() && (bool)it2.GetComponent<AmmoScript>())
		{
			it1.GetComponent<GunScript>().LoadMag(it2.GetComponent<AmmoScript>());
			return;
		}
		if ((bool)it1.GetComponent<AmmoScript>() && (bool)it2.GetComponent<AmmoScript>())
		{
			it1.GetComponent<AmmoScript>().LoadRound(it2.GetComponent<AmmoScript>());
			return;
		}
		if (it1.TryGetComponent<WaterContainerItem>(out var component) && it2.TryGetComponent<WaterContainerItem>(out var component2))
		{
			if (component.SpaceLeft > 0f && it1.id != "craftingbottle")
			{
				PlayerCamera.main.StartLiquidTransfer(component, component2);
			}
			return;
		}
		float a = 1f - it1.condition;
		it1.condition += Mathf.Min(a, it2.condition);
		it2.condition -= Mathf.Min(a, it2.condition);
		CreateCloudMini(it1.transform.position);
		Sound.Play("combine", base.transform.position);
		if (it1.TryGetParentContainer(out var cont) && (cont.AboveHoldingWeight() || it1.totalWeight > cont.maxWeightPerItem))
		{
			cont.UnloadItem(it1);
		}
	}

	public bool CanCombine(Item it1, Item it2)
	{
		if ((bool)it1 && (bool)it2)
		{
			if ((!it1.Stats.combineable || !it2.Stats.combineable || !(it1.condition > 0f) || !(it2.condition > 0f) || !(it1.id == it2.id)) && ((!it1.GetComponent<AmmoScript>() && !it1.GetComponent<GunScript>()) || !it2.GetComponent<AmmoScript>()))
			{
				if ((bool)it1.GetComponent<WaterContainerItem>())
				{
					return it2.GetComponent<WaterContainerItem>();
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public bool HoldingItem(Item item)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].transform.childCount > 0 && (bool)slots[i].transform.GetChild(0).GetComponent<Item>() && slots[i].transform.GetChild(0).GetComponent<Item>() == item)
			{
				return true;
			}
		}
		return false;
	}

	public bool HoldingItem(string id)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (HoldingItem(i) && GetItem(i).id == id)
			{
				return true;
			}
		}
		return false;
	}

	public bool HoldingItem(int slot)
	{
		return slots[slot].transform.childCount > 0;
	}

	public int SlotOf(Item item)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (GetItem(i) == item)
			{
				return i;
			}
		}
		return 0;
	}

	public Item GetItem(int slot)
	{
		if (HoldingItem(slot))
		{
			return slots[slot].transform.GetChild(0).GetComponent<Item>();
		}
		return null;
	}

	public bool DoPickupCheck(Item item, bool noAlerts = false)
	{
		if (HoldingItem(item) || ((bool)item.ParentContainer() && (HoldingItem(item.ParentContainer().GetComponent<Item>()) || HasWearable(item.ParentContainer().GetComponent<Item>()))))
		{
			return true;
		}
		Vector2 vector = (item.transform.parent ? item.transform.position : item.GetComponent<Collider2D>().bounds.center);
		bool flag = Physics2D.Linecast(base.transform.position, vector, LayerMask.GetMask("Ground"));
		bool flag2 = Vector2.Distance(item.transform.position, base.transform.position) < 10f;
		if (flag && (bool)Physics2D.OverlapPoint(vector, LayerMask.GetMask("Ground")))
		{
			flag = false;
		}
		if (!noAlerts)
		{
			if (flag)
			{
				GameObject obj = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Special/PickupLine"), Vector2.zero, Quaternion.identity);
				obj.GetComponent<LineRenderer>().SetPosition(0, base.transform.position);
				obj.GetComponent<LineRenderer>().SetPosition(1, vector);
				PlayerCamera.main.DoAlert(Locale.GetOther("alertitemblocked"));
				UnityEngine.Object.Destroy(obj, 0.5f);
			}
			else if (!flag2)
			{
				PlayerCamera.main.DoAlert(Locale.GetOther("alertitemfar"));
			}
		}
		return !flag && flag2;
	}

	public void PickUpItem(Item item, int slot, bool force = false)
	{
		if ((slots[slot].canPickUp || force) && (bool)item && !HoldingItem(item) && !HoldingItem(slot) && (DoPickupCheck(item) || force) && (!item.Stats.onlyHoldInHands || slots[slot].isHand))
		{
			if (item.TryGetParentContainer(out var cont))
			{
				cont.UnloadItem(item);
				PlayerCamera.main.PlayBackpackSound();
			}
			CreateCloudMini(item.transform.position);
			item.rb.simulated = false;
			item.GetComponent<SpriteRenderer>().sortingOrder = slots[slot].spriteSortOrder;
			item.transform.SetParent(slots[slot].transform);
			item.transform.localPosition = Vector3.zero;
			item.transform.localEulerAngles = new Vector3(0f, 0f, item.Stats.slotRotation);
			item.transform.localScale = new Vector3(1f / slots[slot].limb.transform.localScale.x, 1f, 1f);
			if (item.Stats.HasTag("backflip") && slot > 2)
			{
				item.transform.localEulerAngles += Vector3.back * 90f;
			}
		}
	}

	public void SwapSlots(int slot1, int slot2)
	{
		Item item = GetItem(slot1);
		Item item2 = GetItem(slot2);
		DropItem(slot1);
		DropItem(slot2);
		if ((bool)item)
		{
			PickUpItem(item, slot2, force: true);
		}
		if ((bool)item2)
		{
			PickUpItem(item2, slot1, force: true);
		}
		Sound.Play("switch", base.transform.position);
	}

	public void DropItem(int slot)
	{
		if (HoldingItem(slot))
		{
			Item component = slots[slot].transform.GetChild(0).GetComponent<Item>();
			DropItem(component);
		}
	}

	public void DropItem(Item item)
	{
		if (HoldingItem(item))
		{
			item.rb.simulated = true;
			item.rb.velocity = rb.velocity;
			item.rb.angularVelocity = 0f;
			item.transform.parent = null;
			item.transform.localScale = Vector3.one;
		}
	}

	public int? FirstEmptySlot()
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (!HoldingItem(i) && slots[i].canPickUp)
			{
				return i;
			}
		}
		return null;
	}

	public Limb LimbByName(string nm)
	{
		Limb[] array = limbs;
		foreach (Limb limb in array)
		{
			if (limb.name == nm)
			{
				return limb;
			}
		}
		return null;
	}

	public void WearWearable(Item item)
	{
		Item wearableBySlotID = GetWearableBySlotID(item.Stats.wearSlotId);
		if ((bool)wearableBySlotID)
		{
			if (wearableBySlotID != item)
			{
				PlayerCamera.main.DoAlert(Locale.GetOther("alertalreadywearing").Replace("<1>", item.Stats.rec.recognizable ? item.fullName : Locale.GetOther("unknownobject")).Replace("<2>", wearableBySlotID.Stats.rec.recognizable ? wearableBySlotID.fullName : Locale.GetOther("unknownobject")));
			}
		}
		else
		{
			if (!DoPickupCheck(item))
			{
				return;
			}
			Limb limb = LimbByName(item.Stats.desiredWearLimb);
			if (limb.dismembered)
			{
				PlayerCamera.main.DoAlert(Locale.GetOther("alertlimbmissing").Replace("<1>", limb.shortName).Replace("<2>", item.Stats.rec.recognizable ? item.fullName : Locale.GetOther("unknownobject")));
				return;
			}
			CreateCloudMini(item.transform.position);
			if (item.TryGetParentContainer(out var cont))
			{
				cont.UnloadItem(item);
				PlayerCamera.main.PlayBackpackSound();
			}
			item.rb.simulated = false;
			item.GetComponent<SpriteRenderer>().sortingOrder = limb.GetComponent<SpriteRenderer>().sortingOrder + item.Stats.wearableVisualOffset;
			item.transform.SetParent(limb.transform);
			item.transform.localScale = Vector3.one;
			item.transform.localRotation = Quaternion.identity;
			item.transform.localPosition = Vector3.zero;
			if (item.TryGetComponent<Wearable>(out var component))
			{
				component.CreateSprites(this);
			}
		}
	}

	public void DropWearable(Item item)
	{
		if ((bool)GetWearable(item.id))
		{
			item.rb.simulated = true;
			item.rb.velocity = rb.velocity;
			item.rb.angularVelocity = 0f;
			item.transform.parent = null;
			item.transform.localScale = Vector3.one;
			if (item.TryGetComponent<Wearable>(out var component))
			{
				component.ClearSprites();
			}
		}
	}

	public Item GetWearable(string itemid)
	{
		foreach (Transform item in LimbByName(Item.GlobalItems[itemid].desiredWearLimb).transform)
		{
			if (item.TryGetComponent<Item>(out var component) && component.Stats.wearable && component.id == itemid)
			{
				return component;
			}
		}
		return null;
	}

	public bool HasWearable(string itemid)
	{
		foreach (Transform item in LimbByName(Item.GlobalItems[itemid].desiredWearLimb).transform)
		{
			if (item.TryGetComponent<Item>(out var component) && component.Stats.wearable && component.id == itemid)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasWearable(Item item)
	{
		foreach (Transform item2 in LimbByName(item.Stats.desiredWearLimb).transform)
		{
			if (item2.TryGetComponent<Item>(out var component) && component.Stats.wearable && component == item)
			{
				return true;
			}
		}
		return false;
	}

	public Item GetWearableBySlotID(string id)
	{
		for (int i = 0; i < limbs.Length; i++)
		{
			foreach (Transform item in limbs[i].transform)
			{
				if (item.TryGetComponent<Item>(out var component) && component.Stats.wearable && component.Stats.wearSlotId == id)
				{
					return component;
				}
			}
		}
		return null;
	}

	public List<Item> GetAllWearables()
	{
		List<Item> list = new List<Item>();
		Limb[] array = limbs;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (Transform item in array[i].transform)
			{
				if (item.TryGetComponent<Item>(out var component) && component.Stats.wearable)
				{
					list.Add(component);
				}
			}
		}
		return list;
	}

	public List<Item> GetAllItems()
	{
		List<Item> list = new List<Item>();
		for (int i = 0; i < slots.Length; i++)
		{
			if ((bool)GetItem(i))
			{
				list.Add(GetItem(i));
			}
		}
		return list;
	}

	public Vector2 ThrowVelocity(Item item, float force)
	{
		return (targetLookPos - item.transform.position).normalized * actualJumpSpeed * 2.5f * (1f + skills.STRFrom10 * 0.1f) * (force / Mathf.Clamp(Mathf.Lerp(item.totalWeight, 1f, 0.6f), 0.8f, 3f)) + (Vector3)rb.velocity;
	}

	public void ThrowItem(float force = 1f)
	{
		Item item = GetItem(handSlot);
		if ((bool)item)
		{
			force = Mathf.Clamp01(force);
			DropItem(item);
			Vector2 vector = ThrowVelocity(item, force);
			item.rb.velocity = vector;
			Vector2 vector2 = rb.velocity - vector;
			rb.velocity += vector2 * 0.25f * item.totalWeight;
			item.rb.angularVelocity = UnityEngine.Random.Range(-800f, 800f);
			item.rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
			attackRot -= (isRight ? 20f : (-20f));
			armsAnimator.Play("ArmsSwing", -1, 0f);
			stamina -= 3.5f;
			Sound.Play("BSSwing" + UnityEngine.Random.Range(1, 5), base.transform.position);
		}
	}

	public void Stand(bool force = false)
	{
		if (!((!standing && conscious && shock < 10f && legSpeedMult > 0.01f) || force))
		{
			return;
		}
		float num = limbs[2].transform.eulerAngles.z;
		if (num > 180f)
		{
			num -= 360f;
		}
		accelRot = num * 0.6f;
		rb.velocity = baseLimb.rb.velocity;
		Limb[] array = limbs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].rb.simulated = false;
		}
		rb.MovePosition(base.transform.position + Vector3.up * col.size.y * 0.4f);
		standing = true;
		rb.mass = baseMass;
		col.enabled = true;
		crouchAmount = 1f;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, Vector2.down, origColSize.y * 0.5f, LayerMask.GetMask("Ground"));
		if ((bool)raycastHit2D)
		{
			float num2 = origColSize.y * 0.5f - raycastHit2D.distance;
			base.transform.position += Vector3.up * num2;
			bodyLerpFromRagdoll = true;
			standLerpTime = 0f;
			visualBodyOffset -= Vector2.up * num2;
			array = limbs;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].transform.position -= Vector3.up * num2;
			}
		}
		SetLimbCollisionType(CollisionDetectionMode2D.Continuous);
	}

	public void Ragdoll()
	{
		if (shock < 20f)
		{
			shock = 20f;
		}
		if (standing)
		{
			Limb[] array = limbs;
			foreach (Limb obj in array)
			{
				obj.rb.simulated = true;
				obj.rb.velocity = lastTimeStepVelocity;
			}
			standing = false;
			rb.mass = 0.01f;
			col.enabled = false;
		}
	}

	public void PlaceBody()
	{
		if (WorldGeneration.world.biomeOverride == WorldGeneration.OverrideSceneType.Tutorial)
		{
			base.transform.position = GameObject.Find("TUTORIALSPAWN").transform.position;
			PlayerCamera.main.transform.position = base.transform.position;
			return;
		}
		bool flag = false;
		for (int i = 0; i < WorldGeneration.world.height; i++)
		{
			if (!Physics2D.OverlapBox(Vector2.up * (WorldGeneration.world.halfHeight - i), origColSize, 0f, LayerMask.GetMask("Ground")))
			{
				flag = true;
			}
			else if (flag)
			{
				base.transform.position = Vector2.up * (WorldGeneration.world.halfHeight - i);
				PlayerCamera.main.transform.position = base.transform.position;
				break;
			}
		}
		if (!flag)
		{
			Debug.LogError("No valid player spawnpoint!");
		}
	}

	private void Start()
	{
		Stand(force: true);
		thirstBloodPressure = 1f;
		if (!SaveSystem.loadedRun)
		{
			hunger = UnityEngine.Random.Range(80f, 115f);
			thirst = UnityEngine.Random.Range(75f, 100f);
			weightOffset = UnityEngine.Random.Range(-10f, 10f);
			happiness = UnityEngine.Random.Range(-10f, 10f);
			energy = UnityEngine.Random.Range(80f, 100f);
			bloodPressure = 120f;
			heartRate = 70f;
			respiratoryRate = 100f;
			lastStandTime = -10000f;
		}
		standLerpTime = 1f;
		bodyAffect = base.gameObject.AddComponent<LiquidAffect>();
		StartCoroutine(LastHappinessUpdater());
		StartCoroutine(TheCoroutineThatMakesYouShitYourselfWhenUnconscious());
	}

	public IEnumerator TheCoroutineThatMakesYouShitYourselfWhenUnconscious()
	{
		float timer = 0f;
		while (true)
		{
			timer += 5f;
			if (timer >= 1000f && !conscious)
			{
				timer = 0f;
				Utils.Create("droppings", limbs[2].transform.position, 0f);
			}
			yield return new WaitForSeconds(5f);
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (lastTimeStepVelocity.y < 0f - Mathf.Max(actualJumpSpeed * 2f, jumpSpeed * 1.5f))
		{
			talker.Talk(Locale.GetCharacter("hitgroundhard"));
			Ragdoll();
			return;
		}
		standLerpTime = 0.58f;
		crouchAmount -= lastTimeStepVelocity.y * 0.03f;
		if (lastTimeStepVelocity.y < 0f - jumpSpeed - 1f)
		{
			visualBodyOffset += new Vector2(0f, lastTimeStepVelocity.y * 0.035f);
			if (lastTimeStepVelocity.y < 0f - jumpSpeed - 5f)
			{
				skills.AddExp(1, 0.5f);
				PlayerCamera.main.shaker.Velocity(lastTimeStepVelocity * 0.5f);
			}
		}
	}

	public Limb GetClosestLimb(Vector2 pos)
	{
		float num = 999999f;
		Limb result = limbs[0];
		Limb[] array = limbs;
		foreach (Limb limb in array)
		{
			if (!limb.dismembered && Vector2.Distance(pos, limb.transform.position) < num)
			{
				result = limb;
				num = Vector2.Distance(pos, limb.transform.position);
			}
		}
		return result;
	}

	public bool Attack(AttackInfo atk, int slot)
	{
		if (conscious && attackCooldown <= 0f)
		{
			liquidDrinkTime = 0f;
			slot = handSlot;
			atk.damage *= WorldGeneration.GetRunSettingFloat("attackdamage");
			atk.structuralDamage *= WorldGeneration.GetRunSettingFloat("attackdamage");
			if ((isRight && targetLookPos.x < base.transform.position.x) || (!isRight && targetLookPos.x > base.transform.position.x))
			{
				SwitchDir();
			}
			Vector2 vector = (targetLookPos - limbs[1].transform.position).normalized;
			if (atk.physicalSwing)
			{
				Limb[] useLimbs = slots[slot].useLimbs;
				foreach (Limb limb in useLimbs)
				{
					if (limb.broken || limb.dislocated)
					{
						limb.pain += 2f * limb.brokenPainMultiplier;
					}
				}
				temperature += 0.125f * atk.cooldown;
				if (limbs[0].broken)
				{
					limbs[0].pain += 2f * limbs[0].brokenPainMultiplier;
				}
				if (limbs[1].broken || limbs[1].dislocated)
				{
					limbs[1].pain += 2f * limbs[1].brokenPainMultiplier;
				}
				float num = (isRight ? 1f : (-1f));
				armOffset = Vector2.SignedAngle(limbs[1].transform.right * num, vector);
				visualBodyOffset += vector * (atk.rotateAmount * 0.03f);
				if (!standing)
				{
					for (int j = 3; j < 10; j++)
					{
						limbs[j].rb.AddForce(vector * 800f);
						limbs[1].rb.AddForce(-vector * 800f);
					}
				}
			}
			attackRot -= atk.rotateAmount * (isRight ? 1f : (-1f));
			if (atk.doAttackAnim)
			{
				armsAnimator.Play("ArmsSwing", -1, 0f);
			}
			stamina -= atk.staminaUse;
			float num2 = 1f;
			if (atk.physicalSwing)
			{
				num2 = slots[slot].armPowerMult;
				if (slot == 1)
				{
					num2 *= 0.75f;
				}
				num2 *= 1f + skills.STRFrom10 * 0.0334f;
				attackCooldown = atk.cooldown / (consciousness * 0.01f) * (1f + overEncumberance) / (1f + stimulantMultiplier * 0.66f);
				TryExertSound(atk.cooldown * 1.15f, 0.35f);
			}
			else
			{
				attackCooldown = atk.cooldown;
			}
			if (atk.unarmed)
			{
				num2 *= clawDamageCurve.Evaluate(clawHealth);
			}
			RaycastHit2D[] array = Physics2D.RaycastAll(limbs[1].transform.position, vector, atk.distance);
			Sound.Play(atk.swingSounds[UnityEngine.Random.Range(0, atk.swingSounds.Length)], base.transform.position, twoDimensional: false, pitchShift: true, base.transform, atk.volume);
			if ((bool)atk.attackAnim)
			{
				GameObject obj = UnityEngine.Object.Instantiate(atk.attackAnim);
				obj.transform.eulerAngles = new Vector3(0f, 0f, Vector2.SignedAngle(isRight ? Vector3.right : Vector3.left, vector));
				obj.transform.localScale = new Vector3(isRight ? 1f : (-1f), 1f, 1f);
				obj.transform.position = limbs[1].transform.position;
				obj.transform.SetParent(base.transform);
				UnityEngine.Object.Destroy(obj, 5f);
			}
			bool flag = false;
			RaycastHit2D[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				RaycastHit2D raycastHit2D = array2[i];
				if (!(raycastHit2D.transform != base.transform))
				{
					continue;
				}
				if (raycastHit2D.transform.CompareTag("BlockGround"))
				{
					WorldGeneration.world.DamageBlock(raycastHit2D.point + vector * 0.05f, atk.structuralDamage * num2, hitSound: true, atk.metalMoreDamage);
					WorldGeneration.CreateDamageNumber(raycastHit2D.point, (int)(atk.structuralDamage * num2));
					WorldGeneration.world.CreateHitFlash(PlayerCamera.main.defaultHoverSquareSprite, WorldGeneration.world.BlockToWorldPos(WorldGeneration.world.WorldToBlockPos(raycastHit2D.point + vector * 0.05f)), Quaternion.identity, Color.gray);
					CreateCloudSmall(raycastHit2D.point, raycastHit2D.normal * 4f);
					flag = true;
					if (!atk.piercing)
					{
						break;
					}
				}
				if (!raycastHit2D.transform.TryGetComponent<BuildingEntity>(out var component) || component.cantHit)
				{
					continue;
				}
				if ((bool)raycastHit2D.rigidbody)
				{
					raycastHit2D.rigidbody.AddForceAtPosition(vector * num2 * atk.knockBack, raycastHit2D.point, ForceMode2D.Impulse);
				}
				component.health -= (component.animal ? atk.damage : atk.structuralDamage) * num2 * ((atk.metalMoreDamage && component.metallic) ? 10f : 1f);
				WorldGeneration.CreateDamageNumber(raycastHit2D.point, (int)((component.animal ? atk.damage : atk.structuralDamage) * num2));
				if (component.TryGetComponent<SpriteRenderer>(out var component2))
				{
					WorldGeneration.world.CreateHitFlash(component2.sprite, component.transform.position, component.transform.rotation, Color.red, component.transform);
				}
				Sound.Play(component.hitSound, raycastHit2D.point);
				CreateCloudSmall(raycastHit2D.point, raycastHit2D.normal * 4f);
				if (atk.unarmed)
				{
					if (raycastHit2D.transform.TryGetComponent<SawbladeScript>(out var _))
					{
						Ragdoll();
					}
					if (raycastHit2D.transform.TryGetComponent<CoilScript>(out var component4))
					{
						component4.Shock(limbs[0]);
					}
				}
				if (component.animal)
				{
					raycastHit2D.transform.gameObject.SendMessage("AnimalHit", atk.damage * num2);
					attackCooldown *= 3.5f * atk.attackCooldownMult;
					PlayerCamera.main.lastAttackCool = attackCooldown;
				}
				else
				{
					raycastHit2D.transform.gameObject.SendMessage("BuildingHit", atk, SendMessageOptions.DontRequireReceiver);
				}
				flag = true;
				if (!atk.piercing)
				{
					break;
				}
			}
			if (flag)
			{
				if (standing)
				{
					rb.AddForce(-vector * atk.knockBack * num2, ForceMode2D.Impulse);
				}
				else
				{
					limbs[1].rb.AddForce(-vector * atk.knockBack * num2, ForceMode2D.Impulse);
				}
				if (atk.unarmed)
				{
					clawHealth -= 0.3f;
					if (clawHealth < 20f && UnityEngine.Random.value < 0.1f)
					{
						slots[slot].limb.skinHealth -= 3f;
						slots[slot].limb.muscleHealth -= 2f;
						slots[slot].limb.pain += 12f;
						slots[slot].limb.bleedAmount += UnityEngine.Random.Range(0.35f, 0.85f);
					}
				}
				if (atk.physicalSwing)
				{
					dirtyness += atk.cooldown * 1f;
					skills.AddExp(0, atk.damage / 300f);
				}
				return true;
			}
		}
		return false;
	}

	public void Scream()
	{
		if ((bool)talker)
		{
			talker.Talk(Locale.GetCharacter("fallscream"));
		}
	}

	public void TryStepUp()
	{
		if (!standing || (!((targetLookPos - base.transform.position).normalized.y > -0.9f) && crouching))
		{
			return;
		}
		Vector3 right = Vector3.right;
		right = Vector3.left;
		right = new Vector3(moveDir.x, 0f);
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position - Vector3.up * col.size.y * 0.5f + (Vector3)col.offset, right, col.size.x * 0.5f + col.edgeRadius * 5f, LayerMask.GetMask("Ground"));
		if ((bool)raycastHit2D && !Physics2D.Raycast(raycastHit2D.point + new Vector2(right.x * 0.5f, 1f), Vector2.up, col.size.y, LayerMask.GetMask("Ground")))
		{
			RaycastHit2D raycastHit2D2 = Physics2D.Raycast(raycastHit2D.point + new Vector2(right.x * 0.5f, 1f), Vector2.down, 1f, LayerMask.GetMask("Ground"));
			if ((bool)raycastHit2D2)
			{
				Vector2 vector = Vector3.up * (0.1f + raycastHit2D2.point.y - raycastHit2D.point.y) + right * (raycastHit2D.distance - col.size.x * 0.5f);
				base.transform.position += (Vector3)vector;
				visualBodyOffset += new Vector2(0f - vector.x, 0f - vector.y);
				rb.velocity = new Vector2(lastTimeStepVelocity.x, rb.velocity.y);
			}
		}
	}

	public void CreateCloudBig(Vector2 pos, Vector2? vel = null)
	{
		Vector2 valueOrDefault = vel.GetValueOrDefault();
		if (!vel.HasValue)
		{
			valueOrDefault = rb.velocity;
			vel = valueOrDefault;
		}
		ParticleSystem.MainModule main = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("DustBig"), pos, Quaternion.identity).GetComponent<ParticleSystem>().main;
		main.emitterVelocity = vel.Value;
	}

	public void CreateCloudSmall(Vector2 pos, Vector2? vel = null)
	{
		Vector2 valueOrDefault = vel.GetValueOrDefault();
		if (!vel.HasValue)
		{
			valueOrDefault = rb.velocity;
			vel = valueOrDefault;
		}
		ParticleSystem.MainModule main = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("DustSmall"), pos, Quaternion.identity).GetComponent<ParticleSystem>().main;
		main.emitterVelocity = vel.Value;
	}

	public void SetVelocity(Vector2 vel)
	{
		if (standing)
		{
			rb.velocity = vel;
			lastTimeStepVelocity = vel;
			return;
		}
		Limb[] array = limbs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].rb.velocity = vel;
		}
	}

	public void CreateCloudMini(Vector2 pos, Vector2? vel = null)
	{
		Vector2 valueOrDefault = vel.GetValueOrDefault();
		if (!vel.HasValue)
		{
			valueOrDefault = rb.velocity;
			vel = valueOrDefault;
		}
		ParticleSystem.MainModule main = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("DustMini"), pos, Quaternion.identity).GetComponent<ParticleSystem>().main;
		main.emitterVelocity = vel.Value;
	}

	public void TryExertSound(float chance = 0.4f, float volume = 0.25f)
	{
		if (UnityEngine.Random.value < chance)
		{
			Sound.Play("exert" + UnityEngine.Random.Range(1, 5), base.transform.position, twoDimensional: true, pitchShift: true, base.transform, volume);
		}
	}

	public void Jump()
	{
		if (!standing || !(jumpCooldown <= 0f) || forceWalk || !movingAllowed)
		{
			return;
		}
		float num = 1f;
		for (int i = 0; i < slots.Length; i++)
		{
			Item item = GetItem(i);
			if ((bool)item && item.Stats.jumpHeightMultChange != 0f)
			{
				num *= item.Stats.jumpHeightMultChange + 1f;
			}
		}
		if (grounded || (bool)currentClimbable || timeSinceGrounded <= 0.11f || bodyAffect.wasWater)
		{
			jumpCooldown = 0.25f;
			if ((bool)currentClimbable)
			{
				StopClimbing();
				rb.velocity = new Vector2(moveDir.x * actualMaxSpeed * 0.5f, rb.velocity.y);
				accelRot -= moveDir.x * actualMaxSpeed;
			}
			else
			{
				FootStep();
			}
			rb.velocity = new Vector2(rb.velocity.x, actualJumpSpeed * num);
			grounded = false;
			stamina -= 1f * (1f + overEncumberance) * (bodyAffect.wasWater ? 0.35f : 1f);
			temperature += 0.045f;
			for (int j = 0; j < legLimbs.Length; j++)
			{
				if (legLimbs[j].dislocated || legLimbs[j].broken)
				{
					legLimbs[j].pain += 6f * legLimbs[j].brokenPainMultiplier;
				}
			}
			if (limbs[0].broken)
			{
				limbs[0].pain += 2f * limbs[0].brokenPainMultiplier;
			}
			if (limbs[1].dislocated)
			{
				limbs[1].pain += 2f * limbs[1].brokenPainMultiplier;
			}
			if (crouchAmount > 0.5f)
			{
				standLerpTime = 0.5f;
			}
			skills.AddExp(1, 0.1f);
			TryExertSound(0.6f, 0.45f);
			CreateCloudBig(base.transform.position + Vector3.down * origColSize.y * 0.5f);
			PlayerCamera.main.wantsJumpInput = false;
		}
		else
		{
			if ((!canWalljumpLeft && !canWalljumpRight) || (!firstWallJump && (!canWalljumpLeft || !lastJumpedOnRightWall) && (!canWalljumpRight || lastJumpedOnRightWall)))
			{
				return;
			}
			Vector2 vector = Vector2.left;
			if (canWalljumpLeft)
			{
				vector = Vector2.right;
			}
			FootStep();
			if (canWalljumpRight)
			{
				lastJumpedOnRightWall = true;
			}
			else
			{
				lastJumpedOnRightWall = false;
			}
			Item wearable = GetWearable("climbingclaws");
			if ((bool)wearable)
			{
				wearable.condition -= 0.004f;
				firstWallJump = true;
			}
			else
			{
				firstWallJump = false;
			}
			jumpCooldown = 0.25f;
			if (limbs[0].broken)
			{
				limbs[0].pain += 2f * limbs[0].brokenPainMultiplier;
			}
			if (limbs[1].dislocated)
			{
				limbs[1].pain += 2f * limbs[1].brokenPainMultiplier;
			}
			rb.velocity = new Vector2(vector.x * actualJumpSpeed, actualJumpSpeed * num);
			stamina -= 0.7f * (1f + overEncumberance);
			temperature += 0.036f;
			for (int k = 0; k < legLimbs.Length; k++)
			{
				if (legLimbs[k].dislocated || legLimbs[k].broken)
				{
					legLimbs[k].pain += 4f * legLimbs[k].brokenPainMultiplier;
				}
			}
			skills.AddExp(1, 0.05f);
			slidingRight = false;
			slidingLeft = false;
			TryExertSound(0.6f, 0.45f);
			CreateCloudBig(base.transform.position + Vector3.down * origColSize.y * 0.5f);
			PlayerCamera.main.wantsJumpInput = false;
		}
	}

	public float GetTotalEncumberance()
	{
		float num = 0f;
		List<Container> list = new List<Container>();
		foreach (Item allItem in GetAllItems())
		{
			num += allItem.totalWeight;
			if (allItem.TryGetComponent<Container>(out var component))
			{
				list.Add(component);
			}
		}
		foreach (Item allWearable in GetAllWearables())
		{
			num += allWearable.totalWeight;
			if (allWearable.TryGetComponent<Container>(out var component2))
			{
				list.Add(component2);
			}
		}
		foreach (Container item in list)
		{
			num += item.GetEncumberance();
		}
		return num;
	}

	public void Burp()
	{
		burpTimer = UnityEngine.Random.Range(5f, 10f);
	}

	public void Eat(float hungerAmount, float weightGain)
	{
		DropItem(2);
		eatTime = 0.5f;
		if (limbs[0].dislocated || disfigured)
		{
			limbs[0].pain += hungerAmount * 0.5f;
			hungerAmount *= 0.75f;
			weightGain *= 0.75f;
		}
		float num = hunger;
		hunger += hungerAmount;
		if (hunger > 125f)
		{
			hunger = 125f;
		}
		float num2 = hunger - num;
		sicknessAmount += (hungerAmount - num2) * 1.2f;
		if (hungerAmount - num2 > 3f && UnityEngine.Random.Range(0f, 1f) < 0.2f)
		{
			vomiter.Vomit();
		}
		if (hunger > 90f && UnityEngine.Random.value < 0.1f)
		{
			burpTimer = UnityEngine.Random.Range(5f, 10f);
		}
		if (dirtyness >= 75f && UnityEngine.Random.value < 0.1f)
		{
			sicknessAmount += 8f;
		}
		weightOffset += weightGain;
		if ((bool)PlayerCamera.main)
		{
			PlayerCamera.main.caloriesConsumed += Mathf.RoundToInt(weightGain * 210f);
		}
	}

	private void FixedUpdate()
	{
		if (standing)
		{
			if (grounded && rb.velocity.y < 1f)
			{
				endedJump = false;
			}
			rb.gravityScale = ((rb.velocity.y > 0f && endedJump && moveDir.y != 1f && rb.velocity.y <= jumpSpeed) ? 2.5f : 1f);
			if (rb.velocity.x < actualMaxSpeed && moveDir.x > 0f)
			{
				rb.AddForce(Vector2.right * actualMoveForce);
			}
			if (rb.velocity.x > 0f - actualMaxSpeed && moveDir.x < 0f)
			{
				rb.AddForce(Vector2.left * actualMoveForce);
			}
			if (grounded && rb.velocity.x > actualMaxSpeed)
			{
				rb.velocity = new Vector2(actualMaxSpeed, rb.velocity.y);
			}
			if (grounded && rb.velocity.x < 0f - actualMaxSpeed)
			{
				rb.velocity = new Vector2(0f - actualMaxSpeed, rb.velocity.y);
			}
			if (grounded && groundNormalDir.x != 0f)
			{
				Vector2 vector = groundNormalDir;
				if (moveDir.x < 0f && vector.x < 0f)
				{
					vector.x = 0f;
				}
				if (moveDir.x > 0f && vector.x > 0f)
				{
					vector.x = 0f;
				}
				rb.AddForce(new Vector2((0f - groundNormalDir.x) * rb.mass * 52.5f, Mathf.Abs(groundNormalDir.x) * rb.mass * 22.5f));
			}
			if (!slippery && (moveDir.x == 0f || (moveDir.x > 0f && rb.velocity.x < 0f) || (moveDir.x < 0f && rb.velocity.x > 0f)))
			{
				if (grounded)
				{
					rb.velocity = new Vector2(rb.velocity.x * (1f - slowdownAmount * legSpeedMult), rb.velocity.y);
				}
				else
				{
					rb.velocity = new Vector2(rb.velocity.x * (1f - slowdownAmount * 0.5f * legSpeedMult), rb.velocity.y);
				}
			}
			TryStepUp();
			if ((slidingLeft || slidingRight) && rb.velocity.y < 0f && !slippery)
			{
				rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * (1f - actualWallSlideSlowdown));
			}
			float num = lastTimeStepVelocity.x - rb.velocity.x;
			float num2 = 6f - Mathf.Clamp01(legSpeedMult) * 5f;
			accelRot = Mathf.Lerp(accelRot, num * 15f * num2, Time.fixedDeltaTime * 5f);
		}
		else if (conscious)
		{
			endedJump = false;
			rb.gravityScale = 1f;
			if (lastTimeStepVelocity.magnitude < 7f || grounded)
			{
				float b = Vector2.SignedAngle(base.transform.right * (isRight ? 1f : (-1f)), (targetLookPos - limbs[1].transform.position).normalized) * 0.6f;
				limbs[0].rb.MoveRotation(Mathf.LerpAngle(limbs[0].transform.eulerAngles.z, b, 0.5f));
			}
			if ((grounded || bodyAffect.wasWater) && shock < 10f && rb.velocity.magnitude < maxSpeed * 0.1f)
			{
				Stand();
			}
			crawlTime += Time.fixedDeltaTime;
			if (Input.GetKey(KeyBinds.GetBind("ragdoll")) && moveDir.x != 0f && crawlTime > 0.8f && (grounded || bodyAffect.wasWater))
			{
				StartCoroutine("Crawl");
				crawlTime = 0f;
			}
		}
		else if (currentLimbCollisionMode == CollisionDetectionMode2D.Continuous)
		{
			SetLimbCollisionType(CollisionDetectionMode2D.None);
		}
		HandleClimbing();
		lastTimeStepVelocity = rb.velocity;
	}

	private void HandleClimbing()
	{
		if (!currentClimbable)
		{
			return;
		}
		if (!standing)
		{
			StopClimbing();
			return;
		}
		if (Vector2.Distance(base.transform.position, currentClimbable.GetPositionAtDistance(climbableProgress)) > 0.75f)
		{
			climbableProgress = currentClimbable.GetGrabInfo(base.transform.position).pathDistance;
			climbVelocity = Mathf.Lerp(climbVelocity, 0f, 0.5f);
			if (grounded)
			{
				StopClimbing();
				return;
			}
		}
		if (moveDir.y > -0.1f || climbVelocity > -6f)
		{
			climbVelocity = Mathf.Lerp(climbVelocity, moveDir.y * 12f * legSpeedMult - currentClimbable.downwardsVelocity, (climbVelocity > -12f) ? (Time.fixedDeltaTime * 7f) : (Time.fixedDeltaTime * 2f));
		}
		else
		{
			climbVelocity += Time.fixedDeltaTime * Physics2D.gravity.y * 0.7f;
		}
		climbableProgress += climbVelocity * Time.fixedDeltaTime;
		if (climbableProgress >= currentClimbable.totalLength)
		{
			climbableProgress = currentClimbable.totalLength;
			climbVelocity = 0f;
		}
		if (climbableProgress <= 0f)
		{
			StopClimbing();
			return;
		}
		rb.velocity = Vector2.up * climbVelocity;
		rb.MovePosition(currentClimbable.GetPositionAtDistance(climbableProgress));
	}

	private IEnumerator Crawl()
	{
		float time = 0.1f;
		while (time > 0f)
		{
			time -= Time.fixedDeltaTime;
			limbs[2].rb.AddForce(Vector2.right * moveDir.x * 6000f + Vector2.up * 3500f * legSpeedMult);
			limbs[2].rb.velocity *= 0.95f;
			yield return new WaitForFixedUpdate();
		}
	}

	public void DoGoreSound()
	{
		Sound.Play($"gore{UnityEngine.Random.Range(1, 6)}", base.transform.position);
	}

	public void UseItemInHand()
	{
		int slot = handSlot;
		if (conscious && HoldingItem(slot) && GetItem(slot).Stats.usable && GetItem(slot).Stats.usableWithLMB)
		{
			GetItem(slot).Stats.useAction(this, GetItem(slot));
			return;
		}
		Attack(new AttackInfo
		{
			damage = 30f,
			structuralDamage = 20f,
			distance = 4.5f,
			knockBack = 200f,
			cooldown = 0.2f,
			attackAnim = Resources.Load<GameObject>("ClawAnim"),
			staminaUse = 1f,
			piercing = false,
			swingSounds = new string[4] { "BSSwing1", "BSSwing2", "BSSwing3", "BSSwing4" },
			volume = 0.3f,
			rotateAmount = 11.5f,
			unarmed = true
		}, 0);
	}

	public void UseItem(Item item)
	{
		if (item.Stats.usable)
		{
			item.Stats.useAction(this, item);
		}
	}

	public void TakeANap()
	{
		if (canTakeNap)
		{
			PlayerCamera.main.ToggleWoundView();
			DropItem(0);
			DropItem(1);
			DropItem(2);
			if (sicknessAmount > 30f || totalHappiness < -50f || temperature < 34.5f || temperature > 38.5f)
			{
				StartCoroutine(AltNapCoroutine());
			}
			else
			{
				StartCoroutine(NapCoroutine());
			}
		}
	}

	private IEnumerator NapCoroutine()
	{
		bodyAnimator.Play("ExperimentLayDown");
		armsAnimator.Play("ArmsLayDown");
		movingAllowed = false;
		yield return new WaitForSeconds(0.3f);
		eyeCloseTime = 0.8f;
		eatTime = 0.7f;
		Sound.Play("stretch", base.transform.position, twoDimensional: false, pitchShift: false, null, 0.5f);
		yield return new WaitForSeconds(1.65f);
		movingAllowed = true;
		consciousness = 10f;
		sleeping = true;
	}

	private IEnumerator AltNapCoroutine()
	{
		bodyAnimator.Play("ExperimentLayDownAlt");
		armsAnimator.Play("ArmsLayDownAlt");
		movingAllowed = false;
		yield return new WaitForSeconds(0.4f);
		eyeCloseTime = 0.8f;
		yield return new WaitForSeconds(0.55f);
		movingAllowed = true;
		consciousness = 10f;
		sleeping = true;
	}

	public void SetLimbCollisionType(CollisionDetectionMode2D col)
	{
		Limb[] array = limbs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].rb.collisionDetectionMode = col;
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(base.transform.position + new Vector3(0f, origColSize.y * 0.25f), origColSize * new Vector2(1f, 0.5f));
	}

	private IEnumerator WaterShake()
	{
		float time = 0f;
		Sound.Play("dogshake", base.transform.position, twoDimensional: false, pitchShift: false, base.transform);
		dogShakeIntensity = 0f;
		while (time < 1f)
		{
			if (time < 0.75f)
			{
				dogShakeIntensity = Mathf.Lerp(dogShakeIntensity, 0.2f, Time.deltaTime * 3f);
			}
			else
			{
				dogShakeIntensity = Mathf.Lerp(dogShakeIntensity, 0f, Time.deltaTime * 7f);
			}
			wetness -= Time.deltaTime * 25f;
			time += Time.deltaTime;
			yield return null;
		}
		dogShakeIntensity = 0f;
	}

	private void Update()
	{
		Painkillers component = GetComponent<Painkillers>();
		if (reversedControls)
		{
			moveDir = -moveDir;
		}
		HandleVariableUpdates();
		HandleBody(component);
		HandleBodyTemperature(component);
		HandleDogWaterShaking();
		HandleRadiationSickness();
		HandlePeriodicChecks();
		HandleGroundedState();
		HandlePhysics();
		HandleVisuals(component);
		HandleSounds();
	}

	private void HandleGroundedState()
	{
		bool flag = false;
		RaycastHit2D raycastHit2D = Physics2D.BoxCast((Vector2)base.transform.position + col.offset, standing ? new Vector2(col.size.x, col.size.y) : new Vector2(col.size.x, 0.25f), 0f, Vector2.down, standing ? (col.edgeRadius + 0.2f) : 3.5f, LayerMask.GetMask("Ground"));
		RaycastHit2D raycastHit2D2 = Physics2D.BoxCast((Vector2)base.transform.position + col.offset, col.size, 0f, Vector2.right, col.edgeRadius + 0.1f, LayerMask.GetMask("Ground"));
		RaycastHit2D raycastHit2D3 = Physics2D.BoxCast((Vector2)base.transform.position + col.offset, col.size, 0f, Vector2.left, col.edgeRadius + 0.1f, LayerMask.GetMask("Ground"));
		slidingRight = moveDir.x > 0f && (bool)raycastHit2D2;
		slidingLeft = moveDir.x < 0f && (bool)raycastHit2D3;
		if (slidingRight)
		{
			timeSinceSlidRight = 0f;
		}
		if (slidingLeft)
		{
			timeSinceSlidLeft = 0f;
		}
		if (!grounded && rb.velocity.y < -2f && (slidingLeft || slidingRight))
		{
			if (!slideSource.isPlaying)
			{
				slideSource.Play();
			}
			slideSource.volume += Time.deltaTime * 10f;
			if (!wallSlideParticle.isPlaying)
			{
				wallSlideParticle.Play();
			}
		}
		else
		{
			slideSource.volume -= Time.deltaTime * 10f;
			if (slideSource.volume <= 0f)
			{
				slideSource.Stop();
			}
			if (wallSlideParticle.isPlaying)
			{
				wallSlideParticle.Stop();
			}
		}
		if ((bool)raycastHit2D)
		{
			flag = true;
			groundNormalDir = raycastHit2D.normal;
			ushort block = WorldGeneration.world.GetBlock(raycastHit2D.point - Vector2.up * 0.5f);
			if (block > 0)
			{
				standingOn = WorldGeneration.world.GetBlockInfo(block);
				curSleep = standingOn.sleep;
			}
			else
			{
				if (raycastHit2D.transform.TryGetComponent<BuildingEntity>(out var component))
				{
					standingOn = WorldGeneration.world.GetBlockInfo(component.blockFootstepSoundId);
				}
				else if (standingOn == null)
				{
					standingOn = WorldGeneration.world.GetBlockInfo(1);
				}
				curSleep = SleepQuality.Okay;
			}
			if (usingSleepingBag)
			{
				curSleep = BumpUpSleepQuality(curSleep);
			}
		}
		else if ((bool)raycastHit2D2)
		{
			ushort block2 = WorldGeneration.world.GetBlock(raycastHit2D2.point + Vector2.right * 0.5f);
			BuildingEntity component2;
			if (block2 > 0)
			{
				standingOn = WorldGeneration.world.GetBlockInfo(block2);
			}
			else if (raycastHit2D2.transform.TryGetComponent<BuildingEntity>(out component2))
			{
				standingOn = WorldGeneration.world.GetBlockInfo(component2.blockFootstepSoundId);
			}
			else if (standingOn == null)
			{
				standingOn = WorldGeneration.world.GetBlockInfo(1);
			}
		}
		else if ((bool)raycastHit2D3)
		{
			ushort block3 = WorldGeneration.world.GetBlock(raycastHit2D3.point + Vector2.left * 0.5f);
			BuildingEntity component3;
			if (block3 > 0)
			{
				standingOn = WorldGeneration.world.GetBlockInfo(block3);
			}
			else if (raycastHit2D3.transform.TryGetComponent<BuildingEntity>(out component3))
			{
				standingOn = WorldGeneration.world.GetBlockInfo(component3.blockFootstepSoundId);
			}
			else if (standingOn == null)
			{
				standingOn = WorldGeneration.world.GetBlockInfo(1);
			}
		}
		slippery = grounded && standingOn != null && standingOn.slippery;
		if (grounded && standingOn != null && standingOn.toxicity > 0f)
		{
			radiationSickness += standingOn.toxicity * Time.deltaTime;
			PlayerCamera.main.SetIrradiateIntensity(standingOn.toxicity * 0.25f);
		}
		if (grounded && standingOn != null && standingOn.health <= 1f)
		{
			for (int i = -1; i < 2; i++)
			{
				Vector2Int pos = WorldGeneration.world.WorldToBlockPos(base.transform.position - Vector3.up * (col.size.y * 0.5f - col.offset.y + col.edgeRadius + 0.5f) + Vector3.right * i);
				if (WorldGeneration.world.GetBlockInfo(WorldGeneration.world.GetBlock(pos)).health == 1f)
				{
					WorldGeneration.world.DamageBlock(pos, 1f);
				}
			}
		}
		if (flag && !grounded)
		{
			bodyAnimator.Play("Grounded");
			if (lastTimeStepVelocity.y < (0f - jumpSpeed) * 0.35f)
			{
				if (lastTimeStepVelocity.y < 0f - jumpSpeed - 5f)
				{
					CreateCloudBig(base.transform.position + Vector3.down * origColSize.y * 0.5f, new Vector2(rb.velocity.x, 0f));
				}
				else
				{
					CreateCloudSmall(base.transform.position + Vector3.down * origColSize.y * 0.5f, new Vector2(rb.velocity.x, 0f));
				}
			}
			if (lastTimeStepVelocity.y < (0f - jumpSpeed) * 1.8f)
			{
				Sound.Play(impactLarge.PickRandom(), base.transform.position);
			}
			else if (lastTimeStepVelocity.y < (0f - jumpSpeed) * 1.2f)
			{
				Sound.Play(impactMedium.PickRandom(), base.transform.position);
			}
			else if (lastTimeStepVelocity.y < (0f - jumpSpeed) * 0.5f)
			{
				Sound.Play(impactSmall.PickRandom(), base.transform.position);
			}
			FootStep();
		}
		else if (!flag && grounded && crouchAmount > 0.5f)
		{
			standLerpTime = 0.85f;
		}
		grounded = flag;
	}

	private void HandleSounds()
	{
		if (rb.velocity.y < (0f - jumpSpeed) * 2.25f && Time.time - fallScreamCooldown > 2f)
		{
			fallScreamCooldown = Time.deltaTime;
			Scream();
		}
	}

	private void HandleBody(Painkillers pnk)
	{
		if (WorldGeneration.world.generatingWorld)
		{
			return;
		}
		HandleCirculation(pnk);
		clawHealth = Mathf.Clamp(clawHealth + Time.deltaTime * clawGrowthRate, 0f, 100f);
		brainGrowSickness = Mathf.Max(brainGrowSickness - Time.deltaTime, 0f);
		dirtyness = Mathf.Clamp(dirtyness + Time.deltaTime / 1500f, 0f, 100f);
		badSleepAmount -= Time.deltaTime;
		breathing = alive && respiratoryRate > 10f;
		caffeinated = Mathf.Max(caffeinated - Time.deltaTime, 0f);
		if (standing && (!conscious || legSpeedMult <= 0f || shock > 10f))
		{
			Ragdoll();
		}
		if (averagePain > 99f)
		{
			shock = 100f;
			Ragdoll();
		}
		hearingLoss = Mathf.Clamp(hearingLoss - Time.deltaTime * 0.05f, 0f, 100f);
		internalBleeding = Mathf.Clamp(internalBleeding - Time.deltaTime * 0.045f, 0f, 100f);
		if (internalBleeding > 5f)
		{
			hemothorax += IntBleedingClamped() * Time.deltaTime * 0.0088f;
		}
		else
		{
			hemothorax -= Time.deltaTime * 0.036f;
			if (hemothorax < 0f)
			{
				hemothorax = 0f;
			}
		}
		if (limbs[1].pain < hemothorax * 0.25f)
		{
			limbs[1].pain += Time.deltaTime * 3.5f;
		}
		bloodVolume -= IntBleedingClamped() * Time.deltaTime * 0.0057f;
		averagePain = 0f;
		totalBleedSpeed = 0f;
		float num = 0f;
		Limb[] array = limbs;
		foreach (Limb limb in array)
		{
			if (!limb.dismembered)
			{
				totalBleedSpeed += limb.bleedAmount * limb.bleedSpeedMult * (limb.blockedBleeding ? 0f : 1f);
				averagePain = Mathf.Max(limb.pain - curAdrenaline * 0.5f, averagePain);
				num += limb.infectionAmount;
				if (hunger <= 0f)
				{
					limb.muscleHealth -= Time.deltaTime * 0.15f * limb.starvationHealthLossMult;
				}
			}
		}
		averagePain *= 1f - skills.RESFrom10 * 0.025f;
		totalBleedSpeed += IntBleedingClamped() * 0.0057f;
		totalBleedSpeed -= bloodRegenSpeed;
		if (totalBleedSpeed < 0f)
		{
			totalBleedSpeed = 0f;
		}
		if (averagePain > 50f && conscious)
		{
			traumaAmount += averagePain * Time.deltaTime * 0.0034f;
		}
		else
		{
			traumaAmount -= Time.deltaTime / 35f;
		}
		traumaAmount = Mathf.Clamp(traumaAmount, 0f, 100f);
		if (num > 100f)
		{
			septicShock += Time.deltaTime * num * 0.00028f * WorldGeneration.GetRunSettingFloat("infectionspeed");
			if (septicShock > 100f)
			{
				septicShock = 100f;
			}
		}
		else
		{
			septicShock -= Time.deltaTime * 0.07f;
			if (septicShock < 0f)
			{
				septicShock = 0f;
			}
		}
		venomTotal = Mathf.MoveTowards(venomTotal, 0f, Time.deltaTime / 10.5f);
		venomCurrent = Mathf.MoveTowards(venomCurrent, venomTotal, Time.deltaTime);
		if (venomCurrent > 0f)
		{
			float num2 = 100f - venomCurrent * 0.5f;
			if (bloodOxygen > num2)
			{
				bloodOxygen = Mathf.MoveTowards(bloodOxygen, num2, Time.deltaTime * 0.7f);
			}
			if (bloodViscosity < venomCurrent)
			{
				bloodViscosity = Mathf.MoveTowards(bloodViscosity, venomCurrent, Time.deltaTime / 4.5f);
			}
			bloodVolume -= venomCurrent * Time.deltaTime / 500f;
		}
		if (averagePain > 75f)
		{
			painShock += Time.deltaTime / 30f;
		}
		else
		{
			painShock -= Time.deltaTime / 30f;
		}
		painShock = Mathf.Clamp01(painShock);
		if (painShock > 0.66f)
		{
			consciousness = 0f;
		}
		if (conscious)
		{
			skills.AddExp(1, Time.deltaTime * averagePain * 0.0125f);
		}
		float num3 = 0f;
		float num4 = Mathf.Min(100f, limbs[0].muscleHealth * 2.1f, bloodOxygen * 1.2f, bloodPressure.Remap(60f, 110f, 30f, 100f), bloodPressure.Remap(140f, 200f, 100f, 30f), 150f - sicknessAmount, sleeping ? 10f : 100f, brainHealth, Mathf.Clamp(energy * 4.2f, 31f, 100f), 100f - num3, 100f * currentTemperatureMovementMult, 150f - averagePain, 100f - radiationSickness * 0.7f, (badSleepAmount > 0f) ? 70f : 100f, (bloodPressure < 60f) ? 0f : 100f);
		if (consciousness > num4)
		{
			consciousness = Mathf.MoveTowards(consciousness, num4, consciousnessFallRate * Time.deltaTime);
		}
		else
		{
			consciousness = Mathf.MoveTowards(consciousness, num4, consciousnessRiseRate * Time.deltaTime * ((Time.time - goodSleepTime < 10f) ? 3f : 1f));
		}
		consciousness = Mathf.Clamp(consciousness, 0f, 100f);
		brainHealth = Mathf.Clamp(brainHealth + Time.deltaTime * ((brainHealth > 0f) ? 0.003f : 0f) * WorldGeneration.GetRunSettingFloat("healingrate"), 0f, 100f);
		if (stamina < 1f)
		{
			shock = 60f;
		}
		sicknessAmount = Mathf.Clamp(sicknessAmount - Time.deltaTime * 0.06f * WorldGeneration.GetRunSettingFloat("metabolismrate"), 0f, 100f);
		if (sicknessAmount >= 95f)
		{
			limbs[2].infected = true;
		}
		if (!conscious)
		{
			if (alive)
			{
				energy += Time.deltaTime * 0.4f * SleepQualityToRegen(forcedSleepQuality ?? curSleep) * WorldGeneration.GetRunSettingFloat("sleepcyclespeed");
				if (sleeping)
				{
					happiness = Mathf.MoveTowards(happiness, 0f, Time.deltaTime * 0.01f * ((happiness < 0f) ? 1f : 0.5f) * WorldGeneration.GetRunSettingFloat("moodnormalizationrate"));
				}
				if (!triedRollingLastStand && brainHealth <= 15f)
				{
					TryLastStand();
				}
			}
		}
		else
		{
			energy -= Time.deltaTime * 0.07f * WorldGeneration.GetRunSettingFloat("sleepcyclespeed") * (2f - stamina * 0.01f) * (1f + sicknessAmount * 0.02f) * (1f - Math.Clamp(totalHappiness * 0.01f, -1f, 0f)) * ((caffeinated > 0f) ? 0.55f : 1f);
			if (sicknessAmount > 20f)
			{
				happiness -= Mathf.Clamp01(sicknessAmount * 0.01f) * Time.deltaTime * 0.05f * WorldGeneration.GetRunSettingFloat("metabolismrate");
			}
			happiness -= Mathf.Clamp01(0.65f - hunger * 0.01f) * Time.deltaTime * 0.065f * WorldGeneration.GetRunSettingFloat("metabolismrate");
			happiness -= Mathf.Clamp01(0.65f - Mathf.Min(thirst, 120f) * 0.01f) * Time.deltaTime * 0.065f * WorldGeneration.GetRunSettingFloat("metabolismrate");
			if (averagePain > 50f)
			{
				happiness -= averagePain * Time.deltaTime * 0.001f;
			}
			if (happiness > -50f)
			{
				happiness -= Mathf.Clamp01(totalBleedSpeed) * Time.deltaTime * 0.12f;
			}
			if (hunger > 101f)
			{
				happiness += hunger * 0.0001f * Time.deltaTime * WorldGeneration.GetRunSettingFloat("metabolismrate");
			}
			if (temperature < 33.5f || temperature > 40f)
			{
				happiness -= Time.deltaTime * 0.03f;
			}
		}
		if (energy > 100f)
		{
			energy = 100f;
		}
		if (energy < 0f)
		{
			energy = 0f;
			if (WorldGeneration.GetRunSettingBool("forcesleep"))
			{
				consciousness = Mathf.Min(consciousness, 10f);
				sleeping = true;
			}
		}
		if (sleeping && (energy >= 99f || (curSleep == SleepQuality.Mediocre && energy > 85f) || (curSleep == SleepQuality.Bad && energy > 70f) || (energy > 1f && averagePain > 31f) || (sicknessAmount > 55f && energy > 40f) || (energy > 50f && (totalHappiness < -50f || hunger < 35f || thirst < 35f || septicShock > 35f || temperature < 30f || temperature > 40.5f)) || (energy > 4f && inWater) || temperature < 30f) && (energy > 99f || (!GetComponent<SleepingPills>() && !WorldGeneration.GetRunSettingBool("nosleeprestrictions"))))
		{
			WakeUp();
		}
		if (alive)
		{
			hunger = Mathf.Clamp(hunger - BaseHungerRate, -50f, 125f);
			thirst = Mathf.Clamp(thirst - BaseThirstRate(tempDiffFromNormal), -50f, 250f);
			if (thirst < 30f && limbs[1].pain < 25f)
			{
				limbs[1].pain = Mathf.MoveTowards(limbs[1].pain, 25f, Time.deltaTime);
			}
			if (thirst < 0f)
			{
				bloodViscosity += Time.deltaTime * 0.25f;
			}
			if (thirst > 175f)
			{
				limbs[0].pain = Mathf.MoveTowards(limbs[0].pain, 50f, Time.deltaTime * 2.5f);
				brainHealth -= Time.deltaTime * 0.05f;
			}
			weightOffset -= Time.deltaTime * ((1f - hunger * 0.01f) * 0.015f + 0.003f) * WorldGeneration.GetRunSettingFloat("metabolismrate");
			if ((standing && Mathf.Abs(moveDir.x) < 0.1f && !exercising && !currentClimbable) || !standing)
			{
				stamina = Mathf.Clamp(stamina + Time.deltaTime * staminaStrength.Evaluate(energy * 0.01f) * (1f - overEncumberance * 0.7f) * 1.4f * ((caffeinated > 0f) ? 2f : 1f) * (((!inWater || hasScubaGear || stamina < 25f) && breathing) ? 1f : 0f) * (1f + skills.RESFrom10 * 0.02f) * WorldGeneration.GetRunSettingFloat("staminaregen"), 0f, Mathf.Max(70f, bloodOxygen));
			}
			else
			{
				stamina = Mathf.Clamp(stamina - Time.deltaTime * 0.1f * (1f + overEncumberance) * ((caffeinated > 0f) ? 0.25f : 1f), 0f, Mathf.Max(70f, bloodOxygen));
				temperature += Time.deltaTime * 0.04f;
			}
			if (strokeAmount > 0f)
			{
				bloodVolume -= Time.deltaTime * 0.1f;
				brainHealth -= Time.deltaTime * 0.025f;
				if (strokeAmount > 90f)
				{
					TryStartFibrillation(forced: true);
				}
			}
			strokeAmount = Mathf.Clamp(strokeAmount, 0f, 100f);
		}
		if (Mathf.Abs(weightOffset) > 60f)
		{
			TryStartFibrillation(forced: true);
		}
		weightOffset = Mathf.Clamp(weightOffset, -80f, 100f);
		float num5 = ((lastStandTime > 0f) ? (-5f) : 1.7f);
		happiness = Mathf.Clamp(happiness, -100f, 100f);
		wetness = Mathf.Clamp(wetness - Time.deltaTime * ((wetness > 75f) ? 0.35f : 0.2f), 0f, 100f);
		brainHealth = Mathf.Clamp(brainHealth, 0f, 100f);
		shock = Mathf.Clamp(shock - Time.deltaTime * (conscious ? 10f : 0f), 0f, 100f);
		adrenaline = Mathf.Clamp(adrenaline - Time.deltaTime * num5, 0f, 100f);
		curAdrenaline = Mathf.MoveTowards(curAdrenaline, adrenaline, Time.deltaTime * 5f);
	}

	public void WakeUp()
	{
		sleeping = false;
		SleepQuality num = forcedSleepQuality ?? curSleep;
		if (num == SleepQuality.Mediocre)
		{
			badSleepAmount = 60f;
		}
		if (num == SleepQuality.Bad)
		{
			badSleepAmount = 150f;
		}
		if (num == SleepQuality.Good)
		{
			caffeinated = Mathf.Max(caffeinated, 180f);
			goodSleepTime = Time.time;
		}
		forcedSleepQuality = null;
		usingSleepingBag = false;
	}

	private void HandlePhysics()
	{
		if (grounded || (bool)currentClimbable)
		{
			timeSlidfor = 0f;
			firstWallJump = true;
			timeSinceGrounded = 0f;
		}
		else
		{
			timeSinceGrounded += Time.deltaTime;
			if (slidingLeft || slidingRight)
			{
				timeSlidfor += Time.deltaTime;
			}
			if (bodyAffect.wasWater)
			{
				firstWallJump = true;
			}
		}
		if (standing)
		{
			timeRagdolled = 0f;
			TryStepUp();
		}
		else
		{
			timeRagdolled += Time.deltaTime;
		}
		if (timeRagdolled > 60f)
		{
			Stand();
		}
		if (moveDir.y > 0.5f && !currentClimbable)
		{
			StartClimbing(Climbable.GetClosestClimbable(base.transform.position));
		}
		inWater = FluidManager.main.WaterInfo(WorldGeneration.world.WorldToBlockPos(limbs[0].transform.position)).type > 0;
		if ((bool)Physics2D.OverlapBox(base.transform.position + new Vector3(0f, origColSize.y * 0.25f), origColSize * new Vector2(1f, 0.5f), 0f, LayerMask.GetMask("Ground")))
		{
			crouching = true;
		}
		if (crouching && !currentClimbable)
		{
			crouchAmount = Mathf.Lerp(crouchAmount, 1f, Time.deltaTime * 6f);
		}
		else
		{
			crouchAmount = Mathf.Lerp(crouchAmount, 0f, Time.deltaTime * 6f);
		}
		crouchAmount = Math.Max(crouchAmount, 1f - energy);
		crouchAmount = Math.Max(crouchAmount, (harmer.timeWasStill - 25f) * 0.1f);
		col.size = Vector2.Lerp(origColSize, origColSize * new Vector2(1f, 0.5f), crouchAmount);
		col.offset = Vector2.Lerp(Vector2.zero, new Vector2(0f, (0f - origColSize.y) * 0.25f), crouchAmount);
		rb.constraints = ((!WorldGeneration.world.generatingWorld && WorldGeneration.world.worldExists) ? RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.FreezeAll);
		if (moveDir.y < 0f && conscious && moveDir.x == 0f && FluidManager.main.HasLiquid(WorldGeneration.world.WorldToBlockPos(base.transform.position - Vector3.up * 2.5f)))
		{
			liquidDrinkTime += Time.deltaTime;
			if (liquidDrinkTime > 2f)
			{
				liquidDrinkTime = 0f;
				FluidManager.main.DrinkLiquid(WorldGeneration.world.WorldToBlockPos(base.transform.position - Vector3.up * 2.5f), this);
			}
		}
		else if (moveDir.y > -1f || !FluidManager.main.HasLiquid(WorldGeneration.world.WorldToBlockPos(base.transform.position - Vector3.up * 2.5f)))
		{
			liquidDrinkTime = 0f;
		}
	}

	private void HandleVisuals(Painkillers pnk)
	{
		float num = 1f;
		float b = 0f;
		if (!isRight)
		{
			num = -1f;
		}
		if (((isRight && targetLookPos.x < base.transform.position.x) || (!isRight && targetLookPos.x > base.transform.position.x)) && (moveDir.x != 0f || attackCooldown > 0f))
		{
			SwitchDir();
		}
		limpAnimatorSpeed = Mathf.MoveTowards(limpAnimatorSpeed, 0f, Time.deltaTime * 5.2f);
		bodyAnimator.speed = 1f + limpAnimatorSpeed;
		burpTimer -= Time.deltaTime;
		if (burpTimer > -50f && burpTimer < 0f)
		{
			burpTimer = -100f;
			eatTime = 1f;
			Sound.Play("burp", base.transform.position, twoDimensional: false, pitchShift: true, base.transform, UnityEngine.Random.Range(0.15f, 0.3f));
		}
		bonusRot = attackRot + accelRot;
		if (moveDir.magnitude < 0.1f && rb.velocity.magnitude < 1f && standing && totalHappiness > -90f && !currentClimbable && !inWater)
		{
			idleTime += Time.deltaTime;
		}
		else
		{
			idleTime = 0f;
			if (bodyAnimator.GetCurrentAnimatorClipInfo(0).Length != 0 && bodyAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "ExperimentSit")
			{
				bodyAnimator.Play("Grounded");
				standLerpTime = 0f;
			}
			if (armsAnimator.GetCurrentAnimatorClipInfo(0).Length != 0 && armsAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "ArmsSit")
			{
				armsAnimator.Play("Grounded");
			}
		}
		if (idleTime > 12f && movingAllowed && !exercising)
		{
			bodyAnimator.Play("ExperimentSit");
			armsAnimator.Play("ArmsSit");
		}
		if (inWater)
		{
			lastLiquidColor = FluidManager.main.LiquidColor(WorldGeneration.world.WorldToBlockPos(limbs[0].transform.position));
		}
		else if (lastLiquidColor != Color.clear)
		{
			PlayerCamera.main.SetDroplets(lastLiquidColor);
			lastLiquidColor = Color.clear;
		}
		if (idleTime < 160f)
		{
			Vector2 vector = ((overrideLookTime > 0f) ? overrideLookPos : ((Vector2)targetLookPos));
			if (isRight && vector.x < limbs[1].transform.position.x + 0.01f)
			{
				vector.x = limbs[1].transform.position.x + 0.5f;
				vector.y = Mathf.Lerp(limbs[1].transform.position.y + (limbs[1].transform.position.y - vector.y), limbs[1].transform.position.y, 0.95f);
			}
			if (!isRight && vector.x > limbs[1].transform.position.x - 0.01f)
			{
				vector.x = limbs[1].transform.position.x - 0.5f;
				vector.y = Mathf.Lerp(limbs[1].transform.position.y + (limbs[1].transform.position.y - vector.y), limbs[1].transform.position.y, 0.95f);
			}
			float num2 = Vector2.SignedAngle(base.transform.right * num, (vector - (Vector2)limbs[1].transform.position).normalized) * 0.6f;
			b = num2 * 0.3f;
			lastHeadAngle = Mathf.LerpAngle(lastHeadAngle, num2, Time.deltaTime * 12f);
			limbs[0].bonusRot = lastHeadAngle - (accelRot + attackRot) + (grounded ? 0f : (crouchAmount * 45f * num));
		}
		else
		{
			limbs[0].bonusRot = 0f;
			lastHeadAngle = 0f;
		}
		torsoLookSmooth = Mathf.Lerp(torsoLookSmooth, b, Time.deltaTime * 4f);
		float num3 = 0f;
		if ((bool)pnk && pnk.actualOpiateReception < -25f)
		{
			num3 = 0.025f;
		}
		if ((bool)harmer && harmer.timeWasStill > 20f)
		{
			num3 += (harmer.timeWasStill - 20f) * 0.01f;
		}
		num3 += dogShakeIntensity + brainShakeIntensity + Mathf.Clamp01(miscShakeIntensity) * 0.05f;
		if (temperature < 32.5f)
		{
			num3 += 0.025f;
		}
		float num4 = ((averagePain > 75f) ? (averagePain * 0.0004f) : 0f) + num3;
		bool flag = grounded && crouchAmount > 0.5f && (bool)Physics2D.Raycast(base.transform.position, Vector2.right * num, 2.1f, LayerMask.GetMask("Ground"));
		visualBodyOffset = Vector2.Lerp(visualBodyOffset, flag ? new Vector2(-1f * num, 0f) : Vector2.zero, Time.deltaTime * 8f);
		float b2 = (Physics2D.OverlapBox(base.transform.position, new Vector2(origColSize.x, 1f), 0f, LayerMask.GetMask("Ground")) ? (-30f) : 0f);
		extraCrouchSmooth = Mathf.Lerp(extraCrouchSmooth, b2, Time.deltaTime * 5f);
		float num5 = (bodyLerpFromRagdoll ? 0.35f : 1f);
		standLerpTime += Time.deltaTime * 2.5f * num5;
		if (standLerpTime >= 1f)
		{
			bodyLerpFromRagdoll = false;
		}
		if (standing)
		{
			Limb[] array = limbs;
			foreach (Limb limb in array)
			{
				Vector2 a = limb.transform.localPosition;
				float z = limb.transform.eulerAngles.z;
				limb.transform.localPosition = (Vector2)bodyAnimator.transform.InverseTransformPoint(limb.animLimb.transform.position) + (visualBodyOffset * base.transform.localScale + Vector2.down * Mathf.Abs(accelRot * 0.015f));
				limb.transform.eulerAngles = new Vector3(0f, 0f, limb.animLimb.eulerAngles.z * num);
				if (limb.bonusRot != 0f)
				{
					limb.transform.RotateAround(limb.transform.TransformPoint(limb.joint.anchor), Vector3.forward, limb.bonusRot);
				}
				float angle = accelRot + ((limb.isLegLimb && limb != limbs[2]) ? (0f + (grounded ? 0f : (crouchAmount * 15f * num))) : (attackRot + torsoLookSmooth - (grounded ? 0f : (crouchAmount * 45f * num)) + ((!limb.isLegLimb || limb == limbs[2]) ? (extraCrouchSmooth * num) : 0f)));
				limb.transform.RotateAround(limbs[2].transform.position, Vector3.forward, angle);
				if (Mathf.Abs(moveDir.x) > 0.1f && (limb.isLegLimb || (limb.isHead && limb.broken)) && (limb.broken || limb.dislocated))
				{
					limb.pain += Time.deltaTime * 3.5f * limb.brokenPainMultiplier;
				}
				if (limb.isArm)
				{
					limb.transform.RotateAround(limbs[1].transform.position, Vector3.forward, (accelRot + (grounded ? 0f : (crouchAmount * 45f)) + attackRot + extraCrouchSmooth) * (0f - num) - torsoLookSmooth + armOffset);
				}
				limb.transform.position += new Vector3(UnityEngine.Random.Range(0f - num4, num4), UnityEngine.Random.Range(0f - num4, num4));
				if (standLerpTime < 1f)
				{
					limb.transform.localPosition = Vector2.Lerp(a, limb.transform.localPosition, standLerpTime * Time.deltaTime * 40f * num5);
					limb.transform.eulerAngles = new Vector3(0f, 0f, Mathf.LerpAngle(z, limb.transform.eulerAngles.z, standLerpTime * Time.deltaTime * 40f * num5));
				}
			}
		}
		bodyAnimator.SetFloat("ForwardSpeed", rb.velocity.x * num);
		bodyAnimator.SetFloat("UpSpeed", currentClimbable ? climbVelocity : rb.velocity.y);
		bodyAnimator.SetBool("grounded", grounded);
		armsAnimator.SetFloat("ForwardSpeed", rb.velocity.x * num);
		armsAnimator.SetFloat("UpSpeed", rb.velocity.y);
		float value = InOutSine(Mathf.Max(crouchAmount, 1f - legSpeedMult)) * 10000f;
		bodyAnimator.SetFloat("CrouchAmount", value);
		armsAnimator.SetFloat("CrouchAmount", value);
		armsAnimator.SetBool("grounded", grounded);
		bodyAnimator.SetBool("climbing", currentClimbable);
		armsAnimator.SetBool("climbing", currentClimbable);
		float num6 = limbs[1].transform.eulerAngles.z;
		if (num6 > 180f)
		{
			num6 -= 360f;
		}
		float value2 = Mathf.Lerp(armsAnimator.GetFloat("gunangle"), Vector2.SignedAngle(base.transform.right * num, (targetLookPos - limbs[1].transform.position).normalized) * num - num6 * num, Time.deltaTime * 8f * slots[handSlot].armPowerMult);
		armsAnimator.SetBool("gun", HoldingItem(handSlot) && GetItem(handSlot).Stats.HasTag("gun"));
		armsAnimator.SetFloat("gunangle", value2);
		if (isRight)
		{
			if (slidingLeft)
			{
				bodyAnimator.SetFloat("wallSideFloat", -1f);
				bodyAnimator.SetFloat("wallSideFloat", -1f);
				bodyAnimator.SetInteger("wallSide", 1);
				if (!grounded)
				{
					bodyAnimator.Play("Wall");
				}
			}
			else if (slidingRight)
			{
				bodyAnimator.SetFloat("wallSideFloat", 1f);
				bodyAnimator.SetInteger("wallSide", 2);
				if (!grounded)
				{
					bodyAnimator.Play("Wall");
				}
			}
			else
			{
				bodyAnimator.SetInteger("wallSide", 0);
			}
		}
		else if (slidingLeft)
		{
			bodyAnimator.SetFloat("wallSideFloat", 1f);
			bodyAnimator.SetInteger("wallSide", 2);
			if (!grounded)
			{
				bodyAnimator.Play("Wall");
			}
		}
		else if (slidingRight)
		{
			bodyAnimator.SetFloat("wallSideFloat", -1f);
			bodyAnimator.SetInteger("wallSide", 1);
			if (!grounded)
			{
				bodyAnimator.Play("Wall");
			}
		}
		else
		{
			bodyAnimator.SetInteger("wallSide", 0);
		}
		radGlow.intensity = Mathf.Clamp01(radiationSickness * 0.02f - 1f);
	}

	private void HandleDogWaterShaking()
	{
		if (conscious && wetness > 30f && temperature < 36.6f && !bodyAffect.wasWater)
		{
			wetShakeTime += Time.deltaTime;
			if (wetShakeTime > 12f)
			{
				wetShakeTime = 0f;
				StartCoroutine("WaterShake");
			}
		}
		else
		{
			wetShakeTime = 0f;
		}
	}

	private void HandleVariableUpdates()
	{
		timeSinceSlidLeft += Time.deltaTime;
		timeSinceSlidRight += Time.deltaTime;
		liquidSlipTime = Mathf.MoveTowards(liquidSlipTime, 0f, Time.deltaTime * 0.5f);
		liquidRagdollBar = Mathf.Clamp01(liquidRagdollBar + Time.deltaTime * 0.2f);
		stimulantMultiplier = Mathf.MoveTowards(stimulantMultiplier, 0f, Time.deltaTime * 0.02f);
		miscShakeIntensity = Mathf.MoveTowards(miscShakeIntensity, 0f, Time.deltaTime);
		clawRegrowTime = Mathf.Max(clawRegrowTime - Time.deltaTime, 0f);
		hungerLimbHealCurrent = hungerLimbHeal.Evaluate(hunger);
		attackRot = Mathf.Lerp(attackRot, 0f, Time.deltaTime * 3f);
		if (bothEyesGone && !eyeGone)
		{
			bothEyesGone = false;
		}
		if (disfigured && limbs[0].muscleHealth > 50f)
		{
			limbs[0].muscleHealth = Mathf.MoveTowards(limbs[0].muscleHealth, 50f, Time.deltaTime);
		}
		fallShakeCooldown -= Time.deltaTime;
		soundCooldown -= Time.deltaTime;
		armOffset = Mathf.LerpAngle(armOffset, 0f, Time.deltaTime * 3f);
		tempDiffFromNormal = temperature - 37f;
		temporarySlowdown = Mathf.MoveTowards(temporarySlowdown, 0f, Time.deltaTime * 0.1f);
		horrifiedLevel = Mathf.MoveTowards(horrifiedLevel, 0f, Time.deltaTime * 20f);
		focusedLevel = Mathf.MoveTowards(focusedLevel, 0f, Time.deltaTime * 20f);
		eyeCloseTime -= Time.unscaledDeltaTime;
		eyeScareTime -= Time.unscaledDeltaTime;
		eyePanicTime -= Time.unscaledDeltaTime;
		overrideLookTime -= Time.unscaledDeltaTime;
		attackCooldown -= Time.deltaTime;
		eatTime -= Time.deltaTime;
		jumpCooldown -= Time.deltaTime;
	}

	private void HandleBodyTemperature(Painkillers pnk)
	{
		if (Time.time - tempCheckTime >= 1f)
		{
			tempCheckTime = Time.time;
			float totalInsulation = GetTotalInsulation();
			temperature = Mathf.Lerp(temperature, WorldGeneration.world.ambientTemperature, baseTemperatureLerpRate / totalInsulation);
			float num = 1f - Mathf.Clamp01(0.3f - energy * 0.01f);
			if ((bool)pnk && pnk.actualOpiateReception > 0f)
			{
				num -= pnk.actualOpiateReception * 0.005f;
			}
			temperature += 0.04f * num;
			if (temperature > 37.5f)
			{
				float num2 = (temperature - 37.5f) * 20f;
				if (wetness < num2)
				{
					wetness += 1f;
				}
			}
			temperature -= wetness * 0.001f;
			if (temperature < 36.5f)
			{
				temperature += Mathf.Max(hunger * 0.01f, 0.3f) * 0.03f * num;
			}
			if (temperature < 32f)
			{
				hunger -= 0.035f;
				temperature += 0.01f;
			}
		}
		if (temperature > 42f)
		{
			brainHealth -= Time.deltaTime * 0.5f;
		}
		if (temperature > 41f)
		{
			bloodVolume -= Time.deltaTime * 0.15f;
		}
	}

	private void HandlePeriodicChecks()
	{
		minuteCheckTime += Time.deltaTime;
		halfMinuteCheckTime += Time.deltaTime;
		secondCheckTime += Time.deltaTime;
		halfSecondCheckTime += Time.unscaledDeltaTime;
		limbBloodUpdateTimer += Time.deltaTime;
		if (minuteCheckTime > 60f)
		{
			minuteCheckTime = 0f;
			GetComponent<PantSound>().TryGrowl();
			if (radiationSickness > 30f)
			{
				if (UnityEngine.Random.value < radiationSickness * 0.0025f * WorldGeneration.GetRunSettingFloat("infectionchance"))
				{
					Limb limb = limbs[UnityEngine.Random.Range(0, limbs.Length)];
					limb.infected = true;
					limb.infectionAmount = Mathf.Max(10f, limb.infectionAmount);
				}
				if (UnityEngine.Random.value < radiationSickness * 0.007f)
				{
					internalBleeding += UnityEngine.Random.value * radiationSickness * 0.75f;
				}
				if (UnityEngine.Random.value < radiationSickness * 0.00015f)
				{
					brainHealth -= 5f;
					limbs[1].muscleHealth = 3.5f;
					vomiter.Vomit();
					shock = 100f;
					bloodViscosity += 30f;
				}
			}
			if (totalHappiness < -30f && UnityEngine.Random.value < (0f - totalHappiness) * 0.01f - 0.1f)
			{
				StartCoroutine("Cry");
			}
		}
		if (halfMinuteCheckTime > 30f)
		{
			halfMinuteCheckTime -= 30f;
			if (brainHealth < 95f && WorldGeneration.GetRunSettingBool("braindamagefx"))
			{
				if (UnityEngine.Random.value > brainHealth * 0.01f && UnityEngine.Random.value < 0.75f)
				{
					talker.Talk(Locale.GetCharacter("hitbycreature"));
					PlayerCamera.main.threatMusicTime = 8f;
				}
				if (UnityEngine.Random.value > brainHealth * 0.01f && UnityEngine.Random.value < 0.8f)
				{
					StartCoroutine("BrainControlReverse");
				}
				if (UnityEngine.Random.value > brainHealth * 0.01f && UnityEngine.Random.value < 0.06f)
				{
					vomiter.Vomit();
				}
				if (UnityEngine.Random.value > brainHealth * 0.01f)
				{
					if (UnityEngine.Random.value < 0.75f)
					{
						DropItem(0);
					}
					if (UnityEngine.Random.value < 0.75f)
					{
						DropItem(1);
					}
					if (UnityEngine.Random.value < 0.75f)
					{
						DropItem(2);
					}
					talker.Talk("..?");
				}
				if (UnityEngine.Random.value > brainHealth * 0.006f)
				{
					StartCoroutine(BrainDamageRagdoll());
				}
				if (consciousness > 20f && (double)UnityEngine.Random.value > (double)brainHealth * 0.01 && !PlayerCamera.main.lastStandPanel.activeSelf)
				{
					PlayerCamera.main.StartCoroutine("FlashBrain");
				}
			}
			for (int i = 0; i < limbs.Length; i++)
			{
				limbs[i].transform.localScale = new Vector3(1f + weightOffset * limbs[i].weightVisualScaleMult * 0.01f * ((weightOffset > 0f) ? 1f : 0.75f), 1f, 1f);
			}
			for (int j = 0; j < slots.Length; j++)
			{
				if (HoldingItem(j))
				{
					GetItem(j).transform.localScale = new Vector3(1f / slots[j].limb.transform.localScale.x, 1f, 1f);
				}
			}
		}
		if (secondCheckTime > 1f)
		{
			secondCheckTime -= 1f;
			currentWeightMovementMult = weightMovementCurve.Evaluate(weightOffset);
			currentTemperatureMovementMult = temperatureMovementCurve.Evaluate(temperature);
			clothingTemperature = 0f;
			bleedClottingSpeed = 0.025f * Mathf.Clamp01(bloodViscosity.Remap(-100f, 0f, 0f, 1f)) * Mathf.Clamp01(1f - venomCurrent / 20f);
			bleedingSpeedMultiplier = (0.01f + Mathf.Clamp01(bloodViscosity.Remap(-100f, 0f, 0.01f, 0f))) * WorldGeneration.GetRunSettingFloat("bleedrate");
			lastStandTime -= 1f;
			if (bloodViscosity > 90f && UnityEngine.Random.value < 0.0166f)
			{
				hasPulmonaryEmbolism = true;
			}
			if (bloodViscosity < 50f && hasPulmonaryEmbolism)
			{
				hasPulmonaryEmbolism = false;
			}
			if (hasPulmonaryEmbolism)
			{
				limbs[1].muscleHealth -= 0.6f;
			}
			if (bloodPressure > 180f && UnityEngine.Random.value < 0.02f)
			{
				strokeAmount = Mathf.Max(strokeAmount, 0.1f);
			}
			if (strokeAmount > 0f)
			{
				strokeAmount += 0.1333f * (WorldGeneration.GetRunSettingBool("strokes") ? 1f : (-1f));
			}
			if (thirst > 175f && UnityEngine.Random.value < 0.01666f)
			{
				TryStartFibrillation(forced: true);
			}
			thirstBloodPressure = thirstBloodPressureCurve.Evaluate(thirst);
			foreach (Item allWearable in GetAllWearables())
			{
				clothingTemperature += allWearable.Stats.wearableIsolation;
			}
			bloodPressureChangeFromMedicine = Mathf.MoveTowards(bloodPressureChangeFromMedicine, 0f, 1f);
			happiness = Mathf.MoveTowards(happiness, 0f, 0.01f * ((happiness < 0f) ? 1f : 0.75f) * WorldGeneration.GetRunSettingFloat("moodnormalizationrate"));
		}
		if (halfSecondCheckTime > 0.5f)
		{
			maxEncumberance = (11f + Mathf.Clamp(weightOffset + 15f, -60f, 0f) * 0.1f - sicknessAmount * 0.025f + ((hunger > 100f) ? 1.5f : 0f) - ((hunger < 40f) ? 1.5f : 0f) - ((thirst < 40f) ? 1f : 0f) + Mathf.Min(skills.STRFrom10 * 0.5f, skills.RESFrom10 * 0.5f)) * WorldGeneration.GetRunSettingFloat("encumbrancecap");
			totalEncumberance = GetTotalEncumberance();
			overEncumberance = Mathf.Clamp01(totalEncumberance / maxEncumberance - 1f);
			if (overEncumberance > 0.15f && standing && idleTime < 5f)
			{
				skills.AddExp(0, 0.1f * Time.timeScale);
				skills.AddExp(1, 0.1f * Time.timeScale);
			}
			halfSecondCheckTime = 0f;
			hasScubaGear = HasWearable("scubadivinggear");
			mindWipe = GetComponent<MindwipeScript>();
			antibioticImmunityTime -= 0.5f;
			if (antibioticImmunityTime < 0f)
			{
				antibioticImmunityTime = 0f;
			}
			float num = (hunger - 70f) * 0.75f;
			float num2 = (thirst - 60f) * 0.3f;
			float num3 = (energy - 60f) * 0.2f;
			float num4 = (temperature - 37f) * 8f;
			float num5 = (bloodVolume - 100f) * 0.2f;
			float num6 = Math.Max(0f, dirtyness - 50f);
			immunity = 100f + num + num2 + num3 + num4 + num5 - num6 - sicknessAmount * 0.8f - radiationSickness * 0.5f;
			if (antibioticImmunityTime > 0f)
			{
				immunity += 70f;
			}
			immunity = Mathf.Clamp(immunity, 0f, 200f);
			curImmunityMult = immunityInfectionSpeed.Evaluate(immunity);
			onHardStimulants = CoUtils.instance.DurationOf("midgradestimulant") > 0f || CoUtils.instance.DurationOf("lowgradestimulant") > 0f || CoUtils.instance.DurationOf("highgradestimulant") > 0f;
		}
		if (limbBloodUpdateTimer > 0.2f)
		{
			limbBloodUpdateTimer = 0f;
			Limb[] array = limbs;
			for (int k = 0; k < array.Length; k++)
			{
				array[k].FurBloodUpdate();
			}
		}
	}

	private void HandleRadiationSickness()
	{
		if (radiationSickness > 0f)
		{
			if (sicknessAmount < radiationSickness * 0.4f)
			{
				sicknessAmount += Mathf.Clamp(Time.deltaTime * 0.45f, 0f, Mathf.Clamp01(radiationSickness - sicknessAmount));
			}
			brainHealth -= Time.deltaTime * radiationSickness * 0.0003f;
			Limb[] array;
			if (radiationSickness > 30f)
			{
				array = limbs;
				foreach (Limb limb in array)
				{
					if (limb.skinHealth > 100f - radiationSickness * 0.5f)
					{
						limb.skinHealth -= Time.deltaTime * 0.2f;
					}
				}
			}
			if (radiationSickness > 10f)
			{
				bloodVolume -= Time.deltaTime * radiationSickness * 0.00025f;
			}
			thirst -= Time.deltaTime * radiationSickness * 0.0002f;
			array = limbs;
			foreach (Limb limb2 in array)
			{
				if (limb2.muscleHealth > 100f - radiationSickness * 0.5f)
				{
					limb2.muscleHealth -= Time.deltaTime * 0.2f;
				}
				if (limb2.pain < radiationSickness * 0.3f)
				{
					limb2.pain += Time.deltaTime * 2f;
				}
			}
			radiationSickness = Mathf.Clamp(radiationSickness - Time.deltaTime * 0.033f, 0f, 100f);
		}
		else
		{
			radiationSickness = Mathf.Max(radiationSickness, 0f);
		}
	}

	public float GetTotalInsulation()
	{
		float num = 1f + clothingTemperature + weightOffset * 0.01f;
		if (usingSleepingBag)
		{
			num += 0.5f;
		}
		if (num < 0.5f)
		{
			num = 0.5f;
		}
		return num;
	}

	private IEnumerator BrainDamageRagdoll()
	{
		float timer = 0f;
		while (timer < 5f)
		{
			timer += Time.deltaTime;
			brainShakeIntensity = timer * 0.05f;
			yield return null;
		}
		brainShakeIntensity = 0f;
		Ragdoll();
	}

	private float IntBleedingClamped()
	{
		return Mathf.Clamp(internalBleeding, 0f, 25f);
	}

	public void Drink(float amt)
	{
		DropItem(2);
		thirst += amt;
		eatTime = 0.5f;
	}

	public static Limb LimbFromObject(GameObject obj, Vector2 pos)
	{
		if (!obj)
		{
			return null;
		}
		if (obj.TryGetComponent<Limb>(out var component))
		{
			return component;
		}
		if (obj.TryGetComponent<Body>(out var component2))
		{
			return component2.GetClosestLimb(pos);
		}
		return null;
	}

	public IEnumerator BrainControlReverse()
	{
		int max = UnityEngine.Random.Range(6, 12);
		for (int i = 0; i < max; i++)
		{
			reversedControls = true;
			yield return new WaitForSeconds(UnityEngine.Random.Range(0.25f, 2f));
			reversedControls = false;
			yield return new WaitForSeconds(UnityEngine.Random.Range(0.6f, 2.5f));
		}
	}

	public IEnumerator Cry()
	{
		specialCrying = true;
		yield return new WaitForSeconds(UnityEngine.Random.Range(10f, 30f));
		specialCrying = false;
	}

	public static float Remap(float value, float from1, float to1, float from2, float to2)
	{
		return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
	}

	public static float InOutSine(float t)
	{
		return (float)(Math.Cos((double)t * Math.PI) - 1.0) / -2f;
	}
}

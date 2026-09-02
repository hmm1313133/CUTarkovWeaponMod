using Newtonsoft.Json;
using UnityEngine;

[Saveable]
[JsonObject(MemberSerialization.OptIn)]
public class GunScript : MonoBehaviour
{
	public enum AmmoType
	{
		Pistol,
		Rifle,
		Shotgun
	}

	public enum RoundInChamber
	{
		Round,
		Casing,
		None
	}

	public enum FiringMode
	{
		Pump,
		SemiAuto,
		Auto
	}

	public enum FeedType
	{
		Mag,
		Direct
	}

	public AmmoType ammoType;

	[JsonProperty]
	public RoundInChamber roundInChamber;

	public FiringMode firingMode;

	public FeedType feedType;

	[JsonProperty]
	public int roundsInMag;

	[JsonProperty]
	public int magCapacity;

	[JsonProperty]
	public bool hasMag;

	[JsonProperty]
	public bool triggerPressed;

	[JsonProperty]
	public bool firingPinStruck;

	public float knockBack;

	public AudioClip fireSound;

	public AudioClip customRack;

	public AudioClip customUnrack;

	public Transform barrel;

	public float structureDamage;

	public float animalDamage;

	public float loudness;

	public float desiredGasTime;

	public int shotsPerFire;

	public float verticalSpread;

	public float conditionLossPerShot;

	[JsonProperty]
	public bool safe;

	public Sprite normalSprite;

	public Sprite rackedSprite;

	public Sprite normalSpriteNoMag;

	public Sprite rackedSpriteNoMag;

	private SpriteRenderer render;

	private Item it;

	private float gasTime;

	[JsonProperty]
	public bool racked;

	[JsonProperty]
	public bool lastRacked;

	public ParticleSystem muzzleParticle;

	private Body body => PlayerCamera.main.body;

	public float JamChance()
	{
		float num = ((!(it.condition > 0.5f)) ? Body.Remap(it.condition, 0f, 0.5f, 0.0025f, 0.1f) : Body.Remap(it.condition, 0.5f, 1f, 0f, 0.0025f));
		if (it.isWet)
		{
			num += 0.045f;
		}
		return num;
	}

	private void Start()
	{
		render = GetComponent<SpriteRenderer>();
		it = GetComponent<Item>();
	}

	public void TryRack()
	{
		racked = !racked;
	}

	public void ToggleSafety()
	{
		safe = !safe;
		Sound.Play("gunsafety", base.transform.position);
	}

	public void LoadMag(AmmoScript ammo)
	{
		if (ammo.ammoType == ammoType)
		{
			if (racked && roundInChamber == RoundInChamber.None && ammo.itemType == AmmoScript.AmmoItemType.Round)
			{
				roundInChamber = RoundInChamber.Round;
				Sound.Play("gunloadshell", base.transform.position);
				Object.Destroy(ammo.gameObject);
			}
			else if (!hasMag && feedType == FeedType.Mag && ammo.itemType == AmmoScript.AmmoItemType.Magazine)
			{
				hasMag = true;
				roundsInMag = ammo.rounds;
				Sound.Play("gunloadmag", base.transform.position);
				Object.Destroy(ammo.gameObject);
			}
			else if (feedType == FeedType.Direct && ammo.itemType == AmmoScript.AmmoItemType.Round && roundsInMag < magCapacity)
			{
				roundsInMag++;
				Sound.Play("gunloadshell", base.transform.position);
				Object.Destroy(ammo.gameObject);
			}
		}
	}

	public void UnloadMag()
	{
		if (hasMag && feedType != FeedType.Direct)
		{
			Sound.Play("gununloadmag", base.transform.position);
			hasMag = false;
			GameObject gameObject = Object.Instantiate(Resources.Load(AmmoScript.AmmoTypeToMagazine(ammoType)), base.transform.position, Quaternion.identity) as GameObject;
			gameObject.GetComponent<AmmoScript>().rounds = roundsInMag;
			body.AutoPickUpItem(gameObject.GetComponent<Item>());
			roundsInMag = 0;
		}
	}

	private void Update()
	{
		if (triggerPressed && !firingPinStruck)
		{
			firingPinStruck = true;
			Sound.Play("guntrigger", base.transform.position);
			if (roundInChamber == RoundInChamber.Round && !racked && !safe)
			{
				Fire();
			}
		}
		if (firingPinStruck && !triggerPressed)
		{
			firingPinStruck = false;
		}
		gasTime -= Time.deltaTime;
		if (gasTime < 0f && gasTime > -100f)
		{
			if (roundsInMag > 0)
			{
				racked = false;
				if (firingMode == FiringMode.Auto)
				{
					firingPinStruck = false;
				}
			}
			gasTime = -100f;
		}
		if (lastRacked && !racked)
		{
			if ((bool)customUnrack)
			{
				Sound.Play(customUnrack, base.transform.position, twoDimensional: true);
			}
			else
			{
				Sound.Play("gununrack", base.transform.position, twoDimensional: true);
			}
			if (roundsInMag > 0 && roundInChamber == RoundInChamber.None)
			{
				if (Random.value > JamChance())
				{
					roundInChamber = RoundInChamber.Round;
					roundsInMag--;
				}
				else
				{
					Sound.Play("gunjam", base.transform.position, twoDimensional: true);
				}
			}
		}
		if (!lastRacked && racked)
		{
			if ((bool)customRack)
			{
				Sound.Play(customRack, base.transform.position, twoDimensional: true);
			}
			else
			{
				Sound.Play("gunrack", base.transform.position, twoDimensional: true);
			}
			if (roundInChamber != RoundInChamber.None)
			{
				if (Random.value > JamChance())
				{
					(Object.Instantiate(Resources.Load((roundInChamber == RoundInChamber.Casing) ? "casing" : AmmoScript.AmmoTypeToItem(ammoType)), base.transform.position, base.transform.rotation) as GameObject).GetComponent<Rigidbody2D>().velocity = base.transform.up * 12f;
					roundInChamber = RoundInChamber.None;
				}
				else
				{
					Sound.Play("gunjam", base.transform.position, twoDimensional: true);
				}
			}
		}
		triggerPressed = false;
		lastRacked = racked;
		render.sprite = ((!racked) ? (hasMag ? normalSprite : normalSpriteNoMag) : (hasMag ? rackedSprite : rackedSpriteNoMag));
	}

	public void Fire(bool suicide = false)
	{
		float num = (body.isRight ? 1f : (-1f));
		body.rb.velocity -= (Vector2)base.transform.right * num * knockBack;
		body.eyeScareTime = 1f;
		body.hearingLoss += loudness * ((body.hearingLoss > 20f) ? 0.2f : 1f);
		muzzleParticle.Play();
		roundInChamber = RoundInChamber.Casing;
		it.condition -= conditionLossPerShot * 0.01f;
		if (firingMode != 0)
		{
			if (Random.value > JamChance())
			{
				racked = true;
				gasTime = desiredGasTime;
			}
			else
			{
				racked = Random.value > 0.5f;
				Sound.Play("gunjam", base.transform.position, twoDimensional: true);
			}
		}
		Sound.Play(fireSound, base.transform.position, twoDimensional: true, pitchShift: false);
		for (int i = 0; i < shotsPerFire; i++)
		{
			TurretScript.Shoot(new FireInfo
			{
				pos = (suicide ? body.transform.position : barrel.position),
				dir = (base.transform.right + base.transform.up * (Random.Range(-1f, 1f) * verticalSpread)) * num,
				doTinnitus = false,
				ignoreBody = !suicide,
				structureDamage = structureDamage,
				animalDamage = animalDamage,
				forceHead = suicide
			});
		}
		body.armsAnimator.SetFloat("gunangle", body.armsAnimator.GetFloat("gunangle") + knockBack * 8f);
	}
}

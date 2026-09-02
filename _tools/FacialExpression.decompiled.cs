using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FacialExpression : MonoBehaviour
{
	public Body body;

	public Light2D eyeLight;

	public SpriteRenderer eyes;

	public SpriteRenderer head;

	public List<Eye> eyeList;

	public Sprite defaultHead;

	public Sprite defaultHeadMouth;

	public Sprite defaultHeadMouthHalf;

	public Sprite[] disfiguredHead;

	public Sprite[] disfiguredHeadHeal;

	public Sprite eyesGone;

	public Sprite eyesGoneHealed;

	public int disfiguredIndex;

	public float disfiguredTimeFullSkin;

	public float eyeTimeHealed;

	public SpriteRenderer nosebleedSprite;

	private float blinkTime;

	private bool doBackEye
	{
		get
		{
			if ((!(body.moveDir.x < 0f) || !body.isRight) && (!(body.moveDir.x > 0f) || body.isRight))
			{
				return body.peekingBehind;
			}
			return true;
		}
	}

	private void Start()
	{
		blinkTime = Random.Range(2f, 4.2f);
		head = GetComponent<SpriteRenderer>();
	}

	private void Update()
	{
		blinkTime -= Time.deltaTime;
		if (blinkTime <= 0f)
		{
			blinkTime = Random.Range(2f, 4.2f);
		}
		float num = 1f;
		Eye eye = eyeList[0];
		bool flag = false;
		if (body.averagePain > 95f || body.eyePanicTime > 0f || body.rb.velocity.y < (0f - body.jumpSpeed) * 2.25f)
		{
			eye = eyeList[4];
			flag = true;
		}
		else if (body.averagePain >= 70f || body.totalHappiness < -75f || body.specialCrying)
		{
			eye = eyeList[5];
			flag = true;
		}
		else if (body.averagePain >= 80f || body.energy < 10f || body.eyeCloseTime > 0f || body.idleTime > 160f || body.radiationSickness > 80f)
		{
			eye = eyeList[2];
			flag = true;
		}
		else if ((body.shock > 21f || body.adrenaline > 20f || !body.breathing || body.averagePain > 50f || body.eyeScareTime > 0f || body.totalBleedSpeed > 0.1f || (!body.standing && !body.grounded) || body.rb.velocity.y < (0f - body.jumpSpeed) * 1.5f || body.radiationSickness > 60f) && !body.mindWipe)
		{
			eye = eyeList[3];
		}
		else if (body.consciousness < 70f || body.energy < 20f || body.averagePain > 35f || body.sicknessAmount > 40f || body.temperature < 32f || body.temperature > 40f || (bool)body.mindWipe)
		{
			eye = eyeList[1];
		}
		else if (body.totalHappiness > 50f)
		{
			eye = eyeList[6];
			flag = true;
		}
		if (!flag)
		{
			if (blinkTime < 0.04f)
			{
				eye = eyeList[1];
			}
			else if (blinkTime < 0.08f)
			{
				eye = eyeList[2];
			}
			else if (blinkTime < 0.12f)
			{
				eye = eyeList[1];
			}
		}
		if (!body.conscious || body.inWater || body.dogShakeIntensity > 0.1f)
		{
			eye = eyeList[2];
		}
		eyes.sprite = (doBackEye ? eye.back : eye.front);
		if (body.eyeGone)
		{
			eyeTimeHealed += Time.deltaTime;
			if (body.isRight || body.bothEyesGone)
			{
				eyes.sprite = ((eyeTimeHealed > 350f) ? eyesGoneHealed : eyesGone);
			}
			num *= (body.bothEyesGone ? 0f : 0.5f);
		}
		if (!body.disfigured)
		{
			if (body.eatTime > 0.15f || body.HoldingItem(2) || body.limbs[0].dislocated)
			{
				head.sprite = defaultHeadMouth;
			}
			else if (body.eatTime > 0f)
			{
				head.sprite = defaultHeadMouthHalf;
			}
			else
			{
				head.sprite = defaultHead;
			}
			float target = ((body.internalBleeding > 5f || body.bloodPressure > 155f) ? 1f : 0f);
			nosebleedSprite.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(nosebleedSprite.color.a, target, Time.deltaTime * 0.1f));
		}
		else
		{
			if (body.limbs[0].skinHealth >= 99f)
			{
				disfiguredTimeFullSkin += Time.deltaTime;
			}
			head.sprite = ((disfiguredTimeFullSkin > 300f) ? disfiguredHeadHeal : disfiguredHead)[disfiguredIndex];
			nosebleedSprite.color = Color.clear;
		}
		eyeLight.intensity = num;
	}
}

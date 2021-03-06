﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class CharacterMovement : MonoBehaviour 
{
	public AudioClip PickUpSound;
	public AudioClip DropSound;
	public ParticleSystem DamageParticles;
	public ParticleSystem DamageParticlesNoCoins;
	public AudioClip DamageSoundWithCoins;
	public AudioClip DamageSoundNoCoins;

	public float KillBelow_Y = -10;
	public int coins = 100;
	public float Speed = 5.0f;
	public float SlowRatio = 0.5f;
	public float RotationSpeed = 10.0f;
	public bool carryingBoulder = false;
	public bool carryingChest = false;
	public bool pushingChest = false;
	bool dropped = false;
	private bool performingActionAnimation = false;
	float gravity = 9.8f;
	float canDropTimer = 0.5f;
	public float ExertionTime{get; private set;}

	Vector3 moveDirection = Vector3.zero;
	CharacterController controller;
	GameObject chestNode;
	Animator animator;
	GameObject playerForward;
	Transform pickedUpObject;
	int pickedUpObjectLayer;
	
	HUDScript hudScript;
	
	// Use this for initialization
	void Start ()
	{
		chestNode = GameObject.Find("ChestNode");
		playerForward = GameObject.Find("PlayerForward");
		animator = GetComponent<Animator>();

		hudScript = GameObject.Find("scoreText").GetComponent<HUDScript>();
		
		hudScript.score = coins;
	}
	
	// Update is called once per frame
	void Update () 
	{

		if (transform.position.y < KillBelow_Y)
			TakeDamage(10);
		if(carryingChest == true)
		{
			canDropTimer -= Time.deltaTime;

			if(canDropTimer < 0 && Input.GetButtonDown("Jump"))
			{
				canDropTimer = 0.5f;
				DropChest();
			}
		}


		if(carryingBoulder == true)
		{
			canDropTimer -= Time.deltaTime;
			
			if(canDropTimer < 0 && Input.GetButtonDown("Jump"))
			{
				canDropTimer = 0.5f;
				DropBoulder();
			}
		}




		//transform.Rotate(new Vector3(0, 90 * Time.deltaTime, 0));
		controller = GetComponent<CharacterController>();
		if (controller.isGrounded && !performingActionAnimation)
		{
			moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
			if (moveDirection.sqrMagnitude > 0.01f)
				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection.normalized), RotationSpeed);

			float ratio = 1.0f;
			if (carryingChest)
				ratio = SlowRatio;
			if (pushingChest)
				ratio = SlowRatio;
			if (carryingBoulder)
				ratio = SlowRatio;
			moveDirection *= ratio * Speed * Time.deltaTime;
		}
		else
			moveDirection = Vector3.zero;
		moveDirection.y -= gravity * Time.deltaTime;
		controller.Move(moveDirection);

		float speed = controller.velocity.magnitude / Speed;
		animator.SetFloat("Speed", speed);

		if(speed > 0.5f || (speed > 0.2f && (carryingChest || carryingBoulder || pushingChest)))
		{
			ExertionTime += Time.deltaTime;
		}
		else
		{
			ExertionTime -= 2*Time.deltaTime;
		}
		ExertionTime = Mathf.Clamp(ExertionTime, 0, 11);

	}
	public void Pushed()
	{
		animator.SetBool("Pushing", true);
		performingActionAnimation = false;
	}
	public void Lifted()
	{
		if (!carryingChest && !carryingBoulder)
		{
			animator.SetBool("Lifting", true);

			if (pickedUpObject.tag == "Boulder")
				carryingBoulder = true;
			else if(pickedUpObject.tag == "Chest")
				carryingChest = true;

			performingActionAnimation = false;
			pickedUpObject.parent = chestNode.transform;
			pickedUpObject.position = chestNode.transform.position;
			pickedUpObject.rigidbody.isKinematic = true;
			pickedUpObjectLayer = pickedUpObject.gameObject.layer;
			pickedUpObject.gameObject.layer = LayerMask.NameToLayer("HeldObject");
			this.PlaySoundEffect(PickUpSound);
		}
	}

	public bool IsCarryingChest()
	{
		return carryingChest;
	}
	public bool IsCarryingBoulder()
	{
		return carryingBoulder;
	}

	public bool Dropped()
	{
		return dropped;
	}

	public void PickUpChest(Transform ob)
	{
		pickedUpObject = ob;
		animator.SetTrigger("Lift");
		performingActionAnimation = true;
	}
	public void DropChest()
	{
		carryingChest = false;
		animator.SetBool("Lifting", false);

		pickedUpObject.gameObject.layer = pickedUpObjectLayer;
		pickedUpObject.transform.parent = null;
		pickedUpObject.rigidbody.isKinematic = false;
		this.PlaySoundEffect(DropSound);
	}

	public void PickUpBoulder(Transform ob)
	{
		pickedUpObject = ob;
		animator.SetTrigger("Lift");
		performingActionAnimation = true;
	}
	public void DropBoulder()
	{
		carryingBoulder = false;
		animator.SetBool("Lifting", false);
		
		pickedUpObject.gameObject.layer = pickedUpObjectLayer;
		pickedUpObject.transform.parent = null;
		pickedUpObject.rigidbody.isKinematic = false;
		this.PlaySoundEffect(DropSound);
	}
	
	public bool IsPushingChest()
	{
		return pushingChest;
	}
	
	public void OnPushingChest()
	{
		pushingChest = true;
		animator.SetTrigger("Push");
		performingActionAnimation = true;
	}
	public void OffPushingChest()
	{
		pushingChest = false;
		animator.SetBool("Pushing", false);
	}

	public Vector3 GetMoveDirection()
	{
		return moveDirection;
	}

	void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if(hit.transform.tag == "Chest" && playerForward.GetComponent<PickUpChest>().CanPush())
		{
			//pushingObject = hit.transform.gameObject;
		}
	}

	public void OnTriggerEnter(Collider other)
	{
		var damage = other.GetComponent<PlayerDamager>();
		if (damage != null)
		{
			DamageParticlesNoCoins.transform.LookAt(other.transform);
			DamageParticlesNoCoins.Play();
			TakeDamage(damage.DamageAmmount);
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		var damage = collision.gameObject.GetComponent<PlayerDamager>();
		if (damage != null)
		{
			DamageParticlesNoCoins.transform.LookAt(collision.transform);
			DamageParticlesNoCoins.Play();
			TakeDamage(damage.DamageAmmount);
		}

	}
	
	public void TakeDamage(int damage)
	{
		if (carryingChest)
		{
			DamageParticles.Play();
			this.PlaySoundEffect(DamageSoundNoCoins);
			this.PlaySoundEffect(DamageSoundWithCoins);
		}
		else
		{
			this.PlaySoundEffect(DamageSoundNoCoins);
		}
		coins -= damage;
		if (coins < 0)
			coins = 0;

		hudScript.score = coins;

		if (coins == 0)
		{
			Application.LoadLevel(Application.loadedLevel);
		}
	}
}

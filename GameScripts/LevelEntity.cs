﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;
using System.Reflection;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider))]
public class LevelEntity : MonoBehaviour {
	BoxCollider boxCollider;
	SpriteRenderer spriteRenderer;
	Material spriteMaterial;
	MultigenState state;
	MultigenParser multigen;
	MultigenObject mobj;
	WadFile wad;
	public string stateName;
	public float direction;
	static float tick = (1.0f / 35.0f);

	public float reactionTime;

	GameObject target;
	bool justAttacked = false;

	AudioClip seeSound;
	AudioClip attackSound;

	string spriteName {
		get {
			return state.spriteName + state.spriteFrame;
		}
	}

	bool timerActive = true;
	float stateTimer;

	Dictionary<string, Sprite[]> sprites;

	public void LoadMultigen(MultigenParser multigen, MultigenObject mobj, WadFile wad) {
		this.mobj = mobj;
		this.multigen = multigen;
		sprites = new Dictionary<string, Sprite[]>();
		this.wad = wad;

		boxCollider = GetComponent<BoxCollider>();
		boxCollider.isTrigger = !mobj.data["flags"].Contains("MF_SOLID");

		float radius = ReadFracValue(mobj.data["radius"]);
		float height = ReadFracValue(mobj.data["height"]);

		boxCollider.size = new Vector3(radius, height, radius);

		reactionTime = float.Parse(mobj.data["reactiontime"]) * tick;

		spriteMaterial = new Material(Shader.Find("Doom/Texture"));
		Texture2D paletteLookup = new Palette(wad.GetLump("PLAYPAL")).GetLookupTexture();
		Texture2D colormapLookup = new Colormap(wad.GetLump("COLORMAP")).GetLookupTexture();
		spriteMaterial.SetTexture("_Palette", paletteLookup);
		spriteMaterial.SetTexture("_Colormap", colormapLookup);

		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteRenderer.material = spriteMaterial;

		string seeSoundName = ParseSoundName(mobj.data["seesound"]);
		if (wad.Contains(seeSoundName)) {
			seeSound = new DoomSound(wad.GetLump(seeSoundName), seeSoundName).ToAudioClip();
		}

		state = multigen.states[mobj.data["spawnstate"]];
		LoadState();
	}

	string ParseSoundName(string value) {
		return value.Replace("sfx_","DS").ToUpper();
	}

	float ReadFracValue(string value) {
		float output;
		if (value.Contains("*")) {
			output = float.Parse(value.Substring(0, value.IndexOf("*")));
			output /= 64f;
		} else {
			output = float.Parse(value);
		}
		return output;
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		//direction += Time.deltaTime * 100.0f;

		if (sprites[spriteName].Length > 1) {
			float cameraAngle = Mathf.Atan2(Camera.main.transform.position.z - transform.position.z, Camera.main.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
			//cameraAngle = 0f;

			int angle = Mathf.RoundToInt(((cameraAngle - direction)+360.0f) / 45.0f);
			angle = (angle + 8) % 8;
			spriteRenderer.sprite = sprites[spriteName][angle];
		} else {
			spriteRenderer.sprite = sprites[spriteName][0];
		}

		transform.localEulerAngles = new Vector3(0f, Camera.main.transform.eulerAngles.y, 0f);
		
		if (timerActive) {
			stateTimer -= Time.deltaTime;
			if (stateTimer <= 0f) {
				ChangeState();
			}
		}

	}
	
	void LoadState() {
		if (!sprites.ContainsKey(spriteName)) {
			sprites.Add(spriteName, state.GetSprite(wad));
		}

		if (state.name == "S_NULL") {
			GameObject.Destroy(gameObject);
		}

		if (state.duration == "-1") timerActive = false;
		stateTimer += float.Parse(state.duration) * tick;
		MethodInfo method = this.GetType().GetMethod(state.action);
		if (method != null) {
			method.Invoke(this, new object[]{});
		}

		stateName = state.name;
	}

	void ChangeState() {
		state = multigen.states[state.nextState];
		
		LoadState();
	}

	void LookForPlayers() {

	}

	void NewChaseDir() {

	}

	bool CheckRange() {
		return false;
	}

	///// CODEPOINTERS =====================================

	public void A_Look() {
		Vector3 heading = Camera.main.transform.position - transform.position;
		float dot = Vector3.Dot(heading.normalized, Quaternion.AngleAxis(direction, Vector3.forward) * Vector3.right);

		float playerDistance = Vector3.Distance(transform.position, Camera.main.transform.position);

		// If player is in front of the object
		if (dot > 0) {
			RaycastHit raycastinfo;
			Ray checkRay = new Ray(transform.position, (Camera.main.transform.position - transform.position).normalized);

			if (!Physics.Raycast(checkRay, out raycastinfo, playerDistance)) {
				state = multigen.states[mobj.data["seestate"]];
				
				if (seeSound != null) {
					AudioSource.PlayClipAtPoint(seeSound, transform.position);
				}

				ChangeState();
			}
		}
	}

	public void A_Chase() {
		if (reactionTime > 0f) reactionTime -= Time.deltaTime;

		if (target == null) {
			LookForPlayers();

			if (target == null) {
				state = multigen.states[mobj.data["spawnstate"]];
				ChangeState();
			}

			return;
		}

		if (justAttacked) { 
			justAttacked = false;
			NewChaseDir();
			return;
		}

		if (mobj.data["meleestate"] != "S_NULL" && CheckRange()) {
			if (attackSound != null) {
				AudioSource.PlayClipAtPoint(attackSound, transform.position);
			}

			state = multigen.states[mobj.data["meleestate"]];
			ChangeState();
		}		
	}

	public void A_FaceTarget() {
		direction = Mathf.Atan2(Camera.main.transform.position.z - transform.position.z, Camera.main.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
	}

	
}

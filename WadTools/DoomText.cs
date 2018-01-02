﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WadTools;

/*

For generating text with fonts loaded from doom.

*/

public class DoomText {

	private WadFile wad;
	private string lumpIdent = "STCFN0";

	public DoomText(WadFile wad) {
		this.wad = wad;
	}

	public Texture2D Write(string text) {
		int textLength = text.Length;
		Texture2D[] chars = new Texture2D[textLength];
		int[] spacing = new int[textLength];
		int currentSpacing = 0;
		for (int i = 0; i < textLength; i++) {
			chars[i] = GetChar(text[i]);
			spacing[i] = currentSpacing;
			if (chars[i] != null) {
				currentSpacing += chars[i].width;
			} else {
				currentSpacing += 6;
			}
		}

		Texture2D output = new Texture2D(currentSpacing, 8, TextureFormat.RGBA32, false, true);

		for (int i = 0; i < textLength; i++) {
			if (chars[i] != null) {
				output.SetPixels32(spacing[i], 0, chars[i].width, chars[i].height, chars[i].GetPixels32());
				Debug.Log("123123");
			}
		}
		output.Apply();

		output.filterMode = FilterMode.Point;
		return output;
	}

	private Texture2D GetChar(char c) {
		string lumpName = lumpIdent + (int)char.ToUpper(c);
		Debug.Log(lumpName);
		if (wad.Contains(lumpName)) {
			return new DoomGraphic(wad.GetLump(lumpName)).ToRenderMap();
		}
		return null;
	}
	
}
// --------------------------------------------------------------
// ShowSliderValue.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ShowSliderValue : MonoBehaviour
{
	public void UpdateLabel (float value)
	{
		Text lbl = GetComponent<Text>();
		if (lbl != null)
			lbl.text = Mathf.RoundToInt (value * 100) + "%";
	}
}

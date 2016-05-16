// --------------------------------------------------------------
// ActiveStateToggler.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;

public class ActiveStateToggler : MonoBehaviour {

	public void ToggleActive () {
		gameObject.SetActive (!gameObject.activeSelf);
	}
}

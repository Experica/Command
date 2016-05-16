// --------------------------------------------------------------
// UIWindowBackground.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;

public class UIWindowBackground : MonoBehaviour 
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string fadeInTriggerName = "fadeIn";

    [SerializeField]
    private string fadeOutTriggerName = "fadeOut";

    public void FadeIn()
    {
        animator.SetTrigger(fadeInTriggerName);
    }

    public void FadeOut()
    {
        animator.SetTrigger(fadeOutTriggerName);
    }
}
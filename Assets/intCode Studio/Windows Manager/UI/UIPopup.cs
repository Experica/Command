// --------------------------------------------------------------
// UIPopup.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class UIPopup : MonoBehaviour
{
    private Action confirm;
    private Action cancel;

    [SerializeField]
    private UIWindowBackground background;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string showTriggerName = "show";

    [SerializeField]
    private string closeTriggerName = "close";

    [SerializeField]
    private Text tittle;

    [SerializeField]
    private Text description;

    [SerializeField]
    private Text confirmButtonText;

    [SerializeField]
    private Text cancelButtonText;

    private void Start()
    {
        SetPopupActive(false);
    }

    public void Setup(string tittleText, string descriptionText, string confirmText, string cancelText, Action confirm, Action cancel, bool hasCancelButton)
    {
        Show();

        tittle.text = tittleText;
        description.text = descriptionText;
        this.confirm = confirm;
        this.cancel = cancel;
        this.confirmButtonText.text = confirmText;
        this.cancelButtonText.text = cancelText;

        if (!hasCancelButton)
            cancelButtonText.transform.parent.gameObject.SetActive(false);
        else
            cancelButtonText.transform.parent.gameObject.SetActive(true);
    }

    private void Show()
    {
        SetPopupActive(true);
        animator.SetTrigger(showTriggerName);
        background.FadeIn();
    }

    public void Confirm()
    {
        if(confirm != null)
            confirm();

        Close();
    }

    public void Cancel()
    {
        if(cancel != null)
            cancel();

        Close();
    }

    private void Close()
    {
        animator.SetTrigger(closeTriggerName);
        background.FadeOut();
        StartCoroutine(Tools.GetMethodName(DeactivateWindow));
    }                               

    private IEnumerator DeactivateWindow()
    {
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        SetPopupActive(false);
    }

    private void SetPopupActive(bool active)
    {
        gameObject.SetActive(active);
        background.gameObject.SetActive(active);
    }
}
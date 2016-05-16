// --------------------------------------------------------------
// UIWindow.cs is part of the VLab project.
// Copyright (c) 2016 All Rights Reserved
// Li Alex Zhang fff008@gmail.com
// 5-9-2016
// --------------------------------------------------------------

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public abstract class UIWindow : MonoBehaviour, IWindow
{
    [SerializeField]
    private UIWindowBackground background;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string showTriggerName = "show";

    [SerializeField]
    private string closeTriggerName = "close";

    [SerializeField]
    private Text tittleText;

    [SerializeField]
    private string windowTittle;

    [SerializeField]
    private Button backButton;

    private void Start()
    {
        tittleText.text = windowTittle;

        if (!WindowsController.Instance.NavigationHistory)
            backButton.gameObject.SetActive(false);

        SetWindowActive(false);
    }

    public void Unhide()
    {
        Setup();
        SetWindowActive(true);
        animator.SetTrigger(showTriggerName);
        background.FadeIn();
    }

    public void Hide()
    {
        animator.SetTrigger(closeTriggerName);
        background.FadeOut();
        StartCoroutine(Tools.GetMethodName(DeactivateWindow));
    }

    public void Show()
    {
        Unhide();
        WindowsController.Instance.RegisterWindow(this);
    }

    public void Close()
    {
        WindowsController.Instance.DeleteNavigationHistory();
        Hide();
    }

    private IEnumerator DeactivateWindow()
    {
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        SetWindowActive(false);
    }

    private void SetWindowActive(bool active)
    {
        gameObject.SetActive(active);
        background.gameObject.SetActive(active);
    }

    public void Back()
    {
        WindowsController.Instance.ReturnToLastWindow();
    }

    protected abstract void Setup();
}
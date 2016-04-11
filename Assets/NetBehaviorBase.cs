using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System.Collections;

public class Timer : Stopwatch
{
    public double ElapsedSeconds
    {
        get { return Elapsed.TotalSeconds; }
    }

    public void ReStart()
    {
        Reset();
        Start();
    }

    public void Countdown(double duration)
    {
        if (!IsRunning)
        {
            Start();
        }
        var start = ElapsedSeconds;
        var end = ElapsedSeconds;
        while ((end - start) < duration)
        {
            end = ElapsedSeconds;
        }
    }
}


[NetworkSettings(channel = 0, sendInterval = 0)]
public class NetBehaviorBase : NetworkBehaviour
{
    [SyncVar(hook = "OnVisible")]
    public bool visible = true;
    [SyncVar(hook = "OnPosition")]
    public Vector3 position = new Vector3();
    [SyncVar(hook = "OnOri")]
    public float ori = 0;
    [SyncVar(hook = "OnLength")]
    public float length = 1;
    [SyncVar(hook = "OnWidth")]
    public float width = 1;
    [SyncVar(hook = "OnHeight")]
    public float height = 1;
    [SyncVar(hook = "OnColor")]
    public Color color = new Color();
    [SyncVar(hook ="OnBgColor")]
    public Color bgcolor = new Color();
    [SyncVar(hook ="OnTime")]
    public float time=-1000000;

    public Timer t = new Timer();
    public new Renderer renderer;

    void Awake()
    {
        renderer = gameObject.GetComponent<Renderer>();
    }

    public virtual void OnVisible(bool v)
    {
        renderer.enabled = v;
        visible = v;
    }

    public virtual void OnPosition(Vector3 p)
    {
        transform.position = p;
        position = p;
    }

    public virtual void OnOri(float o)
    {
        transform.eulerAngles = new Vector3(0, 0, o);
        ori = o;
    }

    public virtual void OnLength(float l)
    {
        transform.localScale = new Vector3(l, width, height);
        renderer.material.SetFloat("length", l);
        length = l;
    }

    public virtual void OnWidth(float w)
    {
        transform.localScale = new Vector3(length, w, height);
        renderer.material.SetFloat("width", w);
        width = w;
    }

    public virtual void OnHeight(float h)
    {
        transform.localScale = new Vector3(length, width, h);
        height = h;
    }

    public virtual void OnColor(Color c)
    {
        renderer.material.color = c;
        color = c;
    }

    public virtual void OnBgColor(Color c)
    {
        GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>().backgroundColor = c;
        bgcolor = c;
    }

    public virtual void OnTime(float time)
    {
        t.ReStart();
    }
}

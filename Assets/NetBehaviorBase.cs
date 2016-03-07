using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[NetworkSettings(channel = 0, sendInterval = 0)]
public class NetBehaviorBase : NetworkBehaviour
{
    [SyncVar(hook = "OnVisible")]
    public bool visible = true;
    [SyncVar(hook = "OnPosition")]
    public Vector3 position = new Vector3();
    [SyncVar(hook = "OnOri")]
    public float width = 1;
    [SyncVar(hook = "OnLength")]
    public float ori = 0;
    [SyncVar(hook = "OnWidth")]
    public float length = 1;
    [SyncVar(hook = "OnHeight")]
    public float height = 1;
    [SyncVar(hook = "OnColor")]
    public Color color = new Color();


    public virtual void OnVisible(bool v)
    {
        GetComponent<Renderer>().enabled = v;
        visible = v;
    }

    public virtual void OnPosition(Vector3 p)
    {
        transform.position = p;
        position = p;
    }

    public virtual void OnOri(float o)
    {
        Debug.Log(o);
        //ori = o;
        transform.Rotate(0, 0, o);
        ori = o;
    }

    public virtual void OnLength(float l)
    {
        transform.localScale = new Vector3(l, width, height);
        length = l;
    }

    public virtual void OnWidth(float w)
    {
        transform.localScale = new Vector3(length, w, height);
        width = w;
    }

    public virtual void OnHeight(float h)
    {
        transform.localScale = new Vector3(length, width, height);
        height = h;
    }

    public virtual void OnColor(Color c)
    {

    }

}

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Quad : NetBehaviorBase
{
    [SyncVar(hook ="OnMaskType")]
    public int masktype;

    public virtual void OnMaskType(int t)
    {
        GetComponent<Renderer>().material.SetInt("masktype", t);
        masktype = t;
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}
}

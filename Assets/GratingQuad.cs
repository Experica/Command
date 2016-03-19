using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GratingQuad : NetBehaviorBase
{
    [SyncVar(hook ="OnSpatialFreq")]
    public float spatialfreq;
    [SyncVar(hook ="OnTemporalFreq")]
    public float temporalfreq;
    [SyncVar(hook ="OnSpatialPhase")]
    public float spatialphase;
    [SyncVar(hook ="OnSigma")]
    public float sigma;
    [SyncVar(hook ="OnMinColor")]
    public Color mincolor;
    [SyncVar(hook ="OnMaxColor")]
    public Color maxcolor;
    [SyncVar(hook ="OnMaskType")]
    public int masktype;
    [SyncVar(hook ="OnIsDrifting")]
    public bool isdrifting=true;


    public virtual void OnSpatialFreq(float sf)
    {
        GetComponent<Renderer>().material.SetFloat("sf", sf);
        spatialfreq = sf;
    }

    public virtual void OnTemporalFreq(float tf)
    {
        GetComponent<Renderer>().material.SetFloat("tf", tf);
        temporalfreq = tf;
    }

    public virtual void OnSpatialPhase(float p)
    {
        GetComponent<Renderer>().material.SetFloat("phase", p);
        spatialphase = p;
    }

    public virtual void OnSigma(float s)
    {
        GetComponent<Renderer>().material.SetFloat("sigma", s);
        sigma = s;
    }

    public virtual void OnMinColor(Color c)
    {
        GetComponent<Renderer>().material.SetColor("mincolor", c);
        mincolor = c;
    }

    public virtual void OnMaxColor(Color c)
    {
        GetComponent<Renderer>().material.SetColor("maxcolor", c);
        maxcolor = c;
    }

    public virtual void OnMaskType(int t)
    {
        GetComponent<Renderer>().material.SetInt("masktype", t);
        masktype = t;
    }

    public virtual void OnIsDrifting(bool i)
    {
        if (i)
        {
            t.ReStart();
        }
        else
        {
            t.Stop();
        }
        isdrifting = i;
    }


    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (isdrifting)
        {
            GetComponent<Renderer>().material.SetFloat("t", (float)t.ElapsedSeconds);
        }
	}
}

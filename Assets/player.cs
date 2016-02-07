using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class player : NetworkBehaviour
{
    [SyncVar]
    float ori = 0;
    // Use this for initialization
    void Start()
    {

    }
    IDictionary getparam()
    {
        var d = new Dictionary<string, object>();
        d["test"] = "ok";
        return d;
    }
    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            var x = Input.GetAxis("Horizontal");
            var y = Input.GetAxis("Vertical");
            var p = Input.mousePosition;
            //p.z = transform.position.z;
            float r = System.Convert.ToSingle(Input.GetButton("Fire1"));
            float r1 = System.Convert.ToSingle(Input.GetButton("Fire2"));
            float r2 = System.Convert.ToSingle(Input.GetButton("Jump"));
            ori = ori + r - r1;
            transform.Rotate(0, 0, ori);
            //transform.Rotate(0, 0, r);
            //transform.Rotate(0, 0, -r1);
            transform.localScale = new Vector3(0.1f * x, 0.1f * y, 0) + transform.localScale;
            p.x = (p.x - Screen.width / 2) / (Screen.height / 2) * Camera.main.orthographicSize;
            p.y = (p.y - Screen.height / 2) / (Screen.height / 2) * Camera.main.orthographicSize;
            //transform.position = p;
            //p = Camera.main.ScreenToWorldPoint(p);
            p.z = transform.position.z;
            transform.position = p;

            //GetComponent<Renderer>().material.SetColor("Color",new Color(0, 0, r2,1));
            GetComponent<Renderer>().material.SetFloat("t", Time.timeSinceLevelLoad);
            GetComponent<Renderer>().material.SetFloat("ys", transform.localScale.y);
            GetComponent<Renderer>().material.SetFloat("sigma", 0.05f);

            //Debug.Log(Time.timeSinceLevelLoad);
            //Debug.Log(transform.localScale);
        }
    }
}
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class VLPlayer : MonoBehaviour {
    NetSyncBase player;

	// Use this for initialization
	void Start () {
        //player = GameObject.Find("Quad").GetComponent<Quad>().GetComponent<Quad>();
   }
	
	// Update is called once per frame
	void Update () {
        //player = GameObject.Find("Quad").GetComponent<Quad>().GetComponent<NetSyncBase>();
        //if (player)
        //{
        //    player.length += 0.1f * Input.GetAxis("Horizontal");
        //    player.width += 0.1f * Input.GetAxis("Vertical");
        //}
        //else { Debug.Log("null"); }
        var go = GameObject.Find("Quad");
        var cp = go.GetComponent<Quad>();
        var qs = cp.GetComponent<NetSyncBase>();
        if (qs)
        {
            float r = System.Convert.ToSingle(Input.GetButton("Fire1"));
            float r1 = System.Convert.ToSingle(Input.GetButton("Fire2"));
            qs.ori += r - r1;
            qs.length += 0.1f * Input.GetAxis("Horizontal");
            qs.width += 0.1f * Input.GetAxis("Vertical");
            var p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            p.z = qs.transform.position.z;
            qs.position = p;
        }
        //Debug.Log(qs.name);
    }
}

using UnityEngine;
using System.Collections;

public class exlogictest : ExperimentLogic {
    double ontime = 0;
	// Use this for initialization
	void Start () {
        experiment.trialdur = 0.05;
        experiment.sufITI = 0.05;
        ontime = experiment.timer.ElapsedSeconds;
        
        experiment.timer.Start();
	}
	
	// Update is called once per frame
	void Update () {
        var player = GameObject.Find("Player");
        if (player)
        {
            if (player.activeInHierarchy)
            {
                player.SetActive(false);
            }
        }
        var go = GameObject.Find("Quad");
        var cp = go.GetComponent<Quad>();
        var qs = cp.GetComponent<NetSyncBase>();


        if (!experiment.timer.IsRunning)
        {
            experiment.timer.Start();
        }
        if ((experiment.timer.ElapsedSeconds-ontime)>experiment.trialdur)
        {
            if(qs)
            {
                var p = qs.transform.position;
                p.x = (Random.value * 2 - 1) * 10;
                p.y = (Random.value * 2 - 1) * 10;
                qs.position = p;
                ontime = experiment.timer.ElapsedSeconds;
            }
        }
	}
}

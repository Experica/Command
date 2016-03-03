using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class VLNet : NetworkManager {
    public ExperimentLogic exlogic;
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        exlogic.OnSceneChange(sceneName);
    }
    // Use this for initialization
    void Start () {
        exlogic = GameObject.FindObjectOfType<ExperimentLogic>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

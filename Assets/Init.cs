using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Init : MonoBehaviour {

    private float screenhalfheight, screentoeye;
	// Use this for initialization
	void Start () {
        screenhalfheight = 15;
        screentoeye = 57;
        Camera.main.orthographicSize = Mathf.Rad2Deg * Mathf.Atan2(screenhalfheight, screentoeye);
        //Cursor.visible = false;
        //NetworkServer.Listen(25001);
        //Network.InitializeServer(32, 25001, false);
        //MasterServer.RegisterHost("alex", "VLab", "");
	}
    
    void OnServerInitialized()
 {
     Debug.Log("Server is initialized");
 }
   
// Update is called once per frame
void Update () {
	
	}
}

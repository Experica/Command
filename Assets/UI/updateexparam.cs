using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class updateexparam : MonoBehaviour {

    public GameObject paramlable;

    public void updateparam(Experiment ex)
    {
        addparam("condrepeat", ex.condrepeat);
    }

    void addparam(string name, object value)
    {
        var ui = Instantiate(paramlable);
        var layout = GetComponent<GridLayoutGroup>();
        ui.transform.SetParent(transform);
    }
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

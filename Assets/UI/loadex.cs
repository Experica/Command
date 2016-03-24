using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class loadex : MonoBehaviour {

    GameObject exlogic;
    void Awake()
    {
        exlogic = GameObject.Find("ExperimentLogic");
    }

	public void loadexdef()
    {
        var exname = gameObject.GetComponent<Dropdown>().captionText.text;
        //Debug.Log(exname);

        var ex=VLIO.ReadYaml<Experiment>("Experiment/"+exname + ".yaml");
        //Debug.Log(ex);
        Updateex(ex);
    }

    void Updateex(Experiment ex)
    {
        ex.condtest = null;
        var logic=exlogic.GetComponent<ExperimentLogic>();
        logic.ex = ex;

        var exparam = GameObject.Find("exparamContent").GetComponent<updateexparam>();
        exparam.updateparam(ex);

    }

    void Start()
    {
        loadexdef();
    }
}

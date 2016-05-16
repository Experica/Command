using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using VLab;

public class ConditionPanel : MonoBehaviour {

    public VLUIController uimanager;
    public Dropdown samplemethod;
    public GameObject condcontent,inputprefab;

    public void OnConditionPanel(bool ison)
    {
        if (ison)
        {
            uimanager.exmanager.el.PrepareCondition();
            CreateConditionUI();
        }
        else
        {
            DestroyConditionUI();
        }
        if(samplemethod.options.Count==0)
        {
            samplemethod.AddOptions(Enum.GetNames(typeof(SampleMethod)).ToList());
        }
    }
	
    void CreateConditionUI()
    {
        var cond = uimanager.exmanager.el.condmanager.cond;
        var grid = condcontent.GetComponent<GridLayoutGroup>();
        var fn = cond.Keys.Count;
        if (fn > 0)
        {
            var rn = cond.First().Value.Count + 1;
            grid.constraintCount = rn;
            var rt = (RectTransform)condcontent.transform;
            rt.sizeDelta = new Vector2((grid.cellSize.x + grid.spacing.x) * fn, (grid.cellSize.y + grid.spacing.y) * rn);
        }
        foreach (var f in cond.Keys)
        {
            AddCondFactorLevels(f, cond[f]);
        }
    }

    void AddCondFactorLevels(string name, List<object> value)
    {
        var inputname = Instantiate(inputprefab);
        inputname.name = name;
        var inif = inputname.GetComponent<InputField>();
        inif.text = name;

        inputname.transform.SetParent(condcontent.transform);
        inputname.transform.localScale = new Vector3(1, 1, 1);

        for (var i=0;i<value.Count;i++)
        {
            var inputvalue = Instantiate(inputprefab);
            inputvalue.name = name+"_"+i;
            var ivif = inputvalue.GetComponent<InputField>();
            ivif.text = (string)VLConvert.Convert(value[i], typeof(string));

            inputvalue.transform.SetParent(condcontent.transform);
            inputvalue.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    void DestroyConditionUI()
    {
        for(var i=0;i<condcontent.transform.childCount;i++)
        {
            Destroy(condcontent.transform.GetChild(i).gameObject);
        }
    }

    public void OnSampleMethod(int v)
    {
        uimanager.exmanager.el.condmanager.samplemethod = (SampleMethod)Enum.Parse(typeof(SampleMethod), samplemethod.captionText.text);
    }

}

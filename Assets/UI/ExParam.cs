using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class ExParam : MonoBehaviour
{
    public GameObject input, text;
    public Experiment experiment;
    public Dictionary<string, PropertyInfo> exp;

    public void UpdateParam(Experiment ex)
    {
        experiment = ex;
        exp = exparam(experiment);
        foreach (Transform t in transform)
        {
            Destroy(t.gameObject);
        }

        AddParam("name", experiment.name);
        AddParam("id", experiment.id);
        AddParam("experimenter", experiment.experimenter);

        AddParam("subject_id", experiment.subject.id);
        AddParam("subject_gender", experiment.subject.gender);
        AddParam("subject_age", experiment.subject.age);
        AddParam("subject_size", experiment.subject.size);
        AddParam("subject_weight", experiment.subject.weight);

        AddParam("recordsession", experiment.recordsession);
        AddParam("recordsite", experiment.recordsite);

        AddParam("condrepeat", experiment.condrepeat);
        AddParam("preICI", experiment.preICI);
        AddParam("conddur", experiment.conddur);
        AddParam("sufICI", experiment.sufICI);
        AddParam("preITI", experiment.preITI);
        AddParam("trialdur", experiment.trialdur);
        AddParam("sufITI", experiment.sufITI);
        AddParam("preIBI", experiment.preIBI);
        AddParam("blockdur", experiment.blockdur);
        AddParam("sufIBI", experiment.sufIBI);
        

        var layout = GetComponent<GridLayoutGroup>();
        var rt = (RectTransform)transform;
        var y = Mathf.Max(rt.rect.height, 20 * (layout.cellSize.y + layout.spacing.y));
        rt.sizeDelta = new Vector2(rt.rect.width, y);
    }

    public Dictionary<string, PropertyInfo> exparam(Experiment ex)
    {
        var exp = new Dictionary<string, PropertyInfo>();
        foreach (var p in ex.GetType().GetProperties())
        {
            exp[p.Name] = p;
        }
        return exp;
    }

    public void setexp(string name, object value)
    {
        if (exp.ContainsKey(name))
        {
            exp[name].SetValue(experiment, value, null);
        }
    }

    public void setvalue(InputField value)
    {
        Debug.Log(value.name);
        Debug.Log(experiment.recordsession);
        setexp(value.name, value.text);
        Debug.Log(experiment.recordsession);
    }

    void AddParam(string name, object value)
    {
        if(value==null)
        {
            value = "";
        }
        var t = Instantiate(text);
        t.name = name + "_Text";
        t.GetComponent<Text>().text = name;

        var i = Instantiate(input);
        i.name = name;
        foreach (var itc in i.GetComponentsInChildren<Text>())
        {
            itc.text = value.ToString();
        }
        var iif = i.GetComponent<InputField>();
        iif.onEndEdit.AddListener(delegate { setvalue(iif); });

        t.transform.SetParent(transform);
        i.transform.SetParent(transform);
        t.transform.localScale = new Vector3(1, 1, 1);
        i.transform.localScale = new Vector3(1, 1, 1);
    }
}

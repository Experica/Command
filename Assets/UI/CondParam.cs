using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Windows.Forms;
using System.IO;

public class CondParam : MonoBehaviour {

    public ExperimentLogic logic;
    public InputField input;

    void Awake()
    {
        input = GetComponent<InputField>();
    }

    public void SetValue(InputField value)
    {
        logic.ex.condpath = value.text;
        logic.condmanager.ReadCondition(value.text);
    }

    public void UpdateParam (ExperimentLogic l)
    {
        logic = l;
        //foreach (var itc in GetComponentsInChildren<Text>())
        //{
        //    itc.text = logic.ex.condpath;
        //}
        input.text = logic.ex.condpath;
        input.onEndEdit.AddListener(delegate { SetValue(input); });
        SetValue(input);
    }

    public void OpenFile()
    {
        OpenFileDialog filedialog = new OpenFileDialog();
        filedialog.Title = "Choose Condition File";
        filedialog.InitialDirectory = Directory.GetCurrentDirectory();
        filedialog.Filter = "Condition (*.yaml)|*.yaml|All Files (*.*)|*.*";
        if (filedialog.ShowDialog() == DialogResult.OK)
        {
            input.text = filedialog.FileName;
        }
        SetValue(input);
    }
}

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using IceInternal;
using UnityEngine.VFX;
using System.Linq;
using Experica.NetEnv;
using UnityEngine.Rendering;
using Unity.Properties;
using Unity.Collections;

namespace Experica.Command
{
    public class UI : MonoBehaviour
    {
        public UIController uicontroller;
        public UIDocument uidoc;
        public VisualTreeAsset ExtendButton, ToggleString, ToggleEnum, ToggleBool, ToggleInteger, ToggleUInteger, ToggleFloat, ToggleDouble, ToggleVector2,
            ToggleVector3, ToggleVector4, viewport;

        VisualElement root, controlpanel, experimentpanel, environmentpanel, viewpanel, consolepanel, condpanel, condtestpanel, viewcontent;
        public Toggle server, host, start, pause, startsession;
        public Button newex, saveex, deleteex, addexextendparam;
        public DropdownField experimentlist, experimentsessionlist;
        ScrollView excontent, envcontent;


        void OnEnable()
        {
            root = uidoc.rootVisualElement;
            // Control Panel
            controlpanel = root.Q("ControlPanel");
            server = controlpanel.Q<Toggle>("Server");
            host = controlpanel.Q<Toggle>("Host");
            start = controlpanel.Q<Toggle>("Start");
            pause = controlpanel.Q<Toggle>("Pause");
            startsession = controlpanel.Q<Toggle>("StartSession");
            experimentlist = controlpanel.Q<DropdownField>("ExperimentList");
            experimentsessionlist = controlpanel.Q<DropdownField>("ExperimentSessionList");
            newex = controlpanel.Q<Button>("New");
            saveex = controlpanel.Q<Button>("Save");
            deleteex = controlpanel.Q<Button>("Delete");

            experimentlist.RegisterValueChangedCallback(e => uicontroller.OnExChoiceChanged(e.newValue));
            experimentsessionlist.RegisterValueChangedCallback(e => uicontroller.OnExSessionChoiceChanged(e.newValue));
            server.RegisterValueChangedCallback(e => uicontroller.ToggleServer(e.newValue));
            host.RegisterValueChangedCallback(e => uicontroller.ToggleHost(e.newValue));
            start.RegisterValueChangedCallback(e => uicontroller.exmanager.el?.StartStopExperiment(e.newValue));
            pause.RegisterValueChangedCallback(e => uicontroller.exmanager.el?.PauseResumeExperiment(e.newValue));
            start.SetEnabled(false);
            pause.SetEnabled(false);
            saveex.RegisterCallback<ClickEvent>(e => uicontroller.exmanager.SaveEx(experimentlist.value));
            deleteex.RegisterCallback<ClickEvent>(e =>
            {
                if (uicontroller.exmanager.DeleteEx(experimentlist.value))
                { UpdateExperimentList(uicontroller.exmanager.deffile.Keys.ToList(), uicontroller.config.FirstTestID); }
            });
            // Experiment Panel
            experimentpanel = root.Q("ExperimentPanel");
            excontent = experimentpanel.Q<ScrollView>("ExperimentContent");
            addexextendparam = experimentpanel.Q<Button>("AddExtendParam");
            addexextendparam.RegisterCallback<ClickEvent>(e => AddExExtendParamUI());
            // Environment Panel
            environmentpanel = root.Q("EnvironmentPanel");
            envcontent = environmentpanel.Q<ScrollView>("EnvironmentContent");
            // View Panel
            viewpanel = root.Q("ViewPanel");
            viewcontent = viewpanel.Q("ViewContent");
            // Console Panel
            consolepanel = root.Q("ConsolePanel");
            // Condition Panel
            condpanel = root.Q("ConditionPanel");
            // ConditionTest Panel
            condtestpanel = root.Q("ConditionTestPanel");
        }

        public void UpdateExperimentList(List<string> list, string first = null)
        {
            if (list == null || list.Count == 0) { return; }
            list.Sort();
            if (first != null && list.Contains(first))
            {
                var i = list.IndexOf(first);
                list.RemoveAt(i);
                list.Insert(0, first);
            }
            experimentlist.choices = list;
            experimentlist.index = Mathf.Clamp(experimentlist.index, 0, list.Count - 1);
        }

        public void UpdateExperimentSessionList(List<string> list)
        {
            if (list == null || list.Count == 0) { return; }
            list.Sort();
            experimentsessionlist.choices = list;
            experimentsessionlist.index = Mathf.Clamp(experimentsessionlist.index, 0, list.Count - 1);
        }

        public void UpdateEx(Experiment ex)
        {
            var currentui = excontent.Query<TemplateContainer>().ToList();
            var currentuiname = currentui.Select(i => i.name).ToList();
            var currentproperty = ex.Properties.Keys.Except(uicontroller.config.ExHideParams).ToArray();
            var updateui = currentuiname.Intersect(currentproperty);
            var removeui = currentuiname.Except(currentproperty);
            var addui = currentproperty.Except(currentuiname);

            if (updateui.Count() > 0)
            {
                foreach (var p in updateui)
                {
                    var ui = currentui[currentuiname.IndexOf(p)];
                    var nametoggle = ui.Q<Toggle>("Name");
                    nametoggle.SetValueWithoutNotify(ex.InheritParam.Contains(p));
                    var vi = ui.Q("Value");
                    var vs = ex.Properties[p];
                    vi.ClearBinding("value");
                    vi.SetBinding("value", new DataBinding
                    {
                        dataSource = vs,
                        dataSourcePath = new PropertyPath("Value"),
                    });
                    vs.NotifyValue();
                }
            }
            if (removeui.Count() > 0)
            {
                foreach (var p in removeui)
                {
                    excontent.Remove(currentui[currentuiname.IndexOf(p)]);
                }
            }
            if (addui.Count() > 0)
            {
                foreach (var p in addui)
                {
                    AddParamUI(p, p, ex.Properties[p], ex.InheritParam.Contains(p), uicontroller.ToggleExInherit, excontent);
                }
            }

            foreach (var p in ex.ExtendProperties.Keys.Except(uicontroller.config.ExHideParams).ToArray())
            {
                AddParamUI(p, p, ex.ExtendProperties[p], ex.InheritParam.Contains(p), uicontroller.ToggleExInherit, excontent, true);
            }

        }

        void AddParamUI<T>(string id, string name, IDataSource<T> source, bool isinherit, Action<string, bool> inherithandler, VisualElement parent, bool isextendparam = false)
        {
            AddParamUI(id, name, source.Type, source.Value, isinherit, inherithandler, parent, source, "Value", isextendparam);
        }

        void AddParamUI(string id, string name, Type T, object value, bool isinherit, Action<string, bool> inherithandler, VisualElement parent, object datasource = null, string datapath = "Value", bool isextendparam = false)
        {
            TemplateContainer ui;
            VisualElement input;

            if (T.IsEnum)
            {
                ui = ToggleEnum.Instantiate();
                ui.name = id;

                var vi = ui.Q<EnumField>("Value");
                vi.Init((Enum)value);
                input = vi;
            }
            else if (T == typeof(bool))
            {
                ui = ToggleBool.Instantiate();
                ui.name = id;

                var vi = ui.Q<Toggle>("Value");
                vi.value = (bool)value;
                vi.label = vi.value ? "True" : "False";
                vi.RegisterValueChangedCallback(e => vi.label = e.newValue ? "True" : "False");
                input = vi;
            }
            else if (T == typeof(int))
            {
                ui = ToggleInteger.Instantiate();
                ui.name = id;

                var vi = ui.Q<IntegerField>("Value");
                vi.value = (int)value;
                input = vi;
            }
            else if (T == typeof(uint))
            {
                ui = ToggleUInteger.Instantiate();
                ui.name = id;

                var vi = ui.Q<UnsignedIntegerField>("Value");
                vi.value = (uint)value;
                input = vi;
            }
            else if (T == typeof(float))
            {
                ui = ToggleFloat.Instantiate();
                ui.name = id;

                var vi = ui.Q<FloatField>("Value");
                vi.value = (float)value;
                input = vi;
            }
            else if (T == typeof(double))
            {
                ui = ToggleDouble.Instantiate();
                ui.name = id;

                var vi = ui.Q<DoubleField>("Value");
                vi.value = (double)value;
                input = vi;
            }
            else if (T == typeof(Vector2))
            {
                ui = ToggleVector2.Instantiate();
                ui.name = id;

                var vi = ui.Q<Vector2Field>("Value");
                vi.value = (Vector2)value;
                input = vi;
            }
            else if (T == typeof(Vector3))
            {
                ui = ToggleVector3.Instantiate();
                ui.name = id;

                var vi = ui.Q<Vector3Field>("Value");
                vi.value = (Vector3)value;
                input = vi;
            }
            else if (T == typeof(Vector4))
            {
                ui = ToggleVector4.Instantiate();
                ui.name = id;

                var vi = ui.Q<Vector4Field>("Value");
                vi.value = (Vector4)value;
                input = vi;
            }
            else if (T == typeof(Color))
            {
                ui = ToggleVector4.Instantiate();
                ui.name = id;

                var vi = ui.Q<Vector4Field>("Value");
                vi.value = (Color)value;
                input = vi;
            }
            else
            {
                ui = ToggleString.Instantiate();
                ui.name = id;

                var vi = ui.Q<TextField>("Value");
                vi.value = value.Convert<string>(T);
                input = vi;
            }

            var nametoggle = ui.Q<Toggle>("Name");
            nametoggle.label = name;
            nametoggle.value = isinherit;
            nametoggle.RegisterValueChangedCallback(e => inherithandler(id, e.newValue));

            if (datasource != null)
            {
                var binding = new DataBinding
                {
                    dataSource = datasource,
                    dataSourcePath = new PropertyPath(datapath),
                };
                if (T == typeof(Color))
                {
                    binding.sourceToUiConverters.AddConverter((ref object s) => { var c = (Color)s; return new Vector4(c.r, c.g, c.b, c.a); });
                    binding.uiToSourceConverters.AddConverter((ref Vector4 v) => (object)new Color(v.x, v.y, v.z, v.w));
                }
                else if (T == typeof(FixedString512Bytes))
                {
                    binding.sourceToUiConverters.AddConverter((ref object s) => s.ToString());
                    binding.uiToSourceConverters.AddConverter((ref string v) => (object)new FixedString512Bytes(v));
                }
                input.SetBinding("value", binding);
            }
            if (isextendparam)
            {
                var deletebutton = ExtendButton.Instantiate().Q<Button>("Delete");
                deletebutton.RegisterCallback<ClickEvent>(e => DeleteExExtendParam(ui));
                ui.Q("Root").Insert(0, deletebutton);
            }
            parent.Add(ui);
        }

        void AddExExtendParamUI()
        {
            Debug.Log("add ExExtendParam");
        }

        void DeleteExExtendParam(VisualElement ui)
        {
            var name = ui.name;
            Debug.Log($"delete ExExtendParam: {name}");
            //excontent.Remove(ui);
            //uicontroller.exmanager.el.ex.RemoveExtendProperty(name);
        }


        public void UpdateEnv()
        {
            envcontent.Clear();
            var envps = uicontroller.exmanager.el.envmanager.GetParamSources(active: !uicontroller.config.IsShowInactiveEnvParam);
            //var envps = uicontroller.exmanager.el.envmanager.GetParamSources(!uicontroller.config.IsShowEnvParamFullName, !uicontroller.config.IsShowInactiveEnvParam);
            foreach (var name in envps.Keys)
            {
                AddParamUI(name, name.FirstSplitHead(), envps[name], uicontroller.exmanager.el.ex.EnvInheritParam.Contains(name), uicontroller.ToggleEnvInherit, envcontent);
            }
        }

        public void UpdateView()
        {
            var currentui = viewcontent.Query<TemplateContainer>().ToList();
            var currentuiname = currentui.Select(i => i.name).ToList();
            var currentcamera = uicontroller.exmanager.el.envmanager.MainCamera;
            var currentcameraname = currentcamera.Select(i => i.ClientID.ToString()).ToList();
            var updateui = currentuiname.Intersect(currentcameraname);
            var removeui = currentuiname.Except(currentcameraname);
            var addui = currentcameraname.Except(currentuiname);
            var total = updateui.Count() + addui.Count();

            if (updateui.Count() > 0)
            {
                foreach (var p in updateui)
                {
                    var ui = currentui[currentuiname.IndexOf(p)];
                    var vp = ui.Q<Image>("Viewport");
                    var camera = currentcamera[currentcameraname.IndexOf(p)];
                    var rt = GetRenderTexture(viewcontent.layout.size, camera.Aspect, (RenderTexture)vp.image);
                    vp.style.height = rt.height;
                    vp.style.width = rt.width;
                    camera.Camera.targetTexture = rt;
                }
            }
            if (removeui.Count() > 0)
            {
                foreach (var p in removeui)
                {
                    viewcontent.Remove(currentui[currentuiname.IndexOf(p)]);
                }
            }
            if (addui.Count() > 0)
            {
                foreach (var p in addui)
                {
                    AddCameraUI(currentcamera[currentcameraname.IndexOf(p)]);
                }
            }
        }

        void AddCameraUI(INetEnvCamera camera)
        {
            var ui = viewport.Instantiate();
            ui.name = camera.ClientID.ToString();
            var vp = ui.Q<Image>("Viewport");
            var rt = GetRenderTexture(viewcontent.layout.size, camera.Aspect);
            vp.image = rt;
            vp.style.height = rt.height;
            vp.style.width = rt.width;
            camera.Camera.targetTexture = rt;
            camera.OnCameraChange += _ => UpdateView();
            viewcontent.Add(ui);
        }

        RenderTexture GetRenderTexture(Vector2 size, float aspect, RenderTexture rt = null)
        {
            float width, height;
            if (size.x / size.y >= aspect)
            {
                width = size.y * aspect;
                height = size.y;
            }
            else
            {
                width = size.x;
                height = size.x / aspect;
            }
            if (rt == null)
            {
                return new RenderTexture(
                new RenderTextureDescriptor()
                {
                    dimension = TextureDimension.Tex2D,
                    depthBufferBits = 32,
                    autoGenerateMips = false,
                    msaaSamples = uicontroller.config.AntiAliasing,
                    colorFormat = RenderTextureFormat.ARGBHalf,
                    sRGB = false,
                    width = Mathf.Max(1, Mathf.FloorToInt(width)),
                    height = Mathf.Max(1, Mathf.FloorToInt(height)),
                    volumeDepth = 1
                })
                {
                    anisoLevel = uicontroller.config.AnisotropicFilterLevel
                };
            }
            else
            {
                rt.Release();
                rt.width = Mathf.Max(1, Mathf.FloorToInt(width));
                rt.height = Mathf.Max(1, Mathf.FloorToInt(height));
                return rt;
            }
        }

        void OnDisable()
        {

        }

    }
}
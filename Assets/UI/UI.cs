/*
UI.cs is part of the Experica.
Copyright (c) 2016 Li Alex Zhang and Contributors

Permission is hereby granted, free of charge, to any person obtaining a 
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
            ToggleVector3, ToggleVector4, viewport, ParamsFoldout;

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
            excontent = experimentpanel.Q<ScrollView>("Content");
            addexextendparam = experimentpanel.Q<Button>("AddExtendParam");
            addexextendparam.RegisterCallback<ClickEvent>(e => AddExExtendParamUI());
            // Environment Panel
            environmentpanel = root.Q("EnvironmentPanel");
            envcontent = environmentpanel.Q<ScrollView>("Content");
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
            excontent.Clear();
            var previousui = excontent.Children().ToList();
            var previousuiname = previousui.Select(i => i.name).ToList();
            // since ExtendParam is a param container and we always show them, so here we do not AddParamUI for the container itself, but add its content
            var currentpropertyname = ex.Properties.Keys.Except(uicontroller.config.ExHideParams).Where(i => i != "ExtendParam").ToArray();
            var ui2update = previousuiname.Intersect(currentpropertyname);
            var ui2remove = previousuiname.Except(currentpropertyname);
            var ui2add = currentpropertyname.Except(previousuiname);

            if (ui2update.Count() > 0)
            {
                foreach (var p in ui2update)
                {
                    var ui = previousui[previousuiname.IndexOf(p)];
                    var nametoggle = ui.Q<Toggle>("Name");
                    nametoggle.SetValueWithoutNotify(ex.InheritParam.Contains(p));
                    var vi = ui.Q("Value");
                    var ds = ex.Properties[p];
                    var db = vi.GetBinding("value") as DataBinding;
                    db.dataSource = ds;
                    ds.NotifyValue();
                }
            }
            if (ui2remove.Count() > 0)
            {
                foreach (var p in ui2remove)
                {
                    excontent.Remove(previousui[previousuiname.IndexOf(p)]);
                }
            }
            if (ui2add.Count() > 0)
            {
                foreach (var p in ui2add)
                {
                    AddParamUI(p, p, ex.Properties[p], ex.InheritParam.Contains(p), uicontroller.ToggleExInherit, excontent);
                }
            }

            foreach (var p in ex.ExtendProperties.Keys.Except(uicontroller.config.ExHideParams).ToArray())
            {
                AddParamUI(p, p, ex.ExtendProperties[p], ex.InheritParam.Contains(p), uicontroller.ToggleExInherit, excontent, true);
            }
            excontent.scrollOffset = excontent.contentRect.size;
        }

        void AddParamUI<T>(string id, string name, IDataSource<T> source, bool isinherit, Action<string, bool> inherithandler, VisualElement parent, bool isextendparam = false)
        {
            AddParamUI(id, name, source.Type, source.Value, isinherit, inherithandler, parent, source, "Value", isextendparam);
        }

        void AddParamUI(string id, string name, Type T, object value, bool isinherit, Action<string, bool> inherithandler, VisualElement parent, object datasource = null, string datapath = "Value", bool isextendparam = false)
        {
            VisualElement ui, valueinput;

            if (T.IsEnum)
            {
                ui = ToggleEnum.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<EnumField>("Value");
                vi.Init((Enum)value);
                valueinput = vi;
            }
            else if (T == typeof(bool))
            {
                ui = ToggleBool.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Toggle>("Value");
                vi.value = (bool)value;
                vi.label = vi.value ? "True" : "False";
                vi.RegisterValueChangedCallback(e => vi.label = e.newValue ? "True" : "False");
                valueinput = vi;
            }
            else if (T == typeof(int))
            {
                ui = ToggleInteger.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<IntegerField>("Value");
                vi.value = (int)value;
                valueinput = vi;
            }
            else if (T == typeof(uint))
            {
                ui = ToggleUInteger.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<UnsignedIntegerField>("Value");
                vi.value = (uint)value;
                valueinput = vi;
            }
            else if (T == typeof(float))
            {
                ui = ToggleFloat.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<FloatField>("Value");
                vi.value = (float)value;
                valueinput = vi;
            }
            else if (T == typeof(double))
            {
                ui = ToggleDouble.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<DoubleField>("Value");
                vi.value = (double)value;
                valueinput = vi;
            }
            else if (T == typeof(Vector2))
            {
                ui = ToggleVector2.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Vector2Field>("Value");
                vi.value = (Vector2)value;
                valueinput = vi;
            }
            else if (T == typeof(Vector3))
            {
                ui = ToggleVector3.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Vector3Field>("Value");
                vi.value = (Vector3)value;
                valueinput = vi;
            }
            else if (T == typeof(Vector4))
            {
                ui = ToggleVector4.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Vector4Field>("Value");
                vi.value = (Vector4)value;
                valueinput = vi;
            }
            else if (T == typeof(Color))
            {
                ui = ToggleVector4.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Vector4Field>("Value");
                vi.value = (Color)value;
                valueinput = vi;
            }
            else
            {
                ui = ToggleString.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<TextField>("Value");
                vi.value = value.Convert<string>(T);
                valueinput = vi;
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
                valueinput.SetBinding("value", binding);
            }
            if (isextendparam)
            {
                var deletebutton = ExtendButton.Instantiate().Q<Button>("Delete");
                deletebutton.RegisterCallback<ClickEvent>(e => DeleteExExtendParam(ui));
                ui.Insert(0, deletebutton);
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


        void AddParamsFoldoutUI(string[] ids, string[] names, IDataSource<object>[] sources, bool[] inherits, Action<string, bool> inherithandler, VisualElement parent, string groupname, bool isextendparam = false)
        {
            var foldout = ParamsFoldout.Instantiate().Q<Foldout>();
            foldout.name = groupname;
            foldout.text = groupname;

            for (int i = 0; i < ids.Length; i++)
            {
                AddParamUI(ids[i], names[i], sources[i], inherits[i], inherithandler, foldout, isextendparam);
            }
            parent.Add(foldout);
        }

        public void UpdateEnv()
        {
            envcontent.Clear();
            var el = uicontroller.exmanager.el;


            //var envps = uicontroller.exmanager.el.envmanager.GetParamSources(active: !uicontroller.config.IsShowInactiveEnvParam);
            ////var envps = uicontroller.exmanager.el.envmanager.GetParamSources(!uicontroller.config.IsShowEnvParamFullName, !uicontroller.config.IsShowInactiveEnvParam);
            //foreach (var name in envps.Keys)
            //{
            //    AddParamUI(name, name.FirstSplitHead(), envps[name], uicontroller.exmanager.el.ex.EnvInheritParam.Contains(name), uicontroller.ToggleEnvInherit, envcontent);
            //}



            var gonames = el.envmanager.GetGameObjectFullNames(!uicontroller.config.IsShowInactiveEnvParam);
            foreach (var goname in gonames)
            {
                var gonvs = el.envmanager.GetParamSourcesByGameObject(goname);
                var nvfullnames = gonvs.Keys.ToArray();
                var nvnames = nvfullnames.Select(i => i.FirstSplitHead()).ToArray();
                var nvsources = gonvs.Values.ToArray();
                var inherits = nvfullnames.Select(i => el.ex.EnvInheritParam.Contains(i)).ToArray();
                AddParamsFoldoutUI(nvfullnames, nvnames, nvsources, inherits, uicontroller.ToggleEnvInherit, envcontent, goname);
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
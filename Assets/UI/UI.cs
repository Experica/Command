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
using System.Security.AccessControl;

namespace Experica.Command
{
    public class UI : MonoBehaviour
    {
        public AppManager appmgr;
        public UIDocument uidoc;
        public VisualTreeAsset ToggleString, ToggleEnum, ToggleBool, ToggleInteger, ToggleUInteger, ToggleFloat, ToggleDouble, ToggleVector2, ToggleVector3, ToggleVector4,
            ParamString, ParamEnum, ParamBool, ParamInteger, ParamUInteger, ParamFloat, ParamDouble, ParamVector2, ParamVector3, ParamVector4,
            ExtendButton, viewport, ParamsFoldout, AboutWindow, ConfigWindow, AddExParamWindow, NewExWindow;

        VisualElement root, mainmenu, maincontent, controlpanel, experimentpanel, environmentpanel, viewpanel, consolepanel, condpanel, condtestpanel, viewcontent;
        public Toggle server, host, start, pause, startsession;
        public Button newex, saveex, deleteex, addexextendparam;
        public DropdownField experimentlist, experimentsessionlist;
        ScrollView excontent, envcontent;


        void OnEnable()
        {
            root = uidoc.rootVisualElement;
            mainmenu = root.Q("MainMenu");
            maincontent = root.Q("MainContent");
            // Main Menu
            mainmenu.Q<Button>("About").RegisterCallback<ClickEvent>(e => OnAboutWindow(root));
            mainmenu.Q<Button>("Config").RegisterCallback<ClickEvent>(e => OnConfigWindow(root));
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

            experimentlist.RegisterValueChangedCallback(e => appmgr.OnExChoiceChanged(e.newValue));
            experimentsessionlist.RegisterValueChangedCallback(e => appmgr.OnExSessionChoiceChanged(e.newValue));
            server.RegisterValueChangedCallback(e => appmgr.ToggleServer(e.newValue));
            host.RegisterValueChangedCallback(e => appmgr.ToggleHost(e.newValue));
            start.RegisterValueChangedCallback(e => appmgr.exmgr.el?.StartStopExperiment(e.newValue));
            pause.RegisterValueChangedCallback(e => appmgr.exmgr.el?.PauseResumeExperiment(e.newValue));
            start.SetEnabled(false);
            pause.SetEnabled(false);
            newex.RegisterCallback<ClickEvent>(e => OnNewExWindow(controlpanel));
            saveex.RegisterCallback<ClickEvent>(e => appmgr.exmgr.SaveEx(experimentlist.value));
            deleteex.RegisterCallback<ClickEvent>(e =>
            {
                if (appmgr.exmgr.DeleteEx(experimentlist.value))
                { UpdateExperimentList(appmgr.exmgr.deffile.Keys.ToList(), appmgr.cfgmgr.config.FirstTestID); }
            });
            // Experiment Panel
            experimentpanel = root.Q("ExperimentPanel");
            excontent = experimentpanel.Q<ScrollView>("Content");
            experimentpanel.Q<Button>("AddParam").RegisterCallback<ClickEvent>(e => OnAddExParamWindow(experimentpanel));
            // Environment Panel
            environmentpanel = root.Q("EnvironmentPanel");
            envcontent = environmentpanel.Q<ScrollView>("Content");
            // View Panel
            viewpanel = root.Q("ViewPanel");
            viewcontent = viewpanel.Q("Content");
            // Console Panel
            consolepanel = root.Q("ConsolePanel");
            // Condition Panel
            condpanel = root.Q("ConditionPanel");
            // ConditionTest Panel
            condtestpanel = root.Q("ConditionTestPanel");
        }

        void OnAboutWindow(VisualElement parent)
        {
            var w = AboutWindow.Instantiate()[0];
            w.Q<Label>("CommandVersion").text = Application.version;
            w.Q<Label>("UnityVersion").text = Application.unityVersion;
            w.Q<Button>("Close").RegisterCallback<ClickEvent>(e => parent.Remove(w));

            w.style.position = Position.Absolute;
            w.style.top = Length.Percent(33);
            w.style.left = Length.Percent(33);
            w.style.width = Length.Percent(33);
            w.style.height = Length.Percent(33);
            parent.Add(w);
        }

        void OnConfigWindow(VisualElement parent)
        {
            var w = ConfigWindow.Instantiate()[0];
            w.Q<Button>("Close").RegisterCallback<ClickEvent>(e => parent.Remove(w));
            var cfgcontent = w.Q<ScrollView>("Content");
            UpdateConfig(appmgr.cfgmgr.config, cfgcontent);

            w.style.position = Position.Absolute;
            w.style.top = Length.Percent(20);
            w.style.left = Length.Percent(25);
            w.style.width = Length.Percent(50);
            w.style.height = Length.Percent(60);
            parent.Add(w);
        }

        void OnNewExWindow(VisualElement parent)
        {
            var w = NewExWindow.Instantiate()[0];
            w.Q<Button>("Close").RegisterCallback<ClickEvent>(e => parent.Remove(w));

            var namefield = w.Q<TextField>("Name");
            var copylist = w.Q<DropdownField>("Copy");
            var exlist = appmgr.exmgr.deffile.Keys.ToList();
            exlist.Insert(0, ""); // add "empty" option for no copy
            UpdateDropdown(copylist, exlist);
            var errorout = w.Q<Label>("ErrOut");
            w.Q<Button>("Confirm").RegisterCallback<ClickEvent>(e =>
            {
                var name = namefield.value;
                if (string.IsNullOrEmpty(name))
                {
                    errorout.text = "Name Empty";
                    return;
                }
                else if (appmgr.exmgr.deffile.ContainsKey(name))
                {
                    errorout.text = "Name Conflict";
                    return;
                }
                parent.Remove(w);

                if (appmgr.exmgr.NewEx(name, copylist.value))
                {
                    UpdateExperimentList(appmgr.exmgr.deffile.Keys.ToList(), appmgr.cfgmgr.config.FirstTestID);
                    experimentlist.value = name;
                }
            });

            w.style.position = Position.Absolute;
            w.style.top = Length.Percent(20);
            w.style.left = Length.Percent(20);
            w.style.width = Length.Percent(60);
            w.style.height = Length.Percent(60);
            parent.Add(w);
        }

        public void UpdateExperimentList(List<string> list, string first = null) => UpdateDropdown(experimentlist, list, first);

        public void UpdateExperimentSessionList(List<string> list) => UpdateDropdown(experimentsessionlist, list);

        void UpdateDropdown(DropdownField which, List<string> list, string first = null)
        {
            if (list == null || list.Count == 0) { return; }
            list.Sort();
            if (first != null && list.Contains(first))
            {
                var i = list.IndexOf(first);
                list.RemoveAt(i);
                list.Insert(0, first);
            }
            which.choices = list;
            if (which.index < 0 || which.index > list.Count - 1)
            {
                which.index = Mathf.Clamp(which.index, 0, list.Count - 1);
            }
        }

        public void UpdateConfig(CommandConfig config, ScrollView content)
        {
            content.Clear();
            var previousui = content.Children().ToList();
            var previousuiname = previousui.Select(i => i.name).ToList();
            // since ExtendParam is a param container and we always show them, so here we do not AddParamUI for the container itself, but add its content
            var currentpropertyname = config.properties.Keys.Where(i => i != "ExtendParam").ToArray();
            var ui2update = previousuiname.Intersect(currentpropertyname);
            var ui2remove = previousuiname.Except(currentpropertyname);
            var ui2add = currentpropertyname.Except(previousuiname);

            if (ui2update.Count() > 0)
            {
                foreach (var p in ui2update)
                {
                    var ui = previousui[previousuiname.IndexOf(p)];
                    var namelabel = ui.Q<Label>("Name");
                    namelabel.text = p;
                    var vi = ui.Q("Value");
                    var ds = config.properties[p];
                    var db = vi.GetBinding("value") as DataBinding;
                    db.dataSource = ds;
                    ds.NotifyValue();
                }
            }
            if (ui2remove.Count() > 0)
            {
                foreach (var p in ui2remove)
                {
                    content.Remove(previousui[previousuiname.IndexOf(p)]);
                }
            }
            if (ui2add.Count() > 0)
            {
                foreach (var p in ui2add)
                {
                    AddParamUI(p, p, config.properties[p], false, null, content);
                }
            }

            foreach (var p in config.extendproperties.Keys.ToArray())
            {
                AddParamUI(p, p, config.extendproperties[p], false, null, content, config, true);
            }
            content.scrollOffset = content.contentRect.size;
        }

        public void UpdateEx(Experiment ex)
        {
            excontent.Clear();
            var previousui = excontent.Children().ToList();
            var previousuiname = previousui.Select(i => i.name).ToList();
            // since ExtendParam is a param container and we always show them, so here we do not AddParamUI for the container itself, but add its content
            var currentpropertyname = ex.properties.Keys.Except(appmgr.cfgmgr.config.ExHideParams).Where(i => i != "ExtendParam").ToArray();
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
                    var ds = ex.properties[p];
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
                    AddParamUI(p, p, ex.properties[p], ex.InheritParam.Contains(p), appmgr.ToggleExInherit, excontent);
                }
            }

            foreach (var p in ex.extendproperties.Keys.Except(appmgr.cfgmgr.config.ExHideParams).ToArray())
            {
                AddParamUI(p, p, ex.extendproperties[p], ex.InheritParam.Contains(p), appmgr.ToggleExInherit, excontent, appmgr.exmgr.el.ex, true);
            }
            excontent.scrollOffset = excontent.contentRect.size;
        }

        void AddParamUI<T>(string id, string name, IDataSource<T> source, bool isinherit, Action<string, bool> inherithandler, VisualElement parent, DataClass datasourceclass = null, bool isextendparam = false)
        {
            AddParamUI(id, name, source.Type, source.Value, isinherit, inherithandler, parent, datasourceclass, source, "Value", isextendparam);
        }

        void AddParamUI(string id, string name, Type T, object value, bool isinherit, Action<string, bool> inherithandler, VisualElement parent, DataClass datasourceclass = null, object datasource = null, string datapath = "Value", bool isextendparam = false)
        {
            VisualElement ui, valueinput;

            if (T.IsEnum)
            {
                var asset = inherithandler == null ? ParamEnum : ToggleEnum;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<EnumField>("Value");
                vi.Init((Enum)value);
                valueinput = vi;
            }
            else if (T == typeof(bool))
            {
                var asset = inherithandler == null ? ParamBool : ToggleBool;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Toggle>("Value");
                vi.value = (bool)value;
                vi.label = vi.value ? "True" : "False";
                vi.RegisterValueChangedCallback(e => vi.label = e.newValue ? "True" : "False");
                valueinput = vi;
            }
            else if (T == typeof(int))
            {
                var asset = inherithandler == null ? ParamInteger : ToggleInteger;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<IntegerField>("Value");
                vi.value = (int)value;
                valueinput = vi;
            }
            else if (T == typeof(uint))
            {
                var asset = inherithandler == null ? ParamUInteger : ToggleUInteger;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<UnsignedIntegerField>("Value");
                vi.value = (uint)value;
                valueinput = vi;
            }
            else if (T == typeof(float))
            {
                var asset = inherithandler == null ? ParamFloat : ToggleFloat;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<FloatField>("Value");
                vi.value = (float)value;
                valueinput = vi;
            }
            else if (T == typeof(double))
            {
                var asset = inherithandler == null ? ParamDouble : ToggleDouble;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<DoubleField>("Value");
                vi.value = (double)value;
                valueinput = vi;
            }
            else if (T == typeof(Vector2))
            {
                var asset = inherithandler == null ? ParamVector2 : ToggleVector2;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Vector2Field>("Value");
                vi.value = (Vector2)value;
                valueinput = vi;
            }
            else if (T == typeof(Vector3))
            {
                var asset = inherithandler == null ? ParamVector3 : ToggleVector3;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Vector3Field>("Value");
                vi.value = (Vector3)value;
                valueinput = vi;
            }
            else if (T == typeof(Vector4))
            {
                var asset = inherithandler == null ? ParamVector4 : ToggleVector4;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Vector4Field>("Value");
                vi.value = (Vector4)value;
                valueinput = vi;
            }
            else if (T == typeof(Color))
            {
                var asset = inherithandler == null ? ParamVector4 : ToggleVector4;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<Vector4Field>("Value");
                vi.value = (Color)value;
                valueinput = vi;
            }
            else
            {
                var asset = inherithandler == null ? ParamString : ToggleString;
                ui = asset.Instantiate()[0];
                ui.name = id;

                var vi = ui.Q<TextField>("Value");
                vi.value = value.Convert<string>(T);
                valueinput = vi;
            }

            if (inherithandler == null)
            {
                ui.Q<Button>("Name").text = name;
            }
            else
            {
                var nametoggle = ui.Q<Toggle>("Name");
                nametoggle.label = name;
                nametoggle.value = isinherit;
                nametoggle.RegisterValueChangedCallback(e => inherithandler(id, e.newValue));
            }

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
                else if (T.IsGenericType && (T.GetGenericTypeDefinition() == typeof(List<>) || T.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    binding.sourceToUiConverters.AddConverter((ref object s) => s.Convert<string>(T));
                    binding.uiToSourceConverters.AddConverter((ref string v) => v.Convert(typeof(string), T));
                }
                valueinput.SetBinding("value", binding);
            }
            if (isextendparam && datasourceclass != null)
            {
                var deletebutton = ExtendButton.Instantiate().Q<Button>("Delete");
                deletebutton.RegisterCallback<ClickEvent>(e =>
                {
                    datasourceclass.RemoveExtendProperty(id);
                    parent.Remove(ui);
                });
                ui.Insert(0, deletebutton);
            }
            parent.Add(ui);
        }

        void OnAddExParamWindow(VisualElement parent)
        {
            var w = AddExParamWindow.Instantiate()[0];
            w.Q<Button>("Close").RegisterCallback<ClickEvent>(e => parent.Remove(w));

            var namefield = w.Q<TextField>("Name");
            var valuefield = w.Q<TextField>("Value");
            var errorout = w.Q<Label>("ErrOut");
            w.Q<Button>("Confirm").RegisterCallback<ClickEvent>(e =>
            {
                var name = namefield.value;
                if (string.IsNullOrEmpty(name))
                {
                    errorout.text = "Name Empty";
                    return;
                }
                else if (appmgr.exmgr.el.ex.ContainsParam(name))
                {
                    errorout.text = "Name Conflict";
                    return;
                }
                var value = valuefield.value.TryParse();
                var s = appmgr.exmgr.el.ex.AddExtendProperty(name, value);
                parent.Remove(w);

                //AddParamUI(s.Name, s.Name, s,uicontroller.exmanager.el. ex.InheritParam.Contains(s.Name), uicontroller.ToggleExInherit, excontent, true);
                UpdateEx(appmgr.exmgr.el.ex);
            });

            w.style.position = Position.Absolute;
            w.style.top = Length.Percent(25);
            w.style.left = Length.Percent(20);
            w.style.width = Length.Percent(60);
            w.style.height = Length.Percent(50);
            parent.Add(w);
        }


        void AddParamsFoldoutUI(string[] ids, string[] names, IDataSource<object>[] sources, bool[] inherits, Action<string, bool> inherithandler, VisualElement parent, string groupname, DataClass datasourceclass = null, bool isextendparam = false)
        {
            var foldout = ParamsFoldout.Instantiate().Q<Foldout>();
            foldout.name = groupname;
            foldout.text = groupname;

            for (int i = 0; i < ids.Length; i++)
            {
                AddParamUI(ids[i], names[i], sources[i], inherits[i], inherithandler, foldout, datasourceclass, isextendparam);
            }
            parent.Add(foldout);
        }

        public void ClearEnv() => envcontent.Clear();

        public void UpdateEnv()
        {
            envcontent.Clear();
            var el = appmgr.exmgr.el;


            //var envps = uicontroller.exmanager.el.envmanager.GetParamSources(active: !uicontroller.config.IsShowInactiveEnvParam);
            ////var envps = uicontroller.exmanager.el.envmanager.GetParamSources(!uicontroller.config.IsShowEnvParamFullName, !uicontroller.config.IsShowInactiveEnvParam);
            //foreach (var name in envps.Keys)
            //{
            //    AddParamUI(name, name.FirstSplitHead(), envps[name], uicontroller.exmanager.el.ex.EnvInheritParam.Contains(name), uicontroller.ToggleEnvInherit, envcontent);
            //}



            var gonames = el.envmgr.GetGameObjectFullNames(!appmgr.cfgmgr.config.IsShowInactiveEnvParam);
            foreach (var goname in gonames)
            {
                var gonvs = el.envmgr.GetParamSourcesByGameObject(goname);
                var nvfullnames = gonvs.Keys.ToArray();
                var nvnames = nvfullnames.Select(i => i.FirstSplitHead()).ToArray();
                var nvsources = gonvs.Values.ToArray();
                var inherits = nvfullnames.Select(i => el.ex.EnvInheritParam.Contains(i)).ToArray();
                AddParamsFoldoutUI(nvfullnames, nvnames, nvsources, inherits, appmgr.ToggleEnvInherit, envcontent, goname);
            }
        }

        public void ClearView() => viewcontent.Clear();

        public void UpdateView()
        {
            var currentui = viewcontent.Query<TemplateContainer>().ToList();
            var currentuiname = currentui.Select(i => i.name).ToList();
            var currentcamera = appmgr.exmgr.el.envmgr.MainCamera;
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
                    msaaSamples = appmgr.cfgmgr.config.AntiAliasing,
                    colorFormat = RenderTextureFormat.ARGBHalf,
                    sRGB = false,
                    width = Mathf.Max(1, Mathf.FloorToInt(width)),
                    height = Mathf.Max(1, Mathf.FloorToInt(height)),
                    volumeDepth = 1
                })
                {
                    anisoLevel = appmgr.cfgmgr.config.AnisotropicFilterLevel
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
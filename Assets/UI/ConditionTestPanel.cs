/*
ConditionTestPanel.cs is part of the Experica.
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
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace Experica.Command
{
    public class ConditionTestPanel : MonoBehaviour
    {
        public UI ui;
        private VisualElement root;
        private Label titleLabel;
        private MultiColumnListView content;
        private int condtestidx = -1;
        private Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
        private Button clearButton;

        private void Start()
        {
            Initialize(ui.conditiontestpanel);
        }

        public void Initialize(VisualElement conditionTestPanelElement)
        {
            try
            {
                root = conditionTestPanelElement;
                titleLabel = root.Query<Label>("Title").First();
                content = root.Query<MultiColumnListView>("Content").First();
                clearButton = root.Query<Button>("ClearButton").First();

                if (titleLabel == null || content == null || clearButton == null)
                {
                    Debug.LogError("找不到必要的UI元素！");
                    return;
                }

                // 设置 MultiColumnListView 的样式
                content.style.minHeight = 100;
                content.style.flexGrow = 1;
                content.style.borderTopWidth = 1;
                content.style.borderBottomWidth = 1;
                content.style.borderLeftWidth = 1;
                content.style.borderRightWidth = 1;
                content.style.borderTopColor = Color.gray;
                content.style.borderBottomColor = Color.gray;
                content.style.borderLeftColor = Color.gray;
                content.style.borderRightColor = Color.gray;

                // 设置列
                content.columns.Clear();
                content.columns.Add(new Column { name = "CondTestIndex", title = "", width = 180 });
                content.columns.Add(new Column { name = "CondIndex", title = "", width = 180 });
                content.columns.Add(new Column { name = "CondRepeat", title = "", width = 180 });
                content.columns.Add(new Column { name = "BlockIndex", title = "", width = 180 });
                content.columns.Add(new Column { name = "BlockRepeat", title = "", width = 180 });

                // 设置表头样式
                content.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
                content.showBorder = true;
                content.reorderable = false;
                content.showBoundCollectionSize = true;
                content.style.width = 928; // 设置总宽度
                content.style.minWidth = 928; // 设置最小宽度

                // 初始化数据列
                data["CondTestIndex"] = new List<string>();
                data["CondIndex"] = new List<string>();
                data["CondRepeat"] = new List<string>();
                data["BlockIndex"] = new List<string>();
                data["BlockRepeat"] = new List<string>();

                // 注册 Clear 按钮点击事件
                clearButton.clicked += Clear;

                Debug.Log("ConditionTestPanel 初始化成功");
            }
            catch (Exception e)
            {
                Debug.LogError($"ConditionTestPanel 初始化失败: {e.Message}\n{e.StackTrace}");
            }
        }

        public void Show()
        {
            if (root != null)
            {
                root.style.display = DisplayStyle.Flex;
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.style.display = DisplayStyle.None;
            }
        }

        public void UpdateData(int condTestIndex, int condIndex, int condRepeat, int blockIndex, int blockRepeat)
        {
            if (content == null) return;

            // 更新数据
            data["CondTestIndex"].Add(condTestIndex.ToString());
            data["CondIndex"].Add(condIndex.ToString());
            data["CondRepeat"].Add(condRepeat.ToString());
            data["BlockIndex"].Add(blockIndex.ToString());
            data["BlockRepeat"].Add(blockRepeat.ToString());

            // 更新视图
            content.itemsSource = GetItemsSource();
            content.RefreshItems();
        }

        private List<Dictionary<string, string>> GetItemsSource()
        {
            var items = new List<Dictionary<string, string>>();
            var count = data["CondTestIndex"].Count;

            for (int i = 0; i < count; i++)
            {
                var item = new Dictionary<string, string>
                {
                    ["CondTestIndex"] = data["CondTestIndex"][i],
                    ["CondIndex"] = data["CondIndex"][i],
                    ["CondRepeat"] = data["CondRepeat"][i],
                    ["BlockIndex"] = data["BlockIndex"][i],
                    ["BlockRepeat"] = data["BlockRepeat"][i]
                };
                items.Add(item);
            }

            return items;
        }

        public void Clear()
        {
            if (content == null) return;

            // 清除数据
            foreach (var list in data.Values)
            {
                list.Clear();
            }

            // 更新视图
            content.itemsSource = new List<Dictionary<string, string>>();
            content.RefreshItems();
            condtestidx = -1;
        }
    }
}
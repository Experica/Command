/*
ConsolePanel.cs is part of the Experica.
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
using System.Linq;
using System.IO;

namespace Experica.Command
{
    public class ConsolePanel : MonoBehaviour
    {
        public UI ui;
        private VisualElement root;
        private ScrollView logContent;
        private Label titleLabel;
        private VisualElement statusBar;
        private Button normalButton;
        private Button warningButton;
        private Button errorButton;
        private Button clearButton;
        private int maxEntry = 200;
        private int entryCount = 0;
        private List<Label> logEntries = new List<Label>();
        private int fontSize = 28;
        private string lastLogMessage = "";
        private int duplicateCount = 0;
        private Dictionary<LogType, bool> logTypeVisibility = new Dictionary<LogType, bool>();
        private bool needsScroll = false;
        private Dictionary<LogType, List<Label>> pendingLogs = new Dictionary<LogType, List<Label>>
        {
            { LogType.Log, new List<Label>() },
            { LogType.Warning, new List<Label>() },
            { LogType.Error, new List<Label>() },
            { LogType.Exception, new List<Label>() },
            { LogType.Assert, new List<Label>() }
        };

        private void Awake()
        {
            GlobalLogHandler.Initialize(HandleLog);
        }

        private void Start()
        {
            Initialize( ui.consolepanel);
        }

        private class GlobalLogHandler : ILogHandler
        {
            private static event Action<string, string, LogType> OnLogReceived;
            private static ILogHandler defaultHandler;
            private static GlobalLogHandler instance;
            private static bool isProcessing = false;  // 添加标志防止循环

            public static void Initialize(Action<string, string, LogType> handler)
            {
                OnLogReceived = handler;
                defaultHandler = Debug.unityLogger.logHandler;
                
                // 创建实例并设置
                instance = new GlobalLogHandler();
                Debug.unityLogger.logHandler = instance;
                
                // 确保日志系统启用
                Debug.unityLogger.logEnabled = true;
                Debug.unityLogger.filterLogType = LogType.Log;

                // 输出初始化信息
                defaultHandler?.LogFormat(LogType.Log, null, "ConsolePanel 日志处理器已初始化");
            }

            public static void Cleanup()
            {
                OnLogReceived = null;
                if (defaultHandler != null)
                {
                    Debug.unityLogger.logHandler = defaultHandler;
                    defaultHandler.LogFormat(LogType.Log, null, "ConsolePanel 日志处理器已清理");
                }
                instance = null;
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
                if (isProcessing) return;
                isProcessing = true;
                try
                {
                    // 先转发到我们的处理器
                    OnLogReceived?.Invoke(exception.InnerException?.ToString()??exception.Message, exception.StackTrace, LogType.Exception);
                    // 再转发到默认处理器
                    defaultHandler?.LogException(exception, context);
                }
                finally
                {
                    isProcessing = false;
                }
            }

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (isProcessing) return;
                isProcessing = true;
                try
                {
                    // 先转发到我们的处理器
                    string message = string.Format(format, args);
                    OnLogReceived?.Invoke(message, "", logType);
                    // 再转发到默认处理器
                    defaultHandler?.LogFormat(logType, context, format, args);
                }
                catch (Exception e)
                {
                    // 如果格式化失败，直接使用原始消息
                    OnLogReceived?.Invoke(format, "", logType);
                    defaultHandler?.LogFormat(logType, context, format, args);
                }
                finally
                {
                    isProcessing = false;
                }
            }

            // 添加静态方法用于记录错误
            public static void LogError(string message)
            {
                if (defaultHandler != null)
                {
                    defaultHandler.LogFormat(LogType.Error, null, message);
                }
            }
        }

        public int FontSize
        {
            get => fontSize;
            set
            {
                fontSize = value;
                // 更新所有现有日志的字体大小
                foreach (var label in logEntries)
                {
                    label.style.fontSize = fontSize;
                }
            }
        }

        public void Initialize(VisualElement consolePanelElement)
        {
            Debug.Log("ConsolePanel Initialize called");
            root = consolePanelElement;
            logContent = root.Q<ScrollView>("LogContent");
            titleLabel = root.Q<Label>("Title");
            statusBar = root.Q<VisualElement>("Status");

            // 初始化按钮
            normalButton = root.Q<Button>("NormalTab");
            warningButton = root.Q<Button>("WarningTab");
            errorButton = root.Q<Button>("ErrorTab");
            clearButton = root.Q<Button>("ClearButton");

            // 初始化日志类型可见性
            logTypeVisibility[LogType.Log] = true;
            logTypeVisibility[LogType.Warning] = true;
            logTypeVisibility[LogType.Error] = true;
            logTypeVisibility[LogType.Exception] = true;  // Exception默认可见
            logTypeVisibility[LogType.Assert] = true;

            // 设置按钮点击事件
            normalButton.RegisterCallback<ClickEvent>(e => ToggleLogVisibility(LogType.Log));
            warningButton.RegisterCallback<ClickEvent>(e => ToggleLogVisibility(LogType.Warning));
            errorButton.RegisterCallback<ClickEvent>(e => ToggleLogVisibility(LogType.Error));
            clearButton.RegisterCallback<ClickEvent>(e => Clear());

            // 默认显示所有日志
            ShowAllLogs();

            if (logContent == null)
            {
                Debug.LogError("找不到 LogContent 元素！");
                return;
            }

            // 配置ScrollView以确保滚动功能正常
            logContent.mode = ScrollViewMode.Vertical;
            logContent.showHorizontal = false;
            logContent.showVertical = true;
            logContent.verticalScroller.value = 0;
        }

        private void ToggleLogVisibility(LogType logType)
        {
            // 记录切换前的可见性
            bool wasVisible = logTypeVisibility[logType];

            // 切换日志类型可见性
            logTypeVisibility[logType] = !logTypeVisibility[logType];
            
            // 如果切换的是Error类型，同时切换Exception类型
            if (logType == LogType.Error)
            {
                logTypeVisibility[LogType.Exception] = logTypeVisibility[logType];
                // Debug.Log($"ToggleLogVisibility: {logType} 和 Exception 从 {wasVisible} 切换为 {logTypeVisibility[logType]}");
            }
            else
            {
                // Debug.Log($"ToggleLogVisibility: {logType} 从 {wasVisible} 切换为 {logTypeVisibility[logType]}");
            }
            
            // 更新按钮样式
            UpdateButtonStyle(logType);
            
            // 更新日志显示
            UpdateLogVisibility();

            // 如果是从隐藏变为显示，主动滚动到底部，并显示pendingLogs
            if (!wasVisible && logTypeVisibility[logType])
            {
                // Debug.Log($"显示pendingLogs[{logType}]，数量: {pendingLogs[logType].Count}");
                // 按时间顺序插入pendingLogs
                InsertPendingLogsInOrder(logType);
                
                // 如果是Error类型，也显示Exception的pendingLogs
                if (logType == LogType.Error)
                {
                    // Debug.Log($"显示pendingLogs[Exception]，数量: {pendingLogs[LogType.Exception].Count}");
                    InsertPendingLogsInOrder(LogType.Exception);
                }
                
                ScheduleAutoScroll();
            }
        }

        private void UpdateButtonStyle(LogType logType)
        {
            Button button = null;
            switch (logType)
            {
                case LogType.Log:
                    button = normalButton;
                    break;
                case LogType.Warning:
                    button = warningButton;
                    break;
                case LogType.Error:
                    button = errorButton;
                    break;
            }

            if (button != null)
            {
                if (logTypeVisibility[logType])
                {
                    button.AddToClassList("active");
                }
                else
                {
                    button.RemoveFromClassList("active");
                }
            }
        }

        private void UpdateLogVisibility()
        {
            // 更新所有日志条目的可见性
            for (int i = logEntries.Count - 1; i >= 0; i--)
            {
                var label = logEntries[i];
                var logType = GetLogTypeFromLabel(label);
                
                if (logTypeVisibility[logType])
                {
                    // 如果类型可见，确保显示
                    label.style.display = DisplayStyle.Flex;
                    // 确保在UI中
                    if (!logContent.Contains(label))
                    {
                        logContent.Add(label);
                    }
                }
                else
                {
                    // 如果类型不可见，从UI中移除并暂存到pendingLogs
                    if (logContent.Contains(label))
                    {
                        logContent.Remove(label);
                        pendingLogs[logType].Add(label);
                        // Debug.Log($"UpdateLogVisibility: 将已存在的 {logType} 日志移到pendingLogs，当前pendingLogs[{logType}]数量: {pendingLogs[logType].Count}");
                    }
                }
            }
        }

        private LogType GetLogTypeFromLabel(Label label)
        {
            // 根据标签的颜色判断日志类型
            var color = label.style.color.value;
            if (color == Color.yellow)
                return LogType.Warning;
            if (color == Color.red)
            {
                // 红色文本可能是Error或Exception，需要进一步判断
                // 由于Exception和Error都显示为红色，我们将Exception也归类为Error
                // 这样用户点击Error按钮时，Exception也会被隐藏
                return LogType.Error;
            }
            return LogType.Log;
        }

        private void ShowAllLogs()
        {
            // 设置所有按钮为激活状态
            normalButton.AddToClassList("active");
            warningButton.AddToClassList("active");
            errorButton.AddToClassList("active");

            // 设置所有日志类型为可见
            foreach (var logType in logTypeVisibility.Keys.ToList())
            {
                logTypeVisibility[logType] = true;
            }

            // 显示所有日志
            UpdateLogVisibility();
        }

        void OnDisable()
        {
            Debug.Log("ConsolePanel OnDisable called");
            GlobalLogHandler.Cleanup();
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            try
            {
                if (root == null || logContent == null)
                {
                    Debug.LogWarning("ConsolePanel 未正确初始化");
                    return;
                }

                // 过滤掉 GameView 相关的重复警告
                if (type == LogType.Warning && logString.Contains("GameView reduced to a reasonable size"))
                {
                    return;
                }

                // 检查是否是重复的日志
                if (logString == lastLogMessage)
                {
                    duplicateCount++;
                }
                else
                {
                    duplicateCount = 0;
                    lastLogMessage = logString;
                }

                // 创建日志条目
                var timestamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                string firstLine = logString.Split('\n')[0];
                var logMessage = $"{timestamp}: {firstLine}";

                var label = new Label(logMessage);
                label.style.color = GetColorForLogType(type);
                label.style.marginBottom = 2;
                label.style.marginTop = 2;
                label.style.paddingLeft = 5;
                label.style.paddingRight = 5;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.unityTextAlign = TextAnchor.UpperLeft;
                label.style.unityTextOverflowPosition = TextOverflowPosition.End;
                label.style.fontSize = fontSize;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.flexShrink = 1;
                label.style.flexGrow = 1;
                label.style.width = Length.Percent(100);
                label.style.display = logTypeVisibility[type] ? DisplayStyle.Flex : DisplayStyle.None;

                // 如果当前类型不可见，暂存到pendingLogs，不显示
                if (!logTypeVisibility[type])
                {
                    pendingLogs[type].Add(label);
                    // Debug.Log($"日志类型 {type} 被隐藏，已添加到pendingLogs，当前pendingLogs[{type}]数量: {pendingLogs[type].Count}");
                    return;
                }

                // 添加到日志内容区域
                logContent.Add(label);
                logEntries.Add(label);

                // 检查总日志数量
                if (logEntries.Count > maxEntry)
                {
                    var oldestLabel = logEntries[0];
                    logContent.Remove(oldestLabel);
                    logEntries.RemoveAt(0);
                }

                // 修复自动滚动逻辑 - 使用正确的滚动方法
                ScheduleAutoScroll();
            }
            catch (Exception e)
            {
                Debug.LogError($"处理日志时出错: {e.Message}\n{e.StackTrace}");
            }
        }

        public void Clear()
        {
            try
            {
                // 清除所有内容
                logContent.Clear();
                logEntries.Clear();

                // 清空pendingLogs
                foreach (var pendingList in pendingLogs.Values)
                {
                    pendingList.Clear();
                }

                // 重置计数器
                entryCount = 0;
                lastLogMessage = "";
                duplicateCount = 0;
                needsScroll = false;

                // 重置滚动位置
                logContent.verticalScroller.value = 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"清除日志时出错: {e.Message}");
            }
        }

        public void Log(LogType logType, string message, string stackTrace = "")
        {
            try
            {
                var timestamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                var logMessage = $"{timestamp}: {message}";

                var label = new Label(logMessage);
                label.style.color = GetColorForLogType(logType);
                label.style.marginBottom = 2;
                label.style.marginTop = 2;
                label.style.paddingLeft = 5;
                label.style.paddingRight = 5;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.unityTextAlign = TextAnchor.UpperLeft;
                label.style.unityTextOverflowPosition = TextOverflowPosition.End;
                label.style.fontSize = fontSize;
                label.style.display = logTypeVisibility[logType] ? DisplayStyle.Flex : DisplayStyle.None;

                // 如果当前类型不可见，暂存到pendingLogs，不显示
                if (!logTypeVisibility[logType])
                {
                    pendingLogs[logType].Add(label);
                    // Debug.Log($"Log方法: 日志类型 {logType} 被隐藏，已添加到pendingLogs，当前pendingLogs[{logType}]数量: {pendingLogs[logType].Count}");
                    return;
                }

                logContent.Add(label);
                logEntries.Add(label);

                // 检查总日志数量
                if (logEntries.Count > maxEntry)
                {
                    var oldestLabel = logEntries[0];
                    logContent.Remove(oldestLabel);
                    logEntries.RemoveAt(0);
                }

                // 修复自动滚动逻辑 - 使用正确的滚动方法
                ScheduleAutoScroll();
            }
            catch (Exception e)
            {
                // 使用 GlobalLogHandler 的静态方法记录错误
                GlobalLogHandler.LogError($"记录日志时出错: {e.Message}");
            }
        }

        public void Log(LogType logType, object message, string stackTrace = "")
        {
            Log(logType, message?.ToString() ?? "null", stackTrace);
        }

        public void Log(object message, bool istimestamp = true)
        {
            Log(LogType.Log, message?.ToString() ?? "null");
        }

        public void LogWarn(object message, bool istimestamp = true)
        {
            Log(LogType.Warning, message?.ToString() ?? "null");
        }

        public void LogError(object message, bool istimestamp = true)
        {
            Log(LogType.Error, message?.ToString() ?? "null");
        }

        // 新增方法：调度自动滚动
        private void ScheduleAutoScroll()
        {
            // 直接滚动到底部，添加修正偏移量
            if (logContent != null && logContent.verticalScroller != null)
            {
                var scrollValue = logContent.verticalScroller.highValue + 20; // 添加20像素的修正偏移量
                logContent.verticalScroller.value = scrollValue;
            }
            
            // 同时设置标志，在Update中再次尝试
            needsScroll = true;
        }

        private void Update()
        {
            // 在Update中再次执行滚动，确保滚动到底部
            if (needsScroll && logContent != null && logContent.verticalScroller != null)
            {
                var scrollValue = logContent.verticalScroller.highValue + 20; // 添加20像素的修正偏移量
                logContent.verticalScroller.value = scrollValue;
                // Debug.Log($"Update中滚动: value={logContent.verticalScroller.value}, highValue={logContent.verticalScroller.highValue}, 修正后={scrollValue}");
                needsScroll = false;
            }
        }

        /// <summary>
        /// 按时间顺序插入pendingLogs中的日志
        /// </summary>
        /// <param name="logType">要插入的日志类型</param>
        private void InsertPendingLogsInOrder(LogType logType)
        {
            if (pendingLogs[logType].Count == 0) return;

            // 合并logEntries和pendingLogs
            var allLogs = new List<Label>(logEntries);
            allLogs.AddRange(pendingLogs[logType]);
            if (logType == LogType.Error)
                allLogs.AddRange(pendingLogs[LogType.Exception]); // Error和Exception一起处理

            // 按时间戳排序
            allLogs.Sort((a, b) =>
            {
                DateTime ta = ParseLogTime(a.text);
                DateTime tb = ParseLogTime(b.text);
                return ta.CompareTo(tb);
            });

            // 清空UI和logEntries
            logContent.Clear();
            logEntries.Clear();

            // 重新添加所有日志
            foreach (var label in allLogs)
            {
                var type = GetLogTypeFromLabel(label);
                label.style.display = logTypeVisibility[type] ? DisplayStyle.Flex : DisplayStyle.None;
                logContent.Add(label);
                logEntries.Add(label);
            }

            // 清空pendingLogs
            pendingLogs[logType].Clear();
            if (logType == LogType.Error)
                pendingLogs[LogType.Exception].Clear();
        }

        /// <summary>
        /// 根据时间戳找到正确的插入位置
        /// </summary>
        /// <param name="timestamp">要插入的时间戳</param>
        /// <returns>插入位置索引</returns>
        private int FindInsertPosition(DateTime timestamp)
        {
            for (int i = 0; i < logEntries.Count; i++)
            {
                var text = logEntries[i].text;
                if (DateTime.TryParse(text.Substring(0, 19), out DateTime entryTime))
                {
                    if (timestamp <= entryTime)
                    {
                        return i;
                    }
                }
            }
            return logEntries.Count; // 如果所有现有日志都更早，插入到末尾
        }

        private Color GetColorForLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                    return Color.white;
                case LogType.Warning:
                    return Color.yellow;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:  // Assert 类型使用红色
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        private DateTime ParseLogTime(string text)
        {
            if (text.Length >= 19 && DateTime.TryParse(text.Substring(0, 19), out DateTime t))
                return t;
            return DateTime.MinValue;
        }
    }
}
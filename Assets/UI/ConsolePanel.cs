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
        private int maxEntry = 100;
        private int entryCount = 0;
        private List<Label> logEntries = new List<Label>();
        private int fontSize = 28;
        private string lastLogMessage = "";
        private int duplicateCount = 0;
        private Dictionary<LogType, bool> logTypeVisibility = new Dictionary<LogType, bool>();

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
            logTypeVisibility[LogType.Exception] = true;
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

            // 检查日志系统状态
            Debug.Log($"日志系统状态检查:");
            Debug.Log($"- 日志处理器类型: {Debug.unityLogger.logHandler.GetType().FullName}");
            Debug.Log($"- 日志过滤级别: {Debug.unityLogger.filterLogType}");
            Debug.Log($"- 日志系统启用状态: {Debug.unityLogger.logEnabled}");
            Debug.Log($"- 当前日志处理器是否为我们自定义的处理器: {Debug.unityLogger.logHandler is GlobalLogHandler}");
            
            // 测试日志
            Debug.Log("测试普通日志");
            Debug.LogWarning("测试警告日志");
            Debug.LogError("测试错误日志");
        }

        private void ToggleLogVisibility(LogType logType)
        {
            // 切换日志类型可见性
            logTypeVisibility[logType] = !logTypeVisibility[logType];
            
            // 更新按钮样式
            UpdateButtonStyle(logType);
            
            // 更新日志显示
            UpdateLogVisibility();
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
            foreach (var label in logEntries)
            {
                var logType = GetLogTypeFromLabel(label);
                label.style.display = logTypeVisibility[logType] ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private LogType GetLogTypeFromLabel(Label label)
        {
            // 根据标签的颜色判断日志类型
            var color = label.style.color.value;
            if (color == Color.yellow)
                return LogType.Warning;
            if (color == Color.red)
                return LogType.Error;
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
                    // 如果重复次数超过阈值，不显示
                    if (duplicateCount > 3)
                    {
                        return;
                    }
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

                if (duplicateCount > 0)
                {
                    logMessage += $" (重复 {duplicateCount} 次)";
                }

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

                // 自动滚动到底部
                logContent.scrollOffset = new Vector2(0, logContent.contentRect.height);
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

                // 重置计数器
                entryCount = 0;
                lastLogMessage = "";
                duplicateCount = 0;

                // 重置滚动位置
                logContent.scrollOffset = Vector2.zero;
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

                logContent.Add(label);
                logEntries.Add(label);

                // 检查总日志数量
                if (logEntries.Count > maxEntry)
                {
                    var oldestLabel = logEntries[0];
                    logContent.Remove(oldestLabel);
                    logEntries.RemoveAt(0);
                }

                // 自动滚动到底部
                logContent.scrollOffset = new Vector2(0, logContent.contentRect.height);
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
    }
}
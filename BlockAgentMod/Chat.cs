using Modding;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BlockAgentMod
{
    public class Chat : MonoBehaviour
    {
        // ==================== UI 状态变量 ====================
        private bool showWindow = true;
        private Vector2 scrollPosition;

        // 配置项
        private string apiKey = "";
        private string apiUrl = "https://api.openai.com/v1/chat/completions";
        private string modelName = "gpt-3.5-turbo";

        // 对话项
        private string userInput = "";
        private List<string> chatHistory = new List<string>();
        private bool isWaitingForResponse = false;

        // 配置文件路径
        private string configFilePath;

        // ==================== 强类型 JSON 类（替代 dynamic）====================
        [Serializable]
        public class ChatRequest
        {
            public string model;
            public Message[] messages;
        }

        [Serializable]
        public class Message
        {
            public string role;
            public string content;
        }

        [Serializable]
        public class ChatResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        public class Choice
        {
            public Message message;
        }

        [Serializable]
        public class ApiConfig
        {
            public string ApiUrl;
            public string ApiKey;
            public string ModelName;
        }

        // ==================== 初始化 ====================
        void Start()
        {
            // 初始化配置文件路径
            configFilePath = Path.Combine("/", "AIBrainConfig.json");
            //LoadConfig();
            chatHistory.Add("系统: AI大脑已启动，请先配置API信息。");
        }

        // ==================== UI 绘制 ====================
        void OnGUI()
        {
            if (GUI.Button(new Rect(Screen.width - 120, 10, 110, 30), "AI 大脑面板"))
            {
                showWindow = !showWindow;
            }

            if (!showWindow) return;

            float windowWidth = 400;
            float windowHeight = 500;
            float windowX = Screen.width - windowWidth - 10;
            float windowY = 50;
            GUI.Window(9999, new Rect(windowX, windowY, windowWidth, windowHeight), DrawMainWindow, "AI 驾驶员大脑");
        }

        private void DrawMainWindow(int windowId)
        {
            // 配置区
            GUILayout.Label("【API 配置】", GUI.skin.box);
            GUILayout.Label("API 地址:");
            apiUrl = GUILayout.TextField(apiUrl);
            GUILayout.Label("API Key:");
            apiKey = GUILayout.PasswordField(apiKey, '*');
            GUILayout.Label("模型名称:");
            modelName = GUILayout.TextField(modelName);

            if (GUILayout.Button("保存配置"))
            {
                //SaveConfig();
                chatHistory.Add("系统: 配置已保存。");
            }

            GUILayout.Space(10);

            // 对话显示区
            GUILayout.Label("【对话记录】", GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            foreach (var msg in chatHistory)
            {
                GUILayout.Label(msg, GUILayout.Width(380));
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // 输入与发送区
            GUILayout.BeginHorizontal();
            userInput = GUILayout.TextField(userInput);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = !isWaitingForResponse;

            if (GUILayout.Button("发送", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(userInput.Trim()))
                {
                    SendToAI(userInput.Trim());
                    userInput = "";
                }
            }

            GUI.enabled = wasEnabled;
            GUILayout.EndHorizontal();

            if (isWaitingForResponse)
            {
                GUILayout.Label("AI 正在思考中...");
            }

            GUI.DragWindow();
        }

        // ==================== 网络通信（使用 WWW 发送 POST）====================
        private void SendToAI(string message)
        {
            chatHistory.Add("玩家: " + message);
            isWaitingForResponse = true;
            StartCoroutine(SendToAICoroutine(message));
        }

        private IEnumerator SendToAICoroutine(string message)
        {
            // 1. 构建请求体（强类型）
            ChatRequest request = new ChatRequest
            {
                model = modelName,
                messages = new Message[]
                {
                    new Message { role = "user", content = message }
                }
            };
            string jsonBody = JsonConvert.SerializeObject(request);

            // 2. 构建请求头
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + apiKey },
                { "Content-Type", "application/json" }
            };

            // 3. 发送 POST 请求
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            WWW www = new WWW(apiUrl, bodyRaw, headers);

            // 4. 等待响应
            yield return www;

            // 5. 处理响应
            if (!string.IsNullOrEmpty(www.error))
            {
                chatHistory.Add("系统网络错误: " + www.error);
            }
            else
            {
                try
                {
                    // 使用强类型类解析（不再用 dynamic）
                    ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(www.text);

                    if (response != null && response.choices != null && response.choices.Length > 0)
                    {
                        string aiReply = response.choices[0].message.content;
                        chatHistory.Add("AI: " + aiReply);
                    }
                    else
                    {
                        chatHistory.Add("系统: AI 返回了空回复。");
                    }
                }
                catch (Exception ex)
                {
                    chatHistory.Add("JSON解析错误: " + ex.Message);
                }
            }

            isWaitingForResponse = false;
        }

        // ==================== 配置读写 ====================
        //private void SaveConfig()
        //{
        //    try
        //    {
        //        ApiConfig config = new ApiConfig
        //        {
        //            ApiUrl = apiUrl,
        //            ApiKey = apiKey,
        //            ModelName = modelName
        //        };
        //        //File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        //    }
        //    catch (Exception ex)
        //    {
        //        chatHistory.Add("保存配置失败: " + ex.Message);
        //    }
        //}

        //private void LoadConfig()
        //{
        //    try
        //    {
        //        if (File.Exists(configFilePath))
        //        {
        //            string json = File.ReadAllText(configFilePath);
        //            // 使用强类型类解析（不再用 dynamic）
        //            ApiConfig config = JsonConvert.DeserializeObject<ApiConfig>(json);

        //            if (config != null)
        //            {
        //                apiUrl = config.ApiUrl;
        //                apiKey = config.ApiKey;
        //                modelName = config.ModelName;
        //                chatHistory.Add("系统: 已加载本地配置。");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        chatHistory.Add("加载配置失败，使用默认值: " + ex.Message);
        //    }
        //}
    }
}
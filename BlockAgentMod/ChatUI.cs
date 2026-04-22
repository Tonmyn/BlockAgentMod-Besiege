using Modding;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAgentMod
{
    public class ChatUI : MonoBehaviour
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
        private bool loaded = false;

        // ==================== HTTP服务变量 ====================
        private Thread serverThread;
        private bool isRunning = false;

        // 线程安全的队列，用于把浏览器发来的字传给主线程
        private static Queue<string> messageQueue = new Queue<string>();

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
            configFilePath = Path.Combine(BlockAgentMod.Instance.ModDirPath, "AIBrainConfig.json");
            //configFilePath = Modding.ModIO.GetDirectories("/", true);
            Debug.Log(configFilePath);
            LoadConfig();
            if (loaded)
            {
                chatHistory.Add("系统: AI大脑已启动。");
            }
            else
            {
                chatHistory.Add("系统: AI大脑已启动，请先配置API信息。");
            }
           


            isRunning = true;
            serverThread = new Thread(StartServer);
            serverThread.IsBackground = true;
            serverThread.Start();

        }

        void Update()
        {
            // 在主线程的 Update 中检查是否有新消息
            if (messageQueue.Count > 0)
            {
                // 取出浏览器传来的中文
                string msgFromBrowser = messageQueue.Dequeue();

                // 1. 赋值给你游戏内的输入框变量
                userInput = msgFromBrowser;

                // 2. 直接调用你原本的发送逻辑！
                SendToAI(userInput);

                // 3. 清空输入框（模拟发送后的清空）
                userInput = "";
            }
        }

        private void StartServer()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:8899/"); // 监听本地端口
            listener.Start();
            Debug.Log("中文输入桥接服务已启动：http://127.0.0.1:8899/");

            while (isRunning)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    // 如果是访问主页，返回那个极简的输入框网页
                    if (request.HttpMethod == "GET")
                    {
                        string html = GetHtmlPage();
                        byte[] buffer = Encoding.UTF8.GetBytes(html);
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                    }
                    // 如果是提交文本
                    else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/send")
                    {
                        using (var reader = new System.IO.StreamReader(request.InputStream, Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            // 简单解析出文本 (格式: msg=你好世界)
                            string decodedMsg = Uri.UnescapeDataString(body.Replace("msg=", ""));

                            // 放入队列，交给主线程处理
                            lock (messageQueue)
                            {
                                messageQueue.Enqueue(decodedMsg);
                            }
                        }
                        // 给网页回复"成功"
                        byte[] responseBytes = Encoding.UTF8.GetBytes("OK");
                        context.Response.StatusCode = 200;
                        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                        context.Response.OutputStream.Close();
                    }
                }
                catch (Exception) { /* 忽略断开连接等异常 */ }
            }
            listener.Stop();
        }

        // 提供一个方法给游戏内的按钮调用，打开这个网页
        public void OpenChineseInputPage()
        {
            Application.OpenURL("http://127.0.0.1:8899/");
        }

        // 内嵌的极简网页代码（直接作为字符串返回，彻底解决跨域问题）
        private string GetHtmlPage()
        {
            return @"<!DOCTYPE html>
        <html lang='zh-CN'>
        <head>
            <meta charset='UTF-8'>
            <title>中文输入桥</title>
            <style>
                body { background: #222; color: #fff; font-family: 'Microsoft YaHei'; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }
                .box { text-align: center; }
                input { width: 300px; padding: 10px; font-size: 16px; background: #333; color: #fff; border: 1px solid #555; border-radius: 4px; }
                button { padding: 10px 20px; font-size: 16px; background: #0078d7; color: #fff; border: none; border-radius: 4px; cursor: pointer; margin-left: 10px; }
                button:hover { background: #005a9e; }
                p { color: #888; font-size: 12px; margin-top: 15px; }
            </style>
        </head>
        <body>
            <div class='box'>
                <input type='text' id='i' placeholder='在这里输入中文...' autofocus onkeypress='if(event.key===""Enter"")send()'>
                <button onclick='send()'>发送到游戏</button>
                <p>输入后按回车或点击发送，会自动注入到游戏内</p>
            </div>
            <script>
                function send() {
                    const msg = document.getElementById('i').value.trim();
                    if (!msg) return;
                    fetch('/send', { method: 'POST', body: 'msg=' + encodeURIComponent(msg) })
                    .then(() => { document.getElementById('i').value = ''; })
                    .catch(err => alert('无法连接到游戏，请确认游戏已开启该功能'));
                }
            </script>
        </body>
        </html>";
        }

        void OnDestroy()
        {
            isRunning = false;
        }

        // ==================== UI 绘制 ====================
        void OnGUI()
        {
            // 你的游戏内按钮
            if (GUI.Button(new Rect(Screen.width - 250, 10, 110, 30), "🌐 唤起中文输入"))
            {
                OpenChineseInputPage();
            }

            if (GUI.Button(new Rect(Screen.width - 120, 10, 110, 30), "AI 大脑面板"))
            {
                showWindow = !showWindow;
            }

            if (!showWindow) return;

            float windowWidth = 400;
            float windowHeight = 800;
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
                SaveConfig();
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
            Debug.Log("游戏收到中文并开始处理: " + message);
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

         //==================== 配置读写 ====================
        private void SaveConfig()
        {
            try
            {
                ApiConfig config = new ApiConfig
                {
                    ApiUrl = apiUrl,
                    ApiKey = apiKey,
                    ModelName = modelName
                };
                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            catch (Exception ex)
            {
                chatHistory.Add("保存配置失败: " + ex.Message);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    // 使用强类型类解析（不再用 dynamic）
                    ApiConfig config = JsonConvert.DeserializeObject<ApiConfig>(json);

                    if (config != null)
                    {
                        apiUrl = config.ApiUrl;
                        apiKey = config.ApiKey;
                        modelName = config.ModelName;
                        chatHistory.Add("系统: 已加载本地配置。");
                        loaded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                chatHistory.Add("加载配置失败，使用默认值: " + ex.Message);
            }
        }
    }
}
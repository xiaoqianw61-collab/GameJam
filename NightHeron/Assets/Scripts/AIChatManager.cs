using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// Unity 内置 AI 聊天：支持 OpenAI 兼容接口。
/// 在 Inspector 填入 API URL、Key、模型名即可使用。
/// </summary>
public class AIChatManager : MonoBehaviour
{
    [Header("LLM 配置")]
    [Tooltip("OpenAI 兼容 API 地址，例如 https://api.openai.com/v1/chat/completions")]
    public string apiUrl = "https://api.openai.com/v1/chat/completions";

    [Tooltip("你的 API Key")]
    public string apiKey = "";

    [Tooltip("模型名，例如 gpt-3.5-turbo / deepseek-chat / qwen-turbo")]
    public string modelName = "gpt-3.5-turbo";

    [Header("UI 引用")]
    public InputField inputField;
    public Text chatText;
    public ScrollRect scrollRect;
    public Button sendButton;

    [Header("系统设定")]
    [TextArea(2, 4)]
    public string systemPrompt = "你是一位游戏助手，正在帮助玩家玩《夜鹭便便大作战》。请用简短、风趣的中文回答。";

    private StringBuilder history = new StringBuilder();
    private bool isWaiting;

    void Start()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(SendMessage);

        if (inputField != null)
        {
            inputField.onEndEdit.AddListener((value) =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    SendMessage();
            });
        }

        AppendMessage("AI", "你好！我是夜鹭军师，有不懂的可以问我~");
    }

    public void SendMessage()
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;
        if (isWaiting) return;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            AppendMessage("AI", "请先点击场景里的 AIChatManager，在 Inspector 填入 API Key。");
            return;
        }

        string userMsg = inputField.text.Trim();
        AppendMessage("你", userMsg);
        inputField.text = "";
        StartCoroutine(RequestAI(userMsg));
    }

    IEnumerator RequestAI(string userMsg)
    {
        isWaiting = true;
        AppendMessage("AI", "思考中…");

        string json = BuildRequestJson(userMsg);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(apiUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return req.SendWebRequest();

            RemoveLastThinkingLine();

            if (req.result != UnityWebRequest.Result.Success)
            {
                AppendMessage("AI", "请求失败：" + req.error + "\n" + req.downloadHandler.text);
            }
            else
            {
                string reply = ExtractContent(req.downloadHandler.text);
                AppendMessage("AI", reply);
            }
        }

        isWaiting = false;
    }

    string BuildRequestJson(string userMsg)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"model\":\"").Append(EscapeJson(modelName)).Append("\",");
        sb.Append("\"messages\":[");
        sb.Append("{\"role\":\"system\",\"content\":\"").Append(EscapeJson(systemPrompt)).Append("\"},");
        sb.Append("{\"role\":\"user\",\"content\":\"").Append(EscapeJson(userMsg)).Append("\"}");
        sb.Append("],\"temperature\":0.7}");
        return sb.ToString();
    }

    string ExtractContent(string json)
    {
        // 简单字符串解析，避免引入 JSON 库
        string marker = "\"content\":\"";
        int idx = json.IndexOf(marker);
        if (idx < 0) return "AI 没有返回可识别内容。";

        int start = idx + marker.Length;
        int end = json.IndexOf("\"", start);
        if (end < 0) return "AI 返回解析失败。";

        string raw = json.Substring(start, end - start);
        return UnescapeJson(raw);
    }

    void AppendMessage(string sender, string msg)
    {
        history.AppendLine($"[{sender}] {msg}\n");
        if (chatText != null)
        {
            chatText.text = history.ToString();
            Canvas.ForceUpdateCanvases();
        }
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

    void RemoveLastThinkingLine()
    {
        string text = history.ToString();
        int idx = text.LastIndexOf("[AI] 思考中…");
        if (idx >= 0)
        {
            history.Clear();
            history.Append(text.Substring(0, idx));
        }
        if (chatText != null) chatText.text = history.ToString();
    }

    string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    string UnescapeJson(string s)
    {
        return s.Replace("\\n", "\n")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
    }
}

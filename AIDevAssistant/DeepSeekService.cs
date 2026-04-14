using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class DeepSeekService
{
    public static async Task<string> CallChatCompletionAsync(
        string apiUrl,
        string apiKey,
        string modelName,
        string prompt)
    {
        ChatCompletionRequest requestData = new ChatCompletionRequest
        {
            model = modelName,
            temperature = 0.2f,
            stream = false,
            messages = new ChatMessage[]
            {
                new ChatMessage
                {
                    role = "system",
                    content = "You are a Unity client development assistant. Return only code files in the required file-block format. Do not explain. Do not use markdown fences."
                },
                new ChatMessage
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 180;

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                string errorText = request.downloadHandler != null ? request.downloadHandler.text : "";
                throw new Exception($"{request.error}\n{errorText}");
            }

            string responseText = request.downloadHandler.text;
            ChatCompletionResponse responseData = JsonUtility.FromJson<ChatCompletionResponse>(responseText);

            if (responseData == null)
            {
                throw new Exception("响应解析失败：responseData 为空。\n" + responseText);
            }

            if (responseData.error != null && !string.IsNullOrEmpty(responseData.error.message))
            {
                throw new Exception("接口返回错误：\n" + responseData.error.message);
            }

            if (responseData.choices == null || responseData.choices.Length == 0)
            {
                throw new Exception("响应解析失败：choices 为空。\n" + responseText);
            }

            if (responseData.choices[0].message == null)
            {
                throw new Exception("响应解析失败：message 为空。\n" + responseText);
            }

            if (string.IsNullOrWhiteSpace(responseData.choices[0].message.content))
            {
                throw new Exception("响应解析失败：content 为空。\n" + responseText);
            }

            return responseData.choices[0].message.content;
        }
    }

    public static string BuildPrompt(string userRequirement, string localTemplate)
    {
        return
    $@"你是一个 Unity 客户端开发辅助工具，任务是根据需求生成“可以直接落地使用”的 Unity C# 脚本代码。

【用户需求】
{userRequirement}

【已有本地模板】
{localTemplate}

【生成目标】
请基于用户需求和已有本地模板，生成完整、可用、尽量自洽的 Unity C# 代码。

【核心规则】
1. 优先生成“自洽”的结果，避免出现引用了未定义的自定义类型、脚本、组件或数据结构的情况。
2. 如果需求可以在一个 .cs 文件中合理完成，并且不会造成代码冗余，影响后续维护，请优先返回单文件实现，尽量不要为了拆分而拆分。
3. 如果完成需求必须依赖多个脚本文件，请自动一次性返回所有必要文件，不能遗漏任何被引用的自定义类型。
4. 不允许只返回主脚本却引用未定义的辅助脚本。例如如果代码中出现 ShopGoodsItem、BagItemView、TaskData 等自定义类型，那么必须同时给出这些类型对应的完整代码，或者改写实现以避免依赖它们。
5. 返回结果必须尽量能直接保存到 Unity 工程中使用，保持类名、字段命名和方法结构清晰自然。
6. 保留或合理补充常见的 Unity 面板结构，例如 Init、RefreshView、OnClickClose、BindEvents 等方法。
7. 可以补充必要的 using、序列化字段、UI 引用、简单数据结构和辅助类，但不要加入与需求无关的复杂系统。
8. 不要输出解释，不要输出思路分析，不要输出 Markdown 代码块，不要输出多余文字。

【输出格式要求】
1. 无论最终是单文件还是多文件，都必须严格使用下面格式返回：
===FILE: 文件名.cs===
这里放该文件的完整 C# 代码

2. 如果只有一个文件，也必须使用同样格式返回。
3. 如果有多个文件，按依赖主次顺序返回，通常先返回主面板脚本，再返回其依赖的辅助脚本。

【风格要求】
1. 保持 Unity C# 风格，适合客户端/UI 开发场景。
2. 代码要尽量简洁、自然、可读，不要故意堆砌复杂设计。
3. 以“能生成后直接保存、再由开发者继续补业务逻辑”为目标，而不是生成过度抽象的大型框架。
4.可以保留一些必要的中文注释，增加代码的可读性。";
    }

    //用来“去掉 AI 返回结果外面包着的 Markdown 代码块围栏”
    public static string StripCodeFence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        string result = text.Trim();

        if (result.StartsWith("```csharp", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Substring(9).Trim();
        }
        else if (result.StartsWith("```cs", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Substring(5).Trim();
        }
        else if (result.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Substring(3).Trim();
        }

        if (result.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            result = result.Substring(0, result.Length - 3).Trim();
        }

        return result;
    }

    [Serializable]
    public class ChatCompletionRequest
    {
        public string model;
        public float temperature;
        public bool stream;
        public ChatMessage[] messages;
    }

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ChatCompletionResponse
    {
        public ChatChoice[] choices;
        public ChatError error;
    }

    [Serializable]
    public class ChatChoice
    {
        public ChatMessage message;
    }

    [Serializable]
    public class ChatError
    {
        public string message;
    }
}
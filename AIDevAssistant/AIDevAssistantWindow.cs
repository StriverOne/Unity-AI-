using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class AIDevAssistantWindow : EditorWindow
{
    private string requirement = "生成一个背包面板脚本，需要物品列表、关闭按钮和刷新接口。";
    private string output = "";
    // 输入区和输出区分别维护各自的滚动位置
    private Vector2 requirementScrollPos;
    private Vector2 outputScrollPos;

    private string apiUrl;
    private string apiKey;
    private string modelName;

    private string scriptFileName;
    private string saveFolder;

    private bool isRequesting = false;
    private string statusMessage = "准备就绪。";

    // 多文件解析结果
    private List<GeneratedCodeFile> parsedFiles = new List<GeneratedCodeFile>();
    private Vector2 parsedFilesScrollPos;
    private int selectedFileIndex = -1;

    // 保存最近一次 AI 的原始结果，方便重新解析
    private string lastRawAiResponse = "";

    //可视化折叠功能
    private bool showApiSection = false;
    private bool showSaveSection = false;
    private bool showParsedFilesSection = true;

    [MenuItem("Tools/AI Dev Assistant")]
    public static void ShowWindow()
    {
        AIDevAssistantWindow window = GetWindow<AIDevAssistantWindow>("AI Dev Assistant");
        window.minSize = new Vector2(720, 650);
    }

    private void OnEnable()
    {
        AIDevAssistantPrefs.Load(
            out apiUrl,
            out apiKey,
            out modelName,
            out saveFolder,
            out scriptFileName);
    }

    private void OnDisable()
    {
        SavePrefs();
    }

    private void SavePrefs()
    {
        AIDevAssistantPrefs.Save(
            apiUrl,
            apiKey,
            modelName,
            saveFolder,
            scriptFileName);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Unity 编辑器 AI 开发辅助工具", EditorStyles.boldLabel);
        GUILayout.Space(8);

        DrawApiSection();

        GUILayout.Space(10);
        DrawRequirementSection();

        GUILayout.Space(10);
        DrawSaveSection();

        GUILayout.Space(10);
        DrawActionButtonsSection();

        GUILayout.Space(10);
        DrawParsedFilesSection();

        GUILayout.Space(10);
        DrawStatusSection();

        GUILayout.Space(10);
        DrawOutputSection();
    }

    // 绘制 API 配置区域
    private void DrawApiSection()
    {
        showApiSection = EditorGUILayout.Foldout(showApiSection, "API 配置", true);
        if (!showApiSection)
        {
            return;
        }
        apiUrl = EditorGUILayout.TextField("API URL", apiUrl);
        modelName = EditorGUILayout.TextField("Model", modelName);
        apiKey = EditorGUILayout.PasswordField("API Key", apiKey);

        GUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("保存配置", GUILayout.Height(24)))
        {
            SavePrefs();
            statusMessage = "已保存本地配置。";
        }

        if (GUILayout.Button("测试连接", GUILayout.Height(24)))
        {
            TestConnection();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "DeepSeek 默认配置：\n" +
            "API URL = https://api.deepseek.com/chat/completions\n" +
            "Model = deepseek-chat",
            MessageType.None
        );
    }

    // 绘制需求输入区域
    private void DrawRequirementSection()
    {
        EditorGUILayout.LabelField("需求输入", EditorStyles.boldLabel);
        // 给输入区增加滚动能力，避免长需求无法完整查看
        requirementScrollPos = EditorGUILayout.BeginScrollView(requirementScrollPos,GUILayout.Height(140));
        requirement = EditorGUILayout.TextArea(requirement,GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    // 绘制保存设置区域
    private void DrawSaveSection()
    {
        showSaveSection = EditorGUILayout.Foldout(showSaveSection, "保存脚本到 Unity 工程", true);

        if (!showSaveSection)
        {
            return;
        }

        scriptFileName = EditorGUILayout.TextField("脚本名", scriptFileName);
        saveFolder = EditorGUILayout.TextField("保存目录", saveFolder);

        EditorGUILayout.HelpBox(
            "示例保存目录：Assets/Scripts/Generated\n" +
            "保存时会自动创建目录、刷新 AssetDatabase，并尝试在 Project 面板中定位脚本。",
            MessageType.None
        );
    }

    // 绘制操作按钮区域
    private void DrawActionButtonsSection()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("生成本地模板", GUILayout.Height(32)))
        {
            output = LocalTemplateGenerator.Generate(requirement);
            ClearParsedFilesState();
            statusMessage = "已生成本地模板。";
        }

        EditorGUI.BeginDisabledGroup(!CanRequestAI());
        if (GUILayout.Button("AI 优化输出（DeepSeek）", GUILayout.Height(32)))
        {
            GenerateWithAI();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(isRequesting);
        if (GUILayout.Button("保存为 .cs 文件", GUILayout.Height(32)))
        {
            SaveOutputToScriptFile();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("清空输出", GUILayout.Height(32)))
        {
            ClearOutput();
        }

        EditorGUILayout.EndHorizontal();
    }


    // 绘制状态区域
    private void DrawStatusSection()
    {
        EditorGUILayout.LabelField("当前状态", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(statusMessage, isRequesting ? MessageType.Warning : MessageType.Info);
    }

    // 绘制输出区域
    private void DrawOutputSection()
    {
        EditorGUILayout.LabelField("输出结果", EditorStyles.boldLabel);
        outputScrollPos = EditorGUILayout.BeginScrollView(outputScrollPos, GUILayout.ExpandHeight(true));
        output = EditorGUILayout.TextArea(output, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private bool CanRequestAI()
    {
        return !isRequesting
               && !string.IsNullOrWhiteSpace(apiUrl)
               && !string.IsNullOrWhiteSpace(apiKey)
               && !string.IsNullOrWhiteSpace(modelName);
    }

    private async void TestConnection()
    {
        if (!CanRequestAI())
        {
            statusMessage = "测试连接失败：请先填写 API URL、API Key 和 Model。";
            return;
        }

        SavePrefs();

        isRequesting = true;
        statusMessage = "正在测试连接，请稍候...";
        Repaint();

        try
        {
            string result = await DeepSeekService.CallChatCompletionAsync(
                apiUrl,
                apiKey,
                modelName,
                "请只回复：连接成功");

            output = DeepSeekService.StripCodeFence(result);
            ClearParsedFilesState();
            statusMessage = "连接测试成功。";
        }
        catch (Exception ex)
        {
            statusMessage = "连接测试失败。";
            output = $"[ERROR]\n{ex.Message}";
        }
        finally
        {
            isRequesting = false;
            Repaint();
        }
    }

    private async void GenerateWithAI()
    {
        if (string.IsNullOrWhiteSpace(requirement))
        {
            statusMessage = "需求不能为空。";
            return;
        }

        if (!CanRequestAI())
        {
            statusMessage = "请先完整填写 API URL、API Key 和 Model。";
            return;
        }

        SavePrefs();

        isRequesting = true;
        statusMessage = "正在请求 AI，请稍候...";
        Repaint();

        try
        {
            string localTemplate = LocalTemplateGenerator.Generate(requirement);
            string prompt = DeepSeekService.BuildPrompt(requirement, localTemplate);

            string aiResult = await DeepSeekService.CallChatCompletionAsync(
                apiUrl,
                apiKey,
                modelName,
                prompt);

            lastRawAiResponse = DeepSeekService.StripCodeFence(aiResult);
            parsedFiles = MultiFileCodeParser.Parse(lastRawAiResponse);

            if (parsedFiles.Count > 0)
            {
                selectedFileIndex = 0;
                ApplySelectedParsedFile();
                statusMessage = $"AI 生成完成，已识别 {parsedFiles.Count} 个文件。";
            }
            else
            {
                output = lastRawAiResponse;
                selectedFileIndex = -1;
                statusMessage = "AI 生成完成，但未识别到文件块格式。";
            }

        }
        catch (Exception ex)
        {
            statusMessage = "AI 请求失败。";
            output = $"[ERROR]\n{ex.Message}";
        }
        finally
        {
            isRequesting = false;
            Repaint();
        }
    }

    private void SaveOutputToScriptFile()
    {
        string message;
        bool success = ScriptFileSaver.SaveOutputToScriptFile(
            isRequesting,
            output,
            scriptFileName,
            saveFolder,
            out message,
            out string savedCode,
            out string sanitizedScriptName,
            out string normalizedFolder);

        statusMessage = message;

        if (success)
        {
            output = savedCode;
            scriptFileName = sanitizedScriptName;
            saveFolder = normalizedFolder;
            SavePrefs();
        }
    }

    private void ClearOutput()
    {
        output = "";
        ClearParsedFilesState();
        statusMessage = "已清空输出。";
        
        GUI.FocusControl(null);
        GUIUtility.keyboardControl = 0;
        Repaint();
    }

    private void DrawParsedFilesSection()
    {
        showParsedFilesSection = EditorGUILayout.Foldout(showParsedFilesSection, "解析到的文件", true);

        if (!showParsedFilesSection)
        {
            return;
        }

        if (parsedFiles == null || parsedFiles.Count == 0)
        {
            EditorGUILayout.HelpBox("当前还没有解析到文件块。执行 AI 优化模板后，如果返回了文件块格式，会在这里显示。", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField($"文件数量：{parsedFiles.Count}");

        parsedFilesScrollPos = EditorGUILayout.BeginScrollView(parsedFilesScrollPos, GUILayout.Height(60));

        for (int i = 0; i < parsedFiles.Count; i++)
        {
            GeneratedCodeFile file = parsedFiles[i];

            EditorGUILayout.BeginHorizontal();

            bool isSelected = selectedFileIndex == i;
            if (GUILayout.Toggle(isSelected, "", GUILayout.Width(20)))
            {
                selectedFileIndex = i;
                ApplySelectedParsedFile();
            }

            EditorGUILayout.LabelField(file.FileName);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(isRequesting || parsedFiles.Count == 0);
        if (GUILayout.Button("保存全部文件", GUILayout.Height(24)))
        {
            SaveAllParsedFiles();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("清空解析结果", GUILayout.Height(24)))
        {
            ClearParsedFilesState();
            statusMessage = "已清空解析结果。";
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ClearParsedFilesState()
    {
        parsedFiles.Clear();
        selectedFileIndex = -1;
        lastRawAiResponse = "";
    }

    //把当前选中的文件内容同步到输出区。
    private void ApplySelectedParsedFile()
    {
        if (selectedFileIndex < 0 || selectedFileIndex >= parsedFiles.Count)
        {
            return;
        }

        GeneratedCodeFile file = parsedFiles[selectedFileIndex];
        output = file.Content;
        scriptFileName = Path.GetFileNameWithoutExtension(file.FileName);
    }

    private void SaveAllParsedFiles()
    {
        if (parsedFiles == null || parsedFiles.Count == 0)
        {
            statusMessage = "当前没有可保存的文件。";
            return;
        }

        int successCount = 0;
        string lastMessage = "";

        for (int i = 0; i < parsedFiles.Count; i++)
        {
            GeneratedCodeFile file = parsedFiles[i];

            string message;
            bool success = ScriptFileSaver.SaveOutputToScriptFile(
                false,
                file.Content,
                Path.GetFileNameWithoutExtension(file.FileName),
                saveFolder,
                out message,
                out string savedCode,
                out string sanitizedScriptName,
                out string normalizedFolder);

            lastMessage = message;

            if (success)
            {
                successCount++;
                parsedFiles[i].Content = savedCode;
                saveFolder = normalizedFolder;
            }
        }

        SavePrefs();

        if (successCount == parsedFiles.Count)
        {
            statusMessage = $"已成功保存全部 {successCount} 个文件。";
        }
        else
        {
            statusMessage = $"部分保存完成：成功 {successCount}/{parsedFiles.Count}。最后结果：{lastMessage}";
        }
    }
}
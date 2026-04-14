using UnityEditor;

public static class AIDevAssistantPrefs
{
    private const string ApiUrlPrefKey = "AI_DEV_TOOL_API_URL";
    private const string ApiKeyPrefKey = "AI_DEV_TOOL_API_KEY";
    private const string ModelPrefKey = "AI_DEV_TOOL_MODEL";
    private const string SaveFolderPrefKey = "AI_DEV_TOOL_SAVE_FOLDER";
    private const string ScriptNamePrefKey = "AI_DEV_TOOL_SCRIPT_NAME";

    private const string DefaultApiUrl = "https://api.deepseek.com/chat/completions";
    private const string DefaultModelName = "deepseek-chat";
    private const string DefaultSaveFolder = "Assets/Scripts/Generated";
    private const string DefaultScriptName = "GeneratedPanel";

    public static void Load(
        out string apiUrl,
        out string apiKey,
        out string modelName,
        out string saveFolder,
        out string scriptFileName)
    {
        apiUrl = EditorPrefs.GetString(ApiUrlPrefKey, DefaultApiUrl);
        apiKey = EditorPrefs.GetString(ApiKeyPrefKey, "");
        modelName = EditorPrefs.GetString(ModelPrefKey, DefaultModelName);
        saveFolder = EditorPrefs.GetString(SaveFolderPrefKey, DefaultSaveFolder);
        scriptFileName = EditorPrefs.GetString(ScriptNamePrefKey, DefaultScriptName);
    }

    public static void Save(
        string apiUrl,
        string apiKey,
        string modelName,
        string saveFolder,
        string scriptFileName)
    {
        EditorPrefs.SetString(ApiUrlPrefKey, apiUrl ?? "");
        EditorPrefs.SetString(ApiKeyPrefKey, apiKey ?? "");
        EditorPrefs.SetString(ModelPrefKey, modelName ?? "");
        EditorPrefs.SetString(SaveFolderPrefKey, saveFolder ?? DefaultSaveFolder);
        EditorPrefs.SetString(ScriptNamePrefKey, scriptFileName ?? DefaultScriptName);
    }
}
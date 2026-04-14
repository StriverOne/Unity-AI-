using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ScriptFileSaver
{
    private const string DefaultSaveFolder = "Assets/Scripts/Generated";

    public static bool SaveOutputToScriptFile(
        bool isRequesting,
        string output,
        string scriptFileName,
        string saveFolder,
        out string message,
        out string savedCode,
        out string sanitizedScriptName,
        out string normalizedFolder)
    {
        message = "";
        savedCode = output;
        sanitizedScriptName = scriptFileName;
        normalizedFolder = saveFolder;

        if (isRequesting)
        {
            message = "AI 请求进行中，暂时不能保存。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            message = "输出为空，不能保存。";
            EditorUtility.DisplayDialog("保存失败", "输出为空，不能保存。", "确定");
            return false;
        }

        string code = DeepSeekService.StripCodeFence(output).Trim();

        if (!CanSaveCurrentOutput(code))
        {
            message = "当前输出不是可保存的 C# 脚本。";
            EditorUtility.DisplayDialog("保存失败", "当前输出为空、是错误信息，或看起来不是有效的 C# 脚本。", "确定");
            return false;
        }

        string finalScriptName = SanitizeScriptFileName(scriptFileName);
        if (string.IsNullOrWhiteSpace(finalScriptName))
        {
            message = "脚本名不能为空。";
            EditorUtility.DisplayDialog("保存失败", "脚本名不能为空。", "确定");
            return false;
        }

        if (finalScriptName != Path.GetFileNameWithoutExtension(scriptFileName).Trim())
        {
            bool continueSave = EditorUtility.DisplayDialog(
                "脚本名已修正",
                $"检测到非法文件名字符，已自动修正为：{finalScriptName}.cs\n\n是否继续保存？",
                "继续",
                "取消");

            if (!continueSave)
            {
                message = "已取消保存。";
                return false;
            }
        }

        string finalFolder = NormalizeSaveFolder(saveFolder);
        if (!IsValidUnityAssetFolder(finalFolder))
        {
            message = "保存目录不合法。";
            EditorUtility.DisplayDialog(
                "保存失败",
                "保存目录不合法。\n请使用 Unity 工程内路径，例如：Assets/Scripts/Generated",
                "确定");
            return false;
        }

        code = EnsureClassNameMatchesFileName(code, finalScriptName);

        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullFolderPath = Path.Combine(projectRoot, finalFolder.Replace("/", Path.DirectorySeparatorChar.ToString()));
            string fullFilePath = Path.Combine(fullFolderPath, finalScriptName + ".cs");
            string assetPath = finalFolder + "/" + finalScriptName + ".cs";

            if (!Directory.Exists(fullFolderPath))
            {
                Directory.CreateDirectory(fullFolderPath);
            }

            if (File.Exists(fullFilePath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "文件已存在",
                    $"检测到同名脚本已存在：\n{assetPath}\n\n是否覆盖？",
                    "覆盖",
                    "取消");

                if (!overwrite)
                {
                    message = "已取消覆盖。";
                    return false;
                }
            }

            File.WriteAllText(fullFilePath, code, new UTF8Encoding(false));
            AssetDatabase.Refresh();

            MonoScript scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (scriptAsset != null)
            {
                Selection.activeObject = scriptAsset;
                EditorGUIUtility.PingObject(scriptAsset);
            }

            savedCode = code;
            sanitizedScriptName = finalScriptName;
            normalizedFolder = finalFolder;
            message = $"保存成功：{assetPath}";
            EditorUtility.DisplayDialog("保存成功", $"脚本已保存到：\n{assetPath}", "确定");
            return true;
        }
        catch (Exception ex)
        {
            message = "保存失败。";
            EditorUtility.DisplayDialog("保存失败", ex.Message, "确定");
            return false;
        }
    }

    private static bool CanSaveCurrentOutput(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (code.StartsWith("[ERROR]", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string lower = code.ToLowerInvariant();
        if ((lower.Contains("请求失败") || lower.Contains("连接失败") || lower.Contains("响应解析失败") || lower.Contains("exception"))
            && !LooksLikeCSharpCode(code))
        {
            return false;
        }

        return LooksLikeCSharpCode(code);
    }

    private static bool LooksLikeCSharpCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        bool hasClass = Regex.IsMatch(code, @"\bclass\s+[A-Za-z_][A-Za-z0-9_]*");
        bool hasBrace = code.Contains("{") && code.Contains("}");
        bool hasUsingOrNamespace = code.Contains("using ") || code.Contains("namespace ");
        bool hasCommonUnityBaseType =
            code.Contains("MonoBehaviour") ||
            code.Contains("EditorWindow") ||
            code.Contains("ScriptableObject");

        return hasClass && hasBrace && (hasUsingOrNamespace || hasCommonUnityBaseType);
    }

    private static string NormalizeSaveFolder(string folder)
    {
        string result = string.IsNullOrWhiteSpace(folder) ? DefaultSaveFolder : folder.Trim();
        result = result.Replace("\\", "/");

        while (result.Contains("//"))
        {
            result = result.Replace("//", "/");
        }

        result = result.TrimEnd('/');

        if (Path.IsPathRooted(result))
        {
            return string.Empty;
        }

        if (!result.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
        {
            result = "Assets/" + result.TrimStart('/');
        }

        return result;
    }

    private static bool IsValidUnityAssetFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return false;
        }

        string normalized = folder.Replace("\\", "/");

        if (!normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (normalized.Contains("../") || normalized.EndsWith("/..") || normalized == "..")
        {
            return false;
        }

        return true;
    }

    private static string SanitizeScriptFileName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string name = Path.GetFileNameWithoutExtension(rawName.Trim());
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (Array.IndexOf(invalidChars, c) >= 0)
            {
                sb.Append('_');
            }
            else if (char.IsWhiteSpace(c) || c == '-')
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(c);
            }
        }

        string result = sb.ToString().Trim('_');
        if (string.IsNullOrWhiteSpace(result))
        {
            return string.Empty;
        }

        StringBuilder finalName = new StringBuilder();

        if (!IsValidIdentifierStart(result[0]))
        {
            finalName.Append('_');
        }

        for (int i = 0; i < result.Length; i++)
        {
            char c = result[i];
            finalName.Append(IsValidIdentifierPart(c) ? c : '_');
        }

        return finalName.ToString();
    }

    private static bool IsValidIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsValidIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private static string EnsureClassNameMatchesFileName(string code, string fileNameWithoutExtension)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            return code;
        }

        Match match = Regex.Match(code, @"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)");
        if (!match.Success || match.Groups.Count < 2)
        {
            return code;
        }

        Group classNameGroup = match.Groups[1];
        string oldClassName = classNameGroup.Value;

        if (oldClassName == fileNameWithoutExtension)
        {
            return code;
        }

        return code.Substring(0, classNameGroup.Index)
             + fileNameWithoutExtension
             + code.Substring(classNameGroup.Index + classNameGroup.Length);
    }
}
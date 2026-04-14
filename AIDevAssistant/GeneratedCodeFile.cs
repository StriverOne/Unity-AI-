using System;

// 表示 AI 解析出来的一个代码文件
[Serializable]
public class GeneratedCodeFile
{
    public string FileName;
    public string Content;

    public GeneratedCodeFile(string fileName, string content)
    {
        FileName = fileName;
        Content = content;
    }
}
namespace ReLog.RoslynAnalyzer;

public struct AffectedFile
{
    public string FilePath;
    public int LineNumber;
    public string LineText;

    public AffectedFile(string filePath, int lineNumber, string lineText)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        LineText = lineText;
    }
}
namespace WinUIXamlViewer.Models;

public class WorkEntity
{
    public WorkEntity(string title, bool isFile, string path, string text)
    {
        Title = title;
        IsFile = isFile;
        Path = path;
        Text = text;
    }

    public string Title { get; set; }
    public bool IsFile { get; set; }
    public string Path { get; set; }
    public string Text { get; set; }

    public string Json { get; set; }
}

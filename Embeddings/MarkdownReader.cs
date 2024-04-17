public static class MarkdownReader
{
    public static IEnumerable<(string,string)> ReadAll()
    {
        //list all markdown files in the current directory
        var files = System.IO.Directory.GetFiles(Directory.GetCurrentDirectory(), "*.md");
        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            yield return (filename,ReadFile(file));
        }
    }

    private static  string ReadFile(string path)
    {
        return File.ReadAllText(path);
    }
}
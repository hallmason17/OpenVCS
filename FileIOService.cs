public class FileIOService : IFileIOService
{
    public FileIOService()
    {
    }

    public bool SaveIndexFile(Dictionary<string, string> indexDict)
    {
        try
        {
            string fileContents = string.Join(Environment.NewLine, indexDict.Select(kv => kv.Key + "=" + kv.Value));
            using (StreamWriter fs = new StreamWriter(Path.Combine(Constants.repoDir, "index")))
                fs.Write(fileContents);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
        return true;
    }


    public Dictionary<string, string> ReadIndexFile()
    {
        string[] fileContents = File.ReadAllLines(Path.Combine(Constants.repoDir, "index"));
        fileContents.ToList().ForEach(i => Console.WriteLine(i));
        var dict = new Dictionary<string, string>();
        for (int i = 0; i < fileContents.Length; i++)
        {
            string[] subs = fileContents[i].Split("=");
            dict.Add(subs[0], subs[1]);
        }
        return dict;
    }
}

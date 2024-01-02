public class Tree
{
    private readonly ICryptographyService cryptographyService;
    private readonly ICompressionService compressionService;
    public Tree(ICryptographyService cryptographyService, ICompressionService compressionService)
    {

        this.cryptographyService = cryptographyService;
        this.compressionService = compressionService;
    }
    public List<Blob>? blobs;
    public string? sha1;
    public string? fileName;
    public byte[]? fileContents;
    public void SaveTree(string stagingDir, string objectsDir)
    {

        string treeDirName = Path.Combine(stagingDir, cryptographyService.GetSha1(this.blobs[0].sha1) + "tree");
        var treeFileContents = new List<string>();
        for (int i = 0; i < this.blobs.Count(); i++)
        {
            treeFileContents.Add(this.blobs[i].filePermissions + "\t" + this.blobs[i].type + "\t" + this.blobs[i].sha1 + "\t" + this.blobs[i].origFileName + "\n");
        }
        File.WriteAllText(treeDirName, String.Join("", treeFileContents));
        string origTreeContents = File.ReadAllText(treeDirName);
        this.sha1 = cryptographyService.GetSha1(origTreeContents);
        if (!Directory.Exists(Path.Combine(objectsDir, this.sha1.Substring(0, 2))))
        {
            Directory.CreateDirectory(Path.Combine(objectsDir, this.sha1.Substring(0, 2)));
        }
        this.fileName = this.sha1.Substring(1, 38);
        this.fileContents = compressionService.Compress(origTreeContents);
        if (!File.Exists(Path.Combine(objectsDir, this.sha1.Substring(0, 2), this.fileName)))
        {
            File.WriteAllBytes(Path.Combine(objectsDir, this.sha1.Substring(0, 2), this.fileName), this.fileContents);
            Console.WriteLine(this.fileName);
            Console.WriteLine(compressionService.Decompress(this.fileContents));
        }
        File.Delete(treeDirName);
    }
}

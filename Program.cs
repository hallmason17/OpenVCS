using Cocona;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();

builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<ICompressionService, CompressionService>();

var app = builder.Build();

var cryptoService = new CryptographyService();
var compressionService = new CompressionService();
var fileIOService = new FileIOService();
var dirs = new List<string>();

app.AddCommand("dirs", () =>
{
    GetSubDirs(Directory.GetDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories));
    Console.WriteLine(String.Join("\n", dirs));
});

void GetSubDirs(string[] path)
{
    foreach (string subdir in path)
    {
        if (!subdir.Contains(".git") && !subdir.Contains(".open-vcs"))
        {
            dirs.Add(subdir);
        }
    }
}

app.AddCommand("init", () =>
{
    if (!Directory.Exists(Constants.repoDir))
    {
        Directory.CreateDirectory(Constants.repoDir);
        Directory.CreateDirectory(Constants.stagingDir);
        Directory.CreateDirectory(Constants.objectsDir);
    }

    Console.WriteLine(Constants.repoDir);
});

app.AddCommand("add", ([Argument] string[] filesToAdd) =>
{
    var files = new List<string>();
    if (filesToAdd.Length == 0)
    {
        Console.WriteLine("Nothing to add");
    }
    else if (filesToAdd.Length == 1 && filesToAdd.Contains("."))
    {
        foreach (var file in Directory.GetFiles(Constants.currentDir, "*", SearchOption.AllDirectories))
        {
            if (!file.Contains(".git") && !file.Contains(".open-vcs"))
            {
                Console.WriteLine(file);
                files.Add(file);
            }
        }
    }
    else
    {
        foreach (var file in filesToAdd)
        {
            string fileToCheck = String.Concat(Constants.currentDir, "/", file);
            if (!File.Exists(fileToCheck))
            {
                Console.WriteLine("File does not exist");
            }
            else if (File.Exists(fileToCheck))
            {
                files.Add(String.Concat(Constants.currentDir, "/", file));
            }
        }
    }
    var indexDict = new Dictionary<string, string>();
    if (File.Exists(Path.Combine(Constants.repoDir, "index")))
    {
        indexDict = fileIOService.ReadIndexFile();
    }
    foreach (var file in files)
    {
        string fileName = file.Substring(Constants.currentDir.Length + 1);
        long fileSize = file.Length;
        string fileContents = File.ReadAllText(file);
        string fileSha = cryptoService.GetSha1(fileContents);
        if (!indexDict.ContainsKey(fileName) || indexDict[fileName] != fileSha)
        {
            Blob blob = new Blob
            {
                filePermissions = "100644",
                type = "blob",
                origFileName = fileName,
                sha1 = fileSha,
                fileName = fileSha == string.Empty ? string.Empty : fileSha.Substring(2, 38),
                fileContents = compressionService.Compress($"{fileContents}")
            };

            if (fileSha != string.Empty)
            {
                string dirName = blob.sha1.Substring(0, 2);

                if (!Directory.Exists(Path.Combine(Constants.objectsDir, dirName)))
                {
                    Directory.CreateDirectory(Path.Combine(Constants.objectsDir, dirName));
                }

                indexDict[blob.origFileName] = blob.sha1;

                var fullPath = Path.Combine(Constants.objectsDir, dirName, blob.fileName);

                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"Added file: {blob.sha1}");
                    File.WriteAllBytes(fullPath, blob.fileContents);
                }
            }
        }
    }

    foreach (string key in indexDict.Keys.ToList())
    {
        if (!files.Contains(Path.Combine(Constants.currentDir, key)))
        {
            Console.WriteLine($"Removed file: {key}");
            indexDict.Remove(key);
        }
    }
    fileIOService.SaveIndexFile(indexDict);
});

app.AddCommand("commit", () =>
{
    var tree = new Tree(cryptoService, compressionService);
    Dictionary<string, string> index = fileIOService.ReadIndexFile();
    var treeFileContents = new List<string>();
    foreach (var kvp in index)
    {
        treeFileContents.Add("100644" + " blob" + kvp.Key + "/t" + kvp.Value);
    }
    tree.sha1 = cryptoService.GetSha1(String.Join(Environment.NewLine, treeFileContents));
    string dirName = tree.sha1.Substring(0, 2);
    if (!Directory.Exists(Path.Combine(Constants.objectsDir, dirName)))
    {
        Directory.CreateDirectory(Path.Combine(Constants.objectsDir, dirName));
    }
    tree.fileName = tree.sha1.Substring(1, 38);
    tree.fileContents = compressionService.Compress(String.Join(Environment.NewLine, treeFileContents));
    var fullPath = Path.Combine(Constants.objectsDir, dirName, tree.fileName);

    if (!File.Exists(fullPath))
    {
        Console.WriteLine($"Added file: {tree.sha1}");
        File.WriteAllBytes(fullPath, tree.fileContents);
    }

    var commit = new Commit();
});

app.Run();


using Cocona;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

var builder = CoconaApp.CreateBuilder();

builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<ICompressionService, CompressionService>();

var app = builder.Build();

var crypto = new CryptographyService();
var compressor = new CompressionService();
var dirs = new List<string>();
string appName = "open-vcs";
string appDir = "." + appName;
string currentDir = Directory.GetCurrentDirectory();
string[] files = Directory.GetFiles(currentDir);
string repoDir = currentDir + "/" + appDir;
string stagingDir = repoDir + "/staging/";
string objectsDir = repoDir + "/objects/";
var indexDict = new Dictionary<string, byte[]>();

app.AddCommand("dirs", () =>
{
    GetSubDirs(Directory.GetDirectories(Directory.GetCurrentDirectory()));
    Console.WriteLine(String.Join("\n", dirs));
});

void GetSubDirs(string[] path)
{
    foreach (string subdir in path)
    {
        if (Directory.GetDirectories(subdir).Length > 0 && !subdir.Contains(".git") && !subdir.Contains(".open-vcs"))
        {
            dirs.Add(subdir);
            GetSubDirs(Directory.GetDirectories(subdir));
        }
    }
}

app.AddCommand("init", () =>
{
    if (!Directory.Exists(repoDir))
    {
        Directory.CreateDirectory(repoDir);
        Directory.CreateDirectory(stagingDir);
        Directory.CreateDirectory(objectsDir);
    }

    Console.WriteLine(repoDir);
});


app.AddSubCommand("add", x =>
{
    x.AddCommand(".", () =>
    {
        foreach (var file in files)
        {
            Console.WriteLine(file);
            File.Copy(file, Path.Combine(stagingDir, file.Substring(currentDir.Length + 1)));
        }
        var addedFiles = Directory.GetFiles(stagingDir);
        if (addedFiles.Length == 0)
        {
            Console.WriteLine("There are no files to add.");
            return;
        }
        foreach (var file in addedFiles)
        {
            if (!file.Substring(stagingDir.Length).Contains("tree"))
            {
                string fileContents = File.ReadAllText(file);
                string sha1 = crypto.GetSha1(fileContents);

                Blob blob = new Blob
                {
                    filePermissions = "100644",
                    type = "blob",
                    origFileName = file.Substring(stagingDir.Length),
                    sha1 = sha1,
                    fileName = sha1.Substring(2, 38),
                    fileContents = compressor.Compress(fileContents)
                };

                string dirName = blob.sha1.Substring(0, 2);

                if (!Directory.Exists(Path.Combine(objectsDir, dirName)))
                {
                    Directory.CreateDirectory(Path.Combine(objectsDir, dirName));
                }

                indexDict[blob.origFileName] = compressor.Compress(blob.sha1);

                var fullPath = Path.Combine(objectsDir, dirName, blob.fileName);

                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"Added file: {blob.sha1}");
                    File.WriteAllBytes(fullPath, blob.fileContents);
                }
            }
            File.Delete(file);
        }
        foreach (KeyValuePair<string, byte[]> kvp in indexDict)
        {
            byte[] stringToConvert = Encoding.UTF8.GetBytes(kvp.Key + "=");
            byte[] bytesToWrite = new byte[stringToConvert.Length + kvp.Value.Length];
            System.Array.Copy(stringToConvert, 0, bytesToWrite, 0, stringToConvert.Length);
            System.Array.Copy(kvp.Value, 0, bytesToWrite, stringToConvert.Length, kvp.Value.Length);

            using (FileStream fs = new FileStream(Path.Combine(repoDir, "index"), File.Exists(Path.Combine(repoDir, "index")) ? FileMode.Append : FileMode.Create))
                fs.Write(bytesToWrite, 0, bytesToWrite.Length);
        }
    });
});

app.AddCommand("commit", () =>
{
    var addedFiles = Directory.GetFiles(stagingDir);
    if (addedFiles.Length == 0)
    {
        Console.WriteLine("There are no changes to commit.");
        return;
    }
    var commit = new Commit();
    var tree = new Tree(crypto, compressor);
    tree.blobs = new List<Blob>();

    foreach (var file in addedFiles)
    {
        if (!file.Substring(stagingDir.Length).Contains("tree"))
        {
            string fileContents = File.ReadAllText(file);
            string sha1 = crypto.GetSha1(fileContents);
            Blob blob = new Blob
            {
                filePermissions = "100644",
                type = "blob",
                origFileName = file.Substring(stagingDir.Length),
                sha1 = sha1,
                fileName = sha1.Substring(2, 38),
                fileContents = compressor.Compress(fileContents)
            };

            string dirName = blob.sha1.Substring(0, 2);

            if (!Directory.Exists(Path.Combine(objectsDir, dirName)))
            {
                Directory.CreateDirectory(Path.Combine(objectsDir, dirName));
            }

            var fullPath = Path.Combine(objectsDir, dirName, blob.fileName);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Added file: {blob.sha1}");
                File.WriteAllBytes(fullPath, blob.fileContents);
            }
            tree.blobs.Add(blob);
        }
        File.Delete(file);
    }
    tree.SaveTree(stagingDir, objectsDir);
});

app.Run();


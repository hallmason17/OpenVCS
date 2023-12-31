using Cocona;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();
builder.Services.AddSingleton<ICryptographyService, CryptographyService>();
builder.Services.AddSingleton<ICompressionService, CompressionService>();

var app = builder.Build();

string appName = "open-vcs";

string appDir = "." + appName;

var currentDir = Directory.GetCurrentDirectory();
var files = Directory.GetFiles(currentDir);
var repoDir = currentDir + "/" + appDir;
var stagingDir = repoDir + "/staging/";
var objectsDir = repoDir + "/objects/";


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
    });
});

app.AddCommand("commit", () =>
{
    var crypto = new CryptographyService();
    var compressor = new CompressionService();
    var addedFiles = Directory.GetFiles(stagingDir);
    foreach (var file in addedFiles)
    {
        string fileContents = File.ReadAllText(file);
        byte[] compressedText = compressor.Compress(fileContents);
        var origFileName = crypto.GetSha1(file.Substring(stagingDir.Length));
        string dirName = origFileName.Substring(0, 2);
        if (!Directory.Exists(Path.Combine(objectsDir, dirName)))
        {
            Directory.CreateDirectory(Path.Combine(objectsDir, dirName));
        }
        string fileName = origFileName.Substring(dirName.Length);
        var fullPath = Path.Combine(objectsDir, dirName, fileName);
        Console.WriteLine(fullPath);
        File.WriteAllBytes(fullPath, compressedText);
        Console.WriteLine("");
        Console.WriteLine(compressor.Decompress(File.ReadAllBytes(fullPath)));
    }
});

app.Run();



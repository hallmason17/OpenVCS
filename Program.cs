﻿using Cocona;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

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
    if (!Directory.Exists(Constants.repoDir))
    {
        Directory.CreateDirectory(Constants.repoDir);
        Directory.CreateDirectory(Constants.stagingDir);
        Directory.CreateDirectory(Constants.objectsDir);
    }

    Console.WriteLine(Constants.repoDir);
});


app.AddSubCommand("add", x =>
{
    x.AddCommand(".", () =>
    {
        string[] files = Directory.GetFiles(Constants.currentDir);
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
                    fileContents = compressionService.Compress($"blob {fileSize}{fileContents}")
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
});

app.AddCommand("commit", () =>
{
    var addedFiles = Directory.GetFiles(Constants.stagingDir);
    if (addedFiles.Length == 0)
    {
        Console.WriteLine("There are no changes to commit.");
        return;
    }
    var commit = new Commit();
    var tree = new Tree(cryptoService, compressionService);
    tree.blobs = new List<Blob>();

    tree.SaveTree(Constants.stagingDir, Constants.objectsDir);
});

app.Run();


public static class Constants
{

    public const string appName = "open-vcs";
    public const string appDir = "." + appName;
    public static string currentDir = Directory.GetCurrentDirectory();
    public static string repoDir = Directory.GetCurrentDirectory() + "/" + appDir;
    public static string stagingDir = Directory.GetCurrentDirectory() + "/" + appDir + "/staging/";
    public static string objectsDir = Directory.GetCurrentDirectory() + "/" + appDir + "/objects/";
}

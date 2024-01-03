public interface IFileIOService
{
    public bool SaveIndexFile(Dictionary<string, string> indexDict);
    public Dictionary<string, string> ReadIndexFile();
}

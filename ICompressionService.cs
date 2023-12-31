public interface ICompressionService
{
    byte[] Compress(string text);
    string Decompress(byte[] bytes);
}

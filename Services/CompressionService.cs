using System.Text;
using System.IO.Compression;
public class CompressionService : ICompressionService
{
    public CompressionService()
    {

    }
    public byte[] Compress(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using (var msi = new MemoryStream(bytes))
        using (var msj = new MemoryStream())
        {
            using (var gz = new GZipStream(msj, CompressionMode.Compress))
            {
                CopyTo(msi, gz);
            }

            return msj.ToArray();
        }
    }
    public string Decompress(byte[] bytes)
    {
        using (var msi = new MemoryStream(bytes))
        using (var msj = new MemoryStream())
        {
            using (var gz = new GZipStream(msi, CompressionMode.Decompress))
            {
                CopyTo(gz, msj);
            }

            return Encoding.UTF8.GetString(msj.ToArray());
        }
    }

    private void CopyTo(Stream src, Stream dest)
    {
        byte[] bytes = new byte[4096];
        int count;
        while ((count = src.Read(bytes, 0, bytes.Length)) != 0)
        {
            dest.Write(bytes, 0, count);
        }
    }
}

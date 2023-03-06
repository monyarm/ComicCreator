using System;
using System.IO;
using Slugify;

namespace ComicsCreator
{
    public static class Utils {
        public static readonly string[] imageExtensions = new[] { "*.png", "*.jpg", ".jpeg" };
        public static readonly string temp = GetTemporaryDirectory();
        public static string GetHash(string filename)
        {
            // using var md5 = System.Security.Cryptography.MD5.Create();
            var murmur = new FastHashes.MurmurHash128();
            using var stream = File.OpenRead(filename);
            // var hash = md5.ComputeHash(stream);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);
            var hash = BitConverter.ToString(murmur.ComputeHash(bytes)).Replace("-", "");
            return hash;
        }
        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
        public static readonly SlugHelper helper = new();
    }
}
using System;
using System.IO;
using System.Security.Cryptography;

namespace FSAR.Engine
{
    public class FileSystemEngine
    {
        public void CopyFile(string sourceFilePath, string destFilePath)
        {
            if (sourceFilePath == null) throw new ArgumentNullException(nameof(sourceFilePath));
            if (destFilePath == null) throw new ArgumentNullException(nameof(destFilePath));
            if (!File.Exists(sourceFilePath)) throw new FileNotFoundException();

            File.Copy(sourceFilePath, destFilePath, false);
        }

        public bool MergeMd5FileHash(string filePath1, string filePath2)
        {
            using (var md5 = MD5.Create())
            {
                string fileHash1;
                using (var fileStream = File.OpenRead(filePath1))
                {
                    var hash = md5.ComputeHash(fileStream);
                    fileHash1 = Convert.ToBase64String(hash);
                }

                string fileHash2;
                using (var fileStream = File.OpenRead(filePath2))
                {
                    var hash = md5.ComputeHash(fileStream);
                    fileHash2 = Convert.ToBase64String(hash);
                }

                if (string.IsNullOrWhiteSpace(fileHash1) || string.IsNullOrWhiteSpace(fileHash2))
                    return false;

                return string.Equals(fileHash1, fileHash2);
            }
        }
    }
}

using System.IO;
using System.Threading;

namespace FSAR.Engine
{
    public class DummyEngine
    {
        public string GetActualPath(string oldFilePath, string currentAttachmentsFolder)
        {
            var docId = Path.GetFileName(Path.GetDirectoryName(oldFilePath));
            var actualPath = Path.Combine(currentAttachmentsFolder, docId);
            return actualPath;
        }

        public void CopyFile(string filePath, string newActualPath)
        {
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
            }
        }

        public bool MergeMd5FileHash(string filePath, string newActualPath)
        {
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
            }
            return true;
        }
    }
}
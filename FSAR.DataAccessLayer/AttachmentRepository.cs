using System.Collections.Generic;
using System.Linq;
using Dapper;
using FSAR.DomainModel;

namespace FSAR.DataAccessLayer
{
    public class AttachmentRepository : BaseRepository
    {

        protected override string TableName => "DocFlow_Attachment_Attachment";

        private const string TableFields = "Id, DisplayFileName, FilePath";

        public AttachmentRepository()
        {
            CreateFields();
        }

        private void CreateFields()
        {
            var isExists = Session.ExecuteScalar<bool>($@"
IF EXISTS
  ( SELECT TOP(1) 1
   FROM sys.columns
   WHERE Name = N'OldFilePath'
     AND Object_ID = Object_ID(N'{TableName}')) BEGIN
SELECT 1 END ELSE BEGIN
SELECT 0 END");
            if (!isExists)
            {
                Session.Execute($@"ALTER TABLE {TableName} ADD OldFilePath nvarchar(MAX) NULL");
            }
        }

        public List<Attachment> GetAttachmentsNotInCurrentFolder(string currentAttachmentsFolder, int count = 10)
        {
            var attachments =
                Session.Query<Attachment>(
                    $@"
SELECT TOP {count} {TableFields}
FROM {TableName}
WHERE FilePath NOT LIKE '{currentAttachmentsFolder}%'").ToList();

            return attachments;
        }

        public int GetTotalCountAttachmentsNotInCurrentFolder(string currentAttachmentsFolder)
        {
            var count =
                Session.ExecuteScalar<int>(
                    $@"
SELECT COUNT(Id)
FROM {TableName}
WHERE FilePath NOT LIKE '{currentAttachmentsFolder}%'");

            return count;
        }

        public void Update(Attachment attachment)
        {
            Session.Query<Attachment>(
                $@"
UPDATE {TableName}
SET FilePath = @FilePath,
    OldFilePath = @OldFilePath
WHERE Id = @Id", attachment);
        }
    }
}

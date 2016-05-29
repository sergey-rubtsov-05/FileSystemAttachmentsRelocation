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

        public List<Attachment> GetAttachmentByFilePathMask(string mask)
        {
            var attachments = Session.Query<Attachment>($@"
SELECT TOP 10 *
FROM {TableName}").ToList();

            return attachments;
        }

        public List<Attachment> GetAttachmentsNotInCurrentFolder(string currentAttachmentsFolder)
        {
            var attachments =
                Session.Query<Attachment>(
                    $@"
SELECT TOP 10 {TableFields}
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
            return;
            Session.Query<Attachment>(
                $@"
UPDATE {TableName}
SET FilePath = {attachment.FilePath}
WHERE Id = @Id", new { attachment.Id });
        }
    }
}

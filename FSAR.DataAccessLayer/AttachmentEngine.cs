using System.Collections.Generic;
using System.Linq;
using Dapper;
using FSAR.DomainModel;

namespace FSAR.DataAccessLayer
{
    public class AttachmentEngine : BaseEngine
    {

        protected override string TableName => "DocFlow_Attachment_Attachment";
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
SELECT Id, DisplayFileName, FilePath, PreviewFilePath
FROM {TableName}
WHERE FilePath NOT LIKE '{currentAttachmentsFolder}%'").ToList();

            return attachments;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Dapper;
using FSAR.DomainModel;

namespace FSAR.DataAccessLayer
{
    public class AttachmentEngine : BaseEngine
    {
        public List<Attachment> GetAttachmentByFilePathMask(string mask)
        {
            var attachments = Session.Query<Attachment>(@"
SELECT TOP 10 *
FROM DocFlow_Attachment_Attachment").ToList();

            return attachments;
        }
    }
}

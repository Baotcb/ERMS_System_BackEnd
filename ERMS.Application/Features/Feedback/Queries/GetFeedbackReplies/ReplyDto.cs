using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Feedback.Queries.GetFeedbackReplies
{
    public class ReplyDto
    {
        public int Id { get; set; }
        public int? ParentReplyId { get; set; }
        public string ReplyContent { get; set; } = null!;
        public Guid ReplyBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<ReplyDto> Children { get; set; } = new();
    }
}

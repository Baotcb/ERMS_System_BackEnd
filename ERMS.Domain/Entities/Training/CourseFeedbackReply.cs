using ERMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Domain.Entities.Training
{
    public class CourseFeedbackReply : BaseEntityInt
    {
        public int FeedbackId { get; set; }

        public Guid ReplyBy { get; set; }
        public string ReplyContent { get; set; } = null!;
        public bool IsAnonymous { get; set; }
        public string? ReplyByName { get; set; }
        public string? ReplyByAvatarUrl { get; set; }
        public int? ParentReplyId { get; set; } 

        public bool IsDeleted { get; set; }

        // Navigation
        public CourseFeedback Feedback { get; set; } = null!;

        public CourseFeedbackReply? ParentReply { get; set; }
        public ICollection<CourseFeedbackReply> ChildReplies { get; set; } = new List<CourseFeedbackReply>();
    }
}

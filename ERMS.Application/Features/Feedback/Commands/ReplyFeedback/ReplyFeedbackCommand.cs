using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Feedback.Commands.ReplyFeedback
{
    public class ReplyFeedbackCommand : IRequest<int>
    {
        public int FeedbackId { get; set; }
        public string ReplyContent { get; set; } = null!;
        public int? ParentReplyId { get; set; }
    }
}

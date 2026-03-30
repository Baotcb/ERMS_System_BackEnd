using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Feedback.Queries.GetFeedbackReplies
{
    public class GetFeedbackRepliesQuery : IRequest<List<ReplyDto>>
    {
        public int FeedbackId { get; set; }
    }
}

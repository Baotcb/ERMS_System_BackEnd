using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Feedback.Commands.UpdateReply
{
    public class UpdateReplyCommand : IRequest
    {
        public int ReplyId { get; set; }
        public string Content { get; set; } = null!;
    }
}

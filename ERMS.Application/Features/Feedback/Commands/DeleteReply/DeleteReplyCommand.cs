using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Feedback.Commands.DeleteReply
{
    public class DeleteReplyCommand : IRequest
    {
        public int ReplyId { get; set; }
    }
}

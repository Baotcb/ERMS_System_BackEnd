using ERMS.Application.Interface;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Feedback.Commands.UpdateReply
{
    public class UpdateReplyHandler
    : IRequestHandler<UpdateReplyCommand>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateReplyHandler(
            IERMSDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }
        public async Task Handle(
            UpdateReplyCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new Exception("Nội dung phản hồi không được để trống");

            var reply = await _context.CourseFeedbackReplies
                .FirstOrDefaultAsync(x => x.Id == request.ReplyId && !x.IsDeleted, cancellationToken);

            if (reply == null)
                throw new Exception("Không tìm thấy phản hồi");

            if (reply.ReplyBy != _currentUser.UserId)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa phản hồi này");

            reply.ReplyContent = request.Content;
            reply.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

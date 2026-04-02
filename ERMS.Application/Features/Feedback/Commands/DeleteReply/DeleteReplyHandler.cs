using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Feedback.Commands.DeleteReply
{
    public class DeleteReplyHandler
    : IRequestHandler<DeleteReplyCommand>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteReplyHandler(
            IERMSDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            DeleteReplyCommand request,
            CancellationToken cancellationToken)
        {
            var reply = await _context.CourseFeedbackReplies
                .FirstOrDefaultAsync(x => x.Id == request.ReplyId && !x.IsDeleted, cancellationToken);

            if (reply == null)
                throw new Exception("Không tìm thấy phản hồi");

            if (reply.ReplyBy != _currentUser.UserId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa phản hồi này");

            // Soft delete
            reply.IsDeleted = true;
            reply.UpdatedAt = DateTime.UtcNow;

            var children = await _context.CourseFeedbackReplies
    .Where(x => x.ParentReplyId == reply.Id)
    .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                child.IsDeleted = true;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

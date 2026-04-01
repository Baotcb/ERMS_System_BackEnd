using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Feedback.Commands.ReplyFeedback
{
    public class ReplyFeedbackCommandHandler
     : IRequestHandler<ReplyFeedbackCommand, int>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ReplyFeedbackCommandHandler(
            IERMSDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<int> Handle(
            ReplyFeedbackCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.ReplyContent))
                throw new Exception("Nội dung phản hồi không được để trống");

            var feedback = await _context.CourseFeedbacks
                .FirstOrDefaultAsync(x => x.Id == request.FeedbackId && !x.IsDeleted, cancellationToken);

            if (feedback == null)
                throw new Exception("Không tìm thấy phản hồi khóa học");

            if (request.ParentReplyId.HasValue)
            {
                var parent = await _context.CourseFeedbackReplies
                    .FirstOrDefaultAsync(x => x.Id == request.ParentReplyId && !x.IsDeleted, cancellationToken);

                if (parent == null)
                    throw new Exception("Không tìm thấy phản hồi cha");
            }

            var userId = _currentUser.UserId;
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            var reply = new CourseFeedbackReply
            {
                FeedbackId = request.FeedbackId,
                ReplyContent = request.ReplyContent,
                ParentReplyId = request.ParentReplyId,
                IsAnonymous = request.IsAnonymous,
                ReplyByName = request.IsAnonymous ? "Ẩn danh" : user?.FullName,
                ReplyByAvatarUrl = request.IsAnonymous ? null : user?.AvatarUrl,
                ReplyBy = _currentUser.UserId!.Value,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.CourseFeedbackReplies.Add(reply);
            await _context.SaveChangesAsync(cancellationToken);

            return reply.Id; //   int
        }
    }
}

using ERMS.Application.Interface;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Feedback.Queries.GetFeedbackReplies
{
    public class GetFeedbackRepliesQueryHandler
    : IRequestHandler<GetFeedbackRepliesQuery, List<ReplyDto>>
    {
        private readonly IERMSDbContext _context;

        public GetFeedbackRepliesQueryHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReplyDto>> Handle(
            GetFeedbackRepliesQuery request,
            CancellationToken cancellationToken)
        {
            var feedbackExists = await _context.CourseFeedbacks
                .AnyAsync(x => x.Id == request.FeedbackId && !x.IsDeleted, cancellationToken);

            if (!feedbackExists)
                throw new Exception("Không tìm thấy phản hồi khóa học");

            var replies = await _context.CourseFeedbackReplies
                .Where(x => x.FeedbackId == request.FeedbackId && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            var lookup = replies.ToLookup(x => x.ParentReplyId);

            List<ReplyDto> Build(int? parentId)
            {
                return lookup[parentId]
                    .Select(x => new ReplyDto
                    {
                        Id = x.Id,
                        ParentReplyId = x.ParentReplyId,
                        ReplyContent = x.ReplyContent,
                        ReplyBy = x.ReplyBy,
                        IsAnonymous = x.IsAnonymous,
                        ReplyByName = x.ReplyByName,
                        ReplyByAvatarUrl = x.ReplyByAvatarUrl,
                        CreatedAt = x.CreatedAt,
                        Children = Build(x.Id)
                    }).ToList();
            }

            return Build(null);
        }
    }
}

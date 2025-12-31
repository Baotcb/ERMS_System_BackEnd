using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Applications.Commands.UpdateApplication
{
    public class UpdateApplicationHandler : IRequestHandler<UpdateApplicationCommand, bool>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateApplicationHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(UpdateApplicationCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (application == null)
            {
                throw new Exception("Không tìm thấy đơn ứng tuyển.");
            }

            // Kiểm tra quyền: Candidate chỉ có thể update application của mình, Manager/Admin có thể update status
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);

            bool isOwner = candidate != null && application.CandidateId == candidate.UserId;
            bool canUpdateStatus = !string.IsNullOrEmpty(request.Status); // Chỉ manager/admin mới update status

            if (!isOwner && canUpdateStatus)
            {
                // Manager/Admin có thể update status, nhưng cần check role ở controller
                // Ở đây chỉ cho phép update status nếu không phải owner
            }
            else if (!isOwner)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa đơn ứng tuyển này.");
            }

            // Update các field
            if (request.CoverLetter != null)
            {
                application.CoverLetter = request.CoverLetter;
            }

            if (request.Category != null)
            {
                application.Category = request.Category;
            }

            if (request.Status != null)
            {
                application.Status = request.Status;
                
                // Nếu status là Withdrawn, set WithdrawnAt
                if (request.Status == "Withdrawn")
                {
                    application.WithdrawnAt = DateTime.UtcNow;
                    if (request.WithdrawReason != null)
                    {
                        application.WithdrawReason = request.WithdrawReason;
                    }
                }
            }

            if (request.MatchingScore.HasValue)
            {
                application.MatchingScore = request.MatchingScore.Value;
            }

            if (request.WithdrawReason != null)
            {
                application.WithdrawReason = request.WithdrawReason;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationEntity = ERMS.Domain.Entities.Application;

namespace ERMS.Application.Features.Applications.Commands.CreateApplication
{
    public class CreateApplicationHandler : IRequestHandler<CreateApplicationCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateApplicationHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

            // Kiểm tra JobPosting tồn tại
            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

            if (jobPosting == null)
            {
                throw new Exception("Không tìm thấy bài đăng tuyển dụng.");
            }

            // Kiểm tra Resume tồn tại và thuộc về candidate
            var resume = await _context.Resumes
                .Include(r => r.Candidate)
                .FirstOrDefaultAsync(r => r.Id == request.ResumeId, cancellationToken);

            if (resume == null)
            {
                throw new Exception("Không tìm thấy CV.");
            }

            // Xác định CandidateId
            Guid candidateIdToUse;
            
            if (request.CandidateId.HasValue)
            {
                // Nếu có CandidateId trong request, kiểm tra quyền (chỉ admin/manager mới được tạo cho candidate khác)
                // Ở đây có thể thêm logic check role nếu cần
                var specifiedCandidate = await _context.Candidates
                    .FirstOrDefaultAsync(c => c.UserId == request.CandidateId.Value, cancellationToken);

                if (specifiedCandidate == null)
                {
                    throw new Exception("Không tìm thấy ứng viên với CandidateId này.");
                }

                // Kiểm tra Resume thuộc về candidate được chỉ định
                if (resume.CandidateId != request.CandidateId.Value)
                {
                    throw new UnauthorizedAccessException("CV này không thuộc về ứng viên được chỉ định.");
                }

                candidateIdToUse = request.CandidateId.Value;
            }
            else
            {
                // Lấy Candidate từ UserId của user đang đăng nhập
                var candidate = await _context.Candidates
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);

                if (candidate == null)
                {
                    throw new Exception("Bạn chưa có hồ sơ ứng viên.");
                }

                // Kiểm tra Resume thuộc về candidate
                if (resume.CandidateId != candidate.UserId)
                {
                    throw new UnauthorizedAccessException("CV này không thuộc về bạn.");
                }

                candidateIdToUse = candidate.UserId;
            }

            // Kiểm tra đã apply chưa
            var existingApplication = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobId == request.JobId && a.CandidateId == candidateIdToUse, cancellationToken);

            if (existingApplication != null)
            {
                throw new Exception("Ứng viên đã ứng tuyển cho vị trí này rồi.");
            }

            var application = new ApplicationEntity
            {
                Id = Guid.NewGuid(),
                JobId = request.JobId,
                CandidateId = candidateIdToUse,
                ResumeId = request.ResumeId,
                CvUrl = request.CvUrl,
                CoverLetter = request.CoverLetter,
                Category = request.Category,
                ApplicantType = request.ApplicantType,
                Status = "Applied",
                AppliedAt = DateTime.UtcNow
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync(cancellationToken);

            return application.Id;
        }
    }
}


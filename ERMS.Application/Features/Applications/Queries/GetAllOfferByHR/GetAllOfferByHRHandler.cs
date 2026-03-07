using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Applications.Queries.GetAllOfferByHR
{
    public class GetAllOfferByHRHandler : IRequestHandler<GetAllOfferByHRQuery, List<HROfferDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAllOfferByHRHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<HROfferDto>> Handle(GetAllOfferByHRQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");
            }
            if(!_currentUserService.Roles.Contains(AppRoles.HRManager))
            {
                throw new UnauthorizedAccessException("Người dùng không có quyền xem đề nghị.");
            }

            var offers = await _context.Offers
                .Where(o => o.CreatedById == userId && !o.IsDeleted)
                .Select(o => new HROfferDto
                {
                    Id = o.Id,
                    ApplicationId = o.ApplicationId,
                    OfferCode = o.OfferCode,
                    Position = o.Position,
                    DepartmentName = o.Department.DepartmentName,
                    Salary = o.Salary,
                    SalaryFrequency = o.SalaryFrequency,
                    Bonus = o.Bonus,
                    Benefits = o.Benefits,
                    StartDate = o.StartDate,
                    ExpirationDate = o.ExpirationDate,
                    OfferLetterUrl = o.OfferLetterUrl,
                    Status = o.Status,
                    CreatedById = o.CreatedById,
                    ApprovedById = o.ApprovedById,
                    ApprovedAt = o.ApprovedAt,
                    SentAt = o.SentAt,
                    SentById = o.SentById,
                    RespondedAt = o.RespondedAt,
                    CandidateNote = o.CandidateNote
                })
                .ToListAsync(cancellationToken);

            return offers;
        }
    }
}

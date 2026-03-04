using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Applications.Queries.GetOfferByIdOfHR
{
    public class GetOfferByIdOfHRHandler : IRequestHandler<GetOfferByIdOfHRQuery, HROfferDetailDto>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetOfferByIdOfHRHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<HROfferDetailDto> Handle(GetOfferByIdOfHRQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }
            if (!_currentUserService.Roles.Contains(AppRoles.HRManager))
            {
                throw new UnauthorizedAccessException("User does not have permission to view offers");
            }

            var offer = await _context.Offers
                .Where(o => o.Id == request.Id && o.CreatedById == userId && !o.IsDeleted)
                .Select(o => new HROfferDetailDto
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
                .FirstOrDefaultAsync(cancellationToken);

            if (offer == null || (offer.CreatedById != userId))
            {
                throw new Exception($"Offer with ID {request.Id} not found or you don't have permission to access it."); 
            }

            return offer;
        }
    }
}

using ERMS.Application.Exceptions;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Applications.Queries.GetOfferByToken;

public sealed class GetOfferByTokenHandler : IRequestHandler<GetOfferByTokenQuery, GetOfferByTokenResult>
{
    private readonly IERMSDbContext _context;

    public GetOfferByTokenHandler(IERMSDbContext context)
    {
        _context = context;
    }

    public async Task<GetOfferByTokenResult> Handle(GetOfferByTokenQuery request, CancellationToken cancellationToken)
    {
        var token = request.Token.Trim();

        var result = await _context.Offers
            .Where(o =>
                !o.IsDeleted &&
                o.ResponseToken != null &&
                o.ResponseToken == token)
            .Select(o => new GetOfferByTokenResult
            {
                CandidateName = o.Application.ExternalCandidate != null
                    ? o.Application.ExternalCandidate.FullName
                    : o.Application.Candidate.User.FullName,
                Position = o.Position,
                DepartmentName = o.Department.DepartmentName,
                CompanyName = o.Application.JobPosting.Enterprise != null
                    ? o.Application.JobPosting.Enterprise.EnterpriseName
                    : null,
                Salary = o.Salary,
                SalaryFrequency = o.SalaryFrequency,
                StartDate = o.StartDate,
                ExpirationDate = o.ExpirationDate
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException("Offer token không hợp lệ hoặc đã được sử dụng.");

        return result;
    }
}

using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enterprises.Commands.CreateEnterprise
{
    public class CreateEnterpriseHandler
        : IRequestHandler<CreateEnterpriseCommand, Guid>
    {
        private readonly IEnterprisesService _enterprisesService;
        private readonly ICurrentUserService _currentUser;

        public CreateEnterpriseHandler(
            IEnterprisesService enterprisesService,
            ICurrentUserService currentUser)
        {
            _enterprisesService = enterprisesService;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            CreateEnterpriseCommand request,
            CancellationToken cancellationToken)
        {
            var enterprise = new Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = request.EnterpriseName,
                EnterpriseCode = request.EnterpriseCode,
                TaxCode = request.TaxCode,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                Website = request.Website,
                LogoUrl = request.LogoUrl,

                SubscriptionPlanId = request.SubscriptionPlanId,
                SubscriptionStartDate = request.SubscriptionStartDate,
                SubscriptionEndDate = request.SubscriptionEndDate,

                CreatedById = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await _enterprisesService.CreateAsync(enterprise);
            return enterprise.Id;
        }
    }
}

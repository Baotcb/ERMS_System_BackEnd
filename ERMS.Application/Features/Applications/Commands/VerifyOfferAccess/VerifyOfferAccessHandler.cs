using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Applications.Queries.VerifyOfferAccess
{
    public class VerifyOfferAccessHandler : IRequestHandler<VerifyOfferAccessQuery, VerifyOfferAccessResult>
    {
        private readonly IERMSDbContext _context;
        private readonly UserManager<User> _userManager;

        public VerifyOfferAccessHandler(IERMSDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<VerifyOfferAccessResult> Handle(VerifyOfferAccessQuery request, CancellationToken cancellationToken)
        {
          
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new VerifyOfferAccessResult
                {
                    IsValid = false,
                    Message = "Email không hợp lệ."
                };
            }

         
            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                $"OfferAccess-{request.OfferId}",
                request.Token);

            if (!isValid)
            {
                return new VerifyOfferAccessResult
                {
                    IsValid = false,
                    Message = "Token không hợp lệ hoặc đã hết hạn."
                };
            }

      
            var offer = await _context.Offers
                .Include(o => o.Application)
                    .ThenInclude(a => a.Candidate)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId && !o.IsDeleted, cancellationToken);

            if (offer == null)
            {
                return new VerifyOfferAccessResult
                {
                    IsValid = false,
                    Message = "Offer không tồn tại."
                };
            }

       
            if (offer.Application.Candidate.UserId != user.Id)
            {
                return new VerifyOfferAccessResult
                {
                    IsValid = false,
                    Message = "Bạn không có quyền truy cập offer này."
                };
            }

           
            if (offer.ExpirationDate <= DateTime.UtcNow)
            {
                return new VerifyOfferAccessResult
                {
                    IsValid = false,
                    Message = "Offer đã hết hạn."
                };
            }

            return new VerifyOfferAccessResult
            {
                IsValid = true,
                Message = "Xác thực thành công.",
                OfferId = offer.Id,
                OfferCode = offer.OfferCode
            };
        }
    }
}
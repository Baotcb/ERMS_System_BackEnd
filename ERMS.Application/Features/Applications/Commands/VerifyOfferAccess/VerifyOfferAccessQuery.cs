using MediatR;
using System;

namespace ERMS.Application.Features.Applications.Queries.VerifyOfferAccess
{
    public class VerifyOfferAccessQuery : IRequest<VerifyOfferAccessResult>
    {
        public Guid OfferId { get; set; }
        public string Token { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    public class VerifyOfferAccessResult
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public Guid? OfferId { get; set; }
        public string? OfferCode { get; set; }
    }
}
using MediatR;
using System;

namespace ERMS.Application.Features.Applications.Commands.CreateOffer
{
    public class CreateOfferCommand : IRequest<Guid>
    {
        public Guid ApplicationId { get; set; }
        public int DepartmentId { get; set; }
        public string Position { get; set; } = null!;
        public decimal Salary { get; set; }
        public string SalaryFrequency { get; set; } = "Monthly";
        public string? Bonus { get; set; }
        public string? Benefits { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? OfferLetterUrl { get; set; }
    }
}
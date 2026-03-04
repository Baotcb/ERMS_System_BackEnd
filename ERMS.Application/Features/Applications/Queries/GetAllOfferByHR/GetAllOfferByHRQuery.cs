using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Applications.Queries.GetAllOfferByHR
{
    public class GetAllOfferByHRQuery : IRequest<List<HROfferDto>>
    {
    }

    public class HROfferDto
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string? OfferCode { get; set; }
        public string Position { get; set; } = null!;
        public string DepartmentName { get; set; }
        public decimal Salary { get; set; }
        public string SalaryFrequency { get; set; } = null!;
        public string? Bonus { get; set; }
        public string? Benefits { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? OfferLetterUrl { get; set; }
        public string Status { get; set; } = null!;
        public Guid CreatedById { get; set; }
        public Guid? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public Guid? SentById { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? CandidateNote { get; set; }
    }
}

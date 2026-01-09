using MediatR;
using System;

namespace ERMS.Application.Features.Applications.Commands.CreateApplication
{
    public class CreateApplicationCommand : IRequest<Guid>
    {
        public Guid JobId { get; set; }
        public Guid? CandidateId { get; set; } // Optional: nếu không có thì dùng candidate của user đang đăng nhập
        public Guid ResumeId { get; set; }
        public string CvUrl { get; set; } = string.Empty;
        public string? CoverLetter { get; set; }
        public string? Category { get; set; }
        public string ApplicantType { get; set; } = "External";
    }
}


using ERMS.Application.Features.Applications.DTOs;
using MediatR;
using System;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationById
{
    public class GetApplicationByIdQuery : IRequest<ApplicationDto>
    {
        public Guid Id { get; set; }
    }
}


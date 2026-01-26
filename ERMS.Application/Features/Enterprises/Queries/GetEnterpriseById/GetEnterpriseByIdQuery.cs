using ERMS.Application.Features.Enterprises.DTOs;
using MediatR;
using System;

namespace ERMS.Application.Features.Enterprises.Queries.GetEnterpriseById
{
    public class GetEnterpriseByIdQuery : IRequest<EnterpriseDto>
    {
        public Guid Id { get; set; }
    }
}

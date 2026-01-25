using ERMS.Domain.Entities.Candidate;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Interface
{
    public interface ICandidateService
    {
        Task AddCandidateAsync(Candidate c);
    }
}

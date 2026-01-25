using ERMS.Application.Interface;
using ERMS.Domain.Entities.Candidate;
using ERMS.Infrastructure.Data;
using Google;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Infrastructure.Services
{
    public class CandidateService : ICandidateService
    {
        private readonly ERMSDbContext _context;

        public CandidateService(ERMSDbContext context)
        {
            _context = context;
        }

        public async Task AddCandidateAsync(Candidate c)
        {
            if (c == null)
                throw new ArgumentNullException(nameof(c));

            await _context.Candidates.AddAsync(c);
            await _context.SaveChangesAsync();
        }
    }

}


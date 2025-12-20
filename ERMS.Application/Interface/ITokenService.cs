using ERMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Interface
{
    public interface ITokenService
    {
        Task<string> CreateToken(User user);
    }
}

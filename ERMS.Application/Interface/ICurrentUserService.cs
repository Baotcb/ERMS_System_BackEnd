using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Interface
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
    }
}

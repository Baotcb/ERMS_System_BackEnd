using ERMS.Application.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ERMS.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                
                var user = _httpContextAccessor.HttpContext?.User;

               
                var idClaim = user?.FindFirst(ClaimTypes.NameIdentifier);

                if (idClaim == null) return null;

                
                return Guid.TryParse(idClaim.Value, out var userId) ? userId : null;
            }
  //   <ItemGroup>
  //    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  //   </ItemGroup>
        }

    }
}

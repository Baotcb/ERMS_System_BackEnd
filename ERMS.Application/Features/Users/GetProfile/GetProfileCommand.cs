using ERMS.Application.DTO.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Users.GetProfile
{
    public class GetProfileCommand : IRequest<UserProfileDto>
    {

    }
}

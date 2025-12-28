using ERMS.Application.Features.Users.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Users.Commands.GetProfile
{
    public class GetProfileCommand : IRequest<UserProfileDto>
    {

    }
}

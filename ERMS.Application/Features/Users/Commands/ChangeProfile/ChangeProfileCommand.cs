using ERMS.Application.Features.Users.DTO;
using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Users.Commands.ChangeProfile
{
    public class ChangeProfileCommand : IRequest<UserProfileDto>
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Hometown { get; set; }
        public string? Phones { get; set; }
        public string? Address { get; set; }


        }
        }

using System;
using Microsoft.AspNetCore.Identity;

namespace API.Entities;

public class ApplicationRole : IdentityRole<int>
{
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
}

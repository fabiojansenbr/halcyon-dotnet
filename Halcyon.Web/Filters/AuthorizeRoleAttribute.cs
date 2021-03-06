﻿using Halcyon.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Halcyon.Web.Filters
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(params Roles[] roles)
        {
            Roles = string.Join(",", roles.Select(r => r.GetAttributeOfType<DisplayAttribute>().Name));
        }
    }
}

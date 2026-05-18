using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityModel;
using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Services
{
    public class CustomProfileService:IProfileService
    {
         private readonly UserManager<ApplicationUser> _userManager;

         public CustomProfileService(UserManager<ApplicationUser> userManager)
       {
            _userManager = userManager;
       }     
       
         public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
           var user = await _userManager.GetUserAsync(context.Subject);
           var existingClaims = await _userManager.GetClaimsAsync(user);
           var claims = new List<System.Security.Claims.Claim>
           {
               new System.Security.Claims.Claim("username", user.UserName)
              
           };
           context.IssuedClaims.AddRange(claims);
           context.IssuedClaims.Add(existingClaims.FirstOrDefault(x=>x.Type ==JwtClaimTypes.Name));
        }

        public  Task IsActiveAsync(IsActiveContext context)
        {
           return Task.CompletedTask;
        }
    }
}
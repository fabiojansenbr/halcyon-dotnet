﻿using Halcyon.Web.Data;
using Halcyon.Web.Models.Account;
using Halcyon.Web.Models.User;
using Halcyon.Web.Services.Email;
using Halcyon.Web.Services.Hash;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Halcyon.Web.Controllers
{
    [Route("[controller]")]
    public class AccountController : BaseController
    {
        private readonly HalcyonDbContext _context;

        private readonly IHashService _hashService;

        private readonly IEmailService _emailService;

        public AccountController(
            HalcyonDbContext context,
            IHashService hashService,
            IEmailService emailService)
        {
            _context = context;
            _hashService = hashService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailAddress == model.EmailAddress);

            if (existing != null)
            {
                return BadRequest($"User name \"{model.EmailAddress}\" is already taken.");
            }

            var user = new User
            {
                EmailAddress = model.EmailAddress,
                Password = await _hashService.GenerateHashAsync(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                DateOfBirth = model.DateOfBirth
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            var result = new UserCreatedResult
            {
                UserId = user.Id
            };

            return Ok("User successfully registered.", result);
        }

        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailAddress == model.EmailAddress);

            if (user != null && !user.IsLockedOut)
            {
                user.PasswordResetToken = Guid.NewGuid().ToString();
                await _context.SaveChangesAsync();

                var message = new EmailMessage
                {
                    Template = EmailTemplate.ForgotPassword
                };

                message.To.Add(user.EmailAddress);
                message.Data.Add("SiteUrl", Request.PathBase);
                message.Data.Add($"PasswordResetUrl", $"{Request.PathBase}/resetpassword/{user.PasswordResetToken}");

                await _emailService.SendEmailAsync(message);
            }

            return Ok("Instructions as to how to reset your password have been sent to you via email.");
        }

        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailAddress == model.EmailAddress);

            if (
                user == null
                || user.IsLockedOut
                || !model.Token.Equals(user.PasswordResetToken, StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest("Invalid token.");
            }

            user.Password = await _hashService.GenerateHashAsync(model.NewPassword);
            user.PasswordResetToken = null;

            await _context.SaveChangesAsync();

            return Ok("Your password has been reset.");
        }
    }
}

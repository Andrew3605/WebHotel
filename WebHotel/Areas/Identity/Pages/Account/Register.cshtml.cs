#nullable enable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WebHotel.Data;
using WebHotel.Models;

namespace WebHotel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender? _emailSender;
        private readonly AppDbContext _db;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            AppDbContext db,
            IEmailSender? emailSender = null)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _db = db;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password), Compare(nameof(Password))]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required, StringLength(80)]
            public string FullName { get; set; } = string.Empty;

            [Phone]
            public string? PhoneNumber { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
                return Page();

            // 1️⃣ Create Identity user
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            // 2️⃣ If succeeded, create Customer profile + link + assign role
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                // Create Customer profile
                var customer = new Customer
                {
                    FullName = Input.FullName,
                    Email = Input.Email,
                    Phone = Input.PhoneNumber
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();

                // Link Identity user -> Customer table
                user.CustomerId = customer.Id;
                await _userManager.UpdateAsync(user);

                // Assign the "Customer" role
                await _userManager.AddToRoleAsync(user, "Customer");

                // Redirect to Login (no auto-sign in)
                return RedirectToPage("./Login");
            }

            // 3️⃣ If failed, show errors
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            return Page();
        }
    }
}

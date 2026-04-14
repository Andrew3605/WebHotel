using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHotel.Models;
using WebHotel.ViewModels;

namespace WebHotel.Controllers;

[Authorize(Roles = "Admin")]
public class StaffAccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public StaffAccountController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: StaffAccount/Index
    public async Task<IActionResult> Index()
    {
        var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
        return View(staffUsers.OrderBy(u => u.Email).ToList());
    }

    // GET: StaffAccount/Create
    public IActionResult Create()
    {
        return View(new CreateStaffVm());
    }

    // POST: StaffAccount/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Staff");

        TempData["StatusMessage"] = $"Staff account created for {model.Email}.";
        return RedirectToAction(nameof(Index));
    }

    // POST: StaffAccount/Delete
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _userManager.DeleteAsync(user);
        TempData["StatusMessage"] = $"Staff account {user.Email} deleted.";
        return RedirectToAction(nameof(Index));
    }
}

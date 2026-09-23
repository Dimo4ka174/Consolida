// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DB.Authorization;
using System.Linq;

namespace Consolida.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public EmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Email { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Логин обязателен для заполнения.")]
            [Display(Name = "Новый логин")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var currentEmail = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail == currentEmail)
            {
                StatusMessage = "Логин не изменился.";
                return RedirectToPage();
            }

            var existingUser = await _userManager.FindByNameAsync(Input.NewEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(string.Empty, "Такой логин уже используется.");
                await LoadAsync(user);
                return Page();
            }

            var setNameResult = await _userManager.SetUserNameAsync(user, Input.NewEmail);
            if (!setNameResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty,
                    "Не удалось изменить логин: " + string.Join("; ", setNameResult.Errors.Select(e => e.Description)));
                await LoadAsync(user);
                return Page();
            }

            var setEmailResult = await _userManager.SetEmailAsync(user, Input.NewEmail);
            if (!setEmailResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty,
                    "Не удалось изменить email: " + string.Join("; ", setEmailResult.Errors.Select(e => e.Description)));
                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);

            StatusMessage = "Логин успешно изменён.";
            return RedirectToPage();
        }
    }
}
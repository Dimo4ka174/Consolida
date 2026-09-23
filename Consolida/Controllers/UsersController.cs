using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.UserViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DB.Authorization;

namespace Consolida.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Authorize(Policy = "ReadUsers")]
        public async Task<IActionResult> Index(int page = 1, string? searchString = null, string? sortOrder = "Email", int pageSize = 8)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            // Фильтрация
            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => u.Email.Contains(searchString) || u.UserName.Contains(searchString));
            }

            // Сортировка
            usersQuery = sortOrder switch
            {
                "Email_desc" => usersQuery.OrderByDescending(u => u.Email),
                "Email" => usersQuery.OrderBy(u => u.Email),
                _ => usersQuery.OrderBy(u => u.Email),
            };

            var totalItems = await usersQuery.CountAsync();
            var users = await usersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userViewModels = new List<UserWithRolesViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserWithRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles
                });
            }

            var paging = PagingHelpers.Create(totalItems, page, pageSize);
            paging.NamePage = "Users";

            ViewBag.Paging = paging;
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            return View(userViewModels);
        }

        [HttpGet, ActionName("Create")]
        [Authorize(Policy = "CreateUsers")]
        public IActionResult Create() => View();

        [HttpPost, ActionName("Create")]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");

                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost, ActionName("DeleteUser")]
        [Authorize(Policy = "DeleteUsers")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Пользователь {user.Email} успешно удален.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("Index", await GetUsersWithRoles());
        }

        [HttpPost, ActionName("ResetPassword")]
        [Authorize(Policy = "EditUsers")]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, "qwe123");

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Пароль пользователя {user.Email} успешно сброшен.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("Index", await GetUsersWithRoles());
        }

        [HttpGet, ActionName("ManageRoles")]
        [Authorize(Policy = "EditUsers")]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = _roleManager.Roles.ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new ManageRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Roles = roles.Select(r => new RoleSelection
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    IsSelected = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost, ActionName("UpdateRoles")]
        [Authorize(Policy = "EditUsers")]
        public async Task<IActionResult> UpdateRoles(ManageRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, userRoles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Не удалось удалить текущие роли пользователя");
                return View("ManageRoles", model);
            }

            var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName);
            result = await _userManager.AddToRolesAsync(user, selectedRoles);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Роли пользователя {user.UserName} успешно обновлены.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("ManageRoles", model);
        }

        private async Task<List<UserWithRolesViewModel>> GetUsersWithRoles()
        {
            var users = _userManager.Users.ToList();
            var userRolesViewModel = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesViewModel.Add(new UserWithRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles
                });
            }

            return userRolesViewModel;
        }
    }
}

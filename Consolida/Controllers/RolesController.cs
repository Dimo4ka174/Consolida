using Application.ViewModels.RoleViewModel;
using DB.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RolesController> _logger;

        public RolesController(RoleManager<IdentityRole> roleManager, ILogger<RolesController> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet, ActionName("Index")]
        [Authorize(Policy = "ReadRoles")]
        public async Task<IActionResult> Index()
        {
            var viewModels = await GetRolesWithClaims();
            return View(viewModels);
        }

        [HttpPost, ActionName("Create")]
        [Authorize(Policy = "CreateRoles")]
        public async Task<IActionResult> Create(string roleName)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    var role = new IdentityRole { Name = roleName };
                    var result = await _roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = $"Роль '{roleName}' успешно создана.";
                        return RedirectToAction("Index");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                else
                {
                    ModelState.AddModelError("", $"Роль '{roleName}' уже существует.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Имя роли не может быть пустым.");
            }

            return View("Index", await GetRolesWithClaims());
        }

        [HttpPost, ActionName("Edit")]
        [Authorize(Policy = "EditRoles")]
        public async Task<IActionResult> Edit(string oldRoleName, string newRoleName)
        {
            if (string.IsNullOrWhiteSpace(oldRoleName) || string.IsNullOrWhiteSpace(newRoleName))
            {
                TempData["Error"] = "Старое и новое имя роли не должны быть пустыми.";
                return RedirectToAction("Index");
            }

            var role = await _roleManager.FindByNameAsync(oldRoleName);
            if (role == null)
            {
                TempData["Error"] = $"Роль '{oldRoleName}' не найдена.";
                return RedirectToAction("Index");
            }

            if (await _roleManager.RoleExistsAsync(newRoleName))
            {
                TempData["Error"] = $"Роль '{newRoleName}' уже существует.";
                return RedirectToAction("Index");
            }

            role.Name = newRoleName;
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Роль переименована в '{newRoleName}'.";
            }
            else
            {
                LogErrors(result.Errors, $"Переименование роли '{oldRoleName}'");
            }

            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "DeleteRoles")]
        public async Task<IActionResult> Delete(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                TempData["Error"] = "Имя роли не может быть пустым.";
                return RedirectToAction("Index");
            }

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                TempData["Error"] = $"Роль '{roleName}' не найдена.";
                return RedirectToAction("Index");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Роль '{roleName}' удалена.";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                TempData["Error"] = $"Ошибка при удалении роли '{roleName}'.";
            }

            return RedirectToAction("Index");
        }

        private void LogErrors(IEnumerable<IdentityError> errors, string context)
        {
            var message = string.Join("; ", errors.Select(e => e.Description));
            _logger.LogWarning("{Context}: {Message}", context, message);
            TempData["Error"] = message;
        }

        [HttpGet("ManageClaims")]
        [Authorize(Policy = "EditRoles")]
        public async Task<IActionResult> ManageClaims(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.LogWarning($"Роль '{roleName}' не найдена.");
                return NotFound();
            }

            var claims = await _roleManager.GetClaimsAsync(role);

            var permissionClaims = claims.Where(c => c.Type == "Permission").ToList();
            var permissions = typeof(Permissions).GetNestedTypes();

            var model = new ManageClaimsViewModel
            {
                RoleName = role.Name,
                Claims = permissions.SelectMany(category =>
                {
                    var categoryName = category.Name;
                    return category.GetFields()
                        .Where(f => f.IsStatic && f.IsLiteral && !f.IsInitOnly)
                        .Select(f =>
                        {
                            var claimValue = f.GetRawConstantValue().ToString();
                            return new ClaimSelection
                            {
                                Category = PermissionTranslations.GroupTranslations.ContainsKey(categoryName)
                                    ? PermissionTranslations.GroupTranslations[categoryName]
                                    : categoryName,
                                ClaimType = claimValue,
                                IsSelected = permissionClaims.Any(c => c.Value == claimValue)
                            };
                        });
                }).ToList()
            };

            return View(model);
        }

        [HttpPost("UpdateClaims")]
        [Authorize(Policy = "EditRoles")]
        public async Task<IActionResult> UpdateClaims(ManageClaimsViewModel model)
        {
            var role = await _roleManager.FindByNameAsync(model.RoleName);
            if (role == null)
            {
                _logger.LogWarning($"Роль '{model.RoleName}' не найдена.");
                return NotFound();
            }

            var currentClaims = await _roleManager.GetClaimsAsync(role);

            var permissionClaimsToRemove = currentClaims.Where(c => c.Type == "Permission");
            foreach (var claim in currentClaims)
            {
                var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError($"Не удалось удалить клайм: Type={claim.Type}, Value={claim.Value}");
                    foreach (var error in removeResult.Errors)
                    {
                        _logger.LogError($"Ошибка: {error.Code} - {error.Description}");
                    }
                    ModelState.AddModelError("", $"Не удалось удалить клайм '{claim.Type}'.");
                    return View("ManageClaims", model);
                }
                _logger.LogInformation($"Клайм успешно удален: Type={claim.Type}, Value={claim.Value}");
            }

            var selectedClaims = model.Claims
                .Where(c => c.IsSelected)
                .Select(c => new System.Security.Claims.Claim("Permission", c.ClaimType));

            foreach (var claim in selectedClaims)
            {
                var addResult = await _roleManager.AddClaimAsync(role, claim);
                if (!addResult.Succeeded)
                {
                    _logger.LogError($"Не удалось добавить клайм: Type={claim.Type}, Value={claim.Value}");
                    foreach (var error in addResult.Errors)
                    {
                        _logger.LogError($"Ошибка: {error.Code} - {error.Description}");
                    }
                    ModelState.AddModelError("", $"Не удалось добавить клайм '{claim.Value}'.");
                    return View("ManageClaims", model);
                }
            }

            _logger.LogInformation($"Клаймы для роли '{model.RoleName}' успешно обновлены.");
            TempData["SuccessMessage"] = $"Клаймы для роли {model.RoleName} успешно обновлены.";
            return RedirectToAction("Index");
        }

        private async Task<List<RoleViewModel>> GetRolesWithClaims()
        {
            var roles = _roleManager.Roles.ToList();
            var viewModels = new List<RoleViewModel>();

            foreach (var role in roles)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                var groupedClaims = new Dictionary<string, List<ClaimViewModel>>();

                var permissions = typeof(Permissions).GetNestedTypes();
                foreach (var category in permissions)
                {
                    var categoryName = category.Name;
                    var translatedCategoryName = PermissionTranslations.GroupTranslations.ContainsKey(categoryName)
                        ? PermissionTranslations.GroupTranslations[categoryName]
                        : categoryName;

                    var allClaimsInCategory = category.GetFields()
                        .Where(f => f.IsStatic && f.IsLiteral && !f.IsInitOnly)
                        .Select(f => f.GetRawConstantValue().ToString())
                        .ToList();

                    var matchingClaims = claims
                        .Where(c => c.Type == "Permission" && allClaimsInCategory.Contains(c.Value, StringComparer.OrdinalIgnoreCase))
                        .Select(c => new ClaimViewModel
                        {
                            RoleName = role.Name,
                            ClaimType = TranslateClaim(c.Value),
                            ClaimValue = ""
                        })
                        .ToList();

                    if (matchingClaims.Any())
                    {
                        groupedClaims[translatedCategoryName] = matchingClaims;
                    }
                }

                viewModels.Add(new RoleViewModel
                {
                    RoleName = role.Name,
                    GroupedClaims = groupedClaims
                });
            }

            return viewModels;
        }

        private string TranslateClaim(string claimValue)
        {
            var parts = claimValue.Split('_');
            if (parts.Length == 2)
            {
                var objectName = parts[1];

                var translatedObjectName = PermissionTranslations.GroupTranslations.ContainsKey(objectName)
                    ? PermissionTranslations.GroupTranslations[objectName]
                    : objectName;

                return $"{parts[0]}_{translatedObjectName}";
            }

            return claimValue;
        }
    }
}

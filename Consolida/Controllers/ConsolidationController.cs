using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using DB.Entity.Enum;

namespace Consolida.Controllers
{
    [Authorize(Policy = "ReadOrders")]
    public class ConsolidationController : Controller
    {
        private readonly IConsolidationService _consolidationService;
        private readonly ILockService _lockService;
        private readonly IConsolidationNotifier _notifier;

        public ConsolidationController(
            IConsolidationService consolidationService,
            ILockService lockService,
            IConsolidationNotifier notifier)
        {
            _consolidationService = consolidationService;
            _lockService = lockService;
            _notifier = notifier;
        }

        private string CurrentUserDisplayName =>
            User.FindFirst("DisplayName")?.Value
            ?? User.Identity?.Name
            ?? "System";

        private async Task<IActionResult> ExecuteWithConcurrencyHandlingAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Данные были изменены другим пользователем. Обновите страницу и повторите действие."
                });
            }
        }

        // =====================================================================
        //  Просмотр
        // =====================================================================

        [HttpGet]
        public async Task<IActionResult> Index(int weekSpan = 1, decimal? weightLimit = null)
        {
            if (weekSpan < 1) weekSpan = 1;
            if (weekSpan > 10) weekSpan = 10;

            var model = await _consolidationService.GetBoardAsync(weekSpan, weightLimit);
            model.SelectedWeightLimit = weightLimit;
            model.AvailableWeightLimits = await _consolidationService.GetWeightLimitsAsync();
            return View(model);
        }

        [HttpGet("Tracking")]
        public async Task<IActionResult> Tracking()
        {
            var model = await _consolidationService.GetTrackingBoardAsync();
            return View(model);
        }

        // =====================================================================
        //  Лимиты веса
        // =====================================================================

        [HttpGet]
        public async Task<IActionResult> GetWeightLimits()
        {
            var limits = await _consolidationService.GetWeightLimitsAsync();
            return Json(limits);
        }

        [HttpPost]
        public async Task<IActionResult> AddWeightLimit(decimal value)
        {
            if (value <= 0 || value > 100_000) return BadRequest("Некорректное значение");

            return await ExecuteWithConcurrencyHandlingAsync(async () =>
            {
                var changedBy = CurrentUserDisplayName;
                var limit = await _consolidationService.AddWeightLimitAsync(value, changedBy);
                return Ok(new { id = limit.Id, value = limit.Value });
            });
        }

        // =====================================================================
        //  История изменений пула
        // =====================================================================

        [HttpGet]
        public async Task<IActionResult> GetPoolHistory(int poolId)
        {
            var history = await _consolidationService.GetPoolHistoryAsync(poolId);
            return Json(history);
        }

        // =====================================================================
        //  Операции с пулами и заказами
        // =====================================================================

        [HttpPost]
        public async Task<IActionResult> AcceptSuggestion([FromBody] AcceptSuggestionRequest request)
        {
            if (request?.OrderIds == null || request.OrderIds.Count == 0)
                return BadRequest("Некорректные данные");

            var userId = CurrentUserDisplayName;

            return await ExecuteWithConcurrencyHandlingAsync(async () =>
            {
                try
                {
                    await _consolidationService.AcceptSuggestionAsync(request.OrderIds, request.PoolId, userId);

                    await _notifier.NotifyBoardChangedAsync(new ConsolidationChangeInfo
                    {
                        ChangedBy = userId,
                        Action = "SuggestionAccepted",
                        PoolId = request.PoolId,
                        TargetPoolId = request.PoolId
                    });

                    return Ok(new { success = true });
                }
                catch (DbUpdateConcurrencyException) { throw; } // пробрасываем наружу
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> DissolvePool(int poolId)
        {
            var userId = CurrentUserDisplayName;

            var lockResult = await _lockService.TryLockPoolAsync(poolId, userId);
            if (!lockResult.Success)
            {
                TempData["Error"] = lockResult.Message;
                return RedirectToAction("Index");
            }

            try
            {
                return await ExecuteWithConcurrencyHandlingAsync(async () =>
                {
                    await _consolidationService.DissolvePoolAsync(poolId, userId);

                    await _notifier.NotifyBoardChangedAsync(new ConsolidationChangeInfo
                    {
                        ChangedBy = userId,
                        Action = "PoolDissolved",
                        PoolId = poolId
                    });

                    return RedirectToAction("Index");
                });
            }
            finally
            {
                await _lockService.UnlockPoolAsync(poolId, userId);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReturnPoolToPaid(int poolId)
        {
            return await ExecuteWithConcurrencyHandlingAsync(async () =>
            {
                try
                {
                    var changedBy = CurrentUserDisplayName;
                    await _consolidationService.SetPoolStatusAsync(poolId, Status.Paid, null, changedBy);

                    await _notifier.NotifyBoardChangedAsync(new ConsolidationChangeInfo
                    {
                        ChangedBy = changedBy,
                        Action = "PoolStatusChanged",
                        PoolId = poolId
                    });

                    TempData["SuccessMessage"] = "Отгрузка возвращена в статус «Оплачен».";
                }
                catch (DbUpdateConcurrencyException) { throw; }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
                return RedirectToAction("Tracking");
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveOrderFromPool(int orderId)
        {
            var userId = CurrentUserDisplayName;

            var lockResult = await _lockService.TryLockOrderAsync(orderId, userId);
            if (!lockResult.Success)
                return Conflict(new { message = lockResult.Message });

            try
            {
                return await ExecuteWithConcurrencyHandlingAsync(async () =>
                {
                    await _consolidationService.RemoveOrderFromPoolAsync(orderId, userId);

                    await _notifier.NotifyBoardChangedAsync(new ConsolidationChangeInfo
                    {
                        ChangedBy = userId,
                        Action = "OrderRemoved",
                        OrderId = orderId
                    });

                    return RedirectToAction("Index");
                });
            }
            finally
            {
                await _lockService.UnlockOrderAsync(orderId, userId);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddOrderToPool(int orderId, int poolId, decimal? weightLimit)
        {
            var userId = CurrentUserDisplayName;

            var lockResult = await _lockService.TryLockOrderAsync(orderId, userId);
            if (!lockResult.Success)
                return Conflict(new { message = lockResult.Message });

            try
            {
                return await ExecuteWithConcurrencyHandlingAsync(async () =>
                {
                    try
                    {
                        await _consolidationService.AddOrderToPoolAsync(orderId, poolId, weightLimit, userId);

                        await _notifier.NotifyBoardChangedAsync(new ConsolidationChangeInfo
                        {
                            ChangedBy = userId,
                            Action = "OrderAdded",
                            OrderId = orderId,
                            PoolId = poolId
                        });

                        return RedirectToAction("Index");
                    }
                    catch (DbUpdateConcurrencyException) { throw; }
                    catch (Exception ex)
                    {
                        TempData["Error"] = ex.Message;
                        return RedirectToAction("Index");
                    }
                });
            }
            finally
            {
                await _lockService.UnlockOrderAsync(orderId, userId);
            }
        }

        [HttpPost]
        public async Task<IActionResult> MoveOrderToPool(int orderId, int targetPoolId, decimal? weightLimit)
        {
            var userId = CurrentUserDisplayName;

            var lockResult = await _lockService.TryLockOrderAsync(orderId, userId);
            if (!lockResult.Success)
                return Conflict(new { message = lockResult.Message });

            try
            {
                return await ExecuteWithConcurrencyHandlingAsync(async () =>
                {
                    try
                    {
                        await _consolidationService.MoveOrderToPoolAsync(orderId, targetPoolId, weightLimit, userId);

                        await _notifier.NotifyBoardChangedAsync(new ConsolidationChangeInfo
                        {
                            ChangedBy = userId,
                            Action = "OrderMoved",
                            OrderId = orderId,
                            TargetPoolId = targetPoolId
                        });

                        return Ok(new { success = true });
                    }
                    catch (DbUpdateConcurrencyException) { throw; }
                    catch (Exception ex)
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                });
            }
            finally
            {
                await _lockService.UnlockOrderAsync(orderId, userId);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetPoolStatus([FromBody] SetPoolStatusRequest request)
        {
            if (request == null || request.PoolId <= 0)
                return BadRequest("Некорректные данные");

            var userId = CurrentUserDisplayName;

            var lockResult = await _lockService.TryLockPoolAsync(request.PoolId, userId);
            if (!lockResult.Success)
                return Conflict(new { success = false, message = lockResult.Message });

            try
            {
                return await ExecuteWithConcurrencyHandlingAsync(async () =>
                {
                    try
                    {
                        await _consolidationService.SetPoolStatusAsync(request.PoolId, request.NewStatus, request.ExpectedDeliveryDate, userId);

                        await _notifier.NotifyBoardChangedAsync(new ConsolidationChangeInfo
                        {
                            ChangedBy = userId,
                            Action = "PoolStatusChanged",
                            PoolId = request.PoolId
                        });

                        return Ok(new { success = true });
                    }
                    catch (DbUpdateConcurrencyException) { throw; }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { success = false, message = ex.Message });
                    }
                });
            }
            finally
            {
                await _lockService.UnlockPoolAsync(request.PoolId, userId);
            }
        }

        // =====================================================================
        //  DTO для запросов
        // =====================================================================

        public class SetPoolStatusRequest
        {
            public int PoolId { get; set; }
            public Status NewStatus { get; set; }
            public DateTime? ExpectedDeliveryDate { get; set; }
        }

        public class AcceptSuggestionRequest
        {
            public List<int> OrderIds { get; set; } = new();
            public int? PoolId { get; set; }
        }
    }
}

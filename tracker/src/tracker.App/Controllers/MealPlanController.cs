using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tracker.App.Actors;
using tracker.App.Services;
using tracker.Domain.MealPlan;
using tracker.Domain.User;

namespace tracker.App.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class MealPlanController : ControllerBase
{
    private readonly ILogger<MealPlanController> _logger;
    private readonly IMealPlanService _mealPlanService;
    private readonly IActorRef _userActor;

    public MealPlanController(
        ILogger<MealPlanController> logger,
        IMealPlanService mealPlanService,
        IRequiredActor<UserActor> userActor)
    {
        _logger = logger;
        _mealPlanService = mealPlanService;
        _userActor = userActor.ActorRef;
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> CreateMealPlan(string userId, [FromBody] CreateMealPlanRequest request)
    {
        try
        {
            // Get user to check onboarding status and BMR
            var user = await _userActor.Ask<User>(new FetchUser(userId), TimeSpan.FromSeconds(5));

            if (!user.IsCreated)
            {
                return BadRequest("User does not exist");
            }

            if (user.OnboardingState != UserOnboardingState.Complete)
            {
                return BadRequest("User must complete onboarding before creating a meal plan");
            }

            if (!user.BmrWithActivityLevel.HasValue)
            {
                return BadRequest("User BMR not calculated");
            }

            var totalMeals = request.BreakfastCount + request.LunchCount + request.DinnerCount + request.SnackCount;
            
            if (totalMeals == 0)
            {
                return BadRequest("At least one meal type must be selected");
            }

            var result = await _mealPlanService.CreateMealPlanAsync(
                userId,
                request.PlanName,
                user.BmrWithActivityLevel.Value,
                request.BreakfastCount,
                request.LunchCount,
                request.DinnerCount,
                request.SnackCount);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(new
            {
                MealPlanId = result.MealPlanId,
                Message = $"Meal plan created successfully with {totalMeals} total meal slots"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meal plan for user {UserId}", userId);
            return StatusCode(500, "An error occurred while creating the meal plan");
        }
    }

    [HttpGet("{mealPlanId:int}")]
    public async Task<IActionResult> GetMealPlan(int mealPlanId)
    {
        try
        {
            var mealPlan = await _mealPlanService.GetMealPlanAsync(mealPlanId);
            if (mealPlan == null)
            {
                return NotFound();
            }

            return Ok(mealPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meal plan {MealPlanId}", mealPlanId);
            return StatusCode(500, "An error occurred while retrieving the meal plan");
        }
    }

    [HttpDelete("{mealPlanId:int}")]
    public async Task<IActionResult> DeleteMealPlan(int mealPlanId)
    {
        try
        {
            // Get the authenticated user's ID from the claims (same as UserController)
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var deleted = await _mealPlanService.DeleteMealPlanAsync(mealPlanId, userId);
            
            if (!deleted)
            {
                return NotFound("Meal plan not found or you don't have permission to delete it");
            }

            return Ok(new { Message = "Meal plan deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meal plan {MealPlanId}", mealPlanId);
            return StatusCode(500, "An error occurred while deleting the meal plan");
        }
    }

    [HttpGet("{mealPlanId:int}/details")]
    public async Task<IActionResult> GetMealPlanDetails(int mealPlanId)
    {
        try
        {
            var mealPlanDetails = await _mealPlanService.GetMealPlanDetailsAsync(mealPlanId);
            if (mealPlanDetails == null)
            {
                return NotFound();
            }

            return Ok(mealPlanDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meal plan details {MealPlanId}", mealPlanId);
            return StatusCode(500, "An error occurred while retrieving the meal plan details");
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserMealPlans(string userId)
    {
        try
        {
            var mealPlans = await _mealPlanService.GetUserMealPlansAsync(userId);
            return Ok(mealPlans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meal plans for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving meal plans");
        }
    }
}

public record CreateMealPlanRequest(
    string PlanName,
    int BreakfastCount,
    int LunchCount,
    int DinnerCount,
    int SnackCount);

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

            // Get meals per day from questionnaire, default to 3 if not provided
            var mealsPerDay = 3;
            if (user.QuestionnaireAnswers?.TryGetValue("mealsPerDay", out var mealsPerDayStr) == true)
            {
                if (int.TryParse(mealsPerDayStr, out var parsedMeals) && parsedMeals >= 3 && parsedMeals <= 5)
                {
                    mealsPerDay = parsedMeals;
                }
            }

            var result = await _mealPlanService.CreateMealPlanAsync(
                userId,
                request.PlanName,
                request.StartDate,
                request.EndDate,
                user.BmrWithActivityLevel.Value,
                mealsPerDay);

            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(new
            {
                MealPlanId = result.MealPlanId,
                Message = $"Meal plan created successfully with {mealsPerDay} meals per day"
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
    DateTime StartDate,
    DateTime? EndDate = null);


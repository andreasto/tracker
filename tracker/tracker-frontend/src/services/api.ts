// API Configuration
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

// Token Management
const ACCESS_TOKEN_KEY = 'access_token'
const REFRESH_TOKEN_KEY = 'refresh_token'

export const tokenManager = {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY)
  },
  
  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY)
  },
  
  setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  },
  
  removeTokens(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  },
  
  isAuthenticated(): boolean {
    return !!this.getAccessToken()
  }
}

// Enum matching backend UserOnboardingState
export enum UserOnboardingState {
  NotStarted = 0,
  Registered = 1,
  QuestionnaireAnswered = 2,
  StartValuesProvided = 3,
  Complete = 4
}

export interface User {
  userId: string
  name: string
  email: string
  onboardingState: number // 0=NotStarted, 1=Registered, 2=QuestionnaireAnswered, 3=StartValuesProvided, 4=Complete
  questionnaireAnswers?: Record<string, string>
  startWeight?: number
  measurements?: Record<string, number>
  bmrBase?: number
  bmrWithActivityLevel?: number
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  authenticated: boolean
  userId: string
  email: string
  name: string
  accessToken: string
  refreshToken: string
  expiresIn: number
}

export interface RefreshTokenResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
}

export interface AuthenticateResponse {
  authenticated: boolean
  userId: string
  token: string
  refreshToken: string
  expiresIn: number
}

export interface CreateUserRequest {
  name: string
  email: string
  password?: string
}

export interface AnswerQuestionnaireRequest {
  answers: Record<string, string>
}

export interface ProvideStartValuesRequest {
  startWeight: number
  measurements: Record<string, number>
}

export interface ApiError {
  message: string
}

export interface Measurements {
  weight: number
  thigh: number
  glutes: number
  hips: number
  waist: number
  stomach: number
  chest: number
  overarm: number
}

export interface WeeklyCheckIn {
  submittedAt: string
  measurements: Measurements
}

export interface CheckInState {
  userId: string
  weeklyCheckInDay?: number
  checkInHistory?: WeeklyCheckIn[]
}

export interface SubmitCheckInRequest {
  weight: number
  thigh: number
  glutes: number
  hips: number
  waist: number
  stomach: number
  chest: number
  overarm: number
}

export interface CreateMealPlanRequest {
  planName: string
  breakfastCount: number
  lunchCount: number
  dinnerCount: number
  snackCount: number
}

export interface CreateMealPlanResponse {
  mealPlanId: number
  message: string
}

export interface MealPlan {
  mealPlanId: number
  userId: string
  planName: string
  createdAt: string
  totalKcal?: number
  totalProtein?: number
  totalCarbs?: number
  totalFat?: number
}

export interface MealPlanMeal {
  mealPlanMealId: number
  mealPlanId: number
  recipeId?: number
  recipeName: string
  mealType: string
  kcal: number
  protein: number
  carbs: number
  fat: number
  mealOrder: number
}

export interface MealPlanDetails {
  plan: MealPlan
  mealsByType: MealTypeGroup[]
}

export interface MealTypeGroup {
  mealType: string
  meals: MealPlanMeal[]
}

// Recipe types
export interface Recipe {
  recipeId: number
  title: string
  description?: string
  totalCalories: number
  proteinG?: number
  fatG?: number
  carbsG?: number
  servings: number
  prepTimeMin?: number
  tags?: string
  instructions?: string
}

export interface Ingredient {
  ingredientId: number
  name: string
  caloriesPer100G?: number
  proteinPer100G?: number
  fatPer100G?: number
  carbsPer100G?: number
}

export interface RecipeIngredient {
  recipeId: number
  ingredientId: number
  quantityG: number
  ingredientName: string
}

export interface RecipeWithIngredients {
  recipe: Recipe
  ingredients: RecipeIngredient[]
}

export interface ScaledRecipe {
  baseRecipe: Recipe
  scaledIngredients: ScaledIngredient[]
  scalingFactor: number
  targetCalories: number
  actualCalories: number
  scaledProteinG?: number
  scaledFatG?: number
  scaledCarbsG?: number
}

export interface ScaledIngredient {
  ingredientName: string
  originalQuantityG: number
  scaledQuantityG: number
}

export interface CreateRecipeRequest {
  title: string
  description?: string
  totalCalories: number
  proteinG?: number
  fatG?: number
  carbsG?: number
  servings: number
  prepTimeMin?: number
  tags?: string
  instructions?: string
  ingredients: RecipeIngredient[]
}

export interface CreateIngredientRequest {
  name: string
  caloriesPer100G?: number
  proteinPer100G?: number
  fatPer100G?: number
  carbsPer100G?: number
}

export interface AssignRecipeToMealRequest {
  mealId: number
  recipeId: number
  targetCalories: number
}

// Helper function to get auth headers
function getAuthHeaders(): HeadersInit {
  const token = tokenManager.getAccessToken()
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  }
  
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }
  
  return headers
}

// Helper function to handle token refresh on 401 errors
async function refreshAccessToken(): Promise<boolean> {
  const refreshToken = tokenManager.getRefreshToken()
  
  if (!refreshToken) {
    return false
  }

  try {
    const response = await fetch(`${API_BASE_URL}/user/refresh-token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    })

    if (!response.ok) {
      return false
    }

    const data: RefreshTokenResponse = await response.json()
    tokenManager.setTokens(data.accessToken, data.refreshToken)
    return true
  } catch {
    return false
  }
}

// Helper function to make authenticated requests with automatic token refresh
async function fetchWithAuth(url: string, options: RequestInit = {}): Promise<Response> {
  // First attempt
  let response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      ...getAuthHeaders(),
    },
  })

  // If 401, try to refresh token and retry
  if (response.status === 401) {
    const refreshed = await refreshAccessToken()
    
    if (refreshed) {
      // Retry with new token
      response = await fetch(url, {
        ...options,
        headers: {
          ...options.headers,
          ...getAuthHeaders(),
        },
      })
    } else {
      // Refresh failed, remove tokens
      tokenManager.removeTokens()
    }
  }

  return response
}

// API Service
export const api = {
  // Authentication
  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await fetch(`${API_BASE_URL}/user/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Login failed')
    }

    const data: LoginResponse = await response.json()
    
    // Store tokens automatically on successful login
    if (data.authenticated && data.accessToken && data.refreshToken) {
      tokenManager.setTokens(data.accessToken, data.refreshToken)
    }
    
    return data
  },

  async logout(): Promise<void> {
    // Optionally revoke the refresh token on server
    const refreshToken = tokenManager.getRefreshToken()
    if (refreshToken) {
      try {
        await fetch(`${API_BASE_URL}/user/revoke-token`, {
          method: 'POST',
          headers: getAuthHeaders(),
          body: JSON.stringify({ refreshToken }),
        })
      } catch {
        // Ignore errors on logout
      }
    }
    
    tokenManager.removeTokens()
  },
  
  async refreshToken(): Promise<RefreshTokenResponse> {
    const refreshToken = tokenManager.getRefreshToken()
    
    if (!refreshToken) {
      throw new Error('No refresh token available')
    }

    const response = await fetch(`${API_BASE_URL}/user/refresh-token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    })

    if (!response.ok) {
      tokenManager.removeTokens()
      throw new Error('Failed to refresh token')
    }

    const data: RefreshTokenResponse = await response.json()
    tokenManager.setTokens(data.accessToken, data.refreshToken)
    
    return data
  },

  async getCurrentUser(): Promise<User> {
    const response = await fetchWithAuth(`${API_BASE_URL}/user/me`)

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Authentication required')
      }
      throw new Error('Failed to fetch current user')
    }

    return response.json()
  },

  async register(request: CreateUserRequest): Promise<{ userId: string; email: string; name: string; message: string; accessToken?: string; refreshToken?: string }> {
    const response = await fetch(`${API_BASE_URL}/user/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to register user')
    }

    const data = await response.json()
    
    // Store tokens automatically on successful registration
    if (data.accessToken && data.refreshToken) {
      tokenManager.setTokens(data.accessToken, data.refreshToken)
    }
    
    return data
  },

  async createUser(userId: string, request: CreateUserRequest): Promise<User> {
    // Legacy method - kept for backward compatibility
    const response = await fetch(`${API_BASE_URL}/user/${userId}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to create user')
    }

    return response.json()
  },

  async getUser(userId: string): Promise<User> {
    const response = await fetchWithAuth(`${API_BASE_URL}/user/${userId}`)

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Authentication required')
      }
      throw new Error('Failed to fetch user')
    }

    return response.json()
  },

  async answerQuestionnaire(
    userId: string,
    request: AnswerQuestionnaireRequest
  ): Promise<void> {
    const response = await fetchWithAuth(`${API_BASE_URL}/user/${userId}/questionnaire`, {
      method: 'POST',
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to submit questionnaire')
    }
  },

  async provideStartValues(
    userId: string,
    request: ProvideStartValuesRequest
  ): Promise<void> {
    const response = await fetchWithAuth(`${API_BASE_URL}/user/${userId}/start-values`, {
      method: 'POST',
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to submit start values')
    }
  },

  async completeOnboarding(userId: string): Promise<void> {
    const response = await fetchWithAuth(`${API_BASE_URL}/user/${userId}/complete-onboarding`, {
      method: 'POST',
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to complete onboarding')
    }
  },

  async getCheckIns(userId: string): Promise<CheckInState> {
    const response = await fetchWithAuth(`${API_BASE_URL}/checkin/${userId}`)

    if (!response.ok) {
      throw new Error('Failed to fetch check-ins')
    }

    return response.json()
  },

  async submitCheckIn(
    userId: string,
    request: SubmitCheckInRequest
  ): Promise<void> {
    const response = await fetchWithAuth(`${API_BASE_URL}/checkin/${userId}`, {
      method: 'POST',
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to submit check-in')
    }
  },

  async setCheckInDay(userId: string, checkInDay: number): Promise<void> {
    const response = await fetchWithAuth(`${API_BASE_URL}/checkin/${userId}/checkin-day`, {
      method: 'PUT',
      body: JSON.stringify({ checkInDay }),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to set check-in day')
    }
  },

  async setPassword(userId: string, password: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/user/${userId}/password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ password }),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to set password')
    }
  },

  async authenticate(userId: string, password: string): Promise<AuthenticateResponse> {
    const response = await fetch(`${API_BASE_URL}/user/${userId}/authenticate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ password }),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Authentication failed')
    }

    const data: AuthenticateResponse = await response.json()
    
    // Store tokens automatically on successful authentication
    if (data.authenticated && data.token && data.refreshToken) {
      tokenManager.setTokens(data.token, data.refreshToken)
    }
    
    return data
  },

  // Meal Plan APIs
  async createMealPlan(userId: string, request: CreateMealPlanRequest): Promise<CreateMealPlanResponse> {
    const response = await fetchWithAuth(`${API_BASE_URL}/mealplan/${userId}`, {
      method: 'POST',
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to create meal plan')
    }

    return response.json()
  },

  async getMealPlan(mealPlanId: number): Promise<MealPlan> {
    const response = await fetchWithAuth(`${API_BASE_URL}/mealplan/${mealPlanId}`)

    if (!response.ok) {
      throw new Error('Failed to fetch meal plan')
    }

    return response.json()
  },

  async getMealPlanDetails(mealPlanId: number): Promise<MealPlanDetails> {
    const response = await fetchWithAuth(`${API_BASE_URL}/mealplan/${mealPlanId}/details`)

    if (!response.ok) {
      throw new Error('Failed to fetch meal plan details')
    }

    return response.json()
  },

  async getUserMealPlans(userId: string): Promise<MealPlan[]> {
    const response = await fetchWithAuth(`${API_BASE_URL}/mealplan/user/${userId}`)

    if (!response.ok) {
      throw new Error('Failed to fetch user meal plans')
    }

    return response.json()
  },

  async deleteMealPlan(mealPlanId: number): Promise<{ message: string }> {
    const response = await fetchWithAuth(`${API_BASE_URL}/mealplan/${mealPlanId}`, {
      method: 'DELETE',
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to delete meal plan')
    }

    return response.json()
  },

  // Recipe APIs
  async createRecipe(request: CreateRecipeRequest): Promise<{ recipeId: number; message: string }> {
    const response = await fetchWithAuth(`${API_BASE_URL}/recipe`, {
      method: 'POST',
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to create recipe')
    }

    return response.json()
  },

  async getRecipe(recipeId: number): Promise<Recipe> {
    const response = await fetchWithAuth(`${API_BASE_URL}/recipe/${recipeId}`)

    if (!response.ok) {
      throw new Error('Failed to fetch recipe')
    }

    return response.json()
  },

  async getRecipeWithIngredients(recipeId: number): Promise<RecipeWithIngredients> {
    const response = await fetchWithAuth(`${API_BASE_URL}/recipe/${recipeId}/details`)

    if (!response.ok) {
      throw new Error('Failed to fetch recipe details')
    }

    return response.json()
  },

  async getAllRecipes(): Promise<Recipe[]> {
    const response = await fetchWithAuth(`${API_BASE_URL}/recipe`)

    if (!response.ok) {
      throw new Error('Failed to fetch recipes')
    }

    return response.json()
  },

  async searchRecipes(params: {
    searchTerm?: string
    minCalories?: number
    maxCalories?: number
    tags?: string
  }): Promise<Recipe[]> {
    const queryParams = new URLSearchParams()
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm)
    if (params.minCalories !== undefined) queryParams.append('minCalories', params.minCalories.toString())
    if (params.maxCalories !== undefined) queryParams.append('maxCalories', params.maxCalories.toString())
    if (params.tags) queryParams.append('tags', params.tags)

    const response = await fetchWithAuth(`${API_BASE_URL}/recipe/search?${queryParams}`)

    if (!response.ok) {
      throw new Error('Failed to search recipes')
    }

    return response.json()
  },

  async getScaledRecipe(recipeId: number, targetCalories: number): Promise<ScaledRecipe> {
    const response = await fetchWithAuth(
      `${API_BASE_URL}/recipe/${recipeId}/scale?targetCalories=${targetCalories}`
    )

    if (!response.ok) {
      throw new Error('Failed to fetch scaled recipe')
    }

    return response.json()
  },

  async findRecipesByCalories(targetCalories: number, tolerance: number = 0.2): Promise<ScaledRecipe[]> {
    const response = await fetchWithAuth(
      `${API_BASE_URL}/recipe/find-by-calories?targetCalories=${targetCalories}&tolerance=${tolerance}`
    )

    if (!response.ok) {
      throw new Error('Failed to find recipes by calories')
    }

    return response.json()
  },

  async assignRecipeToMeal(request: AssignRecipeToMealRequest): Promise<{ message: string }> {
    const response = await fetchWithAuth(`${API_BASE_URL}/recipe/assign-to-meal`, {
      method: 'POST',
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to assign recipe to meal')
    }

    return response.json()
  },

  // Ingredient APIs
  async createIngredient(request: CreateIngredientRequest): Promise<{ ingredientId: number; message: string }> {
    const response = await fetchWithAuth(`${API_BASE_URL}/recipe/ingredients`, {
      method: 'POST',
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to create ingredient')
    }

    return response.json()
  },

  async getAllIngredients(): Promise<Ingredient[]> {
    const response = await fetchWithAuth(`${API_BASE_URL}/recipe/ingredients`)

    if (!response.ok) {
      throw new Error('Failed to fetch ingredients')
    }

    return response.json()
  },

  async getIngredient(ingredientId: number): Promise<Ingredient> {
    const response = await fetchWithAuth(`${API_BASE_URL}/recipe/ingredients/${ingredientId}`)

    if (!response.ok) {
      throw new Error('Failed to fetch ingredient')
    }

    return response.json()
  },
}

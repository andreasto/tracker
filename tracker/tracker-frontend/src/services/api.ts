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
  startDate: string
  endDate?: string
}

export interface CreateMealPlanResponse {
  mealPlanId: number
  message: string
}

export interface MealPlan {
  mealPlanId: number
  userId: string
  planName: string
  startDate: string
  endDate?: string
  createdAt: string
  totalKcal?: number
  totalProtein?: number
  totalCarbs?: number
  totalFat?: number
}

export interface MealPlanDay {
  mealPlanDayId: number
  mealPlanId: number
  date: string
  dayNumber: number
}

export interface MealPlanMeal {
  mealPlanMealId: number
  mealPlanDayId: number
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
  days: DayWithMeals[]
}

export interface DayWithMeals {
  day: MealPlanDay
  meals: MealPlanMeal[]
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
}


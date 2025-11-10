// API Configuration
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

export interface User {
  userId: string
  name: string
  email: string
  onboardingState: string
  questionnaireAnswers?: Record<string, string>
  startWeight?: number
  measurements?: Record<string, number>
}

export interface CreateUserRequest {
  name: string
  email: string
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

// API Service
export const api = {
  async createUser(userId: string, request: CreateUserRequest): Promise<User> {
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
    const response = await fetch(`${API_BASE_URL}/user/${userId}`)

    if (!response.ok) {
      throw new Error('Failed to fetch user')
    }

    return response.json()
  },

  async answerQuestionnaire(
    userId: string,
    request: AnswerQuestionnaireRequest
  ): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/user/${userId}/questionnaire`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
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
    const response = await fetch(`${API_BASE_URL}/user/${userId}/start-values`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to submit start values')
    }
  },

  async completeOnboarding(userId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/user/${userId}/complete-onboarding`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || 'Failed to complete onboarding')
    }
  },
}


# Tracker Frontend

A React + Vite frontend application for the Tracker user registration and onboarding flow.

## Features

- **User Registration Flow**: Multi-step onboarding process
  1. Create user account (name, email, userId)
  2. Answer questionnaire (goals, activity level, experience)
  3. Provide starting measurements (weight and body measurements)
  4. Complete onboarding
  
- **Dashboard**: View user profile and onboarding data
- **Responsive Design**: Built with Tailwind CSS
- **Type Safety**: Full TypeScript support
- **API Integration**: Connects to the Tracker backend API

## Prerequisites

- Node.js (v18 or higher)
- npm or yarn
- Tracker backend API running on port 5000

## Getting Started

### 1. Install Dependencies

```bash
npm install
```

### 2. Configure Environment

Create a `.env` file or use the default configuration:

```
VITE_API_URL=http://localhost:5000
```

### 3. Start Development Server

```bash
npm run dev
```

The application will be available at `http://localhost:3000`

## Available Scripts

- `npm run dev` - Start development server with hot reload
- `npm run build` - Build for production
- `npm run preview` - Preview production build locally
- `npm run lint` - Run ESLint

## Project Structure

```
src/
├── components/          # React components
│   ├── RegistrationFlow.tsx
│   ├── UserRegistration.tsx
│   ├── Questionnaire.tsx
│   ├── StartValues.tsx
│   ├── CompletionScreen.tsx
│   └── Dashboard.tsx
├── services/           # API services
│   └── api.ts
├── App.tsx            # Main app component with routing
├── main.tsx           # Application entry point
└── index.css          # Global styles with Tailwind
```

## API Endpoints Used

The frontend interacts with the following backend endpoints:

- `POST /user/{userId}` - Create new user
- `GET /user/{userId}` - Fetch user data
- `POST /user/{userId}/questionnaire` - Submit questionnaire answers
- `POST /user/{userId}/start-values` - Submit starting measurements
- `POST /user/{userId}/complete-onboarding` - Complete onboarding

## User Flow

1. **Registration** (`/register`)
   - User enters name, email, and optional userId
   - System creates user account

2. **Questionnaire** (`/register/questionnaire`)
   - User answers questions about goals and activity level
   - Helps personalize the experience

3. **Measurements** (`/register/measurements`)
   - User enters starting weight (required)
   - Optional body measurements (chest, waist, hips, etc.)

4. **Completion** (`/register/complete`)
   - Review and confirm registration
   - Complete onboarding process

5. **Dashboard** (`/dashboard/{userId}`)
   - View profile and all submitted data
   - Starting point for tracking journey

## Technologies

- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **React Router** - Client-side routing
- **Tailwind CSS** - Utility-first CSS framework

## Development Notes

- The API URL can be configured via the `VITE_API_URL` environment variable
- Vite proxy is configured to forward `/user` requests to the backend
- All forms include validation and error handling
- Loading states and error messages are displayed to users

## Building for Production

```bash
npm run build
```

The built files will be in the `dist/` directory and can be served by any static file server.
VITE_API_URL=http://localhost:5000


# Tracker Frontend - Complete Setup Guide

## 🎉 Project Created Successfully!

A complete React + Vite frontend application with user registration flow has been created in the `tracker-frontend` directory.

---

## 📁 Project Structure

```
tracker-frontend/
├── src/
│   ├── components/
│   │   ├── RegistrationFlow.tsx      # Main registration flow coordinator
│   │   ├── UserRegistration.tsx      # Step 1: Create account
│   │   ├── Questionnaire.tsx         # Step 2: Answer questions
│   │   ├── StartValues.tsx           # Step 3: Enter measurements
│   │   ├── CompletionScreen.tsx      # Step 4: Complete onboarding
│   │   └── Dashboard.tsx             # User dashboard
│   ├── services/
│   │   └── api.ts                    # API service layer
│   ├── App.tsx                       # Main app with routing
│   ├── main.tsx                      # Application entry point
│   └── index.css                     # Global styles
├── public/                           # Static assets
├── index.html                        # HTML template
├── package.json                      # Dependencies and scripts
├── vite.config.ts                    # Vite configuration
├── tailwind.config.js                # Tailwind CSS config
├── tsconfig.json                     # TypeScript config
├── .env                              # Environment variables
└── README.md                         # Documentation
```

---

## 🚀 Getting Started

### Prerequisites

1. **Node.js** (v18 or higher) - Install from [nodejs.org](https://nodejs.org/)
2. **Backend API** running on port 5000
3. **PostgreSQL** (optional, for persistence)

### Installation Steps

1. **Install Dependencies**
   ```bash
   cd tracker-frontend
   npm install
   ```

2. **Configure Environment** (optional)
   
   Edit `.env` to change the API URL:
   ```
   VITE_API_URL=http://localhost:5000
   ```

3. **Start Development Server**
   ```bash
   npm run dev
   ```
   
   Or use the quick start script:
   ```bash
   ./start-dev.sh
   ```

4. **Open in Browser**
   
   Navigate to: http://localhost:3000

---

## 📋 Available Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start development server with hot reload |
| `npm run build` | Build for production |
| `npm run preview` | Preview production build |
| `npm run lint` | Run ESLint |

---

## 🎯 User Registration Flow

The application implements a complete 4-step onboarding flow:

### Step 1: User Registration (`/register`)
- Enter full name
- Enter email address
- Optional: Provide custom user ID (auto-generated if left blank)
- **API:** `POST /user/{userId}`

### Step 2: Questionnaire (`/register/questionnaire`)
- Primary goal (Lose Weight, Gain Muscle, etc.)
- Activity level (Sedentary to Extremely Active)
- Experience level (Beginner to Advanced)
- Personal motivation
- **API:** `POST /user/{userId}/questionnaire`

### Step 3: Measurements (`/register/measurements`)
- Starting weight (required)
- Body measurements (optional):
  - Chest
  - Waist
  - Hips
  - Thighs
  - Arms
- **API:** `POST /user/{userId}/start-values`

### Step 4: Completion (`/register/complete`)
- Review user ID
- Complete onboarding
- Navigate to dashboard
- **API:** `POST /user/{userId}/complete-onboarding`

### Dashboard (`/dashboard/{userId}`)
- View user profile
- See questionnaire answers
- Review starting measurements
- Track onboarding status

---

## 🔌 API Integration

### Backend Endpoints Used

The frontend integrates with these backend API endpoints:

```
POST   /user/{userId}                      - Create user
GET    /user/{userId}                      - Get user data
POST   /user/{userId}/questionnaire        - Submit questionnaire
POST   /user/{userId}/start-values         - Submit measurements
POST   /user/{userId}/complete-onboarding  - Complete onboarding
```

### API Configuration

The API URL is configured via the Vite proxy in `vite.config.ts`:

```typescript
server: {
  port: 3000,
  proxy: {
    '/user': {
      target: 'http://localhost:5000',
      changeOrigin: true,
    },
  },
}
```

---

## 🎨 Technologies Used

- **React 18** - UI library
- **TypeScript** - Type safety and better DX
- **Vite** - Fast build tool and dev server
- **React Router v6** - Client-side routing
- **Tailwind CSS** - Utility-first CSS framework
- **ESLint** - Code linting

---

## 🔧 Configuration

### Environment Variables

Create a `.env` file for environment-specific configuration:

```env
VITE_API_URL=http://localhost:5000
```

### Vite Configuration

The `vite.config.ts` includes:
- React plugin
- Development server on port 3000
- Proxy configuration for API requests

### TypeScript Configuration

Two TypeScript configs:
- `tsconfig.json` - Main app configuration
- `tsconfig.node.json` - Node/build tools configuration

---

## 🎨 Styling

The application uses **Tailwind CSS** for styling:

- Responsive design that works on mobile and desktop
- Dark/light mode support via CSS variables
- Consistent color scheme using Tailwind utilities
- Form validation states with visual feedback
- Loading states and error messages

### Color Scheme
- Primary: Blue (`blue-600`)
- Success: Green (`green-600`)
- Error: Red (`red-600`)
- Background: Gray (`gray-50`)

---

## 🧪 Testing the Application

### Manual Testing Flow

1. **Start the Backend API**
   ```bash
   cd src/tracker.App
   dotnet run
   ```

2. **Start the Frontend**
   ```bash
   cd tracker-frontend
   npm run dev
   ```

3. **Test Registration Flow**
   - Navigate to http://localhost:3000
   - Fill in registration form
   - Complete questionnaire
   - Enter measurements
   - Complete onboarding
   - View dashboard

### Test Data Examples

**User Registration:**
```json
{
  "name": "John Doe",
  "email": "john@example.com"
}
```

**Questionnaire:**
```json
{
  "answers": {
    "goal": "Lose Weight",
    "activityLevel": "Moderately Active",
    "experience": "Beginner",
    "motivation": "Feel healthier and more energetic"
  }
}
```

**Measurements:**
```json
{
  "startWeight": 85.5,
  "measurements": {
    "chest": 100,
    "waist": 90,
    "hips": 105,
    "thighs": 62,
    "arms": 36
  }
}
```

---

## 🚨 Troubleshooting

### Issue: Cannot connect to API

**Solution:**
1. Ensure backend is running: `dotnet run` in `src/tracker.App`
2. Check backend is on port 5000
3. Verify CORS is enabled in backend
4. Check `.env` file has correct API URL

### Issue: npm install fails

**Solution:**
1. Ensure Node.js v18+ is installed: `node --version`
2. Clear npm cache: `npm cache clean --force`
3. Delete `node_modules` and `package-lock.json`
4. Run `npm install` again

### Issue: TypeScript errors

**Solution:**
1. Ensure dependencies are installed
2. Restart your IDE/editor
3. Check `tsconfig.json` is correct

### Issue: Styles not loading

**Solution:**
1. Ensure Tailwind is configured: `tailwind.config.js`
2. Check `index.css` imports Tailwind directives
3. Rebuild: `npm run dev`

---

## 📦 Building for Production

### Build the Application

```bash
npm run build
```

This creates optimized files in the `dist/` directory.

### Preview Production Build

```bash
npm run preview
```

### Deploy

The `dist/` folder can be deployed to:
- Netlify
- Vercel
- GitHub Pages
- Any static hosting service
- Nginx/Apache

---

## 🔐 Security Considerations

### Current Implementation

- Basic form validation
- API error handling
- No authentication (planned for future)

### Recommendations for Production

1. **Add Authentication**
   - Implement JWT or OAuth
   - Secure API endpoints
   - Add login/logout flow

2. **Validate Input**
   - Server-side validation
   - Sanitize user input
   - Add rate limiting

3. **HTTPS**
   - Use HTTPS in production
   - Secure cookies
   - CSP headers

---

## 🎯 Next Steps

### Planned Features

1. **Authentication System**
   - User login/logout
   - Session management
   - Password reset

2. **Enhanced Dashboard**
   - Progress charts
   - Goal tracking
   - Historical data

3. **Additional Features**
   - Photo uploads
   - Social sharing
   - Notifications

### How to Extend

1. **Add New Routes**
   - Edit `src/App.tsx`
   - Add route in `<Routes>` component

2. **Add New API Calls**
   - Edit `src/services/api.ts`
   - Add TypeScript interfaces
   - Create new API methods

3. **Add New Components**
   - Create in `src/components/`
   - Import in relevant files
   - Use TypeScript for props

---

## 📚 Additional Resources

- [React Documentation](https://react.dev/)
- [Vite Documentation](https://vitejs.dev/)
- [React Router Documentation](https://reactrouter.com/)
- [Tailwind CSS Documentation](https://tailwindcss.com/)
- [TypeScript Documentation](https://www.typescriptlang.org/)

---

## 🤝 Support

For issues or questions:
1. Check the troubleshooting section
2. Review backend API documentation
3. Check browser console for errors
4. Verify network requests in DevTools

---

## ✅ Summary

You now have a fully functional React + Vite frontend with:

✅ Complete user registration flow  
✅ Multi-step onboarding process  
✅ API integration with backend  
✅ Responsive design with Tailwind CSS  
✅ TypeScript type safety  
✅ Error handling and validation  
✅ Dashboard for viewing user data  
✅ Development and production builds  

**Ready to start developing! 🚀**


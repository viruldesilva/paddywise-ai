# 🌾 PaddyWise Web Client ("Kumburu")

The official web frontend for the **PaddyWise-AI** decision-support platform, engineered with React 19, TypeScript, and Vite.

---

## 🛠️ Tech Stack

- **Framework**: [React 19](https://react.dev/) + [TypeScript](https://www.typescriptlang.org/)
- **Bundler & Dev Server**: [Vite](https://vitejs.dev/)
- **Routing**: [React Router v7](https://reactrouter.com/)
- **HTTP Client**: [Axios](https://axios-http.com/) with single-flight token refresh queue interceptor
- **Icons**: [Lucide React](https://lucide.dev/)
- **Design System**: Vanilla CSS tokens with modern typography (`Fraunces` + `Work Sans`)

---

## 🚀 Getting Started

### Prerequisites
- Node.js (v20+)
- npm

### Installation & Development Server

```bash
# Install dependencies
npm install

# Start Vite development server (starts on http://localhost:5173)
npm run dev

# Type check and build production bundle
npm run build

# Run ESLint
npm run lint

# Preview production build locally
npm run preview
```

> [!NOTE]
> The dev server runs on **port 5173**. The backend API CORS policy (`AllowReactApp`) expects requests from `http://localhost:5173`. The API client in `src/api/axiosInstance.ts` connects to `http://localhost:5164/api`.

---

## 📁 Source Code Organization

```text
src/
├── api/
│   └── axiosInstance.ts      # Configured Axios client with automatic 401 refresh token queue
├── assets/                   # Static branding and image assets
├── components/
│   ├── ProtectedRoute.tsx    # Role-based route guard
│   └── Sidebar.tsx           # Contextual navigation sidebar
├── context/
│   └── AuthContext.tsx       # Authentication state, login, register, token refresh
├── features/
│   ├── field-cultivation/    # Component 1: Fields, cycles, AI plan generation & approval
│   ├── crop-resource/        # Component 2: Activity logging & resource analysis
│   └── pest-disease/         # Component 3: Observations, photo upload & diagnostic reports
├── hooks/
│   └── useReveal.ts          # Scroll-reveal intersection observer hook
├── pages/
│   ├── Home.tsx              # Landing page
│   ├── LoginPage.tsx         # User authentication
│   ├── RegisterPage.tsx      # User registration with role selection
│   ├── DashboardPage.tsx     # Role-customized dashboard (Farmer, Officer, Admin)
│   └── UserManagementPage.tsx# Admin user administration
├── styles/
│   └── global.css            # Agronomic design system tokens & base typography
├── types/                    # Shared TypeScript interfaces & models
└── utils/
    └── roleRoutes.ts         # Role-specific route resolution
```

---

## 🎨 Design System & Colors

Styling relies on curated CSS variables in `src/styles/global.css`:

```css
--cream: #F6F1E3;        /* Parchment background */
--cream-deep: #EDE5CF;   /* Card / panel background */
--forest: #22392A;       /* Primary evergreen tone */
--forest-deep: #182A1E;  /* Deep contrast evergreen */
--shoot: #7FA66C;        /* Seedling green accent */
--shoot-light: #B4CB9C;  /* Soft green border / badge */
--gold: #E1A63B;         /* Golden harvest accent */
--gold-deep: #C48A28;    /* Warning / attention state */
--clay: #A6693F;         /* Earth soil accent */
--ink: #212D1E;          /* Primary dark text */
--ink-soft: #4B5645;     /* Secondary / muted body text */
--line: rgba(33,45,30,.14); /* Subtle separator borders */
```

Typography:
- **Headings**: `Fraunces`, serif
- **Body**: `Work Sans`, sans-serif

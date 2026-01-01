# Strong Towing Services - Angular Frontend

This is the Angular frontend application for Strong Towing Services, migrated from ASP.NET Core MVC to Angular 19.

## Features

### ✅ Completed
- **Home Page**: Hero slider, services overview, features, about section, FAQ, testimonials, blog, and partners
- **Services Page**: Comprehensive service listings, categories, emergency services, service areas, and request form
- **About Page**: Company story, mission & values, team, certifications, fleet & equipment, community involvement
- **Authentication**: Login, register, OTP verification, password reset
- **Customer Dashboard**: Order management, statistics, quick actions, recent orders
- **API Integration**: RESTful API controllers for backend communication
- **Responsive Design**: Mobile-first approach with Bootstrap-like styling
- **Modern UI**: Clean, professional design with smooth animations

### 🚧 Pending
- **Admin Dashboard**: Order management, driver management, analytics
- **Driver Dashboard**: Order acceptance, profile management, earnings tracking

## Technology Stack

- **Angular 19**: Latest version with standalone components
- **TypeScript**: Type-safe development
- **SCSS**: Enhanced CSS with variables and mixins
- **RxJS**: Reactive programming for API calls
- **Splide.js**: Modern slider/carousel library
- **Font Awesome**: Icon library
- **Responsive Design**: Mobile-first approach

## Project Structure

```
src/
├── app/
│   ├── components/
│   │   ├── home/              # Home page components
│   │   ├── hero/              # Hero slider component
│   │   ├── services/          # Services page
│   │   ├── about/             # About page
│   │   ├── auth/              # Authentication components
│   │   │   ├── login/
│   │   │   ├── register/
│   │   │   └── forgot-password/
│   │   ├── dashboard/         # Dashboard components
│   │   │   ├── customer/
│   │   │   ├── driver/
│   │   │   └── admin/
│   │   ├── header/            # Navigation header
│   │   └── footer/            # Site footer
│   ├── services/              # Angular services
│   │   ├── api.service.ts     # HTTP API service
│   │   ├── auth.service.ts    # Authentication service
│   │   └── order.service.ts   # Order management service
│   ├── models/                # TypeScript interfaces
│   │   ├── user.model.ts      # User-related models
│   │   ├── order.model.ts     # Order-related models
│   │   └── service.model.ts   # Service-related models
│   ├── app.component.ts       # Root component
│   ├── app.routes.ts          # Application routing
│   └── app.config.ts          # App configuration
├── environments/              # Environment configurations
└── styles.scss               # Global styles and CSS variables
```

## Getting Started

### Prerequisites
- Node.js (v18 or higher)
- npm or yarn
- Angular CLI

### Installation

1. Navigate to the project directory:
```bash
cd fontend/towing-web
```

2. Install dependencies:
```bash
npm install
```

3. Start the development server:
```bash
npm start
```

4. Open your browser and navigate to `http://localhost:4200`

### Build for Production

```bash
npm run build
```

The build artifacts will be stored in the `dist/` directory.

## API Integration

The frontend communicates with the ASP.NET Core backend through RESTful APIs:

- **Authentication**: `/api/auth/*`
- **Orders**: `/api/orders/*`
- **Services**: `/api/services/*`

### Environment Configuration

Update `src/environments/environment.ts` for development:
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7000/api',
  appName: 'Strong Towing Services',
  version: '1.0.0'
};
```

## Key Features

### 1. Responsive Design
- Mobile-first approach
- Breakpoints: 480px, 768px, 992px, 1200px
- Flexible grid layouts
- Touch-friendly interface

### 2. Modern UI Components
- Hero slider with Splide.js
- Interactive service cards
- Animated counters
- Smooth transitions
- Professional color scheme

### 3. Authentication System
- JWT-based authentication
- Role-based access control
- OTP verification
- Password reset functionality
- File upload support

### 4. State Management
- RxJS observables for reactive programming
- Service-based state management
- Local storage for persistence
- Error handling and loading states

## Development Guidelines

### Component Structure
- Use standalone components
- Implement OnPush change detection where possible
- Follow Angular style guide
- Use TypeScript strict mode

### Styling
- Use SCSS with CSS variables
- Follow BEM methodology
- Mobile-first responsive design
- Consistent spacing and typography

### API Integration
- Use Angular HTTP client
- Implement proper error handling
- Use TypeScript interfaces for type safety
- Follow RESTful conventions

## Deployment

### IIS Server Deployment

The project includes a `web.config` file that configures IIS to handle Angular routing properly. This prevents 403/404 errors when refreshing pages.

#### Deployment Steps:

1. Build the project:
```bash
npm run build
```

2. Deploy the contents of `dist/towing-web/browser/` to your IIS server.

3. The `web.config` file is already included in the build and will:
   - Rewrite all requests to `index.html` for Angular routing
   - Allow the Angular router to handle navigation
   - Prevent 403 errors when refreshing pages

#### IIS Configuration (if needed):

If you still experience issues, ensure the URL Rewrite module is installed on IIS:
1. Download and install [URL Rewrite Module for IIS](https://www.iis.net/downloads/microsoft/url-rewrite)
2. The `web.config` file will work automatically after installation

#### Troubleshooting 403 Errors:

If you get a 403 error when refreshing pages on IIS:
- Ensure the `web.config` file is in your web root directory
- Check that URL Rewrite module is installed
- Verify the Application Pool has proper permissions

### Build Configuration
The project is configured for production builds with:
- Tree shaking for smaller bundles
- AOT compilation
- CSS optimization
- Asset optimization

### Environment Variables
Configure production environment in `src/environments/environment.prod.ts`:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-api-domain.com/api',
  appName: 'Strong Towing Services',
  version: '1.0.0'
};
```

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Contributing

1. Follow the existing code style
2. Write meaningful commit messages
3. Test your changes thoroughly
4. Update documentation as needed

## License

This project is proprietary software for Strong Towing Services.

## Support

For technical support or questions, please contact the development team.
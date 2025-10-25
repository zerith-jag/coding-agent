# Coding Agent Dashboard

Angular 20.3 frontend application for the Coding Agent microservices platform.

## Overview

This is the web-based dashboard for interacting with the Coding Agent system. It provides a modern, responsive interface for managing conversations, tasks, and monitoring system activity.

## Technology Stack

- **Angular**: 20.3
- **Angular Material**: 20.2.10
- **SignalR**: 9.0.6 (@microsoft/signalr)
- **TypeScript**: 5.9.2
- **SCSS**: For styling

## Architecture

The application follows Angular best practices with a feature-based architecture:

```
src/app/
├── core/                    # Core services (singleton)
│   ├── services/
│   │   ├── api.service.ts        # HTTP API wrapper
│   │   └── signalr.service.ts    # SignalR WebSocket service
│   └── guards/                   # Route guards (future)
├── features/                # Feature modules
│   ├── dashboard/           # Main dashboard
│   ├── chat/                # Real-time chat interface
│   └── tasks/               # Task management
├── shared/                  # Shared components, pipes, directives
└── environments/            # Environment configurations
```

## Features

### Current Implementation

- ✅ **Base Layout**: Material toolbar, sidenav, and router-outlet
- ✅ **Routing**: Configured routes for dashboard, chat, and tasks
- ✅ **SignalR Service**: Ready for real-time communication
- ✅ **API Service**: HTTP client wrapper for backend APIs
- ✅ **Environment Configuration**: Dev and production settings
- ✅ **Material Design**: Angular Material components

### Future Features

- 🔲 Real-time chat with AI agents
- 🔲 Task creation and management
- 🔲 Execution monitoring
- 🔲 Performance metrics and charts
- 🔲 User authentication
- 🔲 WebSocket integration with backend services

## Getting Started

### Prerequisites

- Node.js 20.x or higher
- npm 10.x or higher
- Angular CLI 20.3

### Installation

```bash
# Navigate to the frontend directory
cd src/Frontend/coding-agent-dashboard

# Install dependencies
npm install
```

### Development Server

```bash
# Start the development server
npm start

# Or using Angular CLI
ng serve
```

Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

### Build

```bash
# Development build
npm run build

# Production build
ng build --configuration production
```

The build artifacts will be stored in the `dist/` directory.

## Configuration

### Environment Files

- `src/environments/environment.ts` - Development configuration
- `src/environments/environment.prod.ts` - Production configuration

Current settings:
```typescript
environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  signalRUrl: 'http://localhost:5000/hubs/chat',
  version: '2.0.0'
}
```

### API Integration

The `ApiService` provides a simple wrapper around Angular's HttpClient:

```typescript
// Example usage
constructor(private api: ApiService) {}

// GET request
this.api.get<Task[]>('/tasks').subscribe(tasks => {
  console.log(tasks);
});

// POST request
this.api.post<Task>('/tasks', taskData).subscribe(task => {
  console.log('Task created:', task);
});
```

### SignalR Integration

The `SignalRService` handles WebSocket connections:

```typescript
// Example usage
constructor(private signalR: SignalRService) {}

async ngOnInit() {
  // Connect to SignalR hub
  await this.signalR.connect();
  
  // Listen for events
  this.signalR.on<Message>('ReceiveMessage', (message) => {
    console.log('New message:', message);
  });
  
  // Invoke hub methods
  await this.signalR.invoke('SendMessage', conversationId, content);
}
```

## Project Structure

```
coding-agent-dashboard/
├── src/
│   ├── app/                 # Application source
│   ├── environments/        # Environment configs
│   ├── index.html           # Main HTML file
│   ├── main.ts              # Application entry point
│   └── styles.scss          # Global styles
├── public/                  # Static assets
├── angular.json             # Angular CLI configuration
├── package.json             # npm dependencies
├── tsconfig.json            # TypeScript configuration
└── README.md                # This file
```

## Development Guidelines

### Component Creation

Use Angular CLI to generate new components:

```bash
# Generate a new component
ng generate component features/my-feature

# Generate a service
ng generate service core/services/my-service
```

### Code Style

- Follow Angular style guide
- Use standalone components (no NgModules)
- Use signals for reactive state management
- Prefer TypeScript strict mode
- Use SCSS for component styles

### Testing

```bash
# Run unit tests
npm test

# Run tests with coverage
ng test --code-coverage
```

## Integration with Backend

This dashboard integrates with the following backend services:

- **Gateway** (http://localhost:5000) - API Gateway (YARP)
- **Chat Service** - Real-time messaging with SignalR
- **Orchestration Service** - Task execution and monitoring
- **Dashboard BFF** - Backend-for-Frontend aggregation

## Troubleshooting

### Port 4200 Already in Use

```bash
# Kill the process using port 4200
lsof -ti:4200 | xargs kill -9

# Or use a different port
ng serve --port 4201
```

### Build Errors

```bash
# Clear Angular cache
rm -rf .angular/cache

# Reinstall dependencies
rm -rf node_modules package-lock.json
npm install
```

## Contributing

1. Create a feature branch
2. Make your changes
3. Run tests: `npm test`
4. Build: `npm run build`
5. Submit a pull request

## License

See the main repository LICENSE file.

## Version

**Current Version**: 2.0.0

Part of the Coding Agent v2.0 microservices platform.

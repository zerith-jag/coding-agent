# 🤖 Coding Agent - AI-Powered Microservices Platform

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular 20](https://img.shields.io/badge/Angular-20.3-DD0031?logo=angular)](https://angular.io/)
[![Python](https://img.shields.io/badge/Python-3.11+-3776AB?logo=python)](https://www.python.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> **An enterprise-grade AI coding assistant built with microservices architecture**

A sophisticated coding assistant platform that combines real-time chat, task orchestration, ML-powered classification, and automated GitHub operations—all built with modern microservices principles.

---

## 🌟 Features

- **💬 Real-time Chat** - SignalR-powered WebSocket communication
- **🤖 AI Task Orchestration** - Intelligent agent coordination and execution
- **🧠 ML Classification** - Hybrid heuristic + ML task categorization
- **🔧 GitHub Integration** - Automated repository operations and PR management
- **🌐 Browser Automation** - Playwright-powered web interaction
- **📊 CI/CD Monitoring** - Build tracking and automated fixes
- **📈 Observability** - OpenTelemetry with Prometheus, Grafana, and Jaeger
- **🚀 Scalable Architecture** - Independent service deployment and scaling

---

## 🏗️ Architecture

### Microservices (8 Services)

```mermaid
graph TB
    Client[Angular Dashboard] --> Gateway[API Gateway - YARP]
    Gateway --> Chat[Chat Service]
    Gateway --> Orch[Orchestration Service]
    Gateway --> GitHub[GitHub Service]
    Gateway --> Browser[Browser Service]
    Gateway --> CICD[CI/CD Monitor]
    Gateway --> Dashboard[Dashboard BFF]

    Orch --> ML[ML Classifier - Python]

    Chat --> RabbitMQ[(RabbitMQ)]
    Orch --> RabbitMQ
    GitHub --> RabbitMQ

    Chat --> Postgres[(PostgreSQL)]
    Orch --> Postgres
    GitHub --> Postgres

    Chat --> Redis[(Redis Cache)]
    Orch --> Redis
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 9 Minimal APIs, Python FastAPI |
| **Frontend** | Angular 20.3 + NgRx Signal Store |
| **Gateway** | YARP (Yet Another Reverse Proxy) |
| **Messaging** | RabbitMQ + MassTransit |
| **Database** | PostgreSQL (per-service schemas) |
| **Cache** | Redis |
| **Real-time** | SignalR (WebSocket) |
| **Observability** | OpenTelemetry → Prometheus + Grafana + Jaeger |
| **Deployment** | Docker Compose (dev), Kubernetes (prod) |

---

## 📚 Documentation

Comprehensive documentation is available in the [`docs/`](./docs) directory:

### Architecture Guides
- **[📖 Overview](./docs/00-OVERVIEW.md)** - System architecture and design decisions
- **[📋 Service Catalog](./docs/01-SERVICE-CATALOG.md)** - Detailed service specifications
- **[🗓️ Roadmap](./docs/02-IMPLEMENTATION-ROADMAP.md)** - 6-month implementation plan
- **[📁 Solution Structure](./docs/03-SOLUTION-STRUCTURE.md)** - Monorepo layout and CI/CD
- **[🧠 ML & Orchestration ADR](./docs/04-ML-AND-ORCHESTRATION-ADR.md)** - ML architecture decisions
- **[⚡ Quick Start](./docs/QUICK-START.md)** - Quick reference guide

### API Documentation
- API specifications will be available in `docs/api/` (Phase 1)
- OpenAPI/Swagger endpoints for each service

### Contributing
- **[Contributing Guide](./.github/CONTRIBUTING.md)** - How to contribute
- **[Code of Conduct](./.github/CODE_OF_CONDUCT.md)** - Community guidelines
- **[Security Policy](./.github/SECURITY.md)** - Reporting vulnerabilities
- **[Copilot Guide](./.github/COPILOT.md)** - GitHub Copilot best practices

---

## 🚀 Quick Start

### Prerequisites

- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 20+** - [Download](https://nodejs.org/)
- **Python 3.11+** - [Download](https://www.python.org/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/)

### Clone & Setup

```bash
# Clone the repository
git clone https://github.com/zerith-jag/coding-agent.git
cd coding-agent

# Setup will be available in Phase 0 (Week 2)
# Stay tuned for docker-compose.yml and setup scripts
```

### Development Roadmap

**Current Status**: ✅ Architecture Design Complete
**Next Phase**: Phase 0 - POC Implementation (Week 2)

| Phase | Timeline | Status |
|-------|----------|--------|
| Phase 0: POC | Weeks 1-2 | ⏳ In Progress |
| Phase 1: Infrastructure | Weeks 3-6 | 📋 Planned |
| Phase 2: Core Services | Weeks 7-12 | 📋 Planned |
| Phase 3: Integration | Weeks 13-16 | 📋 Planned |
| Phase 4: Frontend | Weeks 17-20 | 📋 Planned |
| Phase 5: Migration | Weeks 21-22 | 📋 Planned |
| Phase 6: Stabilization | Weeks 23-24 | 📋 Planned |

**Target Completion**: April 2026

---

## 🎯 Project Goals

### Technical Metrics
- ✅ **API Latency**: p95 < 500ms
- ✅ **Test Coverage**: 85%+ (unit + integration)
- ✅ **Build Time**: < 5 min per service
- ✅ **Deployment**: Daily per service
- ✅ **Availability**: 99.5%+ uptime

### Business Impact
- 🎯 **Feature Velocity**: 2x faster development
- 🎯 **Onboarding**: < 1 day for new developers
- 🎯 **Incident Recovery**: < 5 min
- 🎯 **Cost Efficiency**: 30% reduction via independent scaling

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](./.github/CONTRIBUTING.md) for details.

### Development Workflow
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📋 Project Status

### ✅ Completed
- Architecture design and documentation
- Service specifications and API contracts
- Implementation roadmap and milestones
- CI/CD strategy and testing approach
- Solution structure and monorepo layout

### 🚧 In Progress
- POC: API Gateway + Chat Service
- Initial Docker Compose setup
- Development environment setup

### 📋 Upcoming
- Full infrastructure stack (Phase 1)
- Core service implementation (Phase 2)
- Frontend dashboard (Phase 4)

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

This project was designed with assistance from:
- **GitHub Copilot** - AI-assisted architecture and development
- **Microsoft eShopOnContainers** - Microservices inspiration
- **Clean Architecture** - Domain-driven design principles
- **.NET Community** - Best practices and patterns

---

## 📞 Support & Community

- **Documentation**: [docs/](./docs)
- **Issues**: [GitHub Issues](https://github.com/zerith-jag/coding-agent/issues)
- **Discussions**: [GitHub Discussions](https://github.com/zerith-jag/coding-agent/discussions)

---

## 🔗 Related Links

- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Angular Documentation](https://angular.io/docs)
- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)

---

<p align="center">
  <strong>🎯 Building the Future of AI Coding Assistants</strong>
</p>

<p align="center">
  Made with ❤️ by <a href="https://github.com/zerith-jag">zerith-jag</a>
</p>

<p align="center">
  <sub>Last Updated: October 24, 2025</sub>
</p>

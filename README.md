# Squawker - A Robust Social Messaging Platform

Squawker is a modern social messaging platform built with Clean Architecture principles and cloud-native design. This platform allows users to post short messages called "squawks" with built-in validation, rate limiting, and telemetry.

## Features

- **User Messaging**: Create, retrieve, and manage short messages ("squawks")
- **Content Validation**: Automatic filtering of banned terms and duplicate content
- **Rate Limiting**: Prevents rapid-fire posting (20-second minimum between posts)
- **Monitoring**: Comprehensive telemetry and observability
- **Clean Architecture**: Domain-driven design with clear separation of concerns

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code

### Build

Run the following command to build the solution:

```bash
dotnet build -tl
```

### Run Locally

To run the web application:

```bash
cd .\src\Web\
dotnet watch run
```

Navigate to https://localhost:5001. The application will automatically reload if you change any of the source files.

## Monitoring and Telemetry

Squawker includes comprehensive monitoring and telemetry:

- **Performance Metrics**: Track API response times and throughput
- **User Activity**: Monitor squawk creation and retrieval patterns
- **Content Validation**: Track banned term usage and duplicate content attempts
- **Custom Dimensions**: Filter telemetry by user, operation, and content type

## Testing

The solution contains comprehensive test suites:

- **Unit Tests**: Verify business logic and validation rules
- **Integration Tests**: Test repository and database interactions
- **Functional Tests**: End-to-end API testing

Run tests with:

```bash
dotnet test
```

## Development Guidelines

### Code Scaffolding

The template includes support to scaffold new commands and queries.

Start in the Application folder.

Create a new command:

```bash
dotnet new ca-usecase --name CreateSquawk --feature-name Squawks --usecase-type command --return-type Guid
```

Create a new query:

```bash
dotnet new ca-usecase -n GetSquawks -fn Squawks -ut query -rt SquawksVm
```

If you encounter the error *"No templates or subcommands found matching: 'ca-usecase'."*, install the template and try again:

```bash
dotnet new install Clean.Architecture.Solution.Template::9.0.8
```

### Code Styles & Formatting

The project includes [EditorConfig](https://editorconfig.org/) support to help maintain consistent coding styles across various editors and IDEs. The **.editorconfig** file defines coding styles applicable to this solution.

## Project Structure

- **Domain**: Core entities, enums, exceptions, and logic
- **Application**: Business logic and commands/queries
- **Infrastructure**: External concerns like databases and identity
- **Web**: API controllers and UI components
- **Tests**: Unit, integration, and functional tests

## Telemetry Events

Squawker tracks the following events:

- `squawk.banned_term.count`: Number of posts containing banned terms
- `squawk.duplicate.count`: Number of duplicate content submissions
- `result.found`: Whether a squawk lookup found results
- `query.result_count`: Number of squawks returned in queries
- `db.operation`: Database operation type (insert, query, etc.)

## Contributing

1. Create a feature branch (`git checkout -b feature/amazing-feature`)
2. Commit your changes (`git commit -m 'Add some amazing feature'`)
3. Push to the branch (`git push origin feature/amazing-feature`)
4. Open a Pull Request

## Help

To learn more about the template go to the [Clean Architecture project website](https://github.com/jasontaylordev/CleanArchitecture).

For Azure best practices and guidance, refer to:
- [Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/)
- [Cloud Adoption Framework](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/)
- [Well-Architected Framework](https://learn.microsoft.com/en-us/azure/architecture/framework/)

---

Built with ❤️ using Clean Architecture and cloud-native design principles.
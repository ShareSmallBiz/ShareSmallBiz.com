# ShareSmallBiz.com

ShareSmallBiz.com is a web application built with .NET 9 and Bootstrap 5. 

## Features

- Modern web technologies: .NET 9, Bootstrap 5
- Scalable and versatile architecture

## Getting Started

1. Clone the repository
2. Install dependencies
3. Run the application locally

## EF Migrations

From `ShareSmallBiz.Portal`, author new EF Core migrations into the unified `Migrations` folder with the local tool manifest:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add AddYourChange --output-dir Migrations --context ShareSmallBizUserContext
```

To verify the model still scaffolds a clean temporary migration before creating a real one, run:

```powershell
npm run ef:migration:check
```


# Content Management System (CMS)

A comprehensive .NET 9.0-based Content Management System built with ASP.NET Core Razor Pages, featuring user management, financial accounts, KYC processing, product management, and transaction handling.

## 🚀 Features

- **User Management**: Complete user registration, authentication, and profile management
- **Account Management**: Financial account creation and management system
- **KYC (Know Your Customer)**: Customer verification and compliance processing
- **Product Management**: Product catalog and inventory management
- **Transaction Processing**: Financial transaction handling and tracking
- **Responsive Design**: Modern, mobile-friendly interface with Bootstrap

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 9.0 (Razor Pages)
- **Database**: Entity Framework Core with support for:
  - SQL Server
  - PostgreSQL
- **Frontend**: 
  - Bootstrap for responsive design
  - jQuery for interactive functionality
  - Custom CSS styling
- **Authentication**: ASP.NET Core Identity (planned/custom implementation)
- **Environment Management**: DotNetEnv for configuration

## 📁 Project Structure

```
CMS/
├── CMS.Core/           # Core business logic and shared models
├── CMS.Desktop/        # Desktop application (if applicable)
├── CMS.Web/           # Main web application
│   ├── Data/          # Database context and configurations
│   ├── Models/        # Data models and entities
│   ├── Pages/         # Razor Pages
│   ├── wwwroot/       # Static files (CSS, JS, images)
│   └── Properties/    # Application settings
├── CMS.sln           # Visual Studio solution file
└── README.md         # This file
```

## 🏗️ Database Models

The system includes the following core entities:

- **User**: User account information and authentication
- **Account**: Financial accounts associated with users
- **Product**: Product catalog and inventory
- **Transaction**: Financial transactions and transfers
- **KycRequest**: Know Your Customer verification requests

## 🚦 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) or [PostgreSQL](https://www.postgresql.org/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd CMS
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure the database**
   - Update the connection string in `appsettings.json` or `appsettings.Development.json`
   - Choose between SQL Server or PostgreSQL based on your preference

4. **Apply database migrations**
   ```bash
   cd CMS.Web
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

## ⚙️ Configuration

### Database Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your connection string here"
  }
}
```

### Environment Variables

Create a `.env` file in the root directory for sensitive configuration:

```env
DATABASE_URL=your_database_connection_string
SECRET_KEY=your_secret_key
```

## 📋 Available Pages

- **Home** (`/`): Main dashboard and landing page
- **Users** (`/Users`): User management interface
- **Accounts** (`/Accounts`): Financial account management
- **Products** (`/Products`): Product catalog management
- **Transactions** (`/Transactions`): Transaction history and processing
- **KYC** (`/KYC`): Customer verification management
- **Login** (`/Login`): User authentication

## 🧪 Development

### Running in Development Mode

```bash
dotnet run --environment Development
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## 📝 Database Migrations

To create a new migration:

```bash
dotnet ef migrations add MigrationName
```

To apply migrations:

```bash
dotnet ef database update
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

If you encounter any issues or have questions, please:

1. Check the [Issues](../../issues) page for existing solutions
2. Create a new issue with detailed information about the problem
3. Include relevant logs and error messages

## 🔮 Roadmap

- [ ] Implement user authentication and authorization
- [ ] Add role-based access control
- [ ] Implement real-time notifications
- [ ] Add API endpoints for mobile integration
- [ ] Implement advanced reporting features
- [ ] Add multi-language support
- [ ] Implement automated testing suite

---

**Built with ❤️ using ASP.NET Core 9.0**

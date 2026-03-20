# 🥗 BantayNutrisyon — School Feeding Program Nutrition Monitor

> **IT Elective 2 — Final Project / Exam**
> C# Application Development for Sustainable Development
> National Teachers College · BSIT 3.2 · March 2026

---

## 📌 SDG Alignment

**United Nations Sustainable Development Goal 2: Zero Hunger**
**Target 2.2** — End all forms of malnutrition, including achieving the internationally agreed targets on stunting and wasting in children under five years of age, and address the nutritional needs of adolescent girls, pregnant and lactating women, and older persons.

BantayNutrisyon directly supports the **Department of Education's School-Based Feeding Program (SBFP)** by replacing fragmented paper-based records with an organized digital monitoring system that provides real-time visibility into each child's nutritional journey. It addresses the critical gap of inadequate data-driven monitoring at the school level — a root cause identified in multiple Philippine educational nutrition studies.

---

## 📖 Project Description

**BantayNutrisyon** (Filipino: *to watch over nutrition*) is a **C# Windows Forms Application** built to support school administrators and nutritionists in monitoring, recording, and evaluating the nutritional status of students enrolled in the DepEd SBFP.

### What it does

- Records each student's **daily meal attendance and nutrient intake** (macronutrients and micronutrients)
- Automatically **calculates nutritional deficit scores** against DOH RENI 2015 standards
- **Classifies students** as Normal, At-Risk, or Malnourished based on weighted deficit thresholds
- Displays **real-time visual charts** (ScottPlot) of nutritional trends and status distribution
- Generates **asynchronous nutrition reports** in both TXT and JSON formats
- Supports **JSON backup and restore** for data portability and recovery
- Provides **role-based access** for Admins and Nutritionists with BCrypt-hashed authentication
- Logs all application activity via **Serilog rolling log files**

### Architecture

The application follows strict **N-Tier Architecture** with four clearly separated layers:

| Layer | Project | Responsibility |
|---|---|---|
| Presentation | `NutritionMonitor.UI` | Windows Forms — zero business logic |
| Business Logic | `NutritionMonitor.BLL` | Deficit calculation, validation, reporting |
| Data Access | `NutritionMonitor.DAL` | Repository Pattern via Entity Framework Core |
| Models | `NutritionMonitor.Models` | DTOs, Entities, Interfaces, Enums |

Cross-cutting concerns (Security, Logging, Exception Handling) are implemented globally across all tiers via BCrypt.Net-Next, Serilog, and registered `Application.ThreadException` hooks.

---

## ⚙️ Installation & Usage

### Prerequisites

| Requirement | Version |
|---|---|
| Operating System | Windows 10 / 11 (64-bit) |
| .NET Runtime | .NET 10 (Windows) |
| IDE (optional) | Visual Studio 2022 or later |

> No paid licenses, databases servers, or GPU drivers required. All dependencies are free NuGet packages.

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/[GROUP_NAME]/M2STZ_SDG_PROJECT.git
   cd M2STZ_SDG_PROJECT
   ```

2. **Restore dependencies and build**
   ```bash
   cd CODE
   dotnet restore
   dotnet build
   ```

3. **Run the application**
   ```bash
   dotnet run --project NutritionMonitor.UI
   ```
   The SQLite database is created automatically at first launch at:
   `%AppData%\NutritionMonitor\nutrition.db`

4. **Login with the seeded admin account**
   | Field | Value |
   |---|---|
   | Email | `admin@nutrition.local` |
   | Password | `Admin@123` |

### Using the Application

| Module | Description |
|---|---|
| **Dashboard** | Summary stats — total students, meal logs today, at-risk/malnourished counts |
| **Students** | Add, edit, delete, and search student profiles (CRUD) |
| **Meal Logs** | Record daily meal entries with full macro and micronutrient data |
| **Nutrition Analysis** | Run per-student or batch deficit analysis against DOH RENI standards |
| **Visualizations** | ScottPlot charts — Nutrient Trend, Status Overview, Deficit vs RENI |
| **Reports** | Generate async TXT / JSON nutritional summary reports |
| **Backup & Restore** | Export all data to JSON; restore from backup file |
| **Application Logs** | View Serilog rolling log files with level filtering and search |

---

## 📁 Repository Structure

```
M2STZ_SDG_PROJECT/
├── .gitignore
├── README.md
├── CODE/
│   ├── NutritionMonitor.slnx
│   ├── NutritionMonitor.UI/          # Presentation Layer (WinForms)
│   ├── NutritionMonitor.BLL/         # Business Logic Layer
│   ├── NutritionMonitor.DAL/         # Data Access Layer (EF Core + SQLite)
│   └── NutritionMonitor.Models/      # Shared DTOs, Entities, Interfaces
├── INPUT_DATA/
│   ├── initial_seed.json
│   └── nutrition.db
└── DOCUMENTATION/
    ├── SDAD_M2STZ.pdf
    ├── Flowchart_CoreAlgorithm.png
    └── Database_Schema_ERD.png
```

---

## 🧱 Tech Stack

| Technology | Purpose |
|---|---|
| C# / .NET 10 Windows | Core language and runtime |
| Windows Forms | Desktop UI framework |
| Entity Framework Core 10 | ORM and database migrations |
| SQLite | Embedded local database |
| ScottPlot 5 | Nutrition trend and deficit charts |
| BCrypt.Net-Next | Password hashing with per-user salt |
| Serilog + File Sink | Structured rolling log files |
| System.Text.Json | JSON backup/restore and report export |
| Microsoft.Extensions.DependencyInjection | IoC container |

---

## 👥 Contributors

| Name | Role | Primary Contribution |
|---|---|---|
| **Tavera, Rhence Bryan E.** | Project Manager, Documentation Lead & AI Strategist | Project coordination, sprint planning, SDAD documentation, GitHub repository management, AppDbContext setup, BaseRepository\<T\> |
| **Marcella, Justine S.** | Backend Developer | NutritionCalculatorService (RENI deficit algorithm), StudentService, ReportService, UserService, RENI Comparison Engine, deployment setup |
| **Setenta, Kurt Danielle P.** | Frontend Developer & Graphic Designer | LoginForm, DashboardForm, StudentForm, MealLogForm, ReportsForm UI, async event wiring, all visual assets and icons |
| **Moral, Justine Carlo R.** | Operations Support | Global exception handler, Serilog rolling log integration, JSON backup module, JSON restore module, cross-team testing support |
| **Zarate, Kurt Russel B.** | QA Engineer, Data Analyst & Research Lead | Test case development, bug detection, ScottPlot chart implementation (ReportsForm), Trend analysis, RRL/RRS sourcing, Big O Annotation, Models/DTOs/Enums design, algorithm validation |

---

## 📚 References

- Department of Health. (2015). *Philippine Dietary Reference Intakes (RENI 2015)*. DOH Philippines.
- Sadag, M. D. D. (2025). The extent implementation of school based feeding program. *Journal of Education, Society and Behavioural Science, 38*(4), 60–76. https://doi.org/10.9734/jesbs/2025/v38i41401
- Castigador, D. C. (2023). School-based feeding program in the Province of Iloilo. Central Philippine University. https://repository.cpu.edu.ph/handle/20.500.12852/3198
- United Nations. (n.d.). *Goal 2: Zero Hunger*. https://sdgs.un.org/goals/goal2
- Anthropic. (2025). *Claude Sonnet 4.6* [Large language model]. https://www.anthropic.com

---

*Submitted to Ms. Justin Louise R. Neypes · IT Elective 2 · National Teachers College · March 2026*

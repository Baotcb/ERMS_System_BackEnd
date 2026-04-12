# 🏢 ERMS System - Backend API

> **Enterprise Recruitment Management System** — Hệ thống Quản lý Tuyển dụng Doanh nghiệp

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Azure Pipelines](https://img.shields.io/badge/CI%2FCD-Azure%20Pipelines-0078D7?logo=azurepipelines&logoColor=white)](https://azure.microsoft.com/en-us/products/devops/pipelines)

---

## 📋 Mục lục

- [Giới thiệu](#-giới-thiệu)
- [Tính năng chính](#-tính-năng-chính)
- [Kiến trúc hệ thống](#-kiến-trúc-hệ-thống)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Yêu cầu hệ thống](#-yêu-cầu-hệ-thống)
- [Cài đặt & Chạy ứng dụng](#-cài-đặt--chạy-ứng-dụng)
- [Cấu hình](#-cấu-hình)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [API Endpoints](#-api-endpoints)
- [Hệ thống phân quyền](#-hệ-thống-phân-quyền)
- [CI/CD Pipeline](#-cicd-pipeline)
- [Docker](#-docker)
- [Testing](#-testing)
- [Đóng góp](#-đóng-góp)

---

## 📖 Giới thiệu

**ERMS (Enterprise Recruitment Management System)** là hệ thống quản lý tuyển dụng toàn diện dành cho doanh nghiệp, được xây dựng trên nền tảng **ASP.NET Core 10** với kiến trúc **Clean Architecture** kết hợp pattern **CQRS (Command Query Responsibility Segregation)**.

Hệ thống hỗ trợ toàn bộ quy trình tuyển dụng từ **lập kế hoạch → đăng tin → nhận hồ sơ → sàng lọc CV bằng AI → phỏng vấn → đào tạo nhân viên mới**, giúp doanh nghiệp số hóa và tối ưu hóa quy trình nhân sự.

---

## ✨ Tính năng chính

### 🔐 Xác thực & Phân quyền
- Đăng nhập bằng **Email/Password** và **Google OAuth 2.0**
- Xác thực qua **JWT Bearer Token** (Header hoặc Cookie)
- Phân quyền theo **7 vai trò** (Admin, Director, HRManager, DepartmentHead, Trainer, Employee, Candidate)
- Rate Limiting bảo vệ API

### 📝 Quản lý Tuyển dụng
- Lập kế hoạch tuyển dụng (Recruitment Plans)
- Tạo chiến dịch tuyển dụng (Recruitment Campaigns)
- Quản lý chi tiết kế hoạch (Plan Details)
- Đăng tin tuyển dụng (Job Postings)
- Trang việc làm công khai (Public Jobs)

### 📄 Quản lý Hồ sơ Ứng tuyển
- Nộp hồ sơ ứng tuyển trực tuyến
- Upload CV lên **Cloudinary**
- **Chấm điểm CV tự động bằng AI** (Groq AI / Gemini AI)
- Trích xuất nội dung CV từ PDF
- Xử lý CV ngầm (Background Processing)

### 🎤 Phỏng vấn
- Lên lịch phỏng vấn
- Tích hợp **Zoom** tạo phòng họp trực tuyến
- Tích hợp **Google Calendar** tạo sự kiện

### 🏢 Quản lý Doanh nghiệp
- Quản lý thông tin doanh nghiệp (Enterprise)
- Quản lý phòng ban (Departments)
- Quản lý nhân viên (Employees)
- Quản lý kỹ năng (Skills)

### 📚 Đào tạo
- Quản lý khóa đào tạo (Courses)
- Quản lý bài học (Lessons)
- Kiểm tra trắc nghiệm (Quizzes)
- Quản lý kế hoạch đào tạo (Training Plans)
- Yêu cầu đào tạo (Training Requests)
- Chứng chỉ (Certifications)

### 💳 Thanh toán & Gói dịch vụ
- Tích hợp **PayOS** cho thanh toán
- Quản lý gói đăng ký (Subscriptions)

### 📧 Thông báo
- Gửi email thông báo qua **SMTP** (MailKit)
- Email từ chối tự động

### 🌍 Khác
- Xác định vị trí địa lý (Geolocation)
- Phản hồi ứng viên (Feedback)
- Import dữ liệu từ Excel

---

## 🏗 Kiến trúc hệ thống

Dự án tuân theo **Clean Architecture** với 4 layer tách biệt:

```
┌──────────────────────────────────────────────────┐
│            ERMS.API (Presentation Layer)          │
│   Controllers · DI · Program.cs · appsettings    │
├──────────────────────────────────────────────────┤
│         ERMS.Application (Application Layer)      │
│   Features/{Feature}/Commands/ & Queries/         │
│   Interface/ (abstractions) · DTO/                │
├──────────────────────────────────────────────────┤
│            ERMS.Domain (Domain Layer)             │
│   Entities/ · Common/ · Constants/ · Enums/       │
├──────────────────────────────────────────────────┤
│       ERMS.Infrastructure (Infrastructure Layer)  │
│   Data/ (DbContext) · Services/ · Migrations/     │
└──────────────────────────────────────────────────┘
```

**Luồng phụ thuộc:**
```
API → Application → Domain ← Infrastructure
```

> **Nguyên tắc:** Layer trên không phụ thuộc ngược vào layer dưới. Infrastructure phụ thuộc vào Application (implement interfaces) và Domain (sử dụng entities).

### CQRS Pattern

Mỗi use case được tổ chức thành 1 folder riêng với **4 file**:

| Loại | File | Mô tả |
|------|------|--------|
| **Command** (Ghi) | `{Action}Command.cs` | Input DTO, implements `IRequest<TResult>` |
| | `{Action}Handler.cs` | Business logic, implements `IRequestHandler` |
| | `{Action}Result.cs` | Output DTO |
| | `{Action}Validator.cs` | FluentValidation rules |
| **Query** (Đọc) | `{Action}Query.cs` | Input DTO, implements `IRequest<TResponse>` |
| | `{Action}Handler.cs` | Query logic |
| | `{Action}Response.cs` | Output DTO |
| | `{Action}Validator.cs` | FluentValidation rules |

---

## 🛠 Công nghệ sử dụng

### Core
| Công nghệ | Phiên bản | Mục đích |
|-----------|-----------|----------|
| .NET | 10.0 | Runtime & SDK |
| ASP.NET Core | 10.0 | Web API framework |
| Entity Framework Core | 10.0.1 | ORM |
| SQL Server | 2022 | Cơ sở dữ liệu |
| MediatR | 14.0.0 | CQRS Mediator pattern |
| FluentValidation | 12.1.1 | Input validation |

### Authentication & Security
| Công nghệ | Mục đích |
|-----------|----------|
| JWT Bearer | Xác thực API |
| ASP.NET Identity | Quản lý user & role |
| Google OAuth 2.0 | Đăng nhập Google |
| Rate Limiting | Bảo vệ chống spam |

### Tích hợp bên thứ 3
| Dịch vụ | Mục đích |
|---------|----------|
| Cloudinary | Lưu trữ file (CV, ảnh) |
| Groq AI / Gemini AI | Chấm điểm CV tự động |
| Zoom API | Tạo phòng phỏng vấn trực tuyến |
| Google Calendar API | Lên lịch phỏng vấn |
| PayOS | Thanh toán trực tuyến |
| MailKit / MimeKit | Gửi email |
| PdfPig | Trích xuất text từ PDF |
| EPPlus / ClosedXML | Xử lý Excel |

### DevOps & Deployment
| Công nghệ | Mục đích |
|-----------|----------|
| Docker | Container hóa ứng dụng |
| Azure Pipelines | CI/CD tự động |
| SonarCloud | Phân tích chất lượng code |
| Scalar | API Documentation UI |

---

## 💻 Yêu cầu hệ thống

| Yêu cầu | Phiên bản tối thiểu |
|---------|---------------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ |
| [SQL Server](https://www.microsoft.com/sql-server) | 2019+ (hoặc SQL Server Express) |
| [Docker](https://www.docker.com/) | 20.10+ *(tùy chọn)* |
| [Git](https://git-scm.com/) | 2.30+ |
| IDE | Visual Studio 2022+ / VS Code / Rider |

---

## 🚀 Cài đặt & Chạy ứng dụng

### 1. Clone repository

```bash
git clone https://github.com/Baotcb/ERMS_System_BackEnd.git
cd ERMS_System_BackEnd
```

### 2. Thiết lập cơ sở dữ liệu

**Cách 1: Sử dụng script SQL**

```bash
# Mở SQL Server Management Studio (SSMS) hoặc Azure Data Studio
# Chạy file database.sql để tạo database và seed data
```

File `database.sql` đã bao gồm cấu trúc bảng và dữ liệu mẫu.

**Cách 2: Sử dụng EF Core Migrations**

```bash
# Cập nhật database từ migrations
dotnet ef database update --project ERMS.Infrastructure --startup-project ERMS.API
```

### 3. Cấu hình biến môi trường

Tạo file `.env` tại thư mục root của project:

```env
# Database
ConnectionStrings__DefaultConnection=Server=localhost;Database=ERMS_DB;Trusted_Connection=True;TrustServerCertificate=True;

# Google OAuth
GoogleAuth__ClientId=your_google_client_id

# Cloudinary (Upload file)
Cloudinary__CloudName=your_cloud_name
Cloudinary__ApiKey=your_api_key
Cloudinary__ApiSecret=your_api_secret

# AI Service (Groq)
Groq__ApiKey=your_groq_api_key
Groq__Model=llama-3.3-70b-versatile

# Zoom (Phỏng vấn trực tuyến)
Zoom__AccountId=your_zoom_account_id
Zoom__ClientId=your_zoom_client_id
Zoom__ClientSecret=your_zoom_client_secret

# PayOS (Thanh toán)
PayOS__ClientId=your_payos_client_id
PayOS__ApiKey=your_payos_api_key
PayOS__ChecksumKey=your_payos_checksum_key

# Email (SMTP)
EmailSettings__SenderEmail=your_email@gmail.com
EmailSettings__Password=your_app_password

# JWT
JwtSettings__Key=ERMS_System_SecretKey_Must_Be_Very_Long_And_Secure

# Client URL
ClientSettings__Url=http://localhost:3000
```

> ⚠️ **Lưu ý:** Không commit file `.env` lên Git. File này đã được thêm vào `.gitignore`.

### 4. Build & Chạy

```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Chạy ứng dụng
dotnet run --project ERMS.API
```

Ứng dụng sẽ chạy tại:
- **API:** `http://localhost:5129` hoặc `https://localhost:7229`
- **API Documentation (Scalar):** `https://localhost:7229/scalar/v1`
- **Health Check:** `https://localhost:7229/health`
- **Database Health:** `https://localhost:7229/db-health`

---

## ⚙ Cấu hình

### appsettings.json

| Section | Mô tả |
|---------|--------|
| `ConnectionStrings:DefaultConnection` | Chuỗi kết nối SQL Server |
| `JwtSettings` | Cấu hình JWT (Key, Issuer, Audience, thời hạn token) |
| `ClientSettings:Url` | URL Frontend (CORS) |
| `EmailSettings` | Cấu hình SMTP gửi email |
| `Cloudinary` | Thông tin Cloudinary account |
| `Gemini` / `Groq` | API key dịch vụ AI |
| `Zoom` | Thông tin Zoom OAuth |
| `PayOS` | Thông tin PayOS |
| `GoogleAuth` | Google OAuth Client ID |

### Cấu hình CORS

Frontend mặc định được phép truy cập từ:
- `http://localhost:3000`
- `https://localhost:3000`
- URL cấu hình trong `ClientSettings:Url`

---

## 📁 Cấu trúc dự án

```
ERMS_System_BackEnd/
│
├── 📂 ERMS.API/                          # Presentation Layer
│   ├── Controllers/                      # API Controllers (21 controllers)
│   │   ├── AdminController.cs
│   │   ├── ApplicationsController.cs
│   │   ├── AuthController.cs
│   │   ├── CourseController.cs
│   │   ├── DepartmentsController.cs
│   │   ├── EmployeesController.cs
│   │   ├── EnterpriseController.cs
│   │   ├── FeedbackController.cs
│   │   ├── GeolocationController.cs
│   │   ├── JobPostingsController.cs
│   │   ├── LessonsController.cs
│   │   ├── PlanDetailsController.cs
│   │   ├── PublicJobsController.cs
│   │   ├── QuizController.cs
│   │   ├── RecruitmentCampaignsController.cs
│   │   ├── RecruitmentPlansController.cs
│   │   ├── SubscriptionController.cs
│   │   ├── TrainingPlanController.cs
│   │   ├── TrainingRequestController.cs
│   │   └── UserController.cs
│   ├── DependencyInjection.cs            # Auth, CORS, Rate Limiter config
│   ├── Program.cs                        # Entry point
│   └── appsettings.json                  # Configuration
│
├── 📂 ERMS.Application/                  # Application Layer
│   ├── Features/                         # CQRS Features (24 modules)
│   │   ├── Admin/
│   │   ├── Applications/
│   │   ├── Auth/
│   │   ├── Certifications/
│   │   ├── Course/
│   │   ├── Departments/
│   │   ├── Employees/
│   │   ├── Enrollments/
│   │   ├── Enterprises/
│   │   ├── Feedback/
│   │   ├── Geolocation/
│   │   ├── Interviews/
│   │   ├── JobPostings/
│   │   ├── Lessons/
│   │   ├── PlanDetails/
│   │   ├── Quizzes/
│   │   ├── RecruitmentCampaigns/
│   │   ├── RecruitmentPlans/
│   │   ├── Skill/
│   │   ├── Subscription/
│   │   ├── Training/
│   │   ├── Users/
│   │   └── Workshop/
│   ├── Interface/                        # Abstractions (21 interfaces)
│   ├── DTO/                              # Shared DTOs
│   └── DependencyInjection.cs            # MediatR registration
│
├── 📂 ERMS.Domain/                       # Domain Layer (không phụ thuộc)
│   ├── Entities/                         # Domain Entities
│   │   ├── Application/                  # Application, Resume, Offer...
│   │   ├── Candidate/
│   │   ├── Enterprise/
│   │   ├── Identity/                     # User entity
│   │   ├── Organization/                 # Department, Position...
│   │   ├── Recruitment/                  # JobPosting, Campaign...
│   │   ├── Skill/
│   │   ├── System/
│   │   └── Training/                     # Course, Lesson, Quiz...
│   ├── Common/                           # BaseEntity, BaseEntityInt
│   ├── Constants/                        # AppRoles, business constants
│   └── Enums/                            # Domain enums
│
├── 📂 ERMS.Infrastructure/               # Infrastructure Layer
│   ├── Data/
│   │   └── ERMSDbContext.cs              # EF Core DbContext
│   ├── Services/                         # Interface implementations
│   ├── Configuration/                    # Settings classes
│   └── Migrations/                       # EF Core Migrations
│
├── 📂 ERMS.UnitTests/                    # Unit Tests
├── 📂 ERMS.API.UnitTests/                # API Layer Tests
├── 📂 ERMS.AutomationTests/              # Integration/E2E Tests
│
├── 📄 ERMS_System.sln                    # Solution file
├── 📄 Dockerfile                         # Docker build
├── 📄 docker-compose.yml                 # Docker Compose
├── 📄 database.sql                       # Script khởi tạo DB
├── 📄 azure-pipelines.yml                # CI/CD Production
├── 📄 azure-pipelines-uat.yml            # CI/CD UAT
└── 📄 azure-pipelines-dev.yml            # CI/CD Development
```

---

## 🔌 API Endpoints

### 🔐 Authentication (`/api/auth`)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|--------|-------|
| POST | `/api/auth/login` | Đăng nhập | Public |
| POST | `/api/auth/register` | Đăng ký | Public |
| POST | `/api/auth/google` | Đăng nhập Google | Public |
| POST | `/api/auth/refresh` | Làm mới token | Authenticated |

### 📝 Recruitment Plans (`/api/recruitment-plans`)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|--------|-------|
| GET | `/api/recruitment-plans` | Danh sách kế hoạch | HRManager, Director |
| POST | `/api/recruitment-plans` | Tạo kế hoạch mới | HRManager |
| PUT | `/api/recruitment-plans/{id}` | Cập nhật kế hoạch | HRManager |
| DELETE | `/api/recruitment-plans/{id}` | Xóa kế hoạch | HRManager |

### 💼 Job Postings (`/api/job-postings`)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|--------|-------|
| GET | `/api/job-postings` | Danh sách tin tuyển dụng | HRManager |
| POST | `/api/job-postings` | Đăng tin mới | HRManager |
| PUT | `/api/job-postings/{id}` | Cập nhật tin | HRManager |

### 🌐 Public Jobs (`/api/public/jobs`)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|--------|-------|
| GET | `/api/public/jobs` | Trang việc làm công khai | Public |
| GET | `/api/public/jobs/{id}` | Chi tiết việc làm | Public |

### 📄 Applications (`/api/applications`)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|--------|-------|
| POST | `/api/applications` | Nộp hồ sơ ứng tuyển | Candidate |
| GET | `/api/applications/my-applications` | Hồ sơ của tôi | Candidate |
| PUT | `/api/applications/{id}/forward` | Chuyển tiếp hồ sơ | HRManager |

### 🏢 Enterprise (`/api/enterprises`)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|--------|-------|
| GET | `/api/enterprises` | Thông tin doanh nghiệp | Authenticated |
| PUT | `/api/enterprises` | Cập nhật doanh nghiệp | Director |

### 👥 Employees (`/api/employees`)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|--------|-------|
| GET | `/api/employees` | Danh sách nhân viên | HRManager, Director |
| POST | `/api/employees` | Thêm nhân viên | HRManager |

### 📚 Courses (`/api/courses`)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|--------|-------|
| GET | `/api/courses` | Danh sách khóa học | Authenticated |
| POST | `/api/courses` | Tạo khóa học | Trainer |

### 🔧 Health Check
| Method | Endpoint | Mô tả |
|--------|----------|--------|
| GET | `/health` | Kiểm tra trạng thái ứng dụng |
| GET | `/db-health` | Kiểm tra kết nối database |

> 📖 Xem đầy đủ tài liệu API tại **Scalar UI**: `/scalar/v1`

---

## 🔑 Hệ thống phân quyền

| Vai trò | Mô tả | Quyền chính |
|---------|--------|-------------|
| **Admin** | Quản trị viên hệ thống | Quản lý toàn bộ hệ thống, diagnostics |
| **Director** | Giám đốc | Phê duyệt kế hoạch, xem báo cáo tổng hợp |
| **HRManager** | Trưởng phòng nhân sự | Quản lý tuyển dụng, nhân viên, phỏng vấn |
| **DepartmentHead** | Trưởng phòng ban | Phê duyệt yêu cầu tuyển dụng của phòng ban |
| **Trainer** | Giảng viên | Quản lý khóa đào tạo, bài học, quiz |
| **Employee** | Nhân viên | Xem thông tin, tham gia đào tạo |
| **Candidate** | Ứng viên | Tìm việc, nộp hồ sơ, theo dõi kết quả |

---

## 🔄 CI/CD Pipeline

Dự án sử dụng **Azure Pipelines** với 3 môi trường:

```
┌───────────┐     ┌───────────┐     ┌───────────┐
│    DEV     │ ──► │    UAT    │ ──► │   PROD    │
│ (DEV branch)│    │(UAT branch)│    │(master)   │
└───────────┘     └───────────┘     └───────────┘
```

| Pipeline | Branch | Mô tả |
|----------|--------|--------|
| `azure-pipelines-dev.yml` | DEV | Build, Test, SonarCloud Analysis, Deploy Dev |
| `azure-pipelines-uat.yml` | UAT | Build, Test, SonarCloud Analysis, Deploy UAT |
| `azure-pipelines.yml` | master | Build, Test, Deploy Production |

Mỗi pipeline thực hiện:
1. ✅ Restore NuGet packages
2. ✅ Build solution
3. ✅ Chạy Unit Tests
4. ✅ Phân tích code với SonarCloud
5. ✅ Build Docker image
6. ✅ Push image lên Docker Hub
7. ✅ Deploy lên server

---

## 🐳 Docker

### Build & Run với Docker

```bash
# Build Docker image
docker build -t erms-api .

# Run container
docker run -d \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="your_connection_string" \
  --name erms_api \
  erms-api
```

### Sử dụng Docker Compose

```bash
# Pull image và chạy
docker-compose up -d

# Xem logs
docker-compose logs -f erms-api

# Dừng container
docker-compose down
```

Image trên Docker Hub: `baotcq/erms-api:latest`

---

## 🧪 Testing

Dự án bao gồm 3 project test:

| Project | Loại | Mô tả |
|---------|------|--------|
| `ERMS.UnitTests` | Unit Test | Test logic Handler, Validator |
| `ERMS.API.UnitTests` | Unit Test | Test Controller layer |
| `ERMS.AutomationTests` | Integration/E2E | Test tích hợp end-to-end |

### Chạy tests

```bash
# Chạy tất cả tests
dotnet test

# Chạy test cụ thể
dotnet test ERMS.UnitTests
dotnet test ERMS.API.UnitTests

# Chạy với chi tiết
dotnet test --verbosity detailed

# Chạy với code coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🤝 Đóng góp

### Quy trình đóng góp

1. **Fork** repository
2. Tạo **branch** mới từ `DEV`:
   ```bash
   git checkout -b feature/ten-tinh-nang
   ```
3. **Commit** với message rõ ràng:
   ```bash
   git commit -m "feat: thêm tính năng XYZ"
   ```
4. **Push** lên branch:
   ```bash
   git push origin feature/ten-tinh-nang
   ```
5. Tạo **Pull Request** vào branch `DEV`

### Quy ước Commit

| Prefix | Mô tả |
|--------|--------|
| `feat:` | Tính năng mới |
| `fix:` | Sửa lỗi |
| `refactor:` | Tái cấu trúc code |
| `docs:` | Cập nhật tài liệu |
| `test:` | Thêm/sửa test |
| `chore:` | Công việc bảo trì |

### Quy ước code

- Tuân thủ **Clean Architecture** — không để layer trên phụ thuộc ngược
- Mỗi Command/Query tạo **1 folder riêng** với đầy đủ 4 file
- Sử dụng **FluentValidation** cho mọi Command/Query
- Controller là **thin layer** — chỉ dispatch qua MediatR
- **Naming convention**: PascalCase cho class, _camelCase cho private field, kebab-case cho route

---

## 📄 Giấy phép

Dự án này được phát triển phục vụ mục đích **Đồ án Tốt nghiệp**.

---

<p align="center">
  Phát triển bởi <strong>ERMS Team</strong> | 2026
</p>

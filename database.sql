USE [master]
GO
/****** Object:  Database [ERMS]    Script Date: 24/01/2026 3:13:47 PM ******/
CREATE DATABASE [ERMS]

GO
ALTER DATABASE [ERMS] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [ERMS] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [ERMS] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [ERMS] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [ERMS] SET ARITHABORT OFF 
GO
ALTER DATABASE [ERMS] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [ERMS] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [ERMS] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [ERMS] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [ERMS] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [ERMS] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [ERMS] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [ERMS] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [ERMS] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [ERMS] SET  ENABLE_BROKER 
GO
ALTER DATABASE [ERMS] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [ERMS] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [ERMS] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [ERMS] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [ERMS] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [ERMS] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [ERMS] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [ERMS] SET RECOVERY FULL 
GO
ALTER DATABASE [ERMS] SET  MULTI_USER 
GO
ALTER DATABASE [ERMS] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [ERMS] SET DB_CHAINING OFF 
GO
ALTER DATABASE [ERMS] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [ERMS] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [ERMS] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [ERMS] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'ERMS', N'ON'
GO
ALTER DATABASE [ERMS] SET QUERY_STORE = OFF
GO
USE [ERMS]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Applications]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Applications](
	[Id] [uniqueidentifier] NOT NULL,
	[JobPostingId] [uniqueidentifier] NOT NULL,
	[CandidateId] [uniqueidentifier] NOT NULL,
	[ResumeId] [uniqueidentifier] NULL,
	[CoverLetter] [nvarchar](max) NULL,
	[ExpectedSalary] [decimal](18, 2) NULL,
	[AvailableStartDate] [datetime2](7) NULL,
	[Stage] [nvarchar](max) NOT NULL,
	[StageUpdatedAt] [datetime2](7) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[Source] [nvarchar](max) NULL,
	[ReferredById] [uniqueidentifier] NULL,
	[Rating] [int] NULL,
	[HRNote] [nvarchar](max) NULL,
	[RejectionReason] [nvarchar](max) NULL,
	[RejectedById] [uniqueidentifier] NULL,
	[RejectedAt] [datetime2](7) NULL,
	[AppliedAt] [datetime2](7) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Applications] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ApprovalHistories]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ApprovalHistories](
	[Id] [uniqueidentifier] NOT NULL,
	[EntityType] [nvarchar](max) NOT NULL,
	[EntityId] [uniqueidentifier] NOT NULL,
	[Action] [nvarchar](max) NOT NULL,
	[PreviousStatus] [nvarchar](max) NULL,
	[NewStatus] [nvarchar](max) NOT NULL,
	[PerformedById] [uniqueidentifier] NOT NULL,
	[Note] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_ApprovalHistories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetRoleClaims]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetRoles]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoles](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserClaims]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserLogins]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](450) NOT NULL,
	[ProviderKey] [nvarchar](450) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserRoles]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [uniqueidentifier] NOT NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUsers]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUsers](
	[Id] [uniqueidentifier] NOT NULL,
	[FullName] [nvarchar](max) NOT NULL,
	[Hometown] [nvarchar](max) NULL,
	[DateOfBirth] [datetime2](7) NULL,
	[AvatarUrl] [nvarchar](max) NULL,
	[DateJoined] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[DepartmentId] [int] NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
 CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AspNetUserTokens]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserTokens](
	[UserId] [uniqueidentifier] NOT NULL,
	[LoginProvider] [nvarchar](450) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Candidates]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Candidates](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[AboutMe] [nvarchar](max) NULL,
	[Headline] [nvarchar](max) NULL,
	[CurrentPosition] [nvarchar](max) NULL,
	[CurrentCompany] [nvarchar](max) NULL,
	[Location] [nvarchar](max) NULL,
	[LinkedInUrl] [nvarchar](max) NULL,
	[PortfolioUrl] [nvarchar](max) NULL,
	[ExpectedSalary] [decimal](18, 2) NULL,
	[NoticePeriod] [int] NULL,
	[IsOpenToWork] [bit] NOT NULL,
	[PreferredJobTypes] [nvarchar](max) NULL,
	[PreferredLocations] [nvarchar](max) NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Candidates] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CandidateSkills]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CandidateSkills](
	[Id] [uniqueidentifier] NOT NULL,
	[CandidateId] [uniqueidentifier] NOT NULL,
	[SkillId] [uniqueidentifier] NOT NULL,
	[ProficiencyLevel] [int] NULL,
	[YearsOfExperience] [int] NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_CandidateSkills] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Courses]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Courses](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[TrainingPlanId] [uniqueidentifier] NULL,
	[CourseName] [nvarchar](max) NOT NULL,
	[CourseCode] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[ThumbnailUrl] [nvarchar](max) NULL,
	[TrainerId] [uniqueidentifier] NOT NULL,
	[DurationMinutes] [int] NULL,
	[Level] [nvarchar](max) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[IsMandatory] [bit] NOT NULL,
	[MaxEnrollments] [int] NULL,
	[EnrollmentDeadline] [datetime2](7) NULL,
	[PublishedAt] [datetime2](7) NULL,
	[CompletionCriteria] [nvarchar](max) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Courses] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CourseSkills]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CourseSkills](
	[Id] [uniqueidentifier] NOT NULL,
	[CourseId] [uniqueidentifier] NOT NULL,
	[SkillId] [uniqueidentifier] NOT NULL,
	[SkillLevelGained] [int] NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_CourseSkills] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CVScreeningResults]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CVScreeningResults](
	[Id] [uniqueidentifier] NOT NULL,
	[ApplicationId] [uniqueidentifier] NOT NULL,
	[OverallScore] [decimal](18, 2) NOT NULL,
	[SkillMatchScore] [decimal](18, 2) NULL,
	[ExperienceMatchScore] [decimal](18, 2) NULL,
	[EducationMatchScore] [decimal](18, 2) NULL,
	[KeywordMatchScore] [decimal](18, 2) NULL,
	[MatchedSkills] [nvarchar](max) NULL,
	[MissingSkills] [nvarchar](max) NULL,
	[Strengths] [nvarchar](max) NULL,
	[Concerns] [nvarchar](max) NULL,
	[Summary] [nvarchar](max) NULL,
	[RawResponse] [nvarchar](max) NULL,
	[ProcessedAt] [datetime2](7) NOT NULL,
	[AIModel] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_CVScreeningResults] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Departments]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Departments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[DepartmentName] [nvarchar](max) NOT NULL,
	[DepartmentCode] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[ManagerId] [uniqueidentifier] NULL,
	[ParentDepartmentId] [int] NULL,
	[IsActive] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Educations]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Educations](
	[Id] [uniqueidentifier] NOT NULL,
	[CandidateId] [uniqueidentifier] NOT NULL,
	[Institution] [nvarchar](max) NOT NULL,
	[Degree] [nvarchar](max) NOT NULL,
	[FieldOfStudy] [nvarchar](max) NULL,
	[StartDate] [datetime2](7) NULL,
	[EndDate] [datetime2](7) NULL,
	[IsCurrent] [bit] NOT NULL,
	[Grade] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Educations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Employees]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Employees](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[EmployeeCode] [nvarchar](max) NOT NULL,
	[DepartmentId] [int] NOT NULL,
	[Position] [nvarchar](max) NULL,
	[JobPositionId] [uniqueidentifier] NULL,
	[HireDate] [datetime2](7) NULL,
	[TerminationDate] [datetime2](7) NULL,
	[EmploymentType] [nvarchar](max) NOT NULL,
	[ManagerId] [uniqueidentifier] NULL,
	[Salary] [decimal](18, 2) NULL,
	[IsTrainer] [bit] NOT NULL,
	[Status] [nvarchar](max) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Enrollments]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Enrollments](
	[Id] [uniqueidentifier] NOT NULL,
	[CourseId] [uniqueidentifier] NOT NULL,
	[EmployeeId] [uniqueidentifier] NOT NULL,
	[EnrolledAt] [datetime2](7) NOT NULL,
	[EnrolledById] [uniqueidentifier] NULL,
	[StartedAt] [datetime2](7) NULL,
	[CompletedAt] [datetime2](7) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[Progress] [int] NOT NULL,
	[LastAccessedAt] [datetime2](7) NULL,
	[CertificateUrl] [nvarchar](max) NULL,
	[CertificateIssuedAt] [datetime2](7) NULL,
	[Note] [nvarchar](max) NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Enrollments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Enterprises]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Enterprises](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseName] [nvarchar](max) NOT NULL,
	[EnterpriseCode] [nvarchar](max) NOT NULL,
	[TaxCode] [nvarchar](max) NULL,
	[Address] [nvarchar](max) NULL,
	[Phone] [nvarchar](max) NULL,
	[Email] [nvarchar](max) NULL,
	[Website] [nvarchar](max) NULL,
	[LogoUrl] [nvarchar](max) NULL,
	[SubscriptionPlanId] [uniqueidentifier] NOT NULL,
	[SubscriptionStartDate] [datetime2](7) NOT NULL,
	[SubscriptionEndDate] [datetime2](7) NOT NULL,
	[SubscriptionStatus] [nvarchar](max) NOT NULL,
	[CreatedById] [uniqueidentifier] NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Enterprises] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InterviewParticipants]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InterviewParticipants](
	[Id] [uniqueidentifier] NOT NULL,
	[InterviewId] [uniqueidentifier] NOT NULL,
	[EmployeeId] [uniqueidentifier] NOT NULL,
	[Role] [nvarchar](max) NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[ConfirmationStatus] [nvarchar](max) NOT NULL,
	[Rating] [int] NULL,
	[Feedback] [nvarchar](max) NULL,
	[Recommendation] [nvarchar](max) NULL,
	[FeedbackSubmittedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_InterviewParticipants] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Interviews]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Interviews](
	[Id] [uniqueidentifier] NOT NULL,
	[ApplicationId] [uniqueidentifier] NOT NULL,
	[InterviewType] [nvarchar](max) NOT NULL,
	[RoundNumber] [int] NOT NULL,
	[ScheduledAt] [datetime2](7) NOT NULL,
	[Duration] [int] NOT NULL,
	[Location] [nvarchar](max) NULL,
	[MeetingLink] [nvarchar](max) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[ScheduledById] [uniqueidentifier] NOT NULL,
	[OverallRating] [int] NULL,
	[OverallFeedback] [nvarchar](max) NULL,
	[Decision] [nvarchar](max) NULL,
	[Note] [nvarchar](max) NULL,
	[CompletedAt] [datetime2](7) NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Interviews] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[JobCompetencies]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobCompetencies](
	[Id] [uniqueidentifier] NOT NULL,
	[JobPositionId] [uniqueidentifier] NOT NULL,
	[SkillId] [uniqueidentifier] NOT NULL,
	[RequiredLevel] [int] NOT NULL,
	[Importance] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_JobCompetencies] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[JobPositions]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobPositions](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[PositionName] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_JobPositions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[JobPostings]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobPostings](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[PlanDetailId] [uniqueidentifier] NULL,
	[DepartmentId] [int] NOT NULL,
	[JobTitle] [nvarchar](max) NOT NULL,
	[JobCode] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Requirements] [nvarchar](max) NULL,
	[Benefits] [nvarchar](max) NULL,
	[EmploymentType] [nvarchar](max) NOT NULL,
	[ExperienceLevel] [nvarchar](max) NULL,
	[EducationLevel] [nvarchar](max) NULL,
	[SalaryRangeMin] [decimal](18, 2) NULL,
	[SalaryRangeMax] [decimal](18, 2) NULL,
	[ShowSalary] [bit] NOT NULL,
	[Location] [nvarchar](max) NULL,
	[RemoteOption] [nvarchar](max) NULL,
	[Quantity] [int] NOT NULL,
	[ApplicationDeadline] [datetime2](7) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[PublishedAt] [datetime2](7) NULL,
	[PublishedById] [uniqueidentifier] NULL,
	[ClosedAt] [datetime2](7) NULL,
	[ViewCount] [int] NOT NULL,
	[ApplicationCount] [int] NOT NULL,
	[CreatedById] [uniqueidentifier] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_JobPostings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[JobSkills]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobSkills](
	[Id] [uniqueidentifier] NOT NULL,
	[JobPostingId] [uniqueidentifier] NOT NULL,
	[SkillId] [uniqueidentifier] NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[MinLevel] [int] NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_JobSkills] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LessonProgresses]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LessonProgresses](
	[Id] [uniqueidentifier] NOT NULL,
	[EnrollmentId] [uniqueidentifier] NOT NULL,
	[LessonId] [uniqueidentifier] NOT NULL,
	[StartedAt] [datetime2](7) NULL,
	[CompletedAt] [datetime2](7) NULL,
	[WatchPercentage] [int] NOT NULL,
	[LastPosition] [int] NULL,
	[TimeSpentMinutes] [int] NOT NULL,
	[Status] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_LessonProgresses] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lessons]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lessons](
	[Id] [uniqueidentifier] NOT NULL,
	[CourseId] [uniqueidentifier] NOT NULL,
	[LessonTitle] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[OrderIndex] [int] NOT NULL,
	[ContentType] [nvarchar](max) NOT NULL,
	[VideoUrl] [nvarchar](max) NULL,
	[VideoDurationMinutes] [int] NULL,
	[DocumentUrl] [nvarchar](max) NULL,
	[ExternalLinkUrl] [nvarchar](max) NULL,
	[Content] [nvarchar](max) NULL,
	[IsPreview] [bit] NOT NULL,
	[EstimatedMinutes] [int] NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Lessons] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Notifications]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notifications](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](max) NOT NULL,
	[Message] [nvarchar](max) NOT NULL,
	[NotificationType] [nvarchar](max) NOT NULL,
	[EntityType] [nvarchar](max) NULL,
	[EntityId] [uniqueidentifier] NULL,
	[ActionUrl] [nvarchar](max) NULL,
	[IsRead] [bit] NOT NULL,
	[ReadAt] [datetime2](7) NULL,
	[IsSent] [bit] NOT NULL,
	[SentAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Offers]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Offers](
	[Id] [uniqueidentifier] NOT NULL,
	[ApplicationId] [uniqueidentifier] NOT NULL,
	[OfferCode] [nvarchar](max) NULL,
	[Position] [nvarchar](max) NOT NULL,
	[DepartmentId] [int] NOT NULL,
	[Salary] [decimal](18, 2) NOT NULL,
	[SalaryFrequency] [nvarchar](max) NOT NULL,
	[Bonus] [nvarchar](max) NULL,
	[Benefits] [nvarchar](max) NULL,
	[StartDate] [datetime2](7) NOT NULL,
	[ExpirationDate] [datetime2](7) NOT NULL,
	[OfferLetterUrl] [nvarchar](max) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[CreatedById] [uniqueidentifier] NOT NULL,
	[ApprovedById] [uniqueidentifier] NULL,
	[ApprovedAt] [datetime2](7) NULL,
	[SentAt] [datetime2](7) NULL,
	[SentById] [uniqueidentifier] NULL,
	[RespondedAt] [datetime2](7) NULL,
	[CandidateNote] [nvarchar](max) NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Offers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OwnershipTransfers]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OwnershipTransfers](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[FromUserId] [uniqueidentifier] NOT NULL,
	[ToUserId] [uniqueidentifier] NOT NULL,
	[Reason] [nvarchar](max) NULL,
	[TransferredAt] [datetime2](7) NOT NULL,
	[ApprovedById] [uniqueidentifier] NULL,
	[Note] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_OwnershipTransfers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PlanDetails]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlanDetails](
	[Id] [uniqueidentifier] NOT NULL,
	[RecruitmentPlanId] [uniqueidentifier] NOT NULL,
	[DepartmentId] [int] NOT NULL,
	[RequestedById] [uniqueidentifier] NOT NULL,
	[PositionTitle] [nvarchar](max) NOT NULL,
	[Quantity] [int] NOT NULL,
	[Priority] [nvarchar](max) NOT NULL,
	[Justification] [nvarchar](max) NULL,
	[RequiredSkills] [nvarchar](max) NULL,
	[MinExperience] [int] NULL,
	[MaxExperience] [int] NULL,
	[EducationLevel] [nvarchar](max) NULL,
	[SalaryRangeMin] [decimal](18, 2) NULL,
	[SalaryRangeMax] [decimal](18, 2) NULL,
	[ExpectedStartDate] [datetime2](7) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[ReviewerId] [uniqueidentifier] NULL,
	[ReviewedAt] [datetime2](7) NULL,
	[ReviewNote] [nvarchar](max) NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_PlanDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QuizAnswers]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QuizAnswers](
	[Id] [uniqueidentifier] NOT NULL,
	[QuizAttemptId] [uniqueidentifier] NOT NULL,
	[QuizQuestionId] [uniqueidentifier] NOT NULL,
	[SelectedAnswer] [nvarchar](max) NULL,
	[IsCorrect] [bit] NULL,
	[PointsEarned] [int] NOT NULL,
	[AnsweredAt] [datetime2](7) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_QuizAnswers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QuizAttempts]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QuizAttempts](
	[Id] [uniqueidentifier] NOT NULL,
	[EnrollmentId] [uniqueidentifier] NOT NULL,
	[QuizId] [uniqueidentifier] NOT NULL,
	[AttemptNumber] [int] NOT NULL,
	[StartedAt] [datetime2](7) NOT NULL,
	[CompletedAt] [datetime2](7) NULL,
	[Score] [decimal](18, 2) NULL,
	[IsPassed] [bit] NULL,
	[TotalQuestions] [int] NOT NULL,
	[CorrectAnswers] [int] NULL,
	[TimeTakenMinutes] [int] NULL,
	[Status] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_QuizAttempts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QuizQuestions]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QuizQuestions](
	[Id] [uniqueidentifier] NOT NULL,
	[QuizId] [uniqueidentifier] NOT NULL,
	[QuestionText] [nvarchar](max) NOT NULL,
	[QuestionType] [nvarchar](max) NOT NULL,
	[Options] [nvarchar](max) NOT NULL,
	[CorrectAnswer] [nvarchar](max) NOT NULL,
	[Explanation] [nvarchar](max) NULL,
	[Points] [int] NOT NULL,
	[OrderIndex] [int] NOT NULL,
	[ImageUrl] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_QuizQuestions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Quizzes]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Quizzes](
	[Id] [uniqueidentifier] NOT NULL,
	[CourseId] [uniqueidentifier] NOT NULL,
	[QuizTitle] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[TimeLimitMinutes] [int] NULL,
	[PassingScore] [int] NOT NULL,
	[MaxAttempts] [int] NULL,
	[ShuffleQuestions] [bit] NOT NULL,
	[ShuffleAnswers] [bit] NOT NULL,
	[ShowCorrectAnswers] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Quizzes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RecruitmentPlans]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RecruitmentPlans](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[PlanName] [nvarchar](max) NOT NULL,
	[PlanCode] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[StartDate] [datetime2](7) NOT NULL,
	[EndDate] [datetime2](7) NOT NULL,
	[TotalBudget] [decimal](18, 2) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[CreatedById] [uniqueidentifier] NOT NULL,
	[ApprovedById] [uniqueidentifier] NULL,
	[ApprovedAt] [datetime2](7) NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_RecruitmentPlans] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Resumes]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Resumes](
	[Id] [uniqueidentifier] NOT NULL,
	[CandidateId] [uniqueidentifier] NOT NULL,
	[FileName] [nvarchar](max) NOT NULL,
	[FileUrl] [nvarchar](max) NOT NULL,
	[FileSize] [int] NULL,
	[FileType] [nvarchar](max) NULL,
	[IsDefault] [bit] NOT NULL,
	[ParsedData] [nvarchar](max) NULL,
	[UploadedAt] [datetime2](7) NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Resumes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SavedJobs]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SavedJobs](
	[Id] [uniqueidentifier] NOT NULL,
	[CandidateId] [uniqueidentifier] NOT NULL,
	[JobPostingId] [uniqueidentifier] NOT NULL,
	[SavedAt] [datetime2](7) NOT NULL,
	[Note] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_SavedJobs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Skills]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Skills](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NULL,
	[SkillName] [nvarchar](max) NOT NULL,
	[SkillCategory] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_Skills] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SubscriptionHistories]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SubscriptionHistories](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[SubscriptionPlanId] [uniqueidentifier] NOT NULL,
	[ActionType] [nvarchar](max) NOT NULL,
	[PreviousPlanId] [uniqueidentifier] NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Currency] [nvarchar](max) NOT NULL,
	[PaymentMethod] [nvarchar](max) NULL,
	[PaymentReference] [nvarchar](max) NULL,
	[PeriodStartDate] [datetime2](7) NOT NULL,
	[PeriodEndDate] [datetime2](7) NOT NULL,
	[Note] [nvarchar](max) NULL,
	[CreatedById] [uniqueidentifier] NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_SubscriptionHistories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SubscriptionPlans]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SubscriptionPlans](
	[Id] [uniqueidentifier] NOT NULL,
	[PlanName] [nvarchar](max) NOT NULL,
	[PlanCode] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[MaxUsers] [int] NOT NULL,
	[MaxJobPostings] [int] NOT NULL,
	[MaxCourses] [int] NOT NULL,
	[PriceMonthly] [decimal](18, 2) NOT NULL,
	[PriceYearly] [decimal](18, 2) NOT NULL,
	[Features] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TrainingPlans]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TrainingPlans](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[PlanName] [nvarchar](max) NOT NULL,
	[PlanCode] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[StartDate] [datetime2](7) NOT NULL,
	[EndDate] [datetime2](7) NOT NULL,
	[TotalBudget] [decimal](18, 2) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[CreatedById] [uniqueidentifier] NOT NULL,
	[ApprovedById] [uniqueidentifier] NULL,
	[ApprovedAt] [datetime2](7) NULL,
	[ReviewNote] [nvarchar](max) NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_TrainingPlans] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TrainingRequests]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TrainingRequests](
	[Id] [uniqueidentifier] NOT NULL,
	[EnterpriseId] [uniqueidentifier] NOT NULL,
	[TrainingPlanId] [uniqueidentifier] NULL,
	[DepartmentId] [int] NOT NULL,
	[RequestedById] [uniqueidentifier] NOT NULL,
	[Subject] [nvarchar](max) NOT NULL,
	[Urgency] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[TargetAudience] [nvarchar](max) NULL,
	[EstimatedParticipants] [int] NULL,
	[EstimatedBudget] [decimal](18, 2) NULL,
	[Status] [nvarchar](max) NOT NULL,
	[ReviewNote] [nvarchar](max) NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_TrainingRequests] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WorkExperiences]    Script Date: 24/01/2026 3:13:48 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WorkExperiences](
	[Id] [uniqueidentifier] NOT NULL,
	[CandidateId] [uniqueidentifier] NOT NULL,
	[CompanyName] [nvarchar](max) NOT NULL,
	[Position] [nvarchar](max) NOT NULL,
	[Location] [nvarchar](max) NULL,
	[StartDate] [datetime2](7) NOT NULL,
	[EndDate] [datetime2](7) NULL,
	[IsCurrent] [bit] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[EmploymentType] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_WorkExperiences] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260123092541_InitialCreate', N'10.0.1')
GO
INSERT [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'9d066231-42d6-4e83-ba40-08de5b120cc8', N'Candidate', N'CANDIDATE', N'b23c4292-95ae-40cc-a4a2-0a3de5193ef4')
GO
INSERT [dbo].[AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'a5424253-9af6-4f12-bceb-08de5b120c42', N'9d066231-42d6-4e83-ba40-08de5b120cc8')
GO
INSERT [dbo].[AspNetUsers] ([Id], [FullName], [Hometown], [DateOfBirth], [AvatarUrl], [DateJoined], [UpdatedAt], [DepartmentId], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount]) VALUES (N'25b16122-c9c6-438b-4a6d-08de5b0cddb9', N'Quang Minh', NULL, NULL, NULL, CAST(N'2026-01-24T05:53:14.9456304' AS DateTime2), NULL, NULL, N'quangminh@gmail.com', N'QUANGMINH@GMAIL.COM', N'quangminh@gmail.com', N'QUANGMINH@GMAIL.COM', 0, N'AQAAAAIAAYagAAAAELLtfmtJ1yepW8m1T4JwsedTDwTbgKiPfPLuRHpQqGxuMTMSfuZTB/AfR3Csn00c0g==', N'RJ4QSCL2HDCBL665RLTRSY7OXV7GNKRL', N'0eb8b2f2-41e6-4802-a5f9-68b61a8899d3', NULL, 0, 0, NULL, 1, 0)
INSERT [dbo].[AspNetUsers] ([Id], [FullName], [Hometown], [DateOfBirth], [AvatarUrl], [DateJoined], [UpdatedAt], [DepartmentId], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount]) VALUES (N'a5424253-9af6-4f12-bceb-08de5b120c42', N'ERMS', NULL, NULL, NULL, CAST(N'2026-01-24T06:30:20.3913293' AS DateTime2), NULL, NULL, N'erms2026@gmail.com', N'ERMS2026@GMAIL.COM', N'erms2026@gmail.com', N'ERMS2026@GMAIL.COM', 1, NULL, N'EC65HYDIP3LHQKQTCRR4UVAY5SZBCG4I', N'e00a0ded-db36-4064-aa1c-5b9558c7221a', NULL, 0, 0, NULL, 1, 0)
GO
/****** Object:  Index [IX_Applications_CandidateId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Applications_CandidateId] ON [dbo].[Applications]
(
	[CandidateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Applications_JobPostingId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Applications_JobPostingId] ON [dbo].[Applications]
(
	[JobPostingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Applications_ReferredById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Applications_ReferredById] ON [dbo].[Applications]
(
	[ReferredById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Applications_RejectedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Applications_RejectedById] ON [dbo].[Applications]
(
	[RejectedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Applications_ResumeId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Applications_ResumeId] ON [dbo].[Applications]
(
	[ResumeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_ApprovalHistories_PerformedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_ApprovalHistories_PerformedById] ON [dbo].[ApprovalHistories]
(
	[PerformedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetRoleClaims_RoleId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [RoleNameIndex]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles]
(
	[NormalizedName] ASC
)
WHERE ([NormalizedName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetUserClaims_UserId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetUserLogins_UserId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetUserRoles_RoleId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles]
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [EmailIndex]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[AspNetUsers]
(
	[NormalizedEmail] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_AspNetUsers_DepartmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_AspNetUsers_DepartmentId] ON [dbo].[AspNetUsers]
(
	[DepartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UserNameIndex]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[AspNetUsers]
(
	[NormalizedUserName] ASC
)
WHERE ([NormalizedUserName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Candidates_UserId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Candidates_UserId] ON [dbo].[Candidates]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CandidateSkills_CandidateId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_CandidateSkills_CandidateId] ON [dbo].[CandidateSkills]
(
	[CandidateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CandidateSkills_SkillId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_CandidateSkills_SkillId] ON [dbo].[CandidateSkills]
(
	[SkillId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Courses_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Courses_EnterpriseId] ON [dbo].[Courses]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Courses_TrainerId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Courses_TrainerId] ON [dbo].[Courses]
(
	[TrainerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Courses_TrainingPlanId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Courses_TrainingPlanId] ON [dbo].[Courses]
(
	[TrainingPlanId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CourseSkills_CourseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_CourseSkills_CourseId] ON [dbo].[CourseSkills]
(
	[CourseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CourseSkills_SkillId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_CourseSkills_SkillId] ON [dbo].[CourseSkills]
(
	[SkillId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_CVScreeningResults_ApplicationId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_CVScreeningResults_ApplicationId] ON [dbo].[CVScreeningResults]
(
	[ApplicationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Departments_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Departments_EnterpriseId] ON [dbo].[Departments]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Departments_ManagerId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Departments_ManagerId] ON [dbo].[Departments]
(
	[ManagerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Departments_ParentDepartmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Departments_ParentDepartmentId] ON [dbo].[Departments]
(
	[ParentDepartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Educations_CandidateId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Educations_CandidateId] ON [dbo].[Educations]
(
	[CandidateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Employees_DepartmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Employees_DepartmentId] ON [dbo].[Employees]
(
	[DepartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Employees_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Employees_EnterpriseId] ON [dbo].[Employees]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Employees_JobPositionId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Employees_JobPositionId] ON [dbo].[Employees]
(
	[JobPositionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Employees_ManagerId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Employees_ManagerId] ON [dbo].[Employees]
(
	[ManagerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Employees_UserId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Employees_UserId] ON [dbo].[Employees]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Enrollments_CourseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Enrollments_CourseId] ON [dbo].[Enrollments]
(
	[CourseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Enrollments_EmployeeId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Enrollments_EmployeeId] ON [dbo].[Enrollments]
(
	[EmployeeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Enrollments_EnrolledById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Enrollments_EnrolledById] ON [dbo].[Enrollments]
(
	[EnrolledById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Enterprises_CreatedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Enterprises_CreatedById] ON [dbo].[Enterprises]
(
	[CreatedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Enterprises_SubscriptionPlanId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Enterprises_SubscriptionPlanId] ON [dbo].[Enterprises]
(
	[SubscriptionPlanId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_InterviewParticipants_EmployeeId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_InterviewParticipants_EmployeeId] ON [dbo].[InterviewParticipants]
(
	[EmployeeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_InterviewParticipants_InterviewId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_InterviewParticipants_InterviewId] ON [dbo].[InterviewParticipants]
(
	[InterviewId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Interviews_ApplicationId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Interviews_ApplicationId] ON [dbo].[Interviews]
(
	[ApplicationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Interviews_ScheduledById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Interviews_ScheduledById] ON [dbo].[Interviews]
(
	[ScheduledById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobCompetencies_JobPositionId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobCompetencies_JobPositionId] ON [dbo].[JobCompetencies]
(
	[JobPositionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobCompetencies_SkillId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobCompetencies_SkillId] ON [dbo].[JobCompetencies]
(
	[SkillId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobPositions_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobPositions_EnterpriseId] ON [dbo].[JobPositions]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobPostings_CreatedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobPostings_CreatedById] ON [dbo].[JobPostings]
(
	[CreatedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobPostings_DepartmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobPostings_DepartmentId] ON [dbo].[JobPostings]
(
	[DepartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobPostings_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobPostings_EnterpriseId] ON [dbo].[JobPostings]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobPostings_PlanDetailId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobPostings_PlanDetailId] ON [dbo].[JobPostings]
(
	[PlanDetailId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobPostings_PublishedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobPostings_PublishedById] ON [dbo].[JobPostings]
(
	[PublishedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobSkills_JobPostingId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobSkills_JobPostingId] ON [dbo].[JobSkills]
(
	[JobPostingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_JobSkills_SkillId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_JobSkills_SkillId] ON [dbo].[JobSkills]
(
	[SkillId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_LessonProgresses_EnrollmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_LessonProgresses_EnrollmentId] ON [dbo].[LessonProgresses]
(
	[EnrollmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_LessonProgresses_LessonId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_LessonProgresses_LessonId] ON [dbo].[LessonProgresses]
(
	[LessonId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Lessons_CourseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Lessons_CourseId] ON [dbo].[Lessons]
(
	[CourseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Notifications_UserId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] ON [dbo].[Notifications]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Offers_ApplicationId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Offers_ApplicationId] ON [dbo].[Offers]
(
	[ApplicationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Offers_ApprovedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Offers_ApprovedById] ON [dbo].[Offers]
(
	[ApprovedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Offers_CreatedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Offers_CreatedById] ON [dbo].[Offers]
(
	[CreatedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Offers_DepartmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Offers_DepartmentId] ON [dbo].[Offers]
(
	[DepartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Offers_SentById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Offers_SentById] ON [dbo].[Offers]
(
	[SentById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OwnershipTransfers_ApprovedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_OwnershipTransfers_ApprovedById] ON [dbo].[OwnershipTransfers]
(
	[ApprovedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OwnershipTransfers_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_OwnershipTransfers_EnterpriseId] ON [dbo].[OwnershipTransfers]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OwnershipTransfers_FromUserId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_OwnershipTransfers_FromUserId] ON [dbo].[OwnershipTransfers]
(
	[FromUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_OwnershipTransfers_ToUserId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_OwnershipTransfers_ToUserId] ON [dbo].[OwnershipTransfers]
(
	[ToUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PlanDetails_DepartmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_PlanDetails_DepartmentId] ON [dbo].[PlanDetails]
(
	[DepartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PlanDetails_RecruitmentPlanId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_PlanDetails_RecruitmentPlanId] ON [dbo].[PlanDetails]
(
	[RecruitmentPlanId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PlanDetails_RequestedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_PlanDetails_RequestedById] ON [dbo].[PlanDetails]
(
	[RequestedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_PlanDetails_ReviewerId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_PlanDetails_ReviewerId] ON [dbo].[PlanDetails]
(
	[ReviewerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_QuizAnswers_QuizAttemptId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_QuizAnswers_QuizAttemptId] ON [dbo].[QuizAnswers]
(
	[QuizAttemptId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_QuizAnswers_QuizQuestionId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_QuizAnswers_QuizQuestionId] ON [dbo].[QuizAnswers]
(
	[QuizQuestionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_QuizAttempts_EnrollmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_QuizAttempts_EnrollmentId] ON [dbo].[QuizAttempts]
(
	[EnrollmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_QuizAttempts_QuizId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_QuizAttempts_QuizId] ON [dbo].[QuizAttempts]
(
	[QuizId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_QuizQuestions_QuizId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_QuizQuestions_QuizId] ON [dbo].[QuizQuestions]
(
	[QuizId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Quizzes_CourseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Quizzes_CourseId] ON [dbo].[Quizzes]
(
	[CourseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RecruitmentPlans_ApprovedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_RecruitmentPlans_ApprovedById] ON [dbo].[RecruitmentPlans]
(
	[ApprovedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RecruitmentPlans_CreatedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_RecruitmentPlans_CreatedById] ON [dbo].[RecruitmentPlans]
(
	[CreatedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RecruitmentPlans_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_RecruitmentPlans_EnterpriseId] ON [dbo].[RecruitmentPlans]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Resumes_CandidateId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Resumes_CandidateId] ON [dbo].[Resumes]
(
	[CandidateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SavedJobs_CandidateId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_SavedJobs_CandidateId] ON [dbo].[SavedJobs]
(
	[CandidateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SavedJobs_JobPostingId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_SavedJobs_JobPostingId] ON [dbo].[SavedJobs]
(
	[JobPostingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Skills_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_Skills_EnterpriseId] ON [dbo].[Skills]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SubscriptionHistories_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_SubscriptionHistories_EnterpriseId] ON [dbo].[SubscriptionHistories]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SubscriptionHistories_PreviousPlanId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_SubscriptionHistories_PreviousPlanId] ON [dbo].[SubscriptionHistories]
(
	[PreviousPlanId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SubscriptionHistories_SubscriptionPlanId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_SubscriptionHistories_SubscriptionPlanId] ON [dbo].[SubscriptionHistories]
(
	[SubscriptionPlanId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TrainingPlans_ApprovedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_TrainingPlans_ApprovedById] ON [dbo].[TrainingPlans]
(
	[ApprovedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TrainingPlans_CreatedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_TrainingPlans_CreatedById] ON [dbo].[TrainingPlans]
(
	[CreatedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TrainingPlans_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_TrainingPlans_EnterpriseId] ON [dbo].[TrainingPlans]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TrainingRequests_DepartmentId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_TrainingRequests_DepartmentId] ON [dbo].[TrainingRequests]
(
	[DepartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TrainingRequests_EnterpriseId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_TrainingRequests_EnterpriseId] ON [dbo].[TrainingRequests]
(
	[EnterpriseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TrainingRequests_RequestedById]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_TrainingRequests_RequestedById] ON [dbo].[TrainingRequests]
(
	[RequestedById] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TrainingRequests_TrainingPlanId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_TrainingRequests_TrainingPlanId] ON [dbo].[TrainingRequests]
(
	[TrainingPlanId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_WorkExperiences_CandidateId]    Script Date: 24/01/2026 3:13:48 PM ******/
CREATE NONCLUSTERED INDEX [IX_WorkExperiences_CandidateId] ON [dbo].[WorkExperiences]
(
	[CandidateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Applications]  WITH CHECK ADD  CONSTRAINT [FK_Applications_AspNetUsers_RejectedById] FOREIGN KEY([RejectedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[Applications] CHECK CONSTRAINT [FK_Applications_AspNetUsers_RejectedById]
GO
ALTER TABLE [dbo].[Applications]  WITH CHECK ADD  CONSTRAINT [FK_Applications_Candidates_CandidateId] FOREIGN KEY([CandidateId])
REFERENCES [dbo].[Candidates] ([Id])
GO
ALTER TABLE [dbo].[Applications] CHECK CONSTRAINT [FK_Applications_Candidates_CandidateId]
GO
ALTER TABLE [dbo].[Applications]  WITH CHECK ADD  CONSTRAINT [FK_Applications_Employees_ReferredById] FOREIGN KEY([ReferredById])
REFERENCES [dbo].[Employees] ([Id])
GO
ALTER TABLE [dbo].[Applications] CHECK CONSTRAINT [FK_Applications_Employees_ReferredById]
GO
ALTER TABLE [dbo].[Applications]  WITH CHECK ADD  CONSTRAINT [FK_Applications_JobPostings_JobPostingId] FOREIGN KEY([JobPostingId])
REFERENCES [dbo].[JobPostings] ([Id])
GO
ALTER TABLE [dbo].[Applications] CHECK CONSTRAINT [FK_Applications_JobPostings_JobPostingId]
GO
ALTER TABLE [dbo].[Applications]  WITH CHECK ADD  CONSTRAINT [FK_Applications_Resumes_ResumeId] FOREIGN KEY([ResumeId])
REFERENCES [dbo].[Resumes] ([Id])
GO
ALTER TABLE [dbo].[Applications] CHECK CONSTRAINT [FK_Applications_Resumes_ResumeId]
GO
ALTER TABLE [dbo].[ApprovalHistories]  WITH CHECK ADD  CONSTRAINT [FK_ApprovalHistories_AspNetUsers_PerformedById] FOREIGN KEY([PerformedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[ApprovalHistories] CHECK CONSTRAINT [FK_ApprovalHistories_AspNetUsers_PerformedById]
GO
ALTER TABLE [dbo].[AspNetRoleClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetRoleClaims] CHECK CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
GO
ALTER TABLE [dbo].[AspNetUserClaims]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserClaims] CHECK CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserLogins]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserLogins] CHECK CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUsers]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUsers_Departments_DepartmentId] FOREIGN KEY([DepartmentId])
REFERENCES [dbo].[Departments] ([Id])
GO
ALTER TABLE [dbo].[AspNetUsers] CHECK CONSTRAINT [FK_AspNetUsers_Departments_DepartmentId]
GO
ALTER TABLE [dbo].[AspNetUserTokens]  WITH CHECK ADD  CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserTokens] CHECK CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[Candidates]  WITH CHECK ADD  CONSTRAINT [FK_Candidates_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Candidates] CHECK CONSTRAINT [FK_Candidates_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[CandidateSkills]  WITH CHECK ADD  CONSTRAINT [FK_CandidateSkills_Candidates_CandidateId] FOREIGN KEY([CandidateId])
REFERENCES [dbo].[Candidates] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CandidateSkills] CHECK CONSTRAINT [FK_CandidateSkills_Candidates_CandidateId]
GO
ALTER TABLE [dbo].[CandidateSkills]  WITH CHECK ADD  CONSTRAINT [FK_CandidateSkills_Skills_SkillId] FOREIGN KEY([SkillId])
REFERENCES [dbo].[Skills] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CandidateSkills] CHECK CONSTRAINT [FK_CandidateSkills_Skills_SkillId]
GO
ALTER TABLE [dbo].[Courses]  WITH CHECK ADD  CONSTRAINT [FK_Courses_Employees_TrainerId] FOREIGN KEY([TrainerId])
REFERENCES [dbo].[Employees] ([Id])
GO
ALTER TABLE [dbo].[Courses] CHECK CONSTRAINT [FK_Courses_Employees_TrainerId]
GO
ALTER TABLE [dbo].[Courses]  WITH CHECK ADD  CONSTRAINT [FK_Courses_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
GO
ALTER TABLE [dbo].[Courses] CHECK CONSTRAINT [FK_Courses_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[Courses]  WITH CHECK ADD  CONSTRAINT [FK_Courses_TrainingPlans_TrainingPlanId] FOREIGN KEY([TrainingPlanId])
REFERENCES [dbo].[TrainingPlans] ([Id])
GO
ALTER TABLE [dbo].[Courses] CHECK CONSTRAINT [FK_Courses_TrainingPlans_TrainingPlanId]
GO
ALTER TABLE [dbo].[CourseSkills]  WITH CHECK ADD  CONSTRAINT [FK_CourseSkills_Courses_CourseId] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CourseSkills] CHECK CONSTRAINT [FK_CourseSkills_Courses_CourseId]
GO
ALTER TABLE [dbo].[CourseSkills]  WITH CHECK ADD  CONSTRAINT [FK_CourseSkills_Skills_SkillId] FOREIGN KEY([SkillId])
REFERENCES [dbo].[Skills] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CourseSkills] CHECK CONSTRAINT [FK_CourseSkills_Skills_SkillId]
GO
ALTER TABLE [dbo].[CVScreeningResults]  WITH CHECK ADD  CONSTRAINT [FK_CVScreeningResults_Applications_ApplicationId] FOREIGN KEY([ApplicationId])
REFERENCES [dbo].[Applications] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CVScreeningResults] CHECK CONSTRAINT [FK_CVScreeningResults_Applications_ApplicationId]
GO
ALTER TABLE [dbo].[Departments]  WITH CHECK ADD  CONSTRAINT [FK_Departments_Departments_ParentDepartmentId] FOREIGN KEY([ParentDepartmentId])
REFERENCES [dbo].[Departments] ([Id])
GO
ALTER TABLE [dbo].[Departments] CHECK CONSTRAINT [FK_Departments_Departments_ParentDepartmentId]
GO
ALTER TABLE [dbo].[Departments]  WITH CHECK ADD  CONSTRAINT [FK_Departments_Employees_ManagerId] FOREIGN KEY([ManagerId])
REFERENCES [dbo].[Employees] ([Id])
GO
ALTER TABLE [dbo].[Departments] CHECK CONSTRAINT [FK_Departments_Employees_ManagerId]
GO
ALTER TABLE [dbo].[Departments]  WITH CHECK ADD  CONSTRAINT [FK_Departments_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Departments] CHECK CONSTRAINT [FK_Departments_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[Educations]  WITH CHECK ADD  CONSTRAINT [FK_Educations_Candidates_CandidateId] FOREIGN KEY([CandidateId])
REFERENCES [dbo].[Candidates] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Educations] CHECK CONSTRAINT [FK_Educations_Candidates_CandidateId]
GO
ALTER TABLE [dbo].[Employees]  WITH CHECK ADD  CONSTRAINT [FK_Employees_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Employees] CHECK CONSTRAINT [FK_Employees_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[Employees]  WITH CHECK ADD  CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY([DepartmentId])
REFERENCES [dbo].[Departments] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Employees] CHECK CONSTRAINT [FK_Employees_Departments_DepartmentId]
GO
ALTER TABLE [dbo].[Employees]  WITH CHECK ADD  CONSTRAINT [FK_Employees_Employees_ManagerId] FOREIGN KEY([ManagerId])
REFERENCES [dbo].[Employees] ([Id])
GO
ALTER TABLE [dbo].[Employees] CHECK CONSTRAINT [FK_Employees_Employees_ManagerId]
GO
ALTER TABLE [dbo].[Employees]  WITH CHECK ADD  CONSTRAINT [FK_Employees_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
GO
ALTER TABLE [dbo].[Employees] CHECK CONSTRAINT [FK_Employees_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[Employees]  WITH CHECK ADD  CONSTRAINT [FK_Employees_JobPositions_JobPositionId] FOREIGN KEY([JobPositionId])
REFERENCES [dbo].[JobPositions] ([Id])
GO
ALTER TABLE [dbo].[Employees] CHECK CONSTRAINT [FK_Employees_JobPositions_JobPositionId]
GO
ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollments_AspNetUsers_EnrolledById] FOREIGN KEY([EnrolledById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[Enrollments] CHECK CONSTRAINT [FK_Enrollments_AspNetUsers_EnrolledById]
GO
ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollments_Courses_CourseId] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
GO
ALTER TABLE [dbo].[Enrollments] CHECK CONSTRAINT [FK_Enrollments_Courses_CourseId]
GO
ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollments_Employees_EmployeeId] FOREIGN KEY([EmployeeId])
REFERENCES [dbo].[Employees] ([Id])
GO
ALTER TABLE [dbo].[Enrollments] CHECK CONSTRAINT [FK_Enrollments_Employees_EmployeeId]
GO
ALTER TABLE [dbo].[Enterprises]  WITH CHECK ADD  CONSTRAINT [FK_Enterprises_AspNetUsers_CreatedById] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[Enterprises] CHECK CONSTRAINT [FK_Enterprises_AspNetUsers_CreatedById]
GO
ALTER TABLE [dbo].[Enterprises]  WITH CHECK ADD  CONSTRAINT [FK_Enterprises_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY([SubscriptionPlanId])
REFERENCES [dbo].[SubscriptionPlans] ([Id])
GO
ALTER TABLE [dbo].[Enterprises] CHECK CONSTRAINT [FK_Enterprises_SubscriptionPlans_SubscriptionPlanId]
GO
ALTER TABLE [dbo].[InterviewParticipants]  WITH CHECK ADD  CONSTRAINT [FK_InterviewParticipants_Employees_EmployeeId] FOREIGN KEY([EmployeeId])
REFERENCES [dbo].[Employees] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[InterviewParticipants] CHECK CONSTRAINT [FK_InterviewParticipants_Employees_EmployeeId]
GO
ALTER TABLE [dbo].[InterviewParticipants]  WITH CHECK ADD  CONSTRAINT [FK_InterviewParticipants_Interviews_InterviewId] FOREIGN KEY([InterviewId])
REFERENCES [dbo].[Interviews] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[InterviewParticipants] CHECK CONSTRAINT [FK_InterviewParticipants_Interviews_InterviewId]
GO
ALTER TABLE [dbo].[Interviews]  WITH CHECK ADD  CONSTRAINT [FK_Interviews_Applications_ApplicationId] FOREIGN KEY([ApplicationId])
REFERENCES [dbo].[Applications] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Interviews] CHECK CONSTRAINT [FK_Interviews_Applications_ApplicationId]
GO
ALTER TABLE [dbo].[Interviews]  WITH CHECK ADD  CONSTRAINT [FK_Interviews_AspNetUsers_ScheduledById] FOREIGN KEY([ScheduledById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[Interviews] CHECK CONSTRAINT [FK_Interviews_AspNetUsers_ScheduledById]
GO
ALTER TABLE [dbo].[JobCompetencies]  WITH CHECK ADD  CONSTRAINT [FK_JobCompetencies_JobPositions_JobPositionId] FOREIGN KEY([JobPositionId])
REFERENCES [dbo].[JobPositions] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[JobCompetencies] CHECK CONSTRAINT [FK_JobCompetencies_JobPositions_JobPositionId]
GO
ALTER TABLE [dbo].[JobCompetencies]  WITH CHECK ADD  CONSTRAINT [FK_JobCompetencies_Skills_SkillId] FOREIGN KEY([SkillId])
REFERENCES [dbo].[Skills] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[JobCompetencies] CHECK CONSTRAINT [FK_JobCompetencies_Skills_SkillId]
GO
ALTER TABLE [dbo].[JobPositions]  WITH CHECK ADD  CONSTRAINT [FK_JobPositions_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[JobPositions] CHECK CONSTRAINT [FK_JobPositions_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[JobPostings]  WITH CHECK ADD  CONSTRAINT [FK_JobPostings_AspNetUsers_CreatedById] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[JobPostings] CHECK CONSTRAINT [FK_JobPostings_AspNetUsers_CreatedById]
GO
ALTER TABLE [dbo].[JobPostings]  WITH CHECK ADD  CONSTRAINT [FK_JobPostings_AspNetUsers_PublishedById] FOREIGN KEY([PublishedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[JobPostings] CHECK CONSTRAINT [FK_JobPostings_AspNetUsers_PublishedById]
GO
ALTER TABLE [dbo].[JobPostings]  WITH CHECK ADD  CONSTRAINT [FK_JobPostings_Departments_DepartmentId] FOREIGN KEY([DepartmentId])
REFERENCES [dbo].[Departments] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[JobPostings] CHECK CONSTRAINT [FK_JobPostings_Departments_DepartmentId]
GO
ALTER TABLE [dbo].[JobPostings]  WITH CHECK ADD  CONSTRAINT [FK_JobPostings_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
GO
ALTER TABLE [dbo].[JobPostings] CHECK CONSTRAINT [FK_JobPostings_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[JobPostings]  WITH CHECK ADD  CONSTRAINT [FK_JobPostings_PlanDetails_PlanDetailId] FOREIGN KEY([PlanDetailId])
REFERENCES [dbo].[PlanDetails] ([Id])
GO
ALTER TABLE [dbo].[JobPostings] CHECK CONSTRAINT [FK_JobPostings_PlanDetails_PlanDetailId]
GO
ALTER TABLE [dbo].[JobSkills]  WITH CHECK ADD  CONSTRAINT [FK_JobSkills_JobPostings_JobPostingId] FOREIGN KEY([JobPostingId])
REFERENCES [dbo].[JobPostings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[JobSkills] CHECK CONSTRAINT [FK_JobSkills_JobPostings_JobPostingId]
GO
ALTER TABLE [dbo].[JobSkills]  WITH CHECK ADD  CONSTRAINT [FK_JobSkills_Skills_SkillId] FOREIGN KEY([SkillId])
REFERENCES [dbo].[Skills] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[JobSkills] CHECK CONSTRAINT [FK_JobSkills_Skills_SkillId]
GO
ALTER TABLE [dbo].[LessonProgresses]  WITH CHECK ADD  CONSTRAINT [FK_LessonProgresses_Enrollments_EnrollmentId] FOREIGN KEY([EnrollmentId])
REFERENCES [dbo].[Enrollments] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[LessonProgresses] CHECK CONSTRAINT [FK_LessonProgresses_Enrollments_EnrollmentId]
GO
ALTER TABLE [dbo].[LessonProgresses]  WITH CHECK ADD  CONSTRAINT [FK_LessonProgresses_Lessons_LessonId] FOREIGN KEY([LessonId])
REFERENCES [dbo].[Lessons] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[LessonProgresses] CHECK CONSTRAINT [FK_LessonProgresses_Lessons_LessonId]
GO
ALTER TABLE [dbo].[Lessons]  WITH CHECK ADD  CONSTRAINT [FK_Lessons_Courses_CourseId] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Lessons] CHECK CONSTRAINT [FK_Lessons_Courses_CourseId]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notifications_AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[Offers]  WITH CHECK ADD  CONSTRAINT [FK_Offers_Applications_ApplicationId] FOREIGN KEY([ApplicationId])
REFERENCES [dbo].[Applications] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Offers] CHECK CONSTRAINT [FK_Offers_Applications_ApplicationId]
GO
ALTER TABLE [dbo].[Offers]  WITH CHECK ADD  CONSTRAINT [FK_Offers_AspNetUsers_ApprovedById] FOREIGN KEY([ApprovedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[Offers] CHECK CONSTRAINT [FK_Offers_AspNetUsers_ApprovedById]
GO
ALTER TABLE [dbo].[Offers]  WITH CHECK ADD  CONSTRAINT [FK_Offers_AspNetUsers_CreatedById] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[Offers] CHECK CONSTRAINT [FK_Offers_AspNetUsers_CreatedById]
GO
ALTER TABLE [dbo].[Offers]  WITH CHECK ADD  CONSTRAINT [FK_Offers_AspNetUsers_SentById] FOREIGN KEY([SentById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[Offers] CHECK CONSTRAINT [FK_Offers_AspNetUsers_SentById]
GO
ALTER TABLE [dbo].[Offers]  WITH CHECK ADD  CONSTRAINT [FK_Offers_Departments_DepartmentId] FOREIGN KEY([DepartmentId])
REFERENCES [dbo].[Departments] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Offers] CHECK CONSTRAINT [FK_Offers_Departments_DepartmentId]
GO
ALTER TABLE [dbo].[OwnershipTransfers]  WITH CHECK ADD  CONSTRAINT [FK_OwnershipTransfers_AspNetUsers_ApprovedById] FOREIGN KEY([ApprovedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[OwnershipTransfers] CHECK CONSTRAINT [FK_OwnershipTransfers_AspNetUsers_ApprovedById]
GO
ALTER TABLE [dbo].[OwnershipTransfers]  WITH CHECK ADD  CONSTRAINT [FK_OwnershipTransfers_AspNetUsers_FromUserId] FOREIGN KEY([FromUserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[OwnershipTransfers] CHECK CONSTRAINT [FK_OwnershipTransfers_AspNetUsers_FromUserId]
GO
ALTER TABLE [dbo].[OwnershipTransfers]  WITH CHECK ADD  CONSTRAINT [FK_OwnershipTransfers_AspNetUsers_ToUserId] FOREIGN KEY([ToUserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[OwnershipTransfers] CHECK CONSTRAINT [FK_OwnershipTransfers_AspNetUsers_ToUserId]
GO
ALTER TABLE [dbo].[OwnershipTransfers]  WITH CHECK ADD  CONSTRAINT [FK_OwnershipTransfers_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[OwnershipTransfers] CHECK CONSTRAINT [FK_OwnershipTransfers_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[PlanDetails]  WITH CHECK ADD  CONSTRAINT [FK_PlanDetails_AspNetUsers_RequestedById] FOREIGN KEY([RequestedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[PlanDetails] CHECK CONSTRAINT [FK_PlanDetails_AspNetUsers_RequestedById]
GO
ALTER TABLE [dbo].[PlanDetails]  WITH CHECK ADD  CONSTRAINT [FK_PlanDetails_AspNetUsers_ReviewerId] FOREIGN KEY([ReviewerId])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[PlanDetails] CHECK CONSTRAINT [FK_PlanDetails_AspNetUsers_ReviewerId]
GO
ALTER TABLE [dbo].[PlanDetails]  WITH CHECK ADD  CONSTRAINT [FK_PlanDetails_Departments_DepartmentId] FOREIGN KEY([DepartmentId])
REFERENCES [dbo].[Departments] ([Id])
GO
ALTER TABLE [dbo].[PlanDetails] CHECK CONSTRAINT [FK_PlanDetails_Departments_DepartmentId]
GO
ALTER TABLE [dbo].[PlanDetails]  WITH CHECK ADD  CONSTRAINT [FK_PlanDetails_RecruitmentPlans_RecruitmentPlanId] FOREIGN KEY([RecruitmentPlanId])
REFERENCES [dbo].[RecruitmentPlans] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PlanDetails] CHECK CONSTRAINT [FK_PlanDetails_RecruitmentPlans_RecruitmentPlanId]
GO
ALTER TABLE [dbo].[QuizAnswers]  WITH CHECK ADD  CONSTRAINT [FK_QuizAnswers_QuizAttempts_QuizAttemptId] FOREIGN KEY([QuizAttemptId])
REFERENCES [dbo].[QuizAttempts] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QuizAnswers] CHECK CONSTRAINT [FK_QuizAnswers_QuizAttempts_QuizAttemptId]
GO
ALTER TABLE [dbo].[QuizAnswers]  WITH CHECK ADD  CONSTRAINT [FK_QuizAnswers_QuizQuestions_QuizQuestionId] FOREIGN KEY([QuizQuestionId])
REFERENCES [dbo].[QuizQuestions] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QuizAnswers] CHECK CONSTRAINT [FK_QuizAnswers_QuizQuestions_QuizQuestionId]
GO
ALTER TABLE [dbo].[QuizAttempts]  WITH CHECK ADD  CONSTRAINT [FK_QuizAttempts_Enrollments_EnrollmentId] FOREIGN KEY([EnrollmentId])
REFERENCES [dbo].[Enrollments] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QuizAttempts] CHECK CONSTRAINT [FK_QuizAttempts_Enrollments_EnrollmentId]
GO
ALTER TABLE [dbo].[QuizAttempts]  WITH CHECK ADD  CONSTRAINT [FK_QuizAttempts_Quizzes_QuizId] FOREIGN KEY([QuizId])
REFERENCES [dbo].[Quizzes] ([Id])
GO
ALTER TABLE [dbo].[QuizAttempts] CHECK CONSTRAINT [FK_QuizAttempts_Quizzes_QuizId]
GO
ALTER TABLE [dbo].[QuizQuestions]  WITH CHECK ADD  CONSTRAINT [FK_QuizQuestions_Quizzes_QuizId] FOREIGN KEY([QuizId])
REFERENCES [dbo].[Quizzes] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QuizQuestions] CHECK CONSTRAINT [FK_QuizQuestions_Quizzes_QuizId]
GO
ALTER TABLE [dbo].[Quizzes]  WITH CHECK ADD  CONSTRAINT [FK_Quizzes_Courses_CourseId] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Quizzes] CHECK CONSTRAINT [FK_Quizzes_Courses_CourseId]
GO
ALTER TABLE [dbo].[RecruitmentPlans]  WITH CHECK ADD  CONSTRAINT [FK_RecruitmentPlans_AspNetUsers_ApprovedById] FOREIGN KEY([ApprovedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[RecruitmentPlans] CHECK CONSTRAINT [FK_RecruitmentPlans_AspNetUsers_ApprovedById]
GO
ALTER TABLE [dbo].[RecruitmentPlans]  WITH CHECK ADD  CONSTRAINT [FK_RecruitmentPlans_AspNetUsers_CreatedById] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[RecruitmentPlans] CHECK CONSTRAINT [FK_RecruitmentPlans_AspNetUsers_CreatedById]
GO
ALTER TABLE [dbo].[RecruitmentPlans]  WITH CHECK ADD  CONSTRAINT [FK_RecruitmentPlans_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RecruitmentPlans] CHECK CONSTRAINT [FK_RecruitmentPlans_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[Resumes]  WITH CHECK ADD  CONSTRAINT [FK_Resumes_Candidates_CandidateId] FOREIGN KEY([CandidateId])
REFERENCES [dbo].[Candidates] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Resumes] CHECK CONSTRAINT [FK_Resumes_Candidates_CandidateId]
GO
ALTER TABLE [dbo].[SavedJobs]  WITH CHECK ADD  CONSTRAINT [FK_SavedJobs_Candidates_CandidateId] FOREIGN KEY([CandidateId])
REFERENCES [dbo].[Candidates] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SavedJobs] CHECK CONSTRAINT [FK_SavedJobs_Candidates_CandidateId]
GO
ALTER TABLE [dbo].[SavedJobs]  WITH CHECK ADD  CONSTRAINT [FK_SavedJobs_JobPostings_JobPostingId] FOREIGN KEY([JobPostingId])
REFERENCES [dbo].[JobPostings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SavedJobs] CHECK CONSTRAINT [FK_SavedJobs_JobPostings_JobPostingId]
GO
ALTER TABLE [dbo].[Skills]  WITH CHECK ADD  CONSTRAINT [FK_Skills_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
GO
ALTER TABLE [dbo].[Skills] CHECK CONSTRAINT [FK_Skills_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[SubscriptionHistories]  WITH CHECK ADD  CONSTRAINT [FK_SubscriptionHistories_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SubscriptionHistories] CHECK CONSTRAINT [FK_SubscriptionHistories_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[SubscriptionHistories]  WITH CHECK ADD  CONSTRAINT [FK_SubscriptionHistories_SubscriptionPlans_PreviousPlanId] FOREIGN KEY([PreviousPlanId])
REFERENCES [dbo].[SubscriptionPlans] ([Id])
GO
ALTER TABLE [dbo].[SubscriptionHistories] CHECK CONSTRAINT [FK_SubscriptionHistories_SubscriptionPlans_PreviousPlanId]
GO
ALTER TABLE [dbo].[SubscriptionHistories]  WITH CHECK ADD  CONSTRAINT [FK_SubscriptionHistories_SubscriptionPlans_SubscriptionPlanId] FOREIGN KEY([SubscriptionPlanId])
REFERENCES [dbo].[SubscriptionPlans] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SubscriptionHistories] CHECK CONSTRAINT [FK_SubscriptionHistories_SubscriptionPlans_SubscriptionPlanId]
GO
ALTER TABLE [dbo].[TrainingPlans]  WITH CHECK ADD  CONSTRAINT [FK_TrainingPlans_AspNetUsers_ApprovedById] FOREIGN KEY([ApprovedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[TrainingPlans] CHECK CONSTRAINT [FK_TrainingPlans_AspNetUsers_ApprovedById]
GO
ALTER TABLE [dbo].[TrainingPlans]  WITH CHECK ADD  CONSTRAINT [FK_TrainingPlans_AspNetUsers_CreatedById] FOREIGN KEY([CreatedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[TrainingPlans] CHECK CONSTRAINT [FK_TrainingPlans_AspNetUsers_CreatedById]
GO
ALTER TABLE [dbo].[TrainingPlans]  WITH CHECK ADD  CONSTRAINT [FK_TrainingPlans_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TrainingPlans] CHECK CONSTRAINT [FK_TrainingPlans_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[TrainingRequests]  WITH CHECK ADD  CONSTRAINT [FK_TrainingRequests_AspNetUsers_RequestedById] FOREIGN KEY([RequestedById])
REFERENCES [dbo].[AspNetUsers] ([Id])
GO
ALTER TABLE [dbo].[TrainingRequests] CHECK CONSTRAINT [FK_TrainingRequests_AspNetUsers_RequestedById]
GO
ALTER TABLE [dbo].[TrainingRequests]  WITH CHECK ADD  CONSTRAINT [FK_TrainingRequests_Departments_DepartmentId] FOREIGN KEY([DepartmentId])
REFERENCES [dbo].[Departments] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TrainingRequests] CHECK CONSTRAINT [FK_TrainingRequests_Departments_DepartmentId]
GO
ALTER TABLE [dbo].[TrainingRequests]  WITH CHECK ADD  CONSTRAINT [FK_TrainingRequests_Enterprises_EnterpriseId] FOREIGN KEY([EnterpriseId])
REFERENCES [dbo].[Enterprises] ([Id])
GO
ALTER TABLE [dbo].[TrainingRequests] CHECK CONSTRAINT [FK_TrainingRequests_Enterprises_EnterpriseId]
GO
ALTER TABLE [dbo].[TrainingRequests]  WITH CHECK ADD  CONSTRAINT [FK_TrainingRequests_TrainingPlans_TrainingPlanId] FOREIGN KEY([TrainingPlanId])
REFERENCES [dbo].[TrainingPlans] ([Id])
GO
ALTER TABLE [dbo].[TrainingRequests] CHECK CONSTRAINT [FK_TrainingRequests_TrainingPlans_TrainingPlanId]
GO
ALTER TABLE [dbo].[WorkExperiences]  WITH CHECK ADD  CONSTRAINT [FK_WorkExperiences_Candidates_CandidateId] FOREIGN KEY([CandidateId])
REFERENCES [dbo].[Candidates] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[WorkExperiences] CHECK CONSTRAINT [FK_WorkExperiences_Candidates_CandidateId]
GO
USE [master]
GO
ALTER DATABASE [ERMS] SET  READ_WRITE 
GO

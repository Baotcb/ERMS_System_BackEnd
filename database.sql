-- =============================================
-- ERMS System - Microsoft SQL Server Schema
-- SaaS Recruitment & Internal Training Management System
-- Version: 1.0
-- Database: Microsoft SQL Server 2016+
-- =============================================

-- =============================================
-- SECTION 1: DROP EXISTING TABLES (Reverse FK order)
-- =============================================
IF OBJECT_ID('dbo.OwnershipTransfer', 'U') IS NOT NULL DROP TABLE dbo.OwnershipTransfer;
IF OBJECT_ID('dbo.SavedJob', 'U') IS NOT NULL DROP TABLE dbo.SavedJob;
IF OBJECT_ID('dbo.Notification', 'U') IS NOT NULL DROP TABLE dbo.Notification;
IF OBJECT_ID('dbo.QuizAnswer', 'U') IS NOT NULL DROP TABLE dbo.QuizAnswer;
IF OBJECT_ID('dbo.QuizAttempt', 'U') IS NOT NULL DROP TABLE dbo.QuizAttempt;
IF OBJECT_ID('dbo.QuizQuestion', 'U') IS NOT NULL DROP TABLE dbo.QuizQuestion;
IF OBJECT_ID('dbo.Quiz', 'U') IS NOT NULL DROP TABLE dbo.Quiz;
IF OBJECT_ID('dbo.LessonProgress', 'U') IS NOT NULL DROP TABLE dbo.LessonProgress;
IF OBJECT_ID('dbo.Lesson', 'U') IS NOT NULL DROP TABLE dbo.Lesson;
IF OBJECT_ID('dbo.Enrollment', 'U') IS NOT NULL DROP TABLE dbo.Enrollment;
IF OBJECT_ID('dbo.CourseSkill', 'U') IS NOT NULL DROP TABLE dbo.CourseSkill;
IF OBJECT_ID('dbo.Course', 'U') IS NOT NULL DROP TABLE dbo.Course;
IF OBJECT_ID('dbo.TrainingPlan', 'U') IS NOT NULL DROP TABLE dbo.TrainingPlan;
IF OBJECT_ID('dbo.Offer', 'U') IS NOT NULL DROP TABLE dbo.Offer;
IF OBJECT_ID('dbo.InterviewParticipant', 'U') IS NOT NULL DROP TABLE dbo.InterviewParticipant;
IF OBJECT_ID('dbo.Interview', 'U') IS NOT NULL DROP TABLE dbo.Interview;
IF OBJECT_ID('dbo.CVScreeningResult', 'U') IS NOT NULL DROP TABLE dbo.CVScreeningResult;
IF OBJECT_ID('dbo.Application', 'U') IS NOT NULL DROP TABLE dbo.Application;
IF OBJECT_ID('dbo.Resume', 'U') IS NOT NULL DROP TABLE dbo.Resume;
IF OBJECT_ID('dbo.CandidateSkill', 'U') IS NOT NULL DROP TABLE dbo.CandidateSkill;
IF OBJECT_ID('dbo.WorkExperience', 'U') IS NOT NULL DROP TABLE dbo.WorkExperience;
IF OBJECT_ID('dbo.Education', 'U') IS NOT NULL DROP TABLE dbo.Education;
IF OBJECT_ID('dbo.Candidate', 'U') IS NOT NULL DROP TABLE dbo.Candidate;
IF OBJECT_ID('dbo.JobSkill', 'U') IS NOT NULL DROP TABLE dbo.JobSkill;
IF OBJECT_ID('dbo.JobPosting', 'U') IS NOT NULL DROP TABLE dbo.JobPosting;
IF OBJECT_ID('dbo.ApprovalHistory', 'U') IS NOT NULL DROP TABLE dbo.ApprovalHistory;
IF OBJECT_ID('dbo.PlanDetail', 'U') IS NOT NULL DROP TABLE dbo.PlanDetail;
IF OBJECT_ID('dbo.RecruitmentPlan', 'U') IS NOT NULL DROP TABLE dbo.RecruitmentPlan;
IF OBJECT_ID('dbo.Skill', 'U') IS NOT NULL DROP TABLE dbo.Skill;
IF OBJECT_ID('dbo.Employee', 'U') IS NOT NULL DROP TABLE dbo.Employee;
IF OBJECT_ID('dbo.UserTokens', 'U') IS NOT NULL DROP TABLE dbo.UserTokens;
IF OBJECT_ID('dbo.UserLogins', 'U') IS NOT NULL DROP TABLE dbo.UserLogins;
IF OBJECT_ID('dbo.UserClaims', 'U') IS NOT NULL DROP TABLE dbo.UserClaims;
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.RoleClaims', 'U') IS NOT NULL DROP TABLE dbo.RoleClaims;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Department', 'U') IS NOT NULL DROP TABLE dbo.Department;
IF OBJECT_ID('dbo.SubscriptionHistory', 'U') IS NOT NULL DROP TABLE dbo.SubscriptionHistory;
IF OBJECT_ID('dbo.Enterprise', 'U') IS NOT NULL DROP TABLE dbo.Enterprise;
IF OBJECT_ID('dbo.SubscriptionPlan', 'U') IS NOT NULL DROP TABLE dbo.SubscriptionPlan;
GO

-- =============================================
-- SECTION 2: ENTERPRISE & SUBSCRIPTION (Multi-tenant Core)
-- =============================================

-- SubscriptionPlan: Defines available subscription tiers (Basic, Pro, Enterprise)
-- This is a shared/global table not tied to any specific tenant
CREATE TABLE dbo.SubscriptionPlan (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key, auto-generated UUID
    PlanName NVARCHAR(100) NOT NULL,                        -- Display name: 'Basic', 'Pro', 'Enterprise'
    PlanCode VARCHAR(50) NOT NULL,                          -- Unique code for API: 'basic', 'pro', 'enterprise'
    Description NVARCHAR(500) NULL,                         -- Marketing description of the plan
    MaxUsers INT NOT NULL DEFAULT 10,                       -- Maximum users allowed in this plan
    MaxJobPostings INT NOT NULL DEFAULT 5,                  -- Maximum active job postings allowed
    MaxCourses INT NOT NULL DEFAULT 10,                     -- Maximum training courses allowed
    PriceMonthly DECIMAL(18,2) NOT NULL DEFAULT 0,          -- Monthly subscription price (USD)
    PriceYearly DECIMAL(18,2) NOT NULL DEFAULT 0,           -- Yearly subscription price (USD) - typically discounted
    Features NVARCHAR(MAX) NULL,                            -- JSON array of feature flags enabled for this plan
    IsActive BIT NOT NULL DEFAULT 1,                        -- Whether this plan is available for new subscriptions
    DisplayOrder INT NOT NULL DEFAULT 0,                    -- Sort order for displaying plans on pricing page
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag (1 = deleted)
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_SubscriptionPlan PRIMARY KEY (Id),
    CONSTRAINT UQ_SubscriptionPlan_PlanCode UNIQUE (PlanCode)
);

-- Enterprise: Tenant organization (company) - core of multi-tenant architecture
-- Each company that signs up gets one Enterprise record
CREATE TABLE dbo.Enterprise (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key, tenant identifier
    EnterpriseName NVARCHAR(200) NOT NULL,                  -- Company/organization name
    EnterpriseCode VARCHAR(50) NOT NULL,                    -- Unique code for subdomain/URL slug
    TaxCode VARCHAR(50) NULL,                               -- Tax identification number
    Address NVARCHAR(500) NULL,                             -- Company headquarters address
    Phone VARCHAR(20) NULL,                                 -- Contact phone number
    Email VARCHAR(255) NULL,                                -- Contact email address
    Website VARCHAR(255) NULL,                              -- Company website URL
    LogoUrl VARCHAR(500) NULL,                              -- URL to company logo image
    SubscriptionPlanId UNIQUEIDENTIFIER NOT NULL,           -- FK to current subscription plan
    SubscriptionStartDate DATETIME NOT NULL,                -- When current subscription period started
    SubscriptionEndDate DATETIME NOT NULL,                  -- When current subscription expires
    SubscriptionStatus VARCHAR(30) NOT NULL DEFAULT 'Active', -- Status: Active, Expired, Cancelled, Trial
    CreatedById UNIQUEIDENTIFIER NULL,                      -- FK to User who created (Director/Owner)
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Enterprise PRIMARY KEY (Id),
    CONSTRAINT UQ_Enterprise_EnterpriseCode UNIQUE (EnterpriseCode),
    CONSTRAINT FK_Enterprise_SubscriptionPlan FOREIGN KEY (SubscriptionPlanId) REFERENCES dbo.SubscriptionPlan(Id),
    CONSTRAINT CK_Enterprise_SubscriptionStatus CHECK (SubscriptionStatus IN ('Active', 'Expired', 'Cancelled', 'Trial', 'PastDue'))
);

-- SubscriptionHistory: Audit trail for subscription changes and payments
-- Tracks plan upgrades, downgrades, renewals, and payment records
CREATE TABLE dbo.SubscriptionHistory (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnterpriseId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enterprise
    SubscriptionPlanId UNIQUEIDENTIFIER NOT NULL,           -- FK to the plan for this period
    ActionType VARCHAR(30) NOT NULL,                        -- Action: Subscribe, Upgrade, Downgrade, Renew, Cancel
    PreviousPlanId UNIQUEIDENTIFIER NULL,                   -- FK to previous plan (for upgrades/downgrades)
    Amount DECIMAL(18,2) NOT NULL DEFAULT 0,                -- Amount paid for this action
    Currency VARCHAR(3) NOT NULL DEFAULT 'USD',             -- Currency code (ISO 4217)
    PaymentMethod VARCHAR(50) NULL,                         -- Payment method: CreditCard, BankTransfer, PayPal
    PaymentReference VARCHAR(100) NULL,                     -- External payment transaction ID
    PeriodStartDate DATETIME NOT NULL,                      -- Subscription period start
    PeriodEndDate DATETIME NOT NULL,                        -- Subscription period end
    Note NVARCHAR(500) NULL,                                -- Additional notes about this transaction
    CreatedById UNIQUEIDENTIFIER NULL,                      -- FK to User who performed the action
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    CONSTRAINT PK_SubscriptionHistory PRIMARY KEY (Id),
    CONSTRAINT FK_SubscriptionHistory_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_SubscriptionHistory_Plan FOREIGN KEY (SubscriptionPlanId) REFERENCES dbo.SubscriptionPlan(Id),
    CONSTRAINT CK_SubscriptionHistory_ActionType CHECK (ActionType IN ('Subscribe', 'Upgrade', 'Downgrade', 'Renew', 'Cancel', 'Refund'))
);
GO

-- =============================================
-- SECTION 3: USER & ORGANIZATION (ASP.NET Identity Compatible)
-- =============================================

-- Roles: ASP.NET Identity compatible roles table
-- Stores system-wide role definitions
CREATE TABLE dbo.Roles (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key, role identifier
    Name NVARCHAR(256) NOT NULL,                            -- Role name: Admin, Director, HRManager, etc.
    NormalizedName NVARCHAR(256) NOT NULL,                  -- Uppercase role name for lookups
    ConcurrencyStamp NVARCHAR(MAX) NULL,                    -- Optimistic concurrency token
    Description NVARCHAR(500) NULL,                         -- Human-readable description of the role
    IsSystemRole BIT NOT NULL DEFAULT 0,                    -- 1 = System role (cannot be deleted)
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    CONSTRAINT PK_Roles PRIMARY KEY (Id),
    CONSTRAINT UQ_Roles_NormalizedName UNIQUE (NormalizedName)
);

-- Department: Organizational units within an enterprise
-- Each enterprise can have multiple departments
CREATE TABLE dbo.Department (
    Id INT IDENTITY(1,1) NOT NULL,                          -- Primary key, auto-increment
    EnterpriseId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enterprise (tenant isolation)
    DepartmentName NVARCHAR(200) NOT NULL,                  -- Department display name
    DepartmentCode VARCHAR(50) NULL,                        -- Internal code for the department
    Description NVARCHAR(500) NULL,                         -- Description of department function
    ManagerId UNIQUEIDENTIFIER NULL,                        -- FK to Employee who manages this dept
    ParentDepartmentId INT NULL,                            -- FK to parent dept (for hierarchy)
    IsActive BIT NOT NULL DEFAULT 1,                        -- Whether department is active
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Department PRIMARY KEY (Id),
    CONSTRAINT FK_Department_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_Department_Parent FOREIGN KEY (ParentDepartmentId) REFERENCES dbo.Department(Id)
);

-- Users: ASP.NET Identity compatible users table
-- Central user account for all system users (employees, candidates, admins)
CREATE TABLE dbo.Users (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key, user identifier
    UserName NVARCHAR(256) NOT NULL,                        -- Login username (usually email)
    NormalizedUserName NVARCHAR(256) NOT NULL,              -- Uppercase username for lookups
    Email NVARCHAR(256) NOT NULL,                           -- Email address
    NormalizedEmail NVARCHAR(256) NOT NULL,                 -- Uppercase email for lookups
    EmailConfirmed BIT NOT NULL DEFAULT 0,                  -- Whether email is verified
    PasswordHash NVARCHAR(MAX) NULL,                        -- Hashed password (null for external logins)
    SecurityStamp NVARCHAR(MAX) NULL,                       -- Security stamp for password changes
    ConcurrencyStamp NVARCHAR(MAX) NULL,                    -- Optimistic concurrency token
    PhoneNumber VARCHAR(20) NULL,                           -- Phone number
    PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,            -- Whether phone is verified
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,                -- Whether 2FA is enabled
    LockoutEnd DATETIMEOFFSET NULL,                         -- When lockout expires (null = not locked)
    LockoutEnabled BIT NOT NULL DEFAULT 1,                  -- Whether account can be locked
    AccessFailedCount INT NOT NULL DEFAULT 0,               -- Failed login attempts counter
    FullName NVARCHAR(200) NOT NULL,                        -- User's full display name
    AvatarUrl VARCHAR(500) NULL,                            -- URL to profile picture
    EnterpriseId UNIQUEIDENTIFIER NULL,                     -- FK to Enterprise (null for candidates)
    DepartmentId INT NULL,                                  -- FK to Department (null for non-employees)
    Status VARCHAR(30) NOT NULL DEFAULT 'Active',           -- Account status: Active, Inactive, Suspended
    LastLoginAt DATETIME NULL,                              -- Last successful login timestamp
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Account creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last profile update timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_NormalizedUserName UNIQUE (NormalizedUserName),
    CONSTRAINT UQ_Users_NormalizedEmail UNIQUE (NormalizedEmail),
    CONSTRAINT FK_Users_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_Users_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id),
    CONSTRAINT CK_Users_Status CHECK (Status IN ('Active', 'Inactive', 'Suspended', 'PendingVerification'))
);

-- UserRoles: Many-to-many relationship between Users and Roles
-- A user can have multiple roles (e.g., Employee + Trainer)
CREATE TABLE dbo.UserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Users
    RoleId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Roles
    AssignedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),      -- When role was assigned
    AssignedById UNIQUEIDENTIFIER NULL,                     -- FK to User who assigned the role
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE
);

-- UserClaims: ASP.NET Identity claims storage
-- Stores additional claims (key-value) for users
CREATE TABLE dbo.UserClaims (
    Id INT IDENTITY(1,1) NOT NULL,                          -- Primary key, auto-increment
    UserId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Users
    ClaimType NVARCHAR(MAX) NULL,                           -- Claim type (e.g., 'permission')
    ClaimValue NVARCHAR(MAX) NULL,                          -- Claim value (e.g., 'can_approve_jobs')
    CONSTRAINT PK_UserClaims PRIMARY KEY (Id),
    CONSTRAINT FK_UserClaims_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);

-- UserLogins: External login providers (Google, Microsoft, etc.)
-- Links external OAuth accounts to local users
CREATE TABLE dbo.UserLogins (
    LoginProvider NVARCHAR(128) NOT NULL,                   -- Provider name: Google, Microsoft, Facebook
    ProviderKey NVARCHAR(128) NOT NULL,                     -- Unique key from provider
    ProviderDisplayName NVARCHAR(MAX) NULL,                 -- Human-readable provider name
    UserId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Users
    CONSTRAINT PK_UserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_UserLogins_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);

-- UserTokens: Authentication tokens storage
-- Stores refresh tokens, password reset tokens, etc.
CREATE TABLE dbo.UserTokens (
    UserId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Users
    LoginProvider NVARCHAR(128) NOT NULL,                   -- Token provider name
    Name NVARCHAR(128) NOT NULL,                            -- Token name (e.g., 'RefreshToken')
    Value NVARCHAR(MAX) NULL,                               -- Token value
    CONSTRAINT PK_UserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_UserTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);

-- RoleClaims: Claims associated with roles
-- All users with a role automatically get these claims
CREATE TABLE dbo.RoleClaims (
    Id INT IDENTITY(1,1) NOT NULL,                          -- Primary key, auto-increment
    RoleId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Roles
    ClaimType NVARCHAR(MAX) NULL,                           -- Claim type
    ClaimValue NVARCHAR(MAX) NULL,                          -- Claim value
    CONSTRAINT PK_RoleClaims PRIMARY KEY (Id),
    CONSTRAINT FK_RoleClaims_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE
);

-- Employee: Extended profile for users who are employees
-- Contains HR-specific information not in base User table
CREATE TABLE dbo.Employee (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key (different from UserId for flexibility)
    UserId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Users (1:1 relationship)
    EnterpriseId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enterprise (denormalized for queries)
    EmployeeCode VARCHAR(50) NOT NULL,                      -- Unique employee ID within company (e.g., EMP001)
    DepartmentId INT NOT NULL,                              -- FK to Department
    Position NVARCHAR(200) NULL,                            -- Job title/position
    HireDate DATETIME NULL,                                 -- Employment start date
    TerminationDate DATETIME NULL,                          -- Employment end date (null if current)
    EmploymentType VARCHAR(30) NOT NULL DEFAULT 'FullTime', -- FullTime, PartTime, Contract, Intern
    ManagerId UNIQUEIDENTIFIER NULL,                        -- FK to Employee (direct manager)
    Salary DECIMAL(18,2) NULL,                              -- Current salary (confidential)
    IsTrainer BIT NOT NULL DEFAULT 0,                       -- Can create and manage training courses
    Status VARCHAR(30) NOT NULL DEFAULT 'Active',           -- Active, OnLeave, Terminated, Probation
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Employee PRIMARY KEY (Id),
    CONSTRAINT UQ_Employee_UserId UNIQUE (UserId),
    CONSTRAINT UQ_Employee_EnterpriseCode UNIQUE (EnterpriseId, EmployeeCode),
    CONSTRAINT FK_Employee_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Employee_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_Employee_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id),
    CONSTRAINT FK_Employee_Manager FOREIGN KEY (ManagerId) REFERENCES dbo.Employee(Id),
    CONSTRAINT CK_Employee_EmploymentType CHECK (EmploymentType IN ('FullTime', 'PartTime', 'Contract', 'Intern', 'Temporary')),
    CONSTRAINT CK_Employee_Status CHECK (Status IN ('Active', 'OnLeave', 'Terminated', 'Probation', 'Resigned'))
);

-- Add FK from Department.ManagerId to Employee after Employee is created
ALTER TABLE dbo.Department ADD CONSTRAINT FK_Department_Manager FOREIGN KEY (ManagerId) REFERENCES dbo.Employee(Id);

-- Add FK from Enterprise.CreatedById to Users after Users is created
ALTER TABLE dbo.Enterprise ADD CONSTRAINT FK_Enterprise_CreatedBy FOREIGN KEY (CreatedById) REFERENCES dbo.Users(Id);
GO

-- =============================================
-- SECTION 4: SKILL CATALOG (Shared)
-- =============================================

-- Skill: Master catalog of skills used in jobs and candidates
-- Shared across enterprise or global (based on EnterpriseId)
CREATE TABLE dbo.Skill (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnterpriseId UNIQUEIDENTIFIER NULL,                     -- FK to Enterprise (null = global skill)
    SkillName NVARCHAR(200) NOT NULL,                       -- Skill display name
    SkillCategory VARCHAR(100) NULL,                        -- Category: Technical, Soft, Language, etc.
    Description NVARCHAR(500) NULL,                         -- Detailed description
    IsActive BIT NOT NULL DEFAULT 1,                        -- Whether skill is available for use
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Skill PRIMARY KEY (Id),
    CONSTRAINT FK_Skill_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id)
);
GO

-- =============================================
-- SECTION 5: RECRUITMENT FLOW
-- =============================================

-- RecruitmentPlan: Recruitment campaign/plan (e.g., "Q1/2026 Hiring")
-- Created by HR Manager, contains multiple hiring requests
CREATE TABLE dbo.RecruitmentPlan (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnterpriseId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enterprise
    PlanName NVARCHAR(200) NOT NULL,                        -- Plan display name (e.g., "Q1 2026 Hiring Plan")
    PlanCode VARCHAR(50) NOT NULL,                          -- Unique code within enterprise
    Description NVARCHAR(MAX) NULL,                         -- Detailed description of the plan
    StartDate DATETIME NOT NULL,                            -- Plan period start date
    EndDate DATETIME NOT NULL,                              -- Plan period end date
    TotalBudget DECIMAL(18,2) NULL,                         -- Total budget allocated for this plan
    Status VARCHAR(30) NOT NULL DEFAULT 'Draft',            -- Draft, Active, Completed, Cancelled
    CreatedById UNIQUEIDENTIFIER NOT NULL,                  -- FK to User (HR Manager who created)
    ApprovedById UNIQUEIDENTIFIER NULL,                     -- FK to User (Director who approved)
    ApprovedAt DATETIME NULL,                               -- When the plan was approved
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_RecruitmentPlan PRIMARY KEY (Id),
    CONSTRAINT UQ_RecruitmentPlan_EnterpriseCode UNIQUE (EnterpriseId, PlanCode),
    CONSTRAINT FK_RecruitmentPlan_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_RecruitmentPlan_CreatedBy FOREIGN KEY (CreatedById) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_RecruitmentPlan_ApprovedBy FOREIGN KEY (ApprovedById) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_RecruitmentPlan_Status CHECK (Status IN ('Draft', 'Active', 'Completed', 'Cancelled'))
);

-- PlanDetail: Individual hiring request within a recruitment plan
-- Submitted by Department Head for specific positions needed
CREATE TABLE dbo.PlanDetail (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    RecruitmentPlanId UNIQUEIDENTIFIER NOT NULL,            -- FK to RecruitmentPlan
    DepartmentId INT NOT NULL,                              -- FK to Department requesting the hire
    RequestedById UNIQUEIDENTIFIER NOT NULL,                -- FK to User (Dept Head who requested)
    PositionTitle NVARCHAR(200) NOT NULL,                   -- Job title to hire for
    Quantity INT NOT NULL DEFAULT 1,                        -- Number of positions to fill
    Priority VARCHAR(30) NOT NULL DEFAULT 'Normal',         -- Urgency: Low, Normal, High, Critical
    Justification NVARCHAR(MAX) NULL,                       -- Business reason for the hire
    RequiredSkills NVARCHAR(MAX) NULL,                      -- JSON array of required skill IDs
    MinExperience INT NULL,                                 -- Minimum years of experience
    MaxExperience INT NULL,                                 -- Maximum years of experience
    EducationLevel VARCHAR(50) NULL,                        -- Required education: Bachelor, Master, etc.
    SalaryRangeMin DECIMAL(18,2) NULL,                      -- Budget min salary
    SalaryRangeMax DECIMAL(18,2) NULL,                      -- Budget max salary
    ExpectedStartDate DATETIME NULL,                        -- When the new hire should start
    Status VARCHAR(30) NOT NULL DEFAULT 'Draft',            -- Draft, PendingApproval, Approved, Rejected, Processing, Completed, Cancelled
    ReviewerId UNIQUEIDENTIFIER NULL,                       -- FK to User who reviewed (Director)
    ReviewedAt DATETIME NULL,                               -- When review decision was made
    ReviewNote NVARCHAR(500) NULL,                          -- Reviewer's comments
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_PlanDetail PRIMARY KEY (Id),
    CONSTRAINT FK_PlanDetail_RecruitmentPlan FOREIGN KEY (RecruitmentPlanId) REFERENCES dbo.RecruitmentPlan(Id),
    CONSTRAINT FK_PlanDetail_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id),
    CONSTRAINT FK_PlanDetail_RequestedBy FOREIGN KEY (RequestedById) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_PlanDetail_Reviewer FOREIGN KEY (ReviewerId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_PlanDetail_Status CHECK (Status IN ('Draft', 'PendingApproval', 'Approved', 'Rejected', 'Processing', 'Completed', 'Cancelled')),
    CONSTRAINT CK_PlanDetail_Priority CHECK (Priority IN ('Low', 'Normal', 'High', 'Critical'))
);

-- ApprovalHistory: Audit trail for all approval/rejection actions
-- Tracks who approved/rejected what and when
CREATE TABLE dbo.ApprovalHistory (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EntityType VARCHAR(50) NOT NULL,                        -- Type: PlanDetail, TrainingPlan, Offer, etc.
    EntityId UNIQUEIDENTIFIER NOT NULL,                     -- FK to the entity being approved
    Action VARCHAR(30) NOT NULL,                            -- Submit, Approve, Reject, Cancel, Revise
    PreviousStatus VARCHAR(30) NULL,                        -- Status before this action
    NewStatus VARCHAR(30) NOT NULL,                         -- Status after this action
    PerformedById UNIQUEIDENTIFIER NOT NULL,                -- FK to User who performed the action
    Note NVARCHAR(MAX) NULL,                                -- Comments/reason for the action
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Action timestamp
    CONSTRAINT PK_ApprovalHistory PRIMARY KEY (Id),
    CONSTRAINT FK_ApprovalHistory_PerformedBy FOREIGN KEY (PerformedById) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_ApprovalHistory_Action CHECK (Action IN ('Submit', 'Approve', 'Reject', 'Cancel', 'Revise', 'Reopen'))
);

-- JobPosting: Published job listings visible to candidates
-- Created from approved PlanDetail by HR Manager
CREATE TABLE dbo.JobPosting (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnterpriseId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enterprise
    PlanDetailId UNIQUEIDENTIFIER NULL,                     -- FK to PlanDetail (optional if standalone job)
    DepartmentId INT NOT NULL,                              -- FK to Department
    JobTitle NVARCHAR(200) NOT NULL,                        -- Position title for display
    JobCode VARCHAR(50) NULL,                               -- Internal job reference code
    Description NVARCHAR(MAX) NOT NULL,                     -- Full job description (HTML/Markdown)
    Requirements NVARCHAR(MAX) NULL,                        -- Job requirements section
    Benefits NVARCHAR(MAX) NULL,                            -- Benefits and perks section
    EmploymentType VARCHAR(30) NOT NULL DEFAULT 'FullTime', -- FullTime, PartTime, Contract, Intern
    ExperienceLevel VARCHAR(30) NULL,                       -- Entry, Junior, Mid, Senior, Lead, Executive
    EducationLevel VARCHAR(50) NULL,                        -- Required education level
    SalaryRangeMin DECIMAL(18,2) NULL,                      -- Min salary (may be hidden from candidates)
    SalaryRangeMax DECIMAL(18,2) NULL,                      -- Max salary (may be hidden from candidates)
    ShowSalary BIT NOT NULL DEFAULT 0,                      -- Whether to display salary on job listing
    Location NVARCHAR(200) NULL,                            -- Work location
    RemoteOption VARCHAR(30) NULL,                          -- OnSite, Remote, Hybrid
    Quantity INT NOT NULL DEFAULT 1,                        -- Number of openings
    ApplicationDeadline DATETIME NULL,                      -- Last date to apply
    Status VARCHAR(30) NOT NULL DEFAULT 'Draft',            -- Draft, Published, Closed, Filled, Cancelled
    PublishedAt DATETIME NULL,                              -- When job was published
    PublishedById UNIQUEIDENTIFIER NULL,                    -- FK to User who published
    ClosedAt DATETIME NULL,                                 -- When job was closed
    ViewCount INT NOT NULL DEFAULT 0,                       -- Number of times job was viewed
    ApplicationCount INT NOT NULL DEFAULT 0,                -- Number of applications received
    CreatedById UNIQUEIDENTIFIER NOT NULL,                  -- FK to User who created the job
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_JobPosting PRIMARY KEY (Id),
    CONSTRAINT FK_JobPosting_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_JobPosting_PlanDetail FOREIGN KEY (PlanDetailId) REFERENCES dbo.PlanDetail(Id),
    CONSTRAINT FK_JobPosting_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id),
    CONSTRAINT FK_JobPosting_PublishedBy FOREIGN KEY (PublishedById) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_JobPosting_CreatedBy FOREIGN KEY (CreatedById) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_JobPosting_Status CHECK (Status IN ('Draft', 'Published', 'Closed', 'Filled', 'Cancelled')),
    CONSTRAINT CK_JobPosting_EmploymentType CHECK (EmploymentType IN ('FullTime', 'PartTime', 'Contract', 'Intern', 'Temporary')),
    CONSTRAINT CK_JobPosting_RemoteOption CHECK (RemoteOption IN ('OnSite', 'Remote', 'Hybrid'))
);

-- JobSkill: Many-to-many relationship between JobPosting and Skill
-- Defines required skills for a job posting
CREATE TABLE dbo.JobSkill (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    JobPostingId UNIQUEIDENTIFIER NOT NULL,                 -- FK to JobPosting
    SkillId UNIQUEIDENTIFIER NOT NULL,                      -- FK to Skill
    IsRequired BIT NOT NULL DEFAULT 1,                      -- 1 = Required, 0 = Nice to have
    MinLevel INT NULL,                                      -- Minimum proficiency level (1-5)
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    CONSTRAINT PK_JobSkill PRIMARY KEY (Id),
    CONSTRAINT UQ_JobSkill UNIQUE (JobPostingId, SkillId),
    CONSTRAINT FK_JobSkill_JobPosting FOREIGN KEY (JobPostingId) REFERENCES dbo.JobPosting(Id) ON DELETE CASCADE,
    CONSTRAINT FK_JobSkill_Skill FOREIGN KEY (SkillId) REFERENCES dbo.Skill(Id)
);
GO

-- =============================================
-- SECTION 6: CANDIDATE PROFILE
-- =============================================

-- Candidate: Profile for job seekers
-- Extends Users table with candidate-specific information
CREATE TABLE dbo.Candidate (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    UserId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Users (1:1 relationship)
    AboutMe NVARCHAR(MAX) NULL,                             -- Personal summary/bio
    Headline NVARCHAR(200) NULL,                            -- Professional headline
    CurrentPosition NVARCHAR(200) NULL,                     -- Current job title
    CurrentCompany NVARCHAR(200) NULL,                      -- Current employer name
    Location NVARCHAR(200) NULL,                            -- Current location
    LinkedInUrl VARCHAR(500) NULL,                          -- LinkedIn profile URL
    PortfolioUrl VARCHAR(500) NULL,                         -- Portfolio/personal website URL
    ExpectedSalary DECIMAL(18,2) NULL,                      -- Expected salary
    NoticePeriod INT NULL,                                  -- Notice period in days
    IsOpenToWork BIT NOT NULL DEFAULT 1,                    -- Actively looking for jobs
    PreferredJobTypes NVARCHAR(200) NULL,                   -- JSON array: FullTime, PartTime, etc.
    PreferredLocations NVARCHAR(500) NULL,                  -- JSON array of preferred locations
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Profile creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last profile update timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Candidate PRIMARY KEY (Id),
    CONSTRAINT UQ_Candidate_UserId UNIQUE (UserId),
    CONSTRAINT FK_Candidate_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);

-- Education: Candidate's educational background
-- Multiple education records per candidate
CREATE TABLE dbo.Education (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CandidateId UNIQUEIDENTIFIER NOT NULL,                  -- FK to Candidate
    Institution NVARCHAR(300) NOT NULL,                     -- School/University name
    Degree NVARCHAR(200) NOT NULL,                          -- Degree type: Bachelor, Master, PhD, etc.
    FieldOfStudy NVARCHAR(200) NULL,                        -- Major/specialization
    StartDate DATETIME NULL,                                -- Start date of study
    EndDate DATETIME NULL,                                  -- End date (null if ongoing)
    IsCurrent BIT NOT NULL DEFAULT 0,                       -- Currently studying here
    Grade NVARCHAR(50) NULL,                                -- GPA or grade achieved
    Description NVARCHAR(MAX) NULL,                         -- Additional details
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    CONSTRAINT PK_Education PRIMARY KEY (Id),
    CONSTRAINT FK_Education_Candidate FOREIGN KEY (CandidateId) REFERENCES dbo.Candidate(Id) ON DELETE CASCADE
);

-- WorkExperience: Candidate's work history
-- Multiple work experience records per candidate
CREATE TABLE dbo.WorkExperience (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CandidateId UNIQUEIDENTIFIER NOT NULL,                  -- FK to Candidate
    CompanyName NVARCHAR(300) NOT NULL,                     -- Employer name
    Position NVARCHAR(200) NOT NULL,                        -- Job title held
    Location NVARCHAR(200) NULL,                            -- Work location
    StartDate DATETIME NOT NULL,                            -- Employment start date
    EndDate DATETIME NULL,                                  -- Employment end date (null if current)
    IsCurrent BIT NOT NULL DEFAULT 0,                       -- Currently working here
    Description NVARCHAR(MAX) NULL,                         -- Job responsibilities and achievements
    EmploymentType VARCHAR(30) NULL,                        -- FullTime, PartTime, Contract, etc.
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    CONSTRAINT PK_WorkExperience PRIMARY KEY (Id),
    CONSTRAINT FK_WorkExperience_Candidate FOREIGN KEY (CandidateId) REFERENCES dbo.Candidate(Id) ON DELETE CASCADE
);

-- CandidateSkill: Many-to-many relationship between Candidate and Skill
-- Tracks candidate's skills with proficiency levels
CREATE TABLE dbo.CandidateSkill (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CandidateId UNIQUEIDENTIFIER NOT NULL,                  -- FK to Candidate
    SkillId UNIQUEIDENTIFIER NOT NULL,                      -- FK to Skill
    ProficiencyLevel INT NULL,                              -- Skill level: 1=Beginner to 5=Expert
    YearsOfExperience INT NULL,                             -- Years using this skill
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    CONSTRAINT PK_CandidateSkill PRIMARY KEY (Id),
    CONSTRAINT UQ_CandidateSkill UNIQUE (CandidateId, SkillId),
    CONSTRAINT FK_CandidateSkill_Candidate FOREIGN KEY (CandidateId) REFERENCES dbo.Candidate(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CandidateSkill_Skill FOREIGN KEY (SkillId) REFERENCES dbo.Skill(Id)
);

-- Resume: Uploaded CV/resume files
-- Candidates can have multiple resume versions
CREATE TABLE dbo.Resume (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CandidateId UNIQUEIDENTIFIER NOT NULL,                  -- FK to Candidate
    FileName NVARCHAR(300) NOT NULL,                        -- Original file name
    FileUrl VARCHAR(500) NOT NULL,                          -- Storage URL/path
    FileSize INT NULL,                                      -- File size in bytes
    FileType VARCHAR(50) NULL,                              -- MIME type: application/pdf, etc.
    IsDefault BIT NOT NULL DEFAULT 0,                       -- Primary resume for applications
    ParsedData NVARCHAR(MAX) NULL,                          -- AI-extracted data in JSON format
    UploadedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),      -- Upload timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Resume PRIMARY KEY (Id),
    CONSTRAINT FK_Resume_Candidate FOREIGN KEY (CandidateId) REFERENCES dbo.Candidate(Id) ON DELETE CASCADE
);
GO

-- =============================================
-- SECTION 7: APPLICATION & INTERVIEW
-- =============================================

-- Application: Job application submitted by candidate
-- Tracks candidate through the hiring pipeline
CREATE TABLE dbo.Application (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    JobPostingId UNIQUEIDENTIFIER NOT NULL,                 -- FK to JobPosting
    CandidateId UNIQUEIDENTIFIER NOT NULL,                  -- FK to Candidate
    ResumeId UNIQUEIDENTIFIER NULL,                         -- FK to Resume used for application
    CoverLetter NVARCHAR(MAX) NULL,                         -- Cover letter text
    ExpectedSalary DECIMAL(18,2) NULL,                      -- Salary expectation for this job
    AvailableStartDate DATETIME NULL,                       -- When candidate can start
    Stage VARCHAR(50) NOT NULL DEFAULT 'Applied',           -- Pipeline stage: Applied, Screening, Interview, Offer, Hired, Rejected
    StageUpdatedAt DATETIME NULL,                           -- When stage last changed
    Status VARCHAR(30) NOT NULL DEFAULT 'Active',           -- Active, Withdrawn, Rejected, Hired
    Source VARCHAR(50) NULL,                                -- How candidate found the job: Direct, LinkedIn, Referral
    ReferredById UNIQUEIDENTIFIER NULL,                     -- FK to Employee (if referral)
    Rating INT NULL,                                        -- Overall candidate rating (1-5)
    HRNote NVARCHAR(MAX) NULL,                              -- Internal HR notes
    RejectionReason NVARCHAR(500) NULL,                     -- Reason if rejected
    RejectedById UNIQUEIDENTIFIER NULL,                     -- FK to User who rejected
    RejectedAt DATETIME NULL,                               -- When rejected
    AppliedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Application submission timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Application PRIMARY KEY (Id),
    CONSTRAINT UQ_Application UNIQUE (JobPostingId, CandidateId), -- One application per job per candidate
    CONSTRAINT FK_Application_JobPosting FOREIGN KEY (JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_Application_Candidate FOREIGN KEY (CandidateId) REFERENCES dbo.Candidate(Id),
    CONSTRAINT FK_Application_Resume FOREIGN KEY (ResumeId) REFERENCES dbo.Resume(Id),
    CONSTRAINT FK_Application_ReferredBy FOREIGN KEY (ReferredById) REFERENCES dbo.Employee(Id),
    CONSTRAINT FK_Application_RejectedBy FOREIGN KEY (RejectedById) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Application_Stage CHECK (Stage IN ('Applied', 'Screening', 'Shortlisted', 'Interview', 'Assessment', 'Offer', 'Hired', 'Rejected', 'OnHold')),
    CONSTRAINT CK_Application_Status CHECK (Status IN ('Active', 'Withdrawn', 'Rejected', 'Hired'))
);

-- CVScreeningResult: AI-powered CV screening results
-- Stores automated screening scores and analysis
CREATE TABLE dbo.CVScreeningResult (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    ApplicationId UNIQUEIDENTIFIER NOT NULL,                -- FK to Application
    OverallScore DECIMAL(5,2) NOT NULL,                     -- Overall match score (0-100)
    SkillMatchScore DECIMAL(5,2) NULL,                      -- Skills match percentage
    ExperienceMatchScore DECIMAL(5,2) NULL,                 -- Experience match percentage
    EducationMatchScore DECIMAL(5,2) NULL,                  -- Education match percentage
    KeywordMatchScore DECIMAL(5,2) NULL,                    -- Keyword relevance score
    MatchedSkills NVARCHAR(MAX) NULL,                       -- JSON array of matched skill IDs
    MissingSkills NVARCHAR(MAX) NULL,                       -- JSON array of missing required skill IDs
    Strengths NVARCHAR(MAX) NULL,                           -- AI-identified strengths
    Concerns NVARCHAR(MAX) NULL,                            -- AI-identified concerns
    Summary NVARCHAR(MAX) NULL,                             -- AI-generated summary
    RawResponse NVARCHAR(MAX) NULL,                         -- Full AI response for debugging
    ProcessedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),     -- When screening was performed
    AIModel VARCHAR(100) NULL,                              -- AI model used for screening
    CONSTRAINT PK_CVScreeningResult PRIMARY KEY (Id),
    CONSTRAINT UQ_CVScreeningResult_Application UNIQUE (ApplicationId),
    CONSTRAINT FK_CVScreeningResult_Application FOREIGN KEY (ApplicationId) REFERENCES dbo.Application(Id) ON DELETE CASCADE
);

-- Interview: Interview scheduling
-- Each application can have multiple interview rounds
CREATE TABLE dbo.Interview (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    ApplicationId UNIQUEIDENTIFIER NOT NULL,                -- FK to Application
    InterviewType VARCHAR(50) NOT NULL,                     -- Phone, Video, OnSite, Technical, Behavioral, Final
    RoundNumber INT NOT NULL DEFAULT 1,                     -- Interview round (1st, 2nd, 3rd...)
    ScheduledAt DATETIME NOT NULL,                          -- Interview date and time
    Duration INT NOT NULL DEFAULT 60,                       -- Duration in minutes
    Location NVARCHAR(500) NULL,                            -- Physical location or video link
    MeetingLink VARCHAR(500) NULL,                          -- Video conference URL
    Status VARCHAR(30) NOT NULL DEFAULT 'Scheduled',        -- Scheduled, Confirmed, Completed, Cancelled, NoShow, Rescheduled
    ScheduledById UNIQUEIDENTIFIER NOT NULL,                -- FK to User (HR who scheduled)
    OverallRating INT NULL,                                 -- Combined rating (1-5)
    OverallFeedback NVARCHAR(MAX) NULL,                     -- Combined feedback summary
    Decision VARCHAR(30) NULL,                              -- Pass, Fail, OnHold, NeedsAnotherRound
    Note NVARCHAR(MAX) NULL,                                -- Additional notes
    CompletedAt DATETIME NULL,                              -- When interview was completed
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Interview PRIMARY KEY (Id),
    CONSTRAINT FK_Interview_Application FOREIGN KEY (ApplicationId) REFERENCES dbo.Application(Id),
    CONSTRAINT FK_Interview_ScheduledBy FOREIGN KEY (ScheduledById) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Interview_Type CHECK (InterviewType IN ('Phone', 'Video', 'OnSite', 'Technical', 'Behavioral', 'Panel', 'Final', 'HR')),
    CONSTRAINT CK_Interview_Status CHECK (Status IN ('Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow', 'Rescheduled')),
    CONSTRAINT CK_Interview_Decision CHECK (Decision IN ('Pass', 'Fail', 'OnHold', 'NeedsAnotherRound'))
);

-- InterviewParticipant: Interviewers assigned to each interview
-- Supports multiple interviewers per interview (panel interviews)
CREATE TABLE dbo.InterviewParticipant (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    InterviewId UNIQUEIDENTIFIER NOT NULL,                  -- FK to Interview
    EmployeeId UNIQUEIDENTIFIER NOT NULL,                   -- FK to Employee (interviewer)
    Role VARCHAR(50) NOT NULL DEFAULT 'Interviewer',        -- Interviewer, Lead, Observer
    IsRequired BIT NOT NULL DEFAULT 1,                      -- Must attend or optional
    ConfirmationStatus VARCHAR(30) NOT NULL DEFAULT 'Pending', -- Pending, Confirmed, Declined
    Rating INT NULL,                                        -- Individual rating (1-5)
    Feedback NVARCHAR(MAX) NULL,                            -- Individual feedback
    Recommendation VARCHAR(30) NULL,                        -- StrongYes, Yes, Neutral, No, StrongNo
    FeedbackSubmittedAt DATETIME NULL,                      -- When feedback was submitted
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    CONSTRAINT PK_InterviewParticipant PRIMARY KEY (Id),
    CONSTRAINT UQ_InterviewParticipant UNIQUE (InterviewId, EmployeeId),
    CONSTRAINT FK_InterviewParticipant_Interview FOREIGN KEY (InterviewId) REFERENCES dbo.Interview(Id) ON DELETE CASCADE,
    CONSTRAINT FK_InterviewParticipant_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee(Id),
    CONSTRAINT CK_InterviewParticipant_Role CHECK (Role IN ('Interviewer', 'Lead', 'Observer', 'Technical', 'HR')),
    CONSTRAINT CK_InterviewParticipant_Status CHECK (ConfirmationStatus IN ('Pending', 'Confirmed', 'Declined', 'Tentative')),
    CONSTRAINT CK_InterviewParticipant_Recommendation CHECK (Recommendation IN ('StrongYes', 'Yes', 'Neutral', 'No', 'StrongNo'))
);

-- Offer: Job offers sent to candidates
-- Tracks offer details and acceptance status
CREATE TABLE dbo.Offer (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    ApplicationId UNIQUEIDENTIFIER NOT NULL,                -- FK to Application
    OfferCode VARCHAR(50) NULL,                             -- Unique offer reference code
    Position NVARCHAR(200) NOT NULL,                        -- Offered job title
    DepartmentId INT NOT NULL,                              -- FK to Department
    Salary DECIMAL(18,2) NOT NULL,                          -- Offered base salary
    SalaryFrequency VARCHAR(30) NOT NULL DEFAULT 'Monthly', -- Monthly, Yearly, Hourly
    Bonus NVARCHAR(500) NULL,                               -- Bonus details
    Benefits NVARCHAR(MAX) NULL,                            -- Benefits package description
    StartDate DATETIME NOT NULL,                            -- Expected start date
    ExpirationDate DATETIME NOT NULL,                       -- Offer expires after this date
    OfferLetterUrl VARCHAR(500) NULL,                       -- URL to offer letter document
    Status VARCHAR(30) NOT NULL DEFAULT 'Draft',            -- Draft, PendingApproval, Sent, Accepted, Rejected, Expired, Withdrawn
    CreatedById UNIQUEIDENTIFIER NOT NULL,                  -- FK to User who created the offer
    ApprovedById UNIQUEIDENTIFIER NULL,                     -- FK to User who approved (Director)
    ApprovedAt DATETIME NULL,                               -- When offer was approved
    SentAt DATETIME NULL,                                   -- When offer was sent to candidate
    SentById UNIQUEIDENTIFIER NULL,                         -- FK to User who sent the offer
    RespondedAt DATETIME NULL,                              -- When candidate responded
    CandidateNote NVARCHAR(MAX) NULL,                       -- Candidate's response note
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Offer PRIMARY KEY (Id),
    CONSTRAINT UQ_Offer_Application UNIQUE (ApplicationId), -- One active offer per application
    CONSTRAINT FK_Offer_Application FOREIGN KEY (ApplicationId) REFERENCES dbo.Application(Id),
    CONSTRAINT FK_Offer_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id),
    CONSTRAINT FK_Offer_CreatedBy FOREIGN KEY (CreatedById) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Offer_ApprovedBy FOREIGN KEY (ApprovedById) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Offer_SentBy FOREIGN KEY (SentById) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Offer_Status CHECK (Status IN ('Draft', 'PendingApproval', 'Approved', 'Sent', 'Accepted', 'Rejected', 'Expired', 'Withdrawn', 'Negotiating')),
    CONSTRAINT CK_Offer_SalaryFrequency CHECK (SalaryFrequency IN ('Hourly', 'Daily', 'Weekly', 'BiWeekly', 'Monthly', 'Yearly'))
);
GO

-- =============================================
-- SECTION 8: TRAINING (LMS) MODULE
-- =============================================

-- TrainingPlan: Training plans submitted by Department Heads
-- Requires Director approval before trainers can create courses
CREATE TABLE dbo.TrainingPlan (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnterpriseId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enterprise
    PlanName NVARCHAR(200) NOT NULL,                        -- Plan display name
    PlanCode VARCHAR(50) NOT NULL,                          -- Unique code within enterprise
    Description NVARCHAR(MAX) NULL,                         -- Detailed description
    DepartmentId INT NOT NULL,                              -- FK to Department requesting training
    RequestedById UNIQUEIDENTIFIER NOT NULL,                -- FK to User (Dept Head)
    TrainingObjectives NVARCHAR(MAX) NULL,                  -- Goals to achieve
    TargetAudience NVARCHAR(500) NULL,                      -- Who should take this training
    EstimatedParticipants INT NULL,                         -- Expected number of trainees
    EstimatedBudget DECIMAL(18,2) NULL,                     -- Budget for the training
    StartDate DATETIME NULL,                                -- Planned start date
    EndDate DATETIME NULL,                                  -- Planned end date
    Status VARCHAR(30) NOT NULL DEFAULT 'Draft',            -- Draft, PendingApproval, Approved, Rejected, Active, Completed, Cancelled
    ReviewerId UNIQUEIDENTIFIER NULL,                       -- FK to User who reviewed (Director)
    ReviewedAt DATETIME NULL,                               -- When review decision was made
    ReviewNote NVARCHAR(500) NULL,                          -- Reviewer's comments
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_TrainingPlan PRIMARY KEY (Id),
    CONSTRAINT UQ_TrainingPlan_EnterpriseCode UNIQUE (EnterpriseId, PlanCode),
    CONSTRAINT FK_TrainingPlan_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_TrainingPlan_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id),
    CONSTRAINT FK_TrainingPlan_RequestedBy FOREIGN KEY (RequestedById) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_TrainingPlan_Reviewer FOREIGN KEY (ReviewerId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_TrainingPlan_Status CHECK (Status IN ('Draft', 'PendingApproval', 'Approved', 'Rejected', 'Active', 'Completed', 'Cancelled'))
);

-- Course: Training course created by Trainer
-- Contains lessons and quiz for employee learning
CREATE TABLE dbo.Course (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnterpriseId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enterprise
    TrainingPlanId UNIQUEIDENTIFIER NULL,                   -- FK to TrainingPlan (optional if standalone)
    CourseName NVARCHAR(200) NOT NULL,                      -- Course display name
    CourseCode VARCHAR(50) NOT NULL,                        -- Unique code within enterprise
    Description NVARCHAR(MAX) NULL,                         -- Course description
    ThumbnailUrl VARCHAR(500) NULL,                         -- Course thumbnail image URL
    TrainerId UNIQUEIDENTIFIER NOT NULL,                    -- FK to Employee (trainer who created)
    DurationMinutes INT NULL,                               -- Estimated total duration
    Level VARCHAR(30) NULL,                                 -- Beginner, Intermediate, Advanced
    Status VARCHAR(30) NOT NULL DEFAULT 'Draft',            -- Draft, Published, Archived
    IsMandatory BIT NOT NULL DEFAULT 0,                     -- Required for all employees
    MaxEnrollments INT NULL,                                -- Max participants (null = unlimited)
    EnrollmentDeadline DATETIME NULL,                       -- Last date to enroll
    PublishedAt DATETIME NULL,                              -- When course was published
    CompletionCriteria VARCHAR(30) NOT NULL DEFAULT 'Quiz', -- Quiz, AllLessons, Both
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Course PRIMARY KEY (Id),
    CONSTRAINT UQ_Course_EnterpriseCode UNIQUE (EnterpriseId, CourseCode),
    CONSTRAINT FK_Course_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_Course_TrainingPlan FOREIGN KEY (TrainingPlanId) REFERENCES dbo.TrainingPlan(Id),
    CONSTRAINT FK_Course_Trainer FOREIGN KEY (TrainerId) REFERENCES dbo.Employee(Id),
    CONSTRAINT CK_Course_Status CHECK (Status IN ('Draft', 'Published', 'Archived')),
    CONSTRAINT CK_Course_Level CHECK (Level IN ('Beginner', 'Intermediate', 'Advanced', 'Expert')),
    CONSTRAINT CK_Course_CompletionCriteria CHECK (CompletionCriteria IN ('Quiz', 'AllLessons', 'Both'))
);

-- CourseSkill: Many-to-many relationship between Course and Skill
-- Skills that this course teaches
CREATE TABLE dbo.CourseSkill (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CourseId UNIQUEIDENTIFIER NOT NULL,                     -- FK to Course
    SkillId UNIQUEIDENTIFIER NOT NULL,                      -- FK to Skill
    SkillLevelGained INT NULL,                              -- Level gained after course (1-5)
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    CONSTRAINT PK_CourseSkill PRIMARY KEY (Id),
    CONSTRAINT UQ_CourseSkill UNIQUE (CourseId, SkillId),
    CONSTRAINT FK_CourseSkill_Course FOREIGN KEY (CourseId) REFERENCES dbo.Course(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CourseSkill_Skill FOREIGN KEY (SkillId) REFERENCES dbo.Skill(Id)
);

-- Lesson: Individual lessons within a course
-- Must be completed sequentially (OrderIndex determines order)
CREATE TABLE dbo.Lesson (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CourseId UNIQUEIDENTIFIER NOT NULL,                     -- FK to Course
    LessonTitle NVARCHAR(300) NOT NULL,                     -- Lesson display title
    Description NVARCHAR(MAX) NULL,                         -- Lesson description
    OrderIndex INT NOT NULL,                                -- Sequence order (1, 2, 3...) - determines unlock sequence
    ContentType VARCHAR(30) NOT NULL DEFAULT 'Video',       -- Video, Document, Link, Interactive
    VideoUrl VARCHAR(500) NULL,                             -- URL to video (YouTube, Vimeo, etc.)
    VideoDurationMinutes INT NULL,                          -- Video length in minutes
    DocumentUrl VARCHAR(500) NULL,                          -- URL to downloadable document
    ExternalLinkUrl VARCHAR(500) NULL,                      -- External resource link
    Content NVARCHAR(MAX) NULL,                             -- Text/HTML content
    IsPreview BIT NOT NULL DEFAULT 0,                       -- Free preview (accessible before enrollment)
    EstimatedMinutes INT NULL,                              -- Estimated time to complete
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Lesson PRIMARY KEY (Id),
    CONSTRAINT UQ_Lesson_CourseOrder UNIQUE (CourseId, OrderIndex), -- Unique order per course
    CONSTRAINT FK_Lesson_Course FOREIGN KEY (CourseId) REFERENCES dbo.Course(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Lesson_ContentType CHECK (ContentType IN ('Video', 'Document', 'Link', 'Interactive', 'Text', 'Quiz'))
);

-- Enrollment: Employee enrollment in a course
-- Tracks progress from start to completion
CREATE TABLE dbo.Enrollment (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CourseId UNIQUEIDENTIFIER NOT NULL,                     -- FK to Course
    EmployeeId UNIQUEIDENTIFIER NOT NULL,                   -- FK to Employee
    EnrolledAt DATETIME NOT NULL DEFAULT GETUTCDATE(),      -- When employee enrolled
    EnrolledById UNIQUEIDENTIFIER NULL,                     -- FK to User who enrolled them (HR or self)
    StartedAt DATETIME NULL,                                -- When employee first started
    CompletedAt DATETIME NULL,                              -- When employee completed
    Status VARCHAR(30) NOT NULL DEFAULT 'NotStarted',       -- NotStarted, InProgress, Completed, Failed, Dropped
    Progress INT NOT NULL DEFAULT 0,                        -- Overall progress percentage (0-100)
    LastAccessedAt DATETIME NULL,                           -- Last time employee accessed course
    CertificateUrl VARCHAR(500) NULL,                       -- URL to completion certificate
    CertificateIssuedAt DATETIME NULL,                      -- When certificate was issued
    Note NVARCHAR(500) NULL,                                -- Additional notes
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Enrollment PRIMARY KEY (Id),
    CONSTRAINT UQ_Enrollment UNIQUE (CourseId, EmployeeId), -- One enrollment per course per employee
    CONSTRAINT FK_Enrollment_Course FOREIGN KEY (CourseId) REFERENCES dbo.Course(Id),
    CONSTRAINT FK_Enrollment_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee(Id),
    CONSTRAINT FK_Enrollment_EnrolledBy FOREIGN KEY (EnrolledById) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Enrollment_Status CHECK (Status IN ('NotStarted', 'InProgress', 'Completed', 'Failed', 'Dropped'))
);

-- LessonProgress: Tracks individual lesson completion per employee
-- WatchPercentage must reach 100% to unlock next lesson
CREATE TABLE dbo.LessonProgress (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnrollmentId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enrollment
    LessonId UNIQUEIDENTIFIER NOT NULL,                     -- FK to Lesson
    StartedAt DATETIME NULL,                                -- When employee started this lesson
    CompletedAt DATETIME NULL,                              -- When employee completed this lesson
    WatchPercentage INT NOT NULL DEFAULT 0,                 -- Video/content completion (0-100) - 100% unlocks next lesson
    LastPosition INT NULL,                                  -- Video resume position in seconds
    TimeSpentMinutes INT NOT NULL DEFAULT 0,                -- Total time spent on this lesson
    Status VARCHAR(30) NOT NULL DEFAULT 'NotStarted',       -- NotStarted, InProgress, Completed
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    CONSTRAINT PK_LessonProgress PRIMARY KEY (Id),
    CONSTRAINT UQ_LessonProgress UNIQUE (EnrollmentId, LessonId),
    CONSTRAINT FK_LessonProgress_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES dbo.Enrollment(Id) ON DELETE CASCADE,
    CONSTRAINT FK_LessonProgress_Lesson FOREIGN KEY (LessonId) REFERENCES dbo.Lesson(Id),
    CONSTRAINT CK_LessonProgress_WatchPercentage CHECK (WatchPercentage >= 0 AND WatchPercentage <= 100),
    CONSTRAINT CK_LessonProgress_Status CHECK (Status IN ('NotStarted', 'InProgress', 'Completed'))
);

-- Quiz: End-of-course assessment
-- Employee must pass (default 80%) to complete the course
CREATE TABLE dbo.Quiz (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CourseId UNIQUEIDENTIFIER NOT NULL,                     -- FK to Course (one quiz per course)
    QuizTitle NVARCHAR(300) NOT NULL,                       -- Quiz display title
    Description NVARCHAR(MAX) NULL,                         -- Quiz instructions
    TimeLimitMinutes INT NULL,                              -- Time limit in minutes (null = unlimited)
    PassingScore INT NOT NULL DEFAULT 80,                   -- Minimum score to pass (percentage) - default 80%
    MaxAttempts INT NULL,                                   -- Maximum attempts allowed (null = unlimited)
    ShuffleQuestions BIT NOT NULL DEFAULT 1,                -- Randomize question order
    ShuffleAnswers BIT NOT NULL DEFAULT 1,                  -- Randomize answer options
    ShowCorrectAnswers BIT NOT NULL DEFAULT 0,              -- Show correct answers after submission
    IsActive BIT NOT NULL DEFAULT 1,                        -- Whether quiz is active
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    IsDeleted BIT NOT NULL DEFAULT 0,                       -- Soft delete flag
    DeletedAt DATETIME NULL,                                -- Timestamp when soft deleted
    CONSTRAINT PK_Quiz PRIMARY KEY (Id),
    CONSTRAINT UQ_Quiz_Course UNIQUE (CourseId),            -- One quiz per course
    CONSTRAINT FK_Quiz_Course FOREIGN KEY (CourseId) REFERENCES dbo.Course(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Quiz_PassingScore CHECK (PassingScore >= 0 AND PassingScore <= 100)
);

-- QuizQuestion: Questions within a quiz
-- Supports multiple choice and true/false
CREATE TABLE dbo.QuizQuestion (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    QuizId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Quiz
    QuestionText NVARCHAR(MAX) NOT NULL,                    -- Question content (can include HTML/Markdown)
    QuestionType VARCHAR(30) NOT NULL DEFAULT 'MultipleChoice', -- MultipleChoice, TrueFalse, MultiSelect
    Options NVARCHAR(MAX) NOT NULL,                         -- JSON array of answer options
    CorrectAnswer NVARCHAR(500) NOT NULL,                   -- Correct answer(s) - single value or JSON array for MultiSelect
    Explanation NVARCHAR(MAX) NULL,                         -- Explanation shown after answering
    Points INT NOT NULL DEFAULT 1,                          -- Points for correct answer
    OrderIndex INT NOT NULL,                                -- Question order in quiz
    ImageUrl VARCHAR(500) NULL,                             -- Optional question image
    IsActive BIT NOT NULL DEFAULT 1,                        -- Whether question is active
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    UpdatedAt DATETIME NULL,                                -- Last modification timestamp
    CONSTRAINT PK_QuizQuestion PRIMARY KEY (Id),
    CONSTRAINT FK_QuizQuestion_Quiz FOREIGN KEY (QuizId) REFERENCES dbo.Quiz(Id) ON DELETE CASCADE,
    CONSTRAINT CK_QuizQuestion_Type CHECK (QuestionType IN ('MultipleChoice', 'TrueFalse', 'MultiSelect'))
);

-- QuizAttempt: Employee's quiz attempt record
-- Each attempt stores the score and pass/fail status
CREATE TABLE dbo.QuizAttempt (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnrollmentId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enrollment
    QuizId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Quiz
    AttemptNumber INT NOT NULL DEFAULT 1,                   -- Attempt sequence (1st, 2nd, 3rd...)
    StartedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- When attempt started
    CompletedAt DATETIME NULL,                              -- When attempt was submitted
    Score DECIMAL(5,2) NULL,                                -- Score achieved (percentage 0-100)
    IsPassed BIT NULL,                                      -- Whether passed (Score >= Quiz.PassingScore)
    TotalQuestions INT NOT NULL,                            -- Number of questions in this attempt
    CorrectAnswers INT NULL,                                -- Number of correct answers
    TimeTakenMinutes INT NULL,                              -- Time taken to complete
    Status VARCHAR(30) NOT NULL DEFAULT 'InProgress',       -- InProgress, Completed, TimedOut, Abandoned
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Record creation timestamp
    CONSTRAINT PK_QuizAttempt PRIMARY KEY (Id),
    CONSTRAINT FK_QuizAttempt_Enrollment FOREIGN KEY (EnrollmentId) REFERENCES dbo.Enrollment(Id),
    CONSTRAINT FK_QuizAttempt_Quiz FOREIGN KEY (QuizId) REFERENCES dbo.Quiz(Id),
    CONSTRAINT CK_QuizAttempt_Status CHECK (Status IN ('InProgress', 'Completed', 'TimedOut', 'Abandoned'))
);

-- QuizAnswer: Individual question answers for each attempt
-- Records what the employee answered
CREATE TABLE dbo.QuizAnswer (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    QuizAttemptId UNIQUEIDENTIFIER NOT NULL,                -- FK to QuizAttempt
    QuizQuestionId UNIQUEIDENTIFIER NOT NULL,               -- FK to QuizQuestion
    SelectedAnswer NVARCHAR(500) NULL,                      -- Employee's answer
    IsCorrect BIT NULL,                                     -- Whether answer was correct
    PointsEarned INT NOT NULL DEFAULT 0,                    -- Points earned for this answer
    AnsweredAt DATETIME NOT NULL DEFAULT GETUTCDATE(),      -- When answer was submitted
    CONSTRAINT PK_QuizAnswer PRIMARY KEY (Id),
    CONSTRAINT UQ_QuizAnswer UNIQUE (QuizAttemptId, QuizQuestionId),
    CONSTRAINT FK_QuizAnswer_Attempt FOREIGN KEY (QuizAttemptId) REFERENCES dbo.QuizAttempt(Id) ON DELETE CASCADE,
    CONSTRAINT FK_QuizAnswer_Question FOREIGN KEY (QuizQuestionId) REFERENCES dbo.QuizQuestion(Id)
);
GO

-- =============================================
-- SECTION 9: SYSTEM TABLES
-- =============================================

-- Notification: User notifications
-- System-generated alerts and messages
CREATE TABLE dbo.Notification (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    UserId UNIQUEIDENTIFIER NOT NULL,                       -- FK to Users (recipient)
    Title NVARCHAR(300) NOT NULL,                           -- Notification title
    Message NVARCHAR(MAX) NOT NULL,                         -- Notification body
    NotificationType VARCHAR(50) NOT NULL,                  -- Type: Application, Interview, Offer, Training, System
    EntityType VARCHAR(50) NULL,                            -- Related entity type (Application, Course, etc.)
    EntityId UNIQUEIDENTIFIER NULL,                         -- Related entity ID
    ActionUrl VARCHAR(500) NULL,                            -- Deep link to related page
    IsRead BIT NOT NULL DEFAULT 0,                          -- Whether notification was read
    ReadAt DATETIME NULL,                                   -- When notification was read
    IsSent BIT NOT NULL DEFAULT 0,                          -- Whether email/push was sent
    SentAt DATETIME NULL,                                   -- When notification was sent
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- Notification creation timestamp
    CONSTRAINT PK_Notification PRIMARY KEY (Id),
    CONSTRAINT FK_Notification_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Notification_Type CHECK (NotificationType IN ('Application', 'Interview', 'Offer', 'Training', 'Approval', 'System', 'Reminder'))
);

-- SavedJob: Jobs saved by candidates
-- Allows candidates to bookmark jobs for later
CREATE TABLE dbo.SavedJob (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    CandidateId UNIQUEIDENTIFIER NOT NULL,                  -- FK to Candidate
    JobPostingId UNIQUEIDENTIFIER NOT NULL,                 -- FK to JobPosting
    SavedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),         -- When job was saved
    Note NVARCHAR(500) NULL,                                -- Candidate's private note
    CONSTRAINT PK_SavedJob PRIMARY KEY (Id),
    CONSTRAINT UQ_SavedJob UNIQUE (CandidateId, JobPostingId),
    CONSTRAINT FK_SavedJob_Candidate FOREIGN KEY (CandidateId) REFERENCES dbo.Candidate(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SavedJob_JobPosting FOREIGN KEY (JobPostingId) REFERENCES dbo.JobPosting(Id)
);

-- OwnershipTransfer: Audit log for enterprise ownership changes
-- Tracks when Director transfers ownership to another employee
CREATE TABLE dbo.OwnershipTransfer (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),           -- Primary key
    EnterpriseId UNIQUEIDENTIFIER NOT NULL,                 -- FK to Enterprise
    FromUserId UNIQUEIDENTIFIER NOT NULL,                   -- FK to previous owner (old Director)
    ToUserId UNIQUEIDENTIFIER NOT NULL,                     -- FK to new owner (new Director)
    Reason NVARCHAR(500) NULL,                              -- Reason for transfer
    TransferredAt DATETIME NOT NULL DEFAULT GETUTCDATE(),   -- When transfer occurred
    ApprovedById UNIQUEIDENTIFIER NULL,                     -- FK to User who approved (if approval required)
    Note NVARCHAR(MAX) NULL,                                -- Additional notes
    CONSTRAINT PK_OwnershipTransfer PRIMARY KEY (Id),
    CONSTRAINT FK_OwnershipTransfer_Enterprise FOREIGN KEY (EnterpriseId) REFERENCES dbo.Enterprise(Id),
    CONSTRAINT FK_OwnershipTransfer_FromUser FOREIGN KEY (FromUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_OwnershipTransfer_ToUser FOREIGN KEY (ToUserId) REFERENCES dbo.Users(Id)
);
GO

-- =============================================
-- SECTION 10: INDEXES FOR PERFORMANCE
-- =============================================

-- Enterprise indexes
CREATE INDEX IX_Enterprise_SubscriptionPlanId ON dbo.Enterprise(SubscriptionPlanId);
CREATE INDEX IX_Enterprise_SubscriptionStatus ON dbo.Enterprise(SubscriptionStatus);
CREATE INDEX IX_Enterprise_IsDeleted ON dbo.Enterprise(IsDeleted);

-- Users indexes
CREATE INDEX IX_Users_EnterpriseId ON dbo.Users(EnterpriseId);
CREATE INDEX IX_Users_DepartmentId ON dbo.Users(DepartmentId);
CREATE INDEX IX_Users_Status ON dbo.Users(Status);
CREATE INDEX IX_Users_Email ON dbo.Users(Email);
CREATE INDEX IX_Users_IsDeleted ON dbo.Users(IsDeleted);

-- Employee indexes
CREATE INDEX IX_Employee_EnterpriseId ON dbo.Employee(EnterpriseId);
CREATE INDEX IX_Employee_DepartmentId ON dbo.Employee(DepartmentId);
CREATE INDEX IX_Employee_ManagerId ON dbo.Employee(ManagerId);
CREATE INDEX IX_Employee_Status ON dbo.Employee(Status);
CREATE INDEX IX_Employee_IsDeleted ON dbo.Employee(IsDeleted);

-- Department indexes
CREATE INDEX IX_Department_EnterpriseId ON dbo.Department(EnterpriseId);
CREATE INDEX IX_Department_ManagerId ON dbo.Department(ManagerId);
CREATE INDEX IX_Department_IsDeleted ON dbo.Department(IsDeleted);

-- RecruitmentPlan indexes
CREATE INDEX IX_RecruitmentPlan_EnterpriseId ON dbo.RecruitmentPlan(EnterpriseId);
CREATE INDEX IX_RecruitmentPlan_Status ON dbo.RecruitmentPlan(Status);
CREATE INDEX IX_RecruitmentPlan_IsDeleted ON dbo.RecruitmentPlan(IsDeleted);

-- PlanDetail indexes
CREATE INDEX IX_PlanDetail_RecruitmentPlanId ON dbo.PlanDetail(RecruitmentPlanId);
CREATE INDEX IX_PlanDetail_DepartmentId ON dbo.PlanDetail(DepartmentId);
CREATE INDEX IX_PlanDetail_Status ON dbo.PlanDetail(Status);
CREATE INDEX IX_PlanDetail_IsDeleted ON dbo.PlanDetail(IsDeleted);

-- JobPosting indexes
CREATE INDEX IX_JobPosting_EnterpriseId ON dbo.JobPosting(EnterpriseId);
CREATE INDEX IX_JobPosting_DepartmentId ON dbo.JobPosting(DepartmentId);
CREATE INDEX IX_JobPosting_Status ON dbo.JobPosting(Status);
CREATE INDEX IX_JobPosting_PublishedAt ON dbo.JobPosting(PublishedAt);
CREATE INDEX IX_JobPosting_IsDeleted ON dbo.JobPosting(IsDeleted);

-- Application indexes
CREATE INDEX IX_Application_JobPostingId ON dbo.Application(JobPostingId);
CREATE INDEX IX_Application_CandidateId ON dbo.Application(CandidateId);
CREATE INDEX IX_Application_Stage ON dbo.Application(Stage);
CREATE INDEX IX_Application_Status ON dbo.Application(Status);
CREATE INDEX IX_Application_AppliedAt ON dbo.Application(AppliedAt);
CREATE INDEX IX_Application_IsDeleted ON dbo.Application(IsDeleted);

-- Interview indexes
CREATE INDEX IX_Interview_ApplicationId ON dbo.Interview(ApplicationId);
CREATE INDEX IX_Interview_ScheduledAt ON dbo.Interview(ScheduledAt);
CREATE INDEX IX_Interview_Status ON dbo.Interview(Status);
CREATE INDEX IX_Interview_IsDeleted ON dbo.Interview(IsDeleted);

-- Candidate indexes
CREATE INDEX IX_Candidate_IsDeleted ON dbo.Candidate(IsDeleted);

-- TrainingPlan indexes
CREATE INDEX IX_TrainingPlan_EnterpriseId ON dbo.TrainingPlan(EnterpriseId);
CREATE INDEX IX_TrainingPlan_DepartmentId ON dbo.TrainingPlan(DepartmentId);
CREATE INDEX IX_TrainingPlan_Status ON dbo.TrainingPlan(Status);
CREATE INDEX IX_TrainingPlan_IsDeleted ON dbo.TrainingPlan(IsDeleted);

-- Course indexes
CREATE INDEX IX_Course_EnterpriseId ON dbo.Course(EnterpriseId);
CREATE INDEX IX_Course_TrainerId ON dbo.Course(TrainerId);
CREATE INDEX IX_Course_Status ON dbo.Course(Status);
CREATE INDEX IX_Course_IsDeleted ON dbo.Course(IsDeleted);

-- Enrollment indexes
CREATE INDEX IX_Enrollment_CourseId ON dbo.Enrollment(CourseId);
CREATE INDEX IX_Enrollment_EmployeeId ON dbo.Enrollment(EmployeeId);
CREATE INDEX IX_Enrollment_Status ON dbo.Enrollment(Status);
CREATE INDEX IX_Enrollment_IsDeleted ON dbo.Enrollment(IsDeleted);

-- LessonProgress indexes
CREATE INDEX IX_LessonProgress_EnrollmentId ON dbo.LessonProgress(EnrollmentId);
CREATE INDEX IX_LessonProgress_LessonId ON dbo.LessonProgress(LessonId);
CREATE INDEX IX_LessonProgress_Status ON dbo.LessonProgress(Status);

-- QuizAttempt indexes
CREATE INDEX IX_QuizAttempt_EnrollmentId ON dbo.QuizAttempt(EnrollmentId);
CREATE INDEX IX_QuizAttempt_QuizId ON dbo.QuizAttempt(QuizId);
CREATE INDEX IX_QuizAttempt_IsPassed ON dbo.QuizAttempt(IsPassed);

-- Notification indexes
CREATE INDEX IX_Notification_UserId ON dbo.Notification(UserId);
CREATE INDEX IX_Notification_IsRead ON dbo.Notification(IsRead);
CREATE INDEX IX_Notification_CreatedAt ON dbo.Notification(CreatedAt);

-- ApprovalHistory indexes
CREATE INDEX IX_ApprovalHistory_EntityType_EntityId ON dbo.ApprovalHistory(EntityType, EntityId);
CREATE INDEX IX_ApprovalHistory_PerformedById ON dbo.ApprovalHistory(PerformedById);
CREATE INDEX IX_ApprovalHistory_CreatedAt ON dbo.ApprovalHistory(CreatedAt);
GO

-- =============================================
-- SECTION 11: SEED DATA - Default Roles
-- =============================================

-- Insert default system roles
INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp, Description, IsSystemRole)
VALUES
    (NEWID(), 'Admin', 'ADMIN', NEWID(), 'System administrator with full access to all features', 1),
    (NEWID(), 'Director', 'DIRECTOR', NEWID(), 'Enterprise owner/director - approves recruitment plans and training plans', 1),
    (NEWID(), 'HRManager', 'HRMANAGER', NEWID(), 'HR Manager - manages recruitment process, job postings, and applications', 1),
    (NEWID(), 'DepartmentHead', 'DEPARTMENTHEAD', NEWID(), 'Department Head - submits hiring requests and training plans for their department', 1),
    (NEWID(), 'Interviewer', 'INTERVIEWER', NEWID(), 'Interviewer - participates in interviews and provides feedback', 1),
    (NEWID(), 'Trainer', 'TRAINER', NEWID(), 'Trainer - creates and manages training courses and content', 1),
    (NEWID(), 'Employee', 'EMPLOYEE', NEWID(), 'Regular employee - can enroll in courses and take training', 1),
    (NEWID(), 'Candidate', 'CANDIDATE', NEWID(), 'Job candidate - can apply for jobs and manage their profile', 1);
GO

-- =============================================
-- SECTION 12: SEED DATA - Default Subscription Plans
-- =============================================

-- Insert default subscription plans
INSERT INTO dbo.SubscriptionPlan (Id, PlanName, PlanCode, Description, MaxUsers, MaxJobPostings, MaxCourses, PriceMonthly, PriceYearly, Features, IsActive, DisplayOrder)
VALUES
    (NEWID(), 'Basic', 'basic', 'Perfect for small teams getting started', 10, 5, 10, 29.00, 290.00,
     '["job_posting","basic_applicant_tracking","email_support"]', 1, 1),

    (NEWID(), 'Pro', 'pro', 'For growing businesses with advanced needs', 50, 20, 50, 99.00, 990.00,
     '["job_posting","advanced_applicant_tracking","ai_cv_screening","training_module","priority_support"]', 1, 2),

    (NEWID(), 'Enterprise', 'enterprise', 'For large organizations with custom requirements', 999999, 999999, 999999, 299.00, 2990.00,
     '["job_posting","advanced_applicant_tracking","ai_cv_screening","training_module","custom_integrations","dedicated_support","sso","api_access"]', 1, 3);
GO

-- =============================================
-- END OF SCHEMA
-- =============================================

PRINT 'ERMS Schema created successfully!';
PRINT 'Tables created: 37';
PRINT 'Indexes created: 45';
PRINT 'Default roles seeded: 8';
PRINT 'Default subscription plans seeded: 3';
GO

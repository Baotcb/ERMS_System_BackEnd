namespace ERMS.Application.Features.Dashboard.Queries.GetTrainingDashboard
{
    public sealed class TrainingDashboardDto
    {
        public int TotalPlans { get; set; }
        public int PendingPlans { get; set; }
        public int ApprovedPlans { get; set; }
        public decimal ApprovedBudget { get; set; }

        public int ActiveCourses { get; set; }
        public int TotalCourses { get; set; }
        public double ReadinessPercent { get; set; }

        public int TotalEnrollments { get; set; }
        public double AvgStudentsPerCourse { get; set; }
        public int CoursesWithContent { get; set; }
    }

    public sealed class GetTrainingDashboardResult
    {
        public TrainingDashboardDto Data { get; set; } = new();
    }
}
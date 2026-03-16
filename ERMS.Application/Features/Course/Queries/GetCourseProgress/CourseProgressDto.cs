namespace ERMS.Application.Features.Courses.Queries.GetCourseProgress;

public class CourseProgressDto
{
    public int TotalLessons { get; set; }

    public int CompletedLessons { get; set; }

    public int ProgressPercentage { get; set; }

    public bool QuizUnlocked { get; set; }
}
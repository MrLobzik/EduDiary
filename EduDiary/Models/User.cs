using Microsoft.AspNetCore.Identity;

namespace EduDiary.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public bool IsTeacher { get; set; }
        public ICollection<Course> CoursesTaught { get; set; }
        public ICollection<CourseStudent> CoursesAttended { get; set; }
        public ICollection<AssignmentSubmission> Submissions { get; set; }
        public ICollection<TestAttempt> TestAttempts { get; set; }
        public ICollection<Attendance> Attendances { get; set; }
    }
}
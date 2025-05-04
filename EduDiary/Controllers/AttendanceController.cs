using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Teacher")]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public AttendanceController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> MarkAttendance(int courseId, DateTime? date = null)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (course.TeacherId != user.Id) return Forbid();

        var selectedDate = date ?? DateTime.Today;

        var students = await _context.CourseStudents
            .Where(cs => cs.CourseId == courseId)
            .Include(cs => cs.Student)
            .Select(cs => cs.Student)
            .ToListAsync();

        var attendances = await _context.Attendances
            .Where(a => a.CourseId == courseId && a.Date == selectedDate)
            .ToListAsync();

        var model = students.Select(student => new AttendanceViewModel
        {
            StudentId = student.Id,
            StudentName = student.FullName,
            Date = selectedDate,
            IsPresent = attendances.FirstOrDefault(a => a.StudentId == student.Id)?.IsPresent ?? false
        }).ToList();

        ViewBag.CourseId = courseId;
        ViewBag.CourseTitle = course.Title;
        ViewBag.Date = selectedDate;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAttendance(int courseId, DateTime date, List<AttendanceViewModel> model)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (course.TeacherId != user.Id) return Forbid();

        foreach (var item in model)
        {
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.CourseId == courseId && a.StudentId == item.StudentId && a.Date == date);

            if (attendance == null)
            {
                _context.Attendances.Add(new Attendance
                {
                    CourseId = courseId,
                    StudentId = item.StudentId,
                    Date = date,
                    IsPresent = item.IsPresent
                });
            }
            else
            {
                attendance.IsPresent = item.IsPresent;
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Course", new { id = courseId });
    }

    public async Task<IActionResult> ViewAttendance(int courseId, string studentId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user.IsTeacher && course.TeacherId != user.Id) return Forbid();
        if (!user.IsTeacher && user.Id != studentId) return Forbid();

        var student = await _userManager.FindByIdAsync(studentId);
        if (student == null) return NotFound();

        var attendances = await _context.Attendances
            .Where(a => a.CourseId == courseId && a.StudentId == studentId)
            .OrderBy(a => a.Date)
            .ToListAsync();

        ViewBag.CourseTitle = course.Title;
        ViewBag.StudentName = student.FullName;

        return View(attendances);
    }
}

public class AttendanceViewModel
{
    public string StudentId { get; set; }
    public string StudentName { get; set; }
    public DateTime Date { get; set; }
    public bool IsPresent { get; set; }
}
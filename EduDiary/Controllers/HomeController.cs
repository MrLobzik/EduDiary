using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (user.IsTeacher)
        {
            var courses = await _context.Courses
                .Where(c => c.TeacherId == user.Id)
                .ToListAsync();
            return View("TeacherDashboard", courses);
        }
        else
        {
            var courses = await _context.CourseStudents
                .Where(cs => cs.StudentId == user.Id)
                .Include(cs => cs.Course)
                .Select(cs => cs.Course)
                .ToListAsync();
            return View("StudentDashboard", courses);
        }
    }
}
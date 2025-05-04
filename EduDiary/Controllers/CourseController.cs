using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class CourseController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public CourseController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Teacher")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create(Course model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            model.TeacherId = user.Id;
            _context.Courses.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var course = await _context.Courses
            .Include(c => c.Sections)
            .Include(c => c.Assignments)
            .Include(c => c.Tests)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        ViewBag.IsTeacher = user.IsTeacher;

        if (user.IsTeacher)
        {
            var students = await _context.CourseStudents
                .Where(cs => cs.CourseId == id)
                .Include(cs => cs.Student)
                .Select(cs => cs.Student)
                .ToListAsync();
            ViewBag.Students = students;
        }

        return View(course);
    }

    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (course.TeacherId != user.Id) return Forbid();

        return View(course);
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Edit(int id, Course model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();
            if (course.TeacherId != user.Id) return Forbid();

            course.Title = model.Title;
            course.Description = model.Description;
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }
        return View(model);
    }

    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> ManageStudents(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (course.TeacherId != user.Id) return Forbid();

        var studentsInCourse = await _context.CourseStudents
            .Where(cs => cs.CourseId == id)
            .Include(cs => cs.Student)
            .Select(cs => cs.Student)
            .ToListAsync();

        var allStudents = await _userManager.Users
            .Where(u => !u.IsTeacher)
            .ToListAsync();

        var studentsNotInCourse = allStudents.Except(studentsInCourse).ToList();

        ViewBag.CourseId = id;
        ViewBag.StudentsNotInCourse = studentsNotInCourse;
        return View(studentsInCourse);
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> AddStudent(int courseId, string studentId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (course.TeacherId != user.Id) return Forbid();

        var student = await _userManager.FindByIdAsync(studentId);
        if (student == null) return NotFound();

        var existing = await _context.CourseStudents
            .FirstOrDefaultAsync(cs => cs.CourseId == courseId && cs.StudentId == studentId);
        if (existing != null) return RedirectToAction("ManageStudents", new { id = courseId });

        _context.CourseStudents.Add(new CourseStudent
        {
            CourseId = courseId,
            StudentId = studentId
        });
        await _context.SaveChangesAsync();
        return RedirectToAction("ManageStudents", new { id = courseId });
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> RemoveStudent(int courseId, string studentId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (course.TeacherId != user.Id) return Forbid();

        var student = await _userManager.FindByIdAsync(studentId);
        if (student == null) return NotFound();

        var courseStudent = await _context.CourseStudents
            .FirstOrDefaultAsync(cs => cs.CourseId == courseId && cs.StudentId == studentId);
        if (courseStudent == null) return NotFound();

        _context.CourseStudents.Remove(courseStudent);
        await _context.SaveChangesAsync();
        return RedirectToAction("ManageStudents", new { id = courseId });
    }
}
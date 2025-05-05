using EduDiary.Data;
using EduDiary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduDiary.Controllers
{
    [Authorize]
    public class AssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment;

        public AssignmentController(ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (course.TeacherId != user.Id) return Forbid();

            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create(int courseId, Assignment model)
        {
            if (ModelState.IsValid)
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (course.TeacherId != user.Id) return Forbid();

                model.CourseId = courseId;
                _context.Assignments.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Course", new { id = courseId });
            }
            ViewBag.CourseId = courseId;
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            ViewBag.IsTeacher = user.IsTeacher;

            if (user.IsTeacher)
            {
                if (assignment.Course.TeacherId != user.Id) return Forbid();
            }
            else
            {
                var isStudentInCourse = await _context.CourseStudents
                    .AnyAsync(cs => cs.CourseId == assignment.CourseId && cs.StudentId == user.Id);
                if (!isStudentInCourse) return Forbid();

                ViewBag.Submission = await _context.AssignmentSubmissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == id && s.StudentId == user.Id);
            }

            return View(assignment);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Edit(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (assignment.Course.TeacherId != user.Id) return Forbid();

            return View(assignment);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Edit(int id, Assignment model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var assignment = await _context.Assignments
                    .Include(a => a.Course)
                    .FirstOrDefaultAsync(a => a.Id == id);
                if (assignment == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (assignment.Course.TeacherId != user.Id) return Forbid();

                assignment.Title = model.Title;
                assignment.Description = model.Description;
                assignment.DueDate = model.DueDate;
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id });
            }
            return View(model);
        }

        public async Task<IActionResult> Submit(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.IsTeacher) return Forbid();

            var isStudentInCourse = await _context.CourseStudents
                .AnyAsync(cs => cs.CourseId == assignment.CourseId && cs.StudentId == user.Id);
            if (!isStudentInCourse) return Forbid();

            var existingSubmission = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == id && s.StudentId == user.Id);
            if (existingSubmission != null)
            {
                return RedirectToAction("EditSubmission", new { id = existingSubmission.Id });
            }

            ViewBag.AssignmentId = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int id, AssignmentSubmission model, IFormFile file)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.IsTeacher) return Forbid();

            var isStudentInCourse = await _context.CourseStudents
                .AnyAsync(cs => cs.CourseId == assignment.CourseId && cs.StudentId == user.Id);
            if (!isStudentInCourse) return Forbid();

            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "submissions");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    model.FilePath = "/submissions/" + uniqueFileName;
                }

                model.AssignmentId = id;
                model.StudentId = user.Id;
                model.SubmissionDate = DateTime.Now;
                _context.AssignmentSubmissions.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id });
            }

            ViewBag.AssignmentId = id;
            return View(model);
        }

        public async Task<IActionResult> EditSubmission(int id)
        {
            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (submission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (submission.StudentId != user.Id) return Forbid();

            return View(submission);
        }

        [HttpPost]
        public async Task<IActionResult> EditSubmission(int id, AssignmentSubmission model, IFormFile file)
        {
            if (id != model.Id) return NotFound();

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (submission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (submission.StudentId != user.Id) return Forbid();

            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    // Удаляем старый файл, если он существует
                    if (!string.IsNullOrEmpty(submission.FilePath))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Загружаем новый файл
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "submissions");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    submission.FilePath = "/submissions/" + uniqueFileName;
                }

                submission.Text = model.Text;
                submission.SubmissionDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = submission.AssignmentId });
            }

            return View(model);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GradeSubmission(int id)
        {
            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (submission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (submission.Assignment.Course.TeacherId != user.Id) return Forbid();

            return View(submission);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GradeSubmission(int id, AssignmentSubmission model)
        {
            if (id != model.Id) return NotFound();

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (submission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (submission.Assignment.Course.TeacherId != user.Id) return Forbid();

            submission.Grade = model.Grade;
            submission.Feedback = model.Feedback;
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = submission.AssignmentId });
        }
    }
}
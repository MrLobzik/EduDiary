using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Teacher")]
public class SectionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public SectionController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

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
    public async Task<IActionResult> Create(int courseId, Section model)
    {
        if (ModelState.IsValid)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (course.TeacherId != user.Id) return Forbid();

            model.CourseId = courseId;
            _context.Sections.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Course", new { id = courseId });
        }
        ViewBag.CourseId = courseId;
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var section = await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (section == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (section.Course.TeacherId != user.Id) return Forbid();

        return View(section);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Section model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var section = await _context.Sections
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (section == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (section.Course.TeacherId != user.Id) return Forbid();

            section.Title = model.Title;
            section.Content = model.Content;
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Course", new { id = section.CourseId });
        }
        return View(model);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var section = await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (section == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (section.Course.TeacherId != user.Id) return Forbid();

        return View(section);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var section = await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (section == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (section.Course.TeacherId != user.Id) return Forbid();

        _context.Sections.Remove(section);
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Course", new { id = section.CourseId });
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Teacher")]
public class ResourceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _environment;

    public ResourceController(ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Create(int sectionId)
    {
        var section = await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (section.Course.TeacherId != user.Id) return Forbid();

        ViewBag.SectionId = sectionId;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(int sectionId, Resource model, IFormFile file)
    {
        var section = await _context.Sections
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (section.Course.TeacherId != user.Id) return Forbid();

        if (ModelState.IsValid)
        {
            if (model.Type == ResourceType.File && file != null)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
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

                model.Url = "/uploads/" + uniqueFileName;
            }

            model.SectionId = sectionId;
            _context.Resources.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Course", new { id = section.CourseId });
        }

        ViewBag.SectionId = sectionId;
        return View(model);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var resource = await _context.Resources
            .Include(r => r.Section)
            .ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resource == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (resource.Section.Course.TeacherId != user.Id) return Forbid();

        return View(resource);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var resource = await _context.Resources
            .Include(r => r.Section)
            .ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resource == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (resource.Section.Course.TeacherId != user.Id) return Forbid();

        if (resource.Type == ResourceType.File && !string.IsNullOrEmpty(resource.Url))
        {
            var filePath = Path.Combine(_environment.WebRootPath, resource.Url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _context.Resources.Remove(resource);
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Course", new { id = resource.Section.CourseId });
    }
}
using EduDiary.Data;
using EduDiary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduDiary.Controllers
{
    [Authorize]
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public TestController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
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
        public async Task<IActionResult> Create(int courseId, Test model)
        {
            if (ModelState.IsValid)
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (course.TeacherId != user.Id) return Forbid();

                model.CourseId = courseId;
                _context.Tests.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManageQuestions", new { id = model.Id });
            }
            ViewBag.CourseId = courseId;
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Course)
                .Include(t => t.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            ViewBag.IsTeacher = user.IsTeacher;

            if (user.IsTeacher)
            {
                if (test.Course.TeacherId != user.Id) return Forbid();
            }
            else
            {
                var isStudentInCourse = await _context.CourseStudents
                    .AnyAsync(cs => cs.CourseId == test.CourseId && cs.StudentId == user.Id);
                if (!isStudentInCourse) return Forbid();

                ViewBag.HasAttempt = await _context.TestAttempts
                    .AnyAsync(a => a.TestId == id && a.StudentId == user.Id);
            }

            return View(test);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ManageQuestions(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Course)
                .Include(t => t.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (test.Course.TeacherId != user.Id) return Forbid();

            return View(test);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateQuestion(int testId, QuestionType type)
        {
            var test = await _context.Tests
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == testId);
            if (test == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (test.Course.TeacherId != user.Id) return Forbid();

            ViewBag.TestId = testId;
            ViewBag.QuestionType = type;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateQuestion(int testId, Question model, List<string> answerTexts, List<bool> isCorrect)
        {
            var test = await _context.Tests
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == testId);
            if (test == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (test.Course.TeacherId != user.Id) return Forbid();

            if (ModelState.IsValid)
            {
                model.TestId = testId;
                _context.Questions.Add(model);
                await _context.SaveChangesAsync();

                if (model.Type != QuestionType.TextAnswer && answerTexts != null)
                {
                    for (int i = 0; i < answerTexts.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(answerTexts[i]))
                        {
                            _context.AnswerOptions.Add(new AnswerOption
                            {
                                Text = answerTexts[i],
                                IsCorrect = isCorrect != null && i < isCorrect.Count && isCorrect[i],
                                QuestionId = model.Id
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("ManageQuestions", new { id = testId });
            }

            ViewBag.TestId = testId;
            ViewBag.QuestionType = model.Type;
            return View(model);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> EditQuestion(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Test)
                .ThenInclude(t => t.Course)
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (question.Test.Course.TeacherId != user.Id) return Forbid();

            ViewBag.TestId = question.TestId;
            return View(question);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> EditQuestion(int id, Question model, List<string> answerTexts, List<bool> isCorrect)
        {
            if (id != model.Id) return NotFound();

            var question = await _context.Questions
                .Include(q => q.Test)
                .ThenInclude(t => t.Course)
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (question.Test.Course.TeacherId != user.Id) return Forbid();

            if (ModelState.IsValid)
            {
                question.Text = model.Text;
                question.Points = model.Points;
                await _context.SaveChangesAsync();

                if (question.Type != QuestionType.TextAnswer)
                {
                    // Удаляем существующие параметры
                    _context.AnswerOptions.RemoveRange(question.AnswerOptions);
                    await _context.SaveChangesAsync();

                    // Добавляем новые параметры
                    if (answerTexts != null)
                    {
                        for (int i = 0; i < answerTexts.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(answerTexts[i]))
                            {
                                _context.AnswerOptions.Add(new AnswerOption
                                {
                                    Text = answerTexts[i],
                                    IsCorrect = isCorrect != null && i < isCorrect.Count && isCorrect[i],
                                    QuestionId = question.Id
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction("ManageQuestions", new { id = question.TestId });
            }

            ViewBag.TestId = question.TestId;
            return View(model);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Test)
                .ThenInclude(t => t.Course)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (question.Test.Course.TeacherId != user.Id) return Forbid();

            return View(question);
        }

        [HttpPost, ActionName("DeleteQuestion")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DeleteQuestionConfirmed(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Test)
                .ThenInclude(t => t.Course)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (question.Test.Course.TeacherId != user.Id) return Forbid();

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageQuestions", new { id = question.TestId });
        }

        public async Task<IActionResult> StartTest(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.IsTeacher) return Forbid();

            var isStudentInCourse = await _context.CourseStudents
                .AnyAsync(cs => cs.CourseId == test.CourseId && cs.StudentId == user.Id);
            if (!isStudentInCourse) return Forbid();

            var existingAttempt = await _context.TestAttempts
                .FirstOrDefaultAsync(a => a.TestId == id && a.StudentId == user.Id);
            if (existingAttempt != null)
            {
                if (existingAttempt.EndTime == null)
                {
                    return RedirectToAction("TakeTest", new { attemptId = existingAttempt.Id });
                }
                else
                {
                    return RedirectToAction("TestResults", new { attemptId = existingAttempt.Id });
                }
            }

            var attempt = new TestAttempt
            {
                TestId = id,
                StudentId = user.Id,
                StartTime = DateTime.Now
            };
            _context.TestAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return RedirectToAction("TakeTest", new { attemptId = attempt.Id });
        }

        public async Task<IActionResult> TakeTest(int attemptId)
        {
            var attempt = await _context.TestAttempts
                .Include(a => a.Test)
                .ThenInclude(t => t.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == attemptId);
            if (attempt == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (attempt.StudentId != user.Id) return Forbid();

            if (attempt.EndTime != null)
            {
                return RedirectToAction("TestResults", new { attemptId });
            }

            // Проверка на окончание выделенного времени
            if (attempt.Test.TimeLimitMinutes > 0)
            {
                var timeElapsed = DateTime.Now - attempt.StartTime;
                if (timeElapsed.TotalMinutes > attempt.Test.TimeLimitMinutes)
                {
                    attempt.EndTime = attempt.StartTime.AddMinutes(attempt.Test.TimeLimitMinutes);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("TestResults", new { attemptId });
                }
            }

            ViewBag.TimeRemaining = attempt.Test.TimeLimitMinutes > 0
                ? attempt.Test.TimeLimitMinutes - (DateTime.Now - attempt.StartTime).TotalMinutes
                : (double?)null;

            return View(attempt);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitTest(int attemptId, Dictionary<int, string> answers, Dictionary<int, List<int>> selectedOptions)
        {
            var attempt = await _context.TestAttempts
                .Include(a => a.Test)
                .ThenInclude(t => t.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == attemptId);
            if (attempt == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (attempt.StudentId != user.Id) return Forbid();

            if (attempt.EndTime != null)
            {
                return RedirectToAction("TestResults", new { attemptId });
            }

            // Сохранение ответов
            foreach (var question in attempt.Test.Questions)
            {
                var studentAnswer = new StudentAnswer
                {
                    QuestionId = question.Id,
                    TestAttemptId = attempt.Id
                };

                if (question.Type == QuestionType.TextAnswer && answers != null && answers.ContainsKey(question.Id))
                {
                    studentAnswer.AnswerText = answers[question.Id];
                }
                else if ((question.Type == QuestionType.SingleChoice || question.Type == QuestionType.MultipleChoice) &&
                         selectedOptions != null && selectedOptions.ContainsKey(question.Id))
                {
                    foreach (var optionId in selectedOptions[question.Id])
                    {
                        studentAnswer.SelectedOptions.Add(new SelectedOption
                        {
                            AnswerOptionId = optionId
                        });
                    }
                }

                _context.StudentAnswers.Add(studentAnswer);
            }

            attempt.EndTime = DateTime.Now;
            await _context.SaveChangesAsync();

            // Посчитаем баллы
            await CalculateTestScore(attempt.Id);

            return RedirectToAction("TestResults", new { attemptId });
        }

        private async Task CalculateTestScore(int attemptId)
        {
            var attempt = await _context.TestAttempts
                .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
                .ThenInclude(q => q.AnswerOptions)
                .Include(a => a.Answers)
                .ThenInclude(a => a.SelectedOptions)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null) return;

            int totalScore = 0;

            foreach (var answer in attempt.Answers)
            {
                if (answer.Question.Type == QuestionType.TextAnswer)
                {
                    // Пропускаем текстовые ответы, они требуют ручной оценки
                    continue;
                }
                else if (answer.Question.Type == QuestionType.SingleChoice)
                {
                    var selectedOption = answer.SelectedOptions.FirstOrDefault();
                    if (selectedOption != null && selectedOption.AnswerOption.IsCorrect)
                    {
                        totalScore += answer.Question.Points;
                    }
                }
                else if (answer.Question.Type == QuestionType.MultipleChoice)
                {
                    var correctOptions = answer.Question.AnswerOptions.Where(o => o.IsCorrect).ToList();
                    var selectedCorrect = answer.SelectedOptions.Count(o => o.AnswerOption.IsCorrect);
                    var selectedIncorrect = answer.SelectedOptions.Count(o => !o.AnswerOption.IsCorrect);

                    if (selectedIncorrect == 0 && selectedCorrect == correctOptions.Count)
                    {
                        totalScore += answer.Question.Points;
                    }
                    else if (selectedIncorrect == 0 && selectedCorrect > 0)
                    {
                        totalScore += (int)(answer.Question.Points * ((double)selectedCorrect / correctOptions.Count));
                    }
                }
            }

            attempt.Score = totalScore;
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> TestResults(int attemptId)
        {
            var attempt = await _context.TestAttempts
                .Include(a => a.Test)
                .ThenInclude(t => t.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .Include(a => a.Answers)
                .ThenInclude(a => a.SelectedOptions)
                .ThenInclude(so => so.AnswerOption)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == attemptId);
            if (attempt == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (attempt.StudentId != user.Id && !user.IsTeacher) return Forbid();

            return View(attempt);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GradeTextAnswer(int answerId)
        {
            var answer = await _context.StudentAnswers
                .Include(a => a.Question)
                .ThenInclude(q => q.Test)
                .ThenInclude(t => t.Course)
                .Include(a => a.TestAttempt)
                .FirstOrDefaultAsync(a => a.Id == answerId);
            if (answer == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (answer.Question.Test.Course.TeacherId != user.Id) return Forbid();

            return View(answer);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GradeTextAnswer(int answerId, int pointsAwarded)
        {
            var answer = await _context.StudentAnswers
                .Include(a => a.Question)
                .Include(a => a.TestAttempt)
                .FirstOrDefaultAsync(a => a.Id == answerId);
            if (answer == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (answer.Question.Test.Course.TeacherId != user.Id) return Forbid();

            if (pointsAwarded < 0 || pointsAwarded > answer.Question.Points)
            {
                ModelState.AddModelError("", "Invalid points awarded");
                return View(answer);
            }

            answer.PointsAwarded = pointsAwarded;
            await _context.SaveChangesAsync();

            // Пересчёт итогового балла
            var attempt = answer.TestAttempt;
            attempt.Score = await _context.StudentAnswers
                .Where(a => a.TestAttemptId == attempt.Id)
                .SumAsync(a => a.PointsAwarded ?? 0);
            await _context.SaveChangesAsync();

            return RedirectToAction("TestResults", new { attemptId = answer.TestAttemptId });
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ViewAttempts(int testId)
        {
            var test = await _context.Tests
                .Include(t => t.Course)
                .Include(t => t.Attempts)
                .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(t => t.Id == testId);
            if (test == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (test.Course.TeacherId != user.Id) return Forbid();

            return View(test);
        }
    }
}
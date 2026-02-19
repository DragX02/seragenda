using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;
using System.Security.Claims;

namespace seragenda.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly AgendaContext _context;

        public CoursesController(AgendaContext context)
        {
            _context = context;
        }

        private async Task<int?> GetUserId()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        [HttpGet("date/{date}")]
        public async Task<IActionResult> GetCoursesForDate(DateTime date)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var dayOfWeek = date.DayOfWeek;
            int dayFlag = dayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 4,
                DayOfWeek.Thursday => 8,
                DayOfWeek.Friday => 16,
                DayOfWeek.Saturday => 32,
                DayOfWeek.Sunday => 64,
                _ => 0
            };

            var courses = await _context.UserCourses
                .Where(c => c.IdUserFk == userId && c.StartDate <= date && c.EndDate >= date)
                .ToListAsync();

            // Filter by day of week using binary flags
            var filtered = courses.Where(c => (c.DaysOfWeek & dayFlag) != 0).ToList();

            return Ok(filtered);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var courses = await _context.UserCourses
                .Where(c => c.IdUserFk == userId)
                .ToListAsync();

            return Ok(courses);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] UserCourse course)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            course.IdUserFk = userId.Value;

            if (course.Id == 0)
            {
                _context.UserCourses.Add(course);
            }
            else
            {
                var existing = await _context.UserCourses
                    .FirstOrDefaultAsync(c => c.Id == course.Id && c.IdUserFk == userId);
                if (existing == null) return NotFound();

                existing.Name = course.Name;
                existing.Color = course.Color;
                existing.StartDate = course.StartDate;
                existing.EndDate = course.EndDate;
                existing.StartTime = course.StartTime;
                existing.EndTime = course.EndTime;
                existing.DaysOfWeek = course.DaysOfWeek;
            }

            await _context.SaveChangesAsync();
            return Ok(course);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var course = await _context.UserCourses
                .FirstOrDefaultAsync(c => c.Id == id && c.IdUserFk == userId);
            if (course == null) return NotFound();

            _context.UserCourses.Remove(course);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}

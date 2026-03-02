// Import ASP.NET Core authorization attributes
using Microsoft.AspNetCore.Authorization;
// Import base MVC/API controller types and result helpers
using Microsoft.AspNetCore.Mvc;
// Import Entity Framework Core for async database operations
using Microsoft.EntityFrameworkCore;
// Import project models (UserCourse, Utilisateur, etc.)
using seragenda.Models;
// Import Claims support for extracting the user's email from the JWT
using System.Security.Claims;

namespace seragenda.Controllers
{
    // All routes are prefixed with /api/courses
    [Route("api/[controller]")]
    // Marks this class as an API controller
    [ApiController]
    // All endpoints require a valid JWT token — anonymous access is denied
    [Authorize]
    /// <summary>
    /// Manages recurring course schedule entries for the authenticated user.
    /// A "course" here represents a repeating class block with a time slot, days of week,
    /// a date range (semester/year), a name, and a display colour.
    /// The "days of week" are encoded as a bitmask (Monday=1, Tuesday=2, Wednesday=4, ...).
    /// </summary>
    public class CoursesController : ControllerBase
    {
        // Entity Framework database context for reading and writing UserCourse records
        private readonly AgendaContext _context;

        /// <summary>
        /// Constructor — receives the database context via dependency injection.
        /// </summary>
        /// <param name="context">The EF Core database context</param>
        public CoursesController(AgendaContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolves the integer primary key of the currently authenticated user
        /// by looking up their email address (stored as the JWT Name claim) in the database.
        /// Returns null if the claim is missing or the user does not exist.
        /// </summary>
        /// <returns>The user's IdUser, or null if not found</returns>
        private async Task<int?> GetUserId()
        {
            // The Name claim was set to the user's email at login time
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            // If the claim is absent, we cannot identify the user
            if (email == null) return null;
            // Find the user record that matches the email
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            // Return only the integer PK, or null if no match
            return user?.IdUser;
        }

        // GET /api/courses/date/{date}
        // Returns all courses scheduled on a specific calendar date for the current user
        [HttpGet("date/{date}")]
        /// <summary>
        /// Retrieves all course entries that occur on a given date.
        /// A course occurs on a date if:
        /// 1. The date falls within the course's StartDate–EndDate range, AND
        /// 2. The day of the week matches one of the bits set in the DaysOfWeek bitmask.
        /// </summary>
        /// <param name="date">The target date (parsed from the route segment)</param>
        public async Task<IActionResult> GetCoursesForDate(DateTime date)
        {
            // Identify the requesting user
            var userId = await GetUserId();
            // Return 401 if the user identity cannot be resolved
            if (userId == null) return Unauthorized();

            // Determine which day-of-week bit corresponds to the requested date
            var dayOfWeek = date.DayOfWeek;

            // Map each day of the week to its bitmask flag value
            // These values match the convention used when courses are saved
            int dayFlag = dayOfWeek switch
            {
                DayOfWeek.Monday    => 1,   // bit 0
                DayOfWeek.Tuesday   => 2,   // bit 1
                DayOfWeek.Wednesday => 4,   // bit 2
                DayOfWeek.Thursday  => 8,   // bit 3
                DayOfWeek.Friday    => 16,  // bit 4
                DayOfWeek.Saturday  => 32,  // bit 5
                DayOfWeek.Sunday    => 64,  // bit 6
                _                   => 0   // Should never happen (all enum values are covered)
            };

            // Fetch all courses for this user that are active on the requested date
            // (i.e., the date falls inside the course's semester date range)
            var courses = await _context.UserCourses
                .Where(c => c.IdUserFk == userId && c.StartDate <= date && c.EndDate >= date)
                .ToListAsync();

            // Apply the day-of-week bitmask filter in memory
            // (bitwise AND is not easily translated to SQL in all providers, so we filter after fetch)
            var filtered = courses.Where(c => (c.DaysOfWeek & dayFlag) != 0).ToList();

            return Ok(filtered);
        }

        // GET /api/courses
        // Returns every course entry created by the current user (for calendar setup/management)
        [HttpGet]
        /// <summary>
        /// Retrieves all course schedule entries belonging to the current user.
        /// Used by the settings/management view to list and edit recurring courses.
        /// </summary>
        public async Task<IActionResult> GetAll()
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Return all courses that belong to this user, in database order
            var courses = await _context.UserCourses
                .Where(c => c.IdUserFk == userId)
                .ToListAsync();

            return Ok(courses);
        }

        // POST /api/courses
        // Creates a new course entry or updates an existing one (upsert pattern based on Id == 0)
        [HttpPost]
        /// <summary>
        /// Creates a new course entry if the submitted Id is 0,
        /// or updates an existing entry if a non-zero Id is provided.
        /// The IdUserFk is always overwritten with the current user's ID to prevent
        /// a user from modifying another user's courses.
        /// </summary>
        /// <param name="course">The course data to save</param>
        public async Task<IActionResult> Save([FromBody] UserCourse course)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Force the owner to be the currently authenticated user regardless of what the client sent
            course.IdUserFk = userId.Value;

            if (course.Id == 0)
            {
                // Id == 0 means this is a new record — add it to the context
                _context.UserCourses.Add(course);
            }
            else
            {
                // Non-zero Id — find the existing record and verify it belongs to this user
                var existing = await _context.UserCourses
                    .FirstOrDefaultAsync(c => c.Id == course.Id && c.IdUserFk == userId);
                // Return 404 if the record does not exist or belongs to someone else
                if (existing == null) return NotFound();

                // Update only the mutable fields; the Id and IdUserFk are intentionally not changed
                existing.Name       = course.Name;
                existing.Color      = course.Color;
                existing.StartDate  = course.StartDate;
                existing.EndDate    = course.EndDate;
                existing.StartTime  = course.StartTime;
                existing.EndTime    = course.EndTime;
                existing.DaysOfWeek = course.DaysOfWeek;
            }

            // Persist the insert or update to the database
            await _context.SaveChangesAsync();
            // Return the saved course (with its new Id if it was a creation)
            return Ok(course);
        }

        // DELETE /api/courses/{id}
        // Permanently removes a course entry owned by the current user
        [HttpDelete("{id}")]
        /// <summary>
        /// Deletes a course entry by its ID.
        /// Verifies that the entry belongs to the requesting user before deletion.
        /// </summary>
        /// <param name="id">The primary key of the course to delete</param>
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            // Find the course that matches both the given ID and the current user's ID
            // This prevents a user from deleting another user's course by guessing an ID
            var course = await _context.UserCourses
                .FirstOrDefaultAsync(c => c.Id == id && c.IdUserFk == userId);
            if (course == null) return NotFound();

            // Remove the entity and persist the deletion
            _context.UserCourses.Remove(course);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}

// Controllers/ParticipantController.cs
using Flare.Data;
using Flare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flare.Controllers
{
    [Authorize(Roles = "participant,admin")]
    public class ParticipantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParticipantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. List of upcoming events
        public async Task<IActionResult> UpcomingEvents()
        {
            var userId = _userManager.GetUserId(User);
            var now = DateTime.UtcNow.Date;
            var events = await _context.Events
                .Where(e => e.Status == "Approved" && e.Date >= now)
                .OrderBy(e => e.Date)
                .ToListAsync();

            var enrollments = await _context.EventEnrollments
                .Where(x => x.ParticipantId == userId)
                .ToListAsync();

            ViewBag.Enrollments = enrollments;
            return View(events);
        }

        // 2. Request to join event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestEnrollment(int eventId)
        {
            var userId = _userManager.GetUserId(User);

            // Check if already requested or enrolled
            var exists = await _context.EventEnrollments
                .AnyAsync(x => x.EventId == eventId && x.ParticipantId == userId && x.Status != "Rejected");
            if (exists)
            {
                TempData["Message"] = "You have already requested or joined this event.";
                return RedirectToAction(nameof(UpcomingEvents));
            }

            // Check capacity
            var @event = await _context.Events.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == eventId);
            var approvedCount = await _context.EventEnrollments.CountAsync(x => x.EventId == eventId && x.Status == "Approved");
            if (@event == null || approvedCount >= @event.Capacity)
            {
                TempData["Message"] = "No seats available.";
                return RedirectToAction(nameof(UpcomingEvents));
            }

            var enrollment = new EventEnrollment
            {
                EventId = eventId,
                ParticipantId = userId,
                Status = "Pending"
            };
            _context.EventEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Enrollment request sent.";
            return RedirectToAction(nameof(UpcomingEvents));
        }

        // 4. List of enrolled events
        public async Task<IActionResult> MyEvents()
        {
            var userId = _userManager.GetUserId(User);
            var enrollments = await _context.EventEnrollments
                .Include(e => e.Event)
                .Where(e => e.ParticipantId == userId && e.Status == "Approved")
                .OrderByDescending(e => e.RequestedAt)
                .ToListAsync();

            return View(enrollments);
        }
    }
}

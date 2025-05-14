using Flare.Data;
using Flare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flare.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Pending Event Requests
        public async Task<IActionResult> PendingEvents()
        {
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.Status == "Pending")
                .ToListAsync();
            return View(events);
        }

        // Approve Event
        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                @event.Status = "Approved";
                _context.Update(@event);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(PendingEvents));
        }

        // Reject Event
        [HttpPost]
        public async Task<IActionResult> RejectEvent(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                @event.Status = "Rejected";
                _context.Update(@event);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(PendingEvents));
        }

        // Organizer List
        public async Task<IActionResult> Organizers()
        {
            var organizers = await _userManager.GetUsersInRoleAsync("organizer");
            return View(organizers);
        }

        // Participant List
        public async Task<IActionResult> Participants()
        {
            var participants = await _userManager.GetUsersInRoleAsync("participant");
            return View(participants);
        }

        // Approved Event List
        public async Task<IActionResult> ApprovedEvents()
        {
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.Status == "Approved")
                .ToListAsync();
            return View(events);
        }

        public async Task<IActionResult> EventsWithParticipants()
        {
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Participants)
                .Include(e => e.Participants) // If using direct navigation
                .ToListAsync();

            var enrollments = await _context.EventEnrollments
                .Include(e => e.Participant)
                .ToListAsync();

            ViewBag.Enrollments = enrollments;
            return View(events);
        }
    }
}

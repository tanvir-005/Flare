using Flare.Data;
using Flare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flare.Controllers
{
    [Authorize(Roles = "organizer,admin")]
    public class OrganizerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrganizerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Organizer/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var events = await _context.Events
                .Where(e => e.OrganizerId == userId)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.Date,
                    e.Time,
                    e.Capacity,
                    e.Venue,
                    e.Status,
                    ParticipantCount = e.Participants.Count
                })
                .ToListAsync();

            var eventViewModels = events.Select(e => new EventViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Date = e.Date,
                Time = e.Time,
                Capacity = e.Capacity,
                Venue = e.Venue,
                Status = e.Status,
                ParticipantCount = e.ParticipantCount
            }).ToList();

            return View(eventViewModels);
        }


        // GET: Organizer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Organizer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var @event = new Event
                {
                    Name = model.Name,
                    Description = model.Description,
                    Date = model.Date,
                    Time = model.Time,
                    Capacity = model.Capacity,
                    Venue = model.Venue,
                    OrganizerId = _userManager.GetUserId(User),
                    Status = "Pending"
                };
                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Organizer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null || @event.OrganizerId != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            var model = new EventEditViewModel
            {
                Id = @event.Id,
                Name = @event.Name,
                Description = @event.Description,
                Date = @event.Date,
                Time = @event.Time,
                Capacity = @event.Capacity,
                Venue = @event.Venue
            };

            return View(model);
        }

        // POST: Organizer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var @event = await _context.Events.FindAsync(id);
                if (@event == null || @event.OrganizerId != _userManager.GetUserId(User))
                {
                    return Unauthorized();
                }

                @event.Name = model.Name;
                @event.Description = model.Description;
                @event.Date = model.Date;
                @event.Time = model.Time;
                @event.Capacity = model.Capacity;
                @event.Venue = model.Venue;

                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Organizer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null || @event.OrganizerId != _userManager.GetUserId(User))
            {
                return Unauthorized();
            }

            return View(@event);
        }

        // POST: Organizer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        // View participant requests for an event
        public async Task<IActionResult> EnrollmentRequests(int eventId)
        {
            var userId = _userManager.GetUserId(User);
            var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId);
            if (@event == null) return Unauthorized();

            var requests = await _context.EventEnrollments
                .Include(e => e.Participant)
                .Where(e => e.EventId == eventId)
                .OrderBy(e => e.Status)
                .ThenByDescending(e => e.RequestedAt)
                .ToListAsync();

            ViewBag.Event = @event;
            return View(requests);
        }

        // Approve participant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEnrollment(int id)
        {
            var enrollment = await _context.EventEnrollments.Include(e => e.Event).FirstOrDefaultAsync(e => e.Id == id);
            if (enrollment == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (enrollment.Event.OrganizerId != userId) return Unauthorized();

            // Check capacity
            var approvedCount = await _context.EventEnrollments.CountAsync(x => x.EventId == enrollment.EventId && x.Status == "Approved");
            if (approvedCount >= enrollment.Event.Capacity)
            {
                TempData["Message"] = "Event is full.";
                return RedirectToAction("EnrollmentRequests", new { eventId = enrollment.EventId });
            }

            enrollment.Status = "Approved";

            // Add participant to event's Participants collection if not already present
            var @event = enrollment.Event;
            if (@event.Participants == null)
                @event.Participants = new List<ApplicationUser>();

            if (!@event.Participants.Any(p => p.Id == enrollment.ParticipantId))
            {
                var participant = await _context.Users.FindAsync(enrollment.ParticipantId);
                if (participant != null)
                    @event.Participants.Add(participant);
            }

            _context.Update(enrollment);
            _context.Update(@event);
            await _context.SaveChangesAsync();

            return RedirectToAction("EnrollmentRequests", new { eventId = enrollment.EventId });
        }

        // Reject participant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectEnrollment(int id)
        {
            var enrollment = await _context.EventEnrollments.Include(e => e.Event).FirstOrDefaultAsync(e => e.Id == id);
            if (enrollment == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (enrollment.Event.OrganizerId != userId) return Unauthorized();

            enrollment.Status = "Rejected";
            _context.Update(enrollment);
            await _context.SaveChangesAsync();
            return RedirectToAction("EnrollmentRequests", new { eventId = enrollment.EventId });
        }
    }
}

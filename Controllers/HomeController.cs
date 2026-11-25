using CSS.Data;
using CSS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 5;

            // ===================== NOTICE PAGINATION =====================
            var totalNotices = await _db.Notices.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalNotices / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var notices = await _db.Notices
                .OrderByDescending(n => n.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            // ===================== UPCOMING EVENTS =====================
            var upcomingEvents = await _db.Events
                .Where(e => e.StartDateTime != null
                         && e.StartDateTime > DateTime.Now
                         && e.IsPublished)
                .OrderBy(e => e.StartDateTime)
                .Take(4)
                .AsNoTracking()
                .ToListAsync();

            // ===================== LATEST / SORTED VIDEOS =====================
            var videos = await _db.Videos
                .OrderBy(v => v.SortOrder)
                .ThenByDescending(v => v.CreatedAt)
                .Take(6)
                .AsNoTracking()
                .ToListAsync();

            // ===================== LIVE STATS =====================
            ViewBag.TotalMembers = await _db.Users.CountAsync();
            ViewBag.EligibleDonors = await _db.Users.CountAsync(u =>
                u.BloodGroup != null && u.DonationEligibility == "Eligible");
            ViewBag.TotalEvents = await _db.Events.CountAsync(e => e.IsPublished);
            ViewBag.TotalNotices = totalNotices;

            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.VideoList = videos;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(notices);
        }

        public IActionResult Privacy() => View();

        public IActionResult Error()
        {
            return View(new ErrorViewModel());
        }
    }
}

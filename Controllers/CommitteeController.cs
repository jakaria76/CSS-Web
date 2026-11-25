using CSS.Data;
using CSS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSS.Controllers
{
    public class CommitteeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommitteeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? category, string? search)
        {
            var topLevel = new List<string>
            {
                "সভাপতি", "সহ-সভাপতি", "সাধারণ সম্পাদক", "যুগ্ম-সাধারণ সম্পাদক"
            };

            var executive = new List<string>
            {
                "সাংগঠনিক সম্পাদক", "সহ-সাংগঠনিক সম্পাদক",
                "দপ্তর সম্পাদক", "সিনিয়র সহ-দপ্তর সম্পাদক", "সহ-দপ্তর সম্পাদক",
                "অর্থ সম্পাদক", "সিনিয়র অর্থ সম্পাদক", "সহ-অর্থ সম্পাদক",
                "শিক্ষা সম্পাদক", "সহ-শিক্ষা সম্পাদক",
                "পরিকল্পনা সম্পাদক", "সহ-পরিকল্পনা সম্পাদক",
                "মানব সম্পদ সম্পাদক", "সহ-মানব সম্পদ সম্পাদক",
                "পরিবেশ সম্পাদক", "সহ-পরিবেশ সম্পাদক",
                "ধর্ম সম্পাদক", "সহ-ধর্ম সম্পাদক",
                "প্রচার সম্পাদক", "সহ-প্রচার সম্পাদক",
                "ব্র্যান্ড ও গণমাধ্যম সম্পাদক", "সিনিয়র ব্র্যান্ড ও গণমাধ্যম সম্পাদক",
                "গ্রাফিক্স ডিজাইনার", "সহ-গ্রাফিক্স ডিজাইনার",
                "ক্রিয়া সম্পাদক", "সহ-ক্রিয়া সম্পাদক",
                "পাঠাগার সম্পাদক", "সহ-পাঠাগার সম্পাদক",
                "সাংস্কৃতিক সম্পাদক", "সহ-সাংস্কৃতিক সম্পাদক",
                "বিজ্ঞান ও প্রযুক্তি সম্পাদক", "সহ-বিজ্ঞান ও প্রযুক্তি সম্পাদক",
                "সমাজ কল্যাণ সম্পাদক", "সহ-সমাজ কল্যাণ সম্পাদক",
                "স্বাস্থ্য সম্পাদক", "সহ-স্বাস্থ্য সম্পাদক",
                "নারী সম্পাদক", "সহ-নারী সম্পাদক",
                "আন্তর্জাতিক সম্পাদক", "সহ-আন্তর্জাতিক সম্পাদক",
                "ছাত্র কল্যাণ সম্পাদক", "সহ-ছাত্র কল্যাণ সম্পাদক",
                "সাহিত্য সম্পাদক", "সহ-সাহিত্য সম্পাদক",
                "তথ্য ও গবেষণা সম্পাদক", "সহ-তথ্য ও গবেষণা সম্পাদক",
                "ত্রাণ ও দুর্যোগ সম্পাদক", "সিনিয়র ত্রাণ ও দুর্যোগ সম্পাদক",
                "সহ-ত্রাণ ও দুর্যোগ সম্পাদক"
            };

            var generalMembers = new List<string>
            {
                "কার্যকরী সদস্য"
            };

            var allMembers = await _context.Users
                .Where(x => x.MemberType == "Committee")
                .AsNoTracking()
                .ToListAsync();

            ViewBag.TotalCount = allMembers.Count;
            ViewBag.TopCount = allMembers.Count(x => x.CommitteePosition != null && topLevel.Contains(x.CommitteePosition));
            ViewBag.ExecutiveCount = allMembers.Count(x => x.CommitteePosition != null && executive.Contains(x.CommitteePosition));
            ViewBag.GeneralCount = allMembers.Count(x => x.CommitteePosition != null && generalMembers.Contains(x.CommitteePosition));
            ViewBag.SelectedCategory = category ?? "";
            ViewBag.Search = search ?? "";

            var members = allMembers;

            if (!string.IsNullOrWhiteSpace(category))
            {
                if (category == "Top")
                    members = members.Where(x => x.CommitteePosition != null && topLevel.Contains(x.CommitteePosition)).ToList();
                else if (category == "Executive")
                    members = members.Where(x => x.CommitteePosition != null && executive.Contains(x.CommitteePosition)).ToList();
                else if (category == "Members")
                    members = members.Where(x => x.CommitteePosition != null && generalMembers.Contains(x.CommitteePosition)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLowerInvariant();
                members = members.Where(x =>
                    (x.FullName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.FullNameBn ?? "").ToLowerInvariant().Contains(q) ||
                    (x.CommitteePosition ?? "").ToLowerInvariant().Contains(q) ||
                    (x.District ?? "").ToLowerInvariant().Contains(q)
                ).ToList();
            }

            var orderedList = topLevel.Concat(executive).Concat(generalMembers).ToList();

            members = members
                .OrderBy(m =>
                {
                    if (string.IsNullOrEmpty(m.CommitteePosition)) return int.MaxValue;
                    var idx = orderedList.IndexOf(m.CommitteePosition);
                    return idx == -1 ? int.MaxValue : idx;
                })
                .ThenBy(m => m.FullName ?? "")
                .ToList();

            var vm = members.Select(m =>
            {
                string? imgSrc = null;
                if (!string.IsNullOrWhiteSpace(m.ProfileImagePath))
                {
                    imgSrc = m.ProfileImagePath;
                }
                else if (m.ProfileImageData != null && m.ProfileImageData.Length > 0)
                {
                    imgSrc = $"data:image/jpeg;base64,{Convert.ToBase64String(m.ProfileImageData)}";
                }

                return new CommitteeMemberVM
                {
                    Id = m.Id,
                    FullName = string.IsNullOrWhiteSpace(m.FullName) ? "CSS Member" : m.FullName,
                    FullNameBn = m.FullNameBn,
                    CommitteePosition = string.IsNullOrWhiteSpace(m.CommitteePosition) ? "Committee Member" : m.CommitteePosition,
                    ProfileImageSrc = imgSrc,
                    District = m.District,
                    BloodGroup = m.BloodGroup,
                    IsTopLevel = m.CommitteePosition != null && topLevel.Contains(m.CommitteePosition)
                };
            }).ToList();

            return View(vm);
        }
    }
}

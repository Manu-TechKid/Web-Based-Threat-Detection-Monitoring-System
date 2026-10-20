using Microsoft.AspNetCore.Mvc;
using RouterDetector.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using RouterDetector.Data;
using Microsoft.EntityFrameworkCore;

namespace RouterDetector.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RouterDetectorContext _context;

        public HomeController(ILogger<HomeController> logger, RouterDetectorContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Redirect to Dashboard instead of showing a home page
            return RedirectToAction("Dashboard");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard(int? saccoId = null)
        {
            var staffPosition = User.Claims.FirstOrDefault(c => c.Type == "StaffPosition")?.Value;
            var userSaccoIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SaccoId")?.Value;
            int? userSaccoId = null;
            if (int.TryParse(userSaccoIdClaim, out var parsedSaccoId))
                userSaccoId = parsedSaccoId;
            bool isAdmin = staffPosition != null && staffPosition.ToLower() == "admin";

            // Get Saccos for sidebar with AsNoTracking for better performance
            List<SystemConfiguration> saccos;
            if (isAdmin)
                saccos = await _context.SystemConfiguration.AsNoTracking().OrderBy(s => s.County).ThenBy(s => s.InstitutionName).ToListAsync();
            else
                saccos = await _context.SystemConfiguration.AsNoTracking().Where(s => s.Id == userSaccoId).ToListAsync();

            ViewBag.Saccos = saccos;
            ViewBag.SelectedSaccoId = saccoId ?? (isAdmin ? null : userSaccoId);
            ViewBag.IsAdmin = isAdmin;

            // Get user's Sacco information for display with AsNoTracking
            if (!isAdmin && userSaccoId.HasValue)
            {
                var userSacco = await _context.SystemConfiguration.AsNoTracking().FirstOrDefaultAsync(sc => sc.Id == userSaccoId.Value);
                ViewBag.UserSacco = userSacco;
            }

            // Filter events by Sacco with AsNoTracking for better performance
            var query = _context.EventLogs.AsNoTracking().AsQueryable();
            if (isAdmin && saccoId.HasValue)
            {
                var selectedSacco = await _context.SystemConfiguration.AsNoTracking().FirstOrDefaultAsync(sc => sc.Id == saccoId.Value);
                if (selectedSacco != null)
                    query = query.Where(e => e.Institution == selectedSacco.InstitutionName);
            }
            else if (!isAdmin && userSaccoId.HasValue)
            {
                var userSacco = await _context.SystemConfiguration.AsNoTracking().FirstOrDefaultAsync(sc => sc.Id == userSaccoId.Value);
                if (userSacco != null)
                    query = query.Where(e => e.Institution == userSacco.InstitutionName);
            }

            // Only load last 30 days of events for better performance
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            query = query.Where(e => e.Timestamp >= thirtyDaysAgo);

            var allEvents = await query.ToListAsync();

            // Basic threat categorization
            var malwareEvents = allEvents.Where(e => e.EventType != null && (
                    e.EventType.Contains("Malware", StringComparison.OrdinalIgnoreCase) ||
                    e.EventType.Contains("Virus", StringComparison.OrdinalIgnoreCase) ||
                    e.EventType.Contains("Worm", StringComparison.OrdinalIgnoreCase) ||
                    e.EventType.Contains("Trojan", StringComparison.OrdinalIgnoreCase) ||
                    e.EventType.Contains("Intrusion", StringComparison.OrdinalIgnoreCase) ||
                e.EventType.Contains("MITM", StringComparison.OrdinalIgnoreCase))).ToList();

            var phishingEvents = allEvents.Where(e => e.EventType != null && 
                e.EventType.Contains("Phishing", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var suspiciousEmailEvents = allEvents.Where(e => e.EventType != null && 
                e.EventType.Contains("Suspicious Email", StringComparison.OrdinalIgnoreCase)).ToList();

            var bruteForceEvents = allEvents.Where(e => e.EventType != null && 
                e.EventType.Contains("Login Attempt", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var ddosEvents = allEvents.Where(e => e.EventType != null && 
                e.EventType.Contains("Policy Violation", StringComparison.OrdinalIgnoreCase) && 
                e.Message != null && e.Message.Contains("DDoS", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var portScanEvents = allEvents.Where(e => e.EventType != null && 
                e.EventType.Contains("Port Scan", StringComparison.OrdinalIgnoreCase)).ToList();
            
            var webAppAttackEvents = allEvents.Where(e => e.EventType != null && 
                e.EventType.Contains("Policy Violation", StringComparison.OrdinalIgnoreCase) && 
                e.Message != null && e.Message.Contains("Web application attack", StringComparison.OrdinalIgnoreCase)).ToList();

            // Analytics calculations
          
            
            var eventsBySeverity = allEvents.GroupBy(e => string.IsNullOrEmpty(e.Severity) ? "Unknown" : e.Severity)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var eventsByRuleType = allEvents.GroupBy(e => string.IsNullOrEmpty(e.RuleType) ? "Unknown" : e.RuleType)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var eventsByActionTaken = allEvents.GroupBy(e => string.IsNullOrEmpty(e.ActionTaken) ? "Unknown" : e.ActionTaken)
                .ToDictionary(g => g.Key, g => g.Count());

            // Time-based analytics
            var eventsByMonth = allEvents.GroupBy(e => e.Timestamp.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var eventsByYear = allEvents.GroupBy(e => e.Timestamp.Year.ToString())
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var eventsByDay = allEvents.GroupBy(e => e.Timestamp.ToString("yyyy-MM-dd"))
                .OrderByDescending(g => g.Key)
                .Take(30)
                .ToDictionary(g => g.Key, g => g.Count());
            
           

            // Ensure all 24 hours are present in EventsByHour
            var allHours = Enumerable.Range(0, 24).Select(h => h.ToString("D2")).ToList();
            var eventsByHourRaw = allEvents.GroupBy(e => e.Timestamp.Hour.ToString("D2"))
                .ToDictionary(g => g.Key, g => g.Count());
            var eventsByHour = new Dictionary<string, int>();
            foreach (var hour in allHours)
            {
                eventsByHour[hour] = eventsByHourRaw.ContainsKey(hour) ? eventsByHourRaw[hour] : 0;
            }

            // Ensure all event types present in the database are included in EventsByType
            var allEventTypes = await _context.EventLogs.AsNoTracking()
                .Where(e => e.Timestamp >= thirtyDaysAgo)
                .Select(e => e.EventType)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToListAsync();
            var eventsByTypeRaw = allEvents.GroupBy(e => e.EventType ?? "Unknown").ToDictionary(g => g.Key, g => g.Count());
            var eventsByType = new Dictionary<string, int>();
            foreach (var eventType in allEventTypes)
            {
                eventsByType[eventType] = eventsByTypeRaw.ContainsKey(eventType) ? eventsByTypeRaw[eventType] : 0;
            }

            // Bar chart: Event type counts per month
            var eventTypes = allEvents.Select(e => e.EventType).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList();
            var months = allEvents.GroupBy(e => e.Timestamp.ToString("yyyy-MM")).OrderBy(g => g.Key).Select(g => g.Key).ToList();
            var eventsByTypePerMonth = new Dictionary<string, List<int>>();
            foreach (var eventType in eventTypes)
            {
                var counts = new List<int>();
                foreach (var month in months)
                {
                    counts.Add(allEvents.Count(e => (e.EventType == eventType) && e.Timestamp.ToString("yyyy-MM") == month));
                }
                eventsByTypePerMonth[eventType] = counts;
            }

            // Source/Destination analytics
            var topSourceIPs = allEvents.Where(e => !string.IsNullOrEmpty(e.SrcIp))
                .GroupBy(e => e.SrcIp)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key!, g => g.Count());
            var topDestinationIPs = allEvents.Where(e => !string.IsNullOrEmpty(e.DstIp))
                .GroupBy(e => e.DstIp)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key!, g => g.Count());
            var topPorts = allEvents.Where(e => e.DstPort.HasValue)
                .GroupBy(e => e.DstPort.ToString())
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            // Attack type counts
            var attackTypeCounts = new Dictionary<string, int>
            {
                { "Port Scan", portScanEvents.Count },
                { "Brute Force", bruteForceEvents.Count },
                { "DDoS", ddosEvents.Count },
                { "Phishing", phishingEvents.Count },
                { "Web App Attack", webAppAttackEvents.Count },
                { "Malware", malwareEvents.Count },
                { "Suspicious Email", suspiciousEmailEvents.Count }
            };

            // Monthly trends
            var monthlyTrends = allEvents.GroupBy(e => e.Timestamp.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .Select(g => new MonthlyTrend
                {
                    Month = g.Key,
                    TotalEvents = g.Count(),
                    MalwareEvents = g.Count(e => e.EventType != null && e.EventType.Contains("Malware", StringComparison.OrdinalIgnoreCase)),
                    PhishingEvents = g.Count(e => e.EventType != null && e.EventType.Contains("Phishing", StringComparison.OrdinalIgnoreCase)),
                    BruteForceEvents = g.Count(e => e.EventType != null && e.EventType.Contains("Login Attempt", StringComparison.OrdinalIgnoreCase)),
                    DdosEvents = g.Count(e => e.EventType != null && e.EventType.Contains("Policy Violation", StringComparison.OrdinalIgnoreCase) && e.Message != null && e.Message.Contains("DDoS", StringComparison.OrdinalIgnoreCase)),
                    PortScanEvents = g.Count(e => e.EventType != null && e.EventType.Contains("Port Scan", StringComparison.OrdinalIgnoreCase)),
                    WebAppAttackEvents = g.Count(e => e.EventType != null && e.EventType.Contains("Policy Violation", StringComparison.OrdinalIgnoreCase) && e.Message != null && e.Message.Contains("Web application attack", StringComparison.OrdinalIgnoreCase))
                }).ToList();

            var viewModel = new DashboardViewModel
            {
                // General Stats
                TotalEventLogs = allEvents.Count,
                TotalThreats = allEvents.Count(e => e.Severity == "High" || e.Severity == "Critical"),
                TotalBlocked = allEvents.Count(e => e.ActionTaken == "Blocked"),
                TotalQuarantined = allEvents.Count(e => e.ActionTaken == "Quarantined"),

                // Analytics
                RecentEvents = allEvents.OrderByDescending(e => e.Timestamp).Take(10).ToList(),
                EventsByType = eventsByType,
                EventsBySeverity = eventsBySeverity,
                EventsByRuleType = eventsByRuleType,
                EventsByActionTaken = eventsByActionTaken,

                // Time-based Analytics
                EventsByMonth = eventsByMonth,
                EventsByYear = eventsByYear,
                EventsByDay = eventsByDay,
                EventsByHour = eventsByHour,

                // Attack Analytics
                AttackTypeCounts = attackTypeCounts,
                TopSourceIPs = topSourceIPs,
                TopDestinationIPs = topDestinationIPs,
                TopPorts = topPorts,

                // Specific Threat Lists (for cards)
                MalwareEvents = malwareEvents,
                PhishingEvents = phishingEvents,
                SuspiciousEmailEvents = suspiciousEmailEvents,
                BruteForceDetections = bruteForceEvents,
                DdosDetections = ddosEvents,
                PortScanDetections = portScanEvents,
                WebAppAttackDetections = webAppAttackEvents,

                // Trends
                MonthlyTrends = monthlyTrends,
                EventsByTypePerMonth = eventsByTypePerMonth,
                Months = months,

                // Filter Options
                StartDate = null, // No start date filter in this view
                EndDate = null,   // No end date filter in this view
                SelectedEventType = null,
                SelectedSeverity = null,
                SelectedRuleType = null,
                SelectedActionTaken = null,
                SelectedSaccoId = saccoId ?? (isAdmin ? null : userSaccoId),
                Saccos = saccos
            };

            ViewBag.EventTypes = await _context.EventTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync();
            ViewBag.RuleTypes = await _context.RuleTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync();
            ViewBag.Severities = await _context.Severities.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync();
            ViewBag.ActionTakens = await _context.ActionTakens.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync();
            ViewBag.Messages = await _context.MessageTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync();
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var allEvents = await _context.EventLogs.AsNoTracking()
                .Where(e => e.Timestamp >= thirtyDaysAgo)
                .ToListAsync();

            var analyticsData = new
            {
                eventsByType = allEvents.GroupBy(e => e.EventType ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                eventsBySeverity = allEvents.GroupBy(e => e.Severity ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                eventsByMonth = allEvents.GroupBy(e => e.Timestamp.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count()),
                attackTypeCounts = new Dictionary<string, int>
                {
                    { "Port Scan", allEvents.Count(e => e.EventType != null && e.EventType.Contains("Port Scan", StringComparison.OrdinalIgnoreCase)) },
                    { "Brute Force", allEvents.Count(e => e.EventType != null && e.EventType.Contains("Login Attempt", StringComparison.OrdinalIgnoreCase)) },
                    { "DDoS", allEvents.Count(e => e.EventType != null && e.EventType.Contains("Policy Violation", StringComparison.OrdinalIgnoreCase) && e.Message != null && e.Message.Contains("DDoS", StringComparison.OrdinalIgnoreCase)) },
                    { "Phishing", allEvents.Count(e => e.EventType != null && e.EventType.Contains("Phishing", StringComparison.OrdinalIgnoreCase)) },
                    { "Web App Attack", allEvents.Count(e => e.EventType != null && e.EventType.Contains("Policy Violation", StringComparison.OrdinalIgnoreCase) && e.Message != null && e.Message.Contains("Web application attack", StringComparison.OrdinalIgnoreCase)) },
                    { "Malware", allEvents.Count(e => e.EventType != null && e.EventType.Contains("Malware", StringComparison.OrdinalIgnoreCase)) }
                }
            };

            return Json(analyticsData);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RouterDetector.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RouterDetector.Controllers
{
    [Authorize]
    public class AnalysisFilterController : Controller
    {
        private readonly RouterDetectorContext _context;
        public AnalysisFilterController(RouterDetectorContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? ruleType, string? message, string? eventType, string? actionTaken, string? severity)
        {
            var logs = _context.EventLogs.AsNoTracking().AsQueryable();

            // Get user's Sacco information for filtering
            var staffPosition = User.Claims.FirstOrDefault(c => c.Type == "StaffPosition")?.Value;
            var userSaccoIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SaccoId")?.Value;
            int? userSaccoId = null;
            if (int.TryParse(userSaccoIdClaim, out var parsedSaccoId))
                userSaccoId = parsedSaccoId;
            bool isAdmin = staffPosition != null && staffPosition.ToLower() == "admin";

            // Filter by Sacco for non-admin users
            if (!isAdmin && userSaccoId.HasValue)
            {
                var userSacco = await _context.SystemConfiguration.AsNoTracking().FirstOrDefaultAsync(sc => sc.Id == userSaccoId.Value);
                if (userSacco != null)
                {
                    logs = logs.Where(e => e.Institution == userSacco.InstitutionName);
                }
            }

            if (!string.IsNullOrEmpty(ruleType))
                logs = logs.Where(e => e.RuleType == ruleType);
            if (!string.IsNullOrEmpty(message))
                logs = logs.Where(e => e.Message == message);
            if (!string.IsNullOrEmpty(eventType))
                logs = logs.Where(e => e.EventType == eventType);
            if (!string.IsNullOrEmpty(actionTaken))
                logs = logs.Where(e => e.ActionTaken == actionTaken);
            if (!string.IsNullOrEmpty(severity))
                logs = logs.Where(e => e.Severity == severity);

            var allLogs = await logs.ToListAsync();

            var model = new AnalysisFilterViewModel
            {
                RuleTypes = await _context.RuleTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(),
                Messages = await _context.MessageTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(),
                EventTypes = await _context.EventTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(),
                ActionTakens = await _context.ActionTakens.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(),
                Severities = await _context.Severities.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(),
                FilteredLogs = allLogs,
                SelectedRuleType = ruleType,
                SelectedMessage = message,
                SelectedEventType = eventType,
                SelectedActionTaken = actionTaken,
                SelectedSeverity = severity
            };
            return View(model);
        }
    }

    public class AnalysisFilterViewModel
    {
        public List<string?> RuleTypes { get; set; } = new();
        public List<string?> Messages { get; set; } = new();
        public List<string?> EventTypes { get; set; } = new();
        public List<string?> ActionTakens { get; set; } = new();
        public List<string?> Severities { get; set; } = new();
        public List<RouterDetector.Models.EventLog> FilteredLogs { get; set; } = new();
        public string? SelectedRuleType { get; set; }
        public string? SelectedMessage { get; set; }
        public string? SelectedEventType { get; set; }
        public string? SelectedActionTaken { get; set; }
        public string? SelectedSeverity { get; set; }
    }
} 
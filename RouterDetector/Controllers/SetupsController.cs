using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RouterDetector.Data;
using RouterDetector.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace RouterDetector.Controllers
{
    [Authorize]
    public class SetupsController : Controller
    {
        private readonly RouterDetectorContext _context;
        public SetupsController(RouterDetectorContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var staffPosition = User?.Claims?.FirstOrDefault(c => c.Type == "StaffPosition")?.Value;
            return staffPosition != null && staffPosition.ToLower() == "admin";
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!IsAdmin())
            {
                context.Result = Forbid();
            }
            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index()
        {
            var model = new SetupsDropdownsViewModel
            {
                EventTypes = await _context.EventTypes.OrderBy(x => x.Name).ToListAsync(),
                RuleTypes = await _context.RuleTypes.OrderBy(x => x.Name).ToListAsync(),
                Severities = await _context.Severities.OrderBy(x => x.Name).ToListAsync(),
                ActionTakens = await _context.ActionTakens.OrderBy(x => x.Name).ToListAsync(),
                Messages = await _context.MessageTypes.OrderBy(x => x.Name).ToListAsync(),
                Saccos = await _context.SystemConfiguration.OrderBy(x => x.County).ThenBy(x => x.InstitutionName).ToListAsync()
            };
            return View(model);
        }

        // EventType CRUD
        [HttpPost]
        public IActionResult AddEventType(string newEventType)
        {
            if (!string.IsNullOrWhiteSpace(newEventType) && !_context.EventTypes.Any(e => e.Name == newEventType))
            {
                _context.EventTypes.Add(new EventType { Name = newEventType });
                _context.SaveChanges();
                TempData["Success"] = "Event Type added.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult EditEventType(int id, string newEventType)
        {
            var et = _context.EventTypes.Find(id);
            if (et != null && !string.IsNullOrWhiteSpace(newEventType))
            {
                et.Name = newEventType;
                _context.SaveChanges();
                TempData["Success"] = "Event Type updated.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteEventType(int id)
        {
            var et = _context.EventTypes.Find(id);
            if (et != null)
            {
                _context.EventTypes.Remove(et);
                _context.SaveChanges();
                TempData["Success"] = "Event Type deleted.";
            }
            return RedirectToAction("Index");
        }

        // RuleType CRUD
        [HttpPost]
        public IActionResult AddRuleType(string newRuleType)
        {
            if (!string.IsNullOrWhiteSpace(newRuleType) && !_context.RuleTypes.Any(e => e.Name == newRuleType))
            {
                _context.RuleTypes.Add(new RuleType { Name = newRuleType });
                _context.SaveChanges();
                TempData["Success"] = "Rule Type added.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult EditRuleType(int id, string newRuleType)
        {
            var rt = _context.RuleTypes.Find(id);
            if (rt != null && !string.IsNullOrWhiteSpace(newRuleType))
            {
                rt.Name = newRuleType;
                _context.SaveChanges();
                TempData["Success"] = "Rule Type updated.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteRuleType(int id)
        {
            var rt = _context.RuleTypes.Find(id);
            if (rt != null)
            {
                _context.RuleTypes.Remove(rt);
                _context.SaveChanges();
                TempData["Success"] = "Rule Type deleted.";
            }
            return RedirectToAction("Index");
        }

        // Severity CRUD
        [HttpPost]
        public IActionResult AddSeverity(string newSeverity)
        {
            if (!string.IsNullOrWhiteSpace(newSeverity) && !_context.Severities.Any(e => e.Name == newSeverity))
            {
                _context.Severities.Add(new Severity { Name = newSeverity });
                _context.SaveChanges();
                TempData["Success"] = "Severity added.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult EditSeverity(int id, string newSeverity)
        {
            var sev = _context.Severities.Find(id);
            if (sev != null && !string.IsNullOrWhiteSpace(newSeverity))
            {
                sev.Name = newSeverity;
                _context.SaveChanges();
                TempData["Success"] = "Severity updated.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteSeverity(int id)
        {
            var sev = _context.Severities.Find(id);
            if (sev != null)
            {
                _context.Severities.Remove(sev);
                _context.SaveChanges();
                TempData["Success"] = "Severity deleted.";
            }
            return RedirectToAction("Index");
        }

        // ActionTaken CRUD
        [HttpPost]
        public IActionResult AddActionTaken(string newActionTaken)
        {
            if (!string.IsNullOrWhiteSpace(newActionTaken) && !_context.ActionTakens.Any(e => e.Name == newActionTaken))
            {
                _context.ActionTakens.Add(new ActionTaken { Name = newActionTaken });
                _context.SaveChanges();
                TempData["Success"] = "Action Taken added.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult EditActionTaken(int id, string newActionTaken)
        {
            var at = _context.ActionTakens.Find(id);
            if (at != null && !string.IsNullOrWhiteSpace(newActionTaken))
            {
                at.Name = newActionTaken;
                _context.SaveChanges();
                TempData["Success"] = "Action Taken updated.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteActionTaken(int id)
        {
            var at = _context.ActionTakens.Find(id);
            if (at != null)
            {
                _context.ActionTakens.Remove(at);
                _context.SaveChanges();
                TempData["Success"] = "Action Taken deleted.";
            }
            return RedirectToAction("Index");
        }

        // MessageType CRUD
        [HttpPost]
        public IActionResult AddMessage(string newMessage)
        {
            if (!string.IsNullOrWhiteSpace(newMessage) && !_context.MessageTypes.Any(e => e.Name == newMessage))
            {
                _context.MessageTypes.Add(new MessageType { Name = newMessage });
                _context.SaveChanges();
                TempData["Success"] = "Message added.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult EditMessage(int id, string newMessage)
        {
            var msg = _context.MessageTypes.Find(id);
            if (msg != null && !string.IsNullOrWhiteSpace(newMessage))
            {
                msg.Name = newMessage;
                _context.SaveChanges();
                TempData["Success"] = "Message updated.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteMessage(int id)
        {
            var msg = _context.MessageTypes.Find(id);
            if (msg != null)
            {
                _context.MessageTypes.Remove(msg);
                _context.SaveChanges();
                TempData["Success"] = "Message deleted.";
            }
            return RedirectToAction("Index");
        }

        // Sacco CRUD
        [HttpPost]
        public IActionResult AddSacco(string institutionName, string county)
        {
            if (!string.IsNullOrWhiteSpace(institutionName) && !string.IsNullOrWhiteSpace(county) && !_context.SystemConfiguration.Any(s => s.InstitutionName == institutionName))
            {
                _context.SystemConfiguration.Add(new SystemConfiguration { InstitutionName = institutionName, County = county, StaffPosition = "Default" });
                _context.SaveChanges();
                TempData["Success"] = "Sacco added.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult EditSacco(int id, string institutionName, string county)
        {
            var sacco = _context.SystemConfiguration.Find(id);
            if (sacco != null && !string.IsNullOrWhiteSpace(institutionName) && !string.IsNullOrWhiteSpace(county))
            {
                sacco.InstitutionName = institutionName;
                sacco.County = county;
                _context.SaveChanges();
                TempData["Success"] = "Sacco updated.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult DeleteSacco(int id)
        {
            var sacco = _context.SystemConfiguration.Find(id);
            if (sacco != null)
            {
                _context.SystemConfiguration.Remove(sacco);
                _context.SaveChanges();
                TempData["Success"] = "Sacco deleted.";
            }
            return RedirectToAction("Index");
        }
    }

    public class SetupsDropdownsViewModel
    {
        public List<EventType> EventTypes { get; set; } = new();
        public List<RuleType> RuleTypes { get; set; } = new();
        public List<Severity> Severities { get; set; } = new();
        public List<ActionTaken> ActionTakens { get; set; } = new();
        public List<MessageType> Messages { get; set; } = new();
        public List<SystemConfiguration> Saccos { get; set; } = new();
    }
} 
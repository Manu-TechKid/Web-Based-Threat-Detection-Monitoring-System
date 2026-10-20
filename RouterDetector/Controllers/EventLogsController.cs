using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouterDetector.Data;
using RouterDetector.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;

namespace RouterDetector.Controllers
{
    [Authorize]
    public class EventLogsController : Controller
    {
        private readonly RouterDetectorContext _context;

        public EventLogsController(RouterDetectorContext context)
        {
            _context = context;
        }

        // GET: EventLogs
        public async Task<IActionResult> Index(
            string? filterIpAddress,
            string? filterProtocol,
            DateTime? filterStartDate,
            DateTime? filterEndDate,
            int pageNumber = 1,
            int pageSize = 50)
        {
            var query = _context.EventLogs.AsNoTracking().AsQueryable();

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
                    query = query.Where(e => e.Institution == userSacco.InstitutionName);
                }
            }

            // Apply default date filter (last 30 days) if no date filter specified
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            if (!filterStartDate.HasValue && !filterEndDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= thirtyDaysAgo);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(filterIpAddress))
            {
                query = query.Where(l => l.SrcIp != null && l.SrcIp.Contains(filterIpAddress) || l.DstIp != null && l.DstIp.Contains(filterIpAddress));
            }

            if (!string.IsNullOrEmpty(filterProtocol))
            {
                query = query.Where(l => l.Protocol == filterProtocol);
            }

            if (filterStartDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= filterStartDate);
            }

            if (filterEndDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= filterEndDate);
            }

            // Get total count before pagination - use a separate query to avoid issues
            var totalRecords = await query.CountAsync();

            // Apply pagination
            var logs = await query
                .OrderByDescending(n => n.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Use a simple view model for now
            var model = new EventLogsViewModel
            {
                Logs = logs,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                FilterIpAddress = filterIpAddress,
                FilterProtocol = filterProtocol,
                FilterStartDate = filterStartDate,
                FilterEndDate = filterEndDate
            };

            return View(model);
        }

        // GET: EventLogs/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Severities = _context.Severities.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            ViewBag.Messages = _context.MessageTypes.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            ViewBag.RuleTypes = _context.RuleTypes.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            ViewBag.EventTypes = _context.EventTypes.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            ViewBag.ActionTakens = _context.ActionTakens.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            return View();
        }

        // POST: EventLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Timestamp,Institution,DeviceName,DeviceType,LogSource,EventType,Severity,Username,SrcIp,DstIp,SrcPort,DstPort,Protocol,ActionTaken,NatSrcIp,NatDstIp,Hostname,Notes,RuleType,LivePcap,Message,LogOccurrence,EventType2,Severity2,UserAccount,ActionTaken2")] EventLog eventLog)
        {
            if (ModelState.IsValid)
            {
                _context.Add(eventLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(eventLog);
        }

        // GET: EventLogs/Import
        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        // POST: EventLogs/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(Microsoft.AspNetCore.Http.IFormFile file)
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("RouterDetectorUser");
            if (file == null || file.Length == 0)
            {
                TempData["ImportError"] = "Please select a valid Excel file.";
                return RedirectToAction(nameof(Import));
            }

            var logs = new List<EventLog>();
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new OfficeOpenXml.ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["ImportError"] = "No worksheet found in the Excel file.";
                            return RedirectToAction(nameof(Import));
                        }
                        int rowCount = worksheet.Dimension.Rows;
                        int colCount = worksheet.Dimension.Columns;
                        // Assume first row is header
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var log = new EventLog();
                            log.Timestamp = worksheet.Cells[row, 2].GetValue<DateTime>();
                            log.Institution = worksheet.Cells[row, 3].GetValue<string>();
                            log.DeviceName = worksheet.Cells[row, 4].GetValue<string>();
                            log.DeviceType = worksheet.Cells[row, 5].GetValue<string>();
                            log.LogSource = worksheet.Cells[row, 6].GetValue<string>();
                            log.EventType = worksheet.Cells[row, 7].GetValue<string>();
                            log.Severity = worksheet.Cells[row, 8].GetValue<string>();
                            log.Username = worksheet.Cells[row, 9].GetValue<string>();
                            log.SrcIp = worksheet.Cells[row, 10].GetValue<string>();
                            log.DstIp = worksheet.Cells[row, 11].GetValue<string>();
                            log.SrcPort = worksheet.Cells[row, 12].GetValue<int?>();
                            log.DstPort = worksheet.Cells[row, 13].GetValue<int?>();
                            log.Protocol = worksheet.Cells[row, 14].GetValue<string>();
                            log.ActionTaken = worksheet.Cells[row, 15].GetValue<string>();
                            log.NatSrcIp = worksheet.Cells[row, 16].GetValue<string>();
                            log.NatDstIp = worksheet.Cells[row, 17].GetValue<string>();
                            log.Hostname = worksheet.Cells[row, 18].GetValue<string>();
                            log.Notes = worksheet.Cells[row, 19].GetValue<string>();
                            log.RuleType = worksheet.Cells[row, 20].GetValue<string>();
                            log.LivePcap = worksheet.Cells[row, 21].GetValue<string>();
                            log.Message = worksheet.Cells[row, 22].GetValue<string>();
                            log.LogOccurrence = worksheet.Cells[row, 23].GetValue<string>();
                            log.EventType2 = worksheet.Cells[row, 24].GetValue<string>();
                            log.Severity2 = worksheet.Cells[row, 25].GetValue<string>();
                            log.UserAccount = worksheet.Cells[row, 26].GetValue<string>();
                            log.ActionTaken2 = worksheet.Cells[row, 27].GetValue<string>();
                            logs.Add(log);
                        }
                    }
                }
                if (logs.Any())
                {
                    int batchSize = 100;
                    for (int i = 0; i < logs.Count; i += batchSize)
                    {
                        var batch = logs.Skip(i).Take(batchSize).ToList();
                        _context.EventLogs.AddRange(batch);
                        await _context.SaveChangesAsync();
                    }
                    TempData["ImportSuccess"] = $"Successfully imported {logs.Count} logs.";
                }
                else
                {
                    TempData["ImportError"] = "No valid logs found in the file.";
                }
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = $"Error importing logs: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckNewLogs(DateTime lastLogTime)
        {
            var hasNewLogs = await _context.EventLogs
                .AnyAsync(l => l.Timestamp > lastLogTime);

            return Json(new { hasNewLogs });
        }

        // GET: EventLogs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventLog = await _context.EventLogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventLog == null)
            {
                return NotFound();
            }

            return View(eventLog);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();
            var log = await _context.EventLogs.FindAsync(id);
            if (log == null)
                return NotFound();
            ViewBag.Severities = _context.Severities.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            ViewBag.Messages = _context.MessageTypes.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            ViewBag.RuleTypes = _context.RuleTypes.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            ViewBag.EventTypes = _context.EventTypes.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            ViewBag.ActionTakens = _context.ActionTakens.OrderBy(x => x.Name).Select(x => x.Name).ToList();
            return View(log);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Notes,Severity,Message,RuleType,EventType")] EventLog log)
        {
            var existing = await _context.EventLogs.FindAsync(id);
            if (existing == null)
                return NotFound();
            existing.Notes = log.Notes;
            existing.Severity = log.Severity;
            existing.Message = log.Message;
            existing.RuleType = log.RuleType;
            existing.EventType = log.EventType;
            _context.Update(existing);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> EditNotes(int id, string notes)
        {
            var log = await _context.EventLogs.FindAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            log.Notes = notes;
            _context.Update(log);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string? filterIpAddress, string? filterProtocol, DateTime? filterStartDate, DateTime? filterEndDate)
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("RouterDetectorUser");
            var query = _context.EventLogs.AsQueryable();
            if (!string.IsNullOrEmpty(filterIpAddress))
                query = query.Where(l => l.SrcIp.Contains(filterIpAddress) || l.DstIp.Contains(filterIpAddress));
            if (!string.IsNullOrEmpty(filterProtocol))
                query = query.Where(l => l.Protocol == filterProtocol);
            if (filterStartDate.HasValue)
                query = query.Where(l => l.Timestamp >= filterStartDate);
            if (filterEndDate.HasValue)
                query = query.Where(l => l.Timestamp <= filterEndDate);
            var logs = await query.OrderByDescending(n => n.Timestamp).ToListAsync();

            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("EventLogs");
                // Header
                ws.Cells[1, 1].Value = "Id";
                ws.Cells[1, 2].Value = "Timestamp";
                ws.Cells[1, 3].Value = "Institution";
                ws.Cells[1, 4].Value = "DeviceName";
                ws.Cells[1, 5].Value = "DeviceType";
                ws.Cells[1, 6].Value = "LogSource";
                ws.Cells[1, 7].Value = "EventType";
                ws.Cells[1, 8].Value = "Severity";
                ws.Cells[1, 9].Value = "Username";
                ws.Cells[1, 10].Value = "SrcIp";
                ws.Cells[1, 11].Value = "DstIp";
                ws.Cells[1, 12].Value = "SrcPort";
                ws.Cells[1, 13].Value = "DstPort";
                ws.Cells[1, 14].Value = "Protocol";
                ws.Cells[1, 15].Value = "ActionTaken";
                ws.Cells[1, 16].Value = "NatSrcIp";
                ws.Cells[1, 17].Value = "NatDstIp";
                ws.Cells[1, 18].Value = "Hostname";
                ws.Cells[1, 19].Value = "Notes";
                ws.Cells[1, 20].Value = "RuleType";
                ws.Cells[1, 21].Value = "LivePcap";
                ws.Cells[1, 22].Value = "Message";
                ws.Cells[1, 23].Value = "LogOccurrence";
                ws.Cells[1, 24].Value = "EventType2";
                ws.Cells[1, 25].Value = "Severity2";
                ws.Cells[1, 26].Value = "UserAccount";
                ws.Cells[1, 27].Value = "ActionTaken2";
                // Data
                for (int i = 0; i < logs.Count; i++)
                {
                    var l = logs[i];
                    ws.Cells[i + 2, 1].Value = l.Id;
                    ws.Cells[i + 2, 2].Value = l.Timestamp;
                    ws.Cells[i + 2, 3].Value = l.Institution;
                    ws.Cells[i + 2, 4].Value = l.DeviceName;
                    ws.Cells[i + 2, 5].Value = l.DeviceType;
                    ws.Cells[i + 2, 6].Value = l.LogSource;
                    ws.Cells[i + 2, 7].Value = l.EventType;
                    ws.Cells[i + 2, 8].Value = l.Severity;
                    ws.Cells[i + 2, 9].Value = l.Username;
                    ws.Cells[i + 2, 10].Value = l.SrcIp;
                    ws.Cells[i + 2, 11].Value = l.DstIp;
                    ws.Cells[i + 2, 12].Value = l.SrcPort;
                    ws.Cells[i + 2, 13].Value = l.DstPort;
                    ws.Cells[i + 2, 14].Value = l.Protocol;
                    ws.Cells[i + 2, 15].Value = l.ActionTaken;
                    ws.Cells[i + 2, 16].Value = l.NatSrcIp;
                    ws.Cells[i + 2, 17].Value = l.NatDstIp;
                    ws.Cells[i + 2, 18].Value = l.Hostname;
                    ws.Cells[i + 2, 19].Value = l.Notes;
                    ws.Cells[i + 2, 20].Value = l.RuleType;
                    ws.Cells[i + 2, 21].Value = l.LivePcap;
                    ws.Cells[i + 2, 22].Value = l.Message;
                    ws.Cells[i + 2, 23].Value = l.LogOccurrence;
                    ws.Cells[i + 2, 24].Value = l.EventType2;
                    ws.Cells[i + 2, 25].Value = l.Severity2;
                    ws.Cells[i + 2, 26].Value = l.UserAccount;
                    ws.Cells[i + 2, 27].Value = l.ActionTaken2;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                var bytes = package.GetAsByteArray();
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EventLogs.xlsx");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf(string? filterIpAddress, string? filterProtocol, DateTime? filterStartDate, DateTime? filterEndDate)
        {
            var query = _context.EventLogs.AsQueryable();
            if (!string.IsNullOrEmpty(filterIpAddress))
                query = query.Where(l => l.SrcIp.Contains(filterIpAddress) || l.DstIp.Contains(filterIpAddress));
            if (!string.IsNullOrEmpty(filterProtocol))
                query = query.Where(l => l.Protocol == filterProtocol);
            if (filterStartDate.HasValue)
                query = query.Where(l => l.Timestamp >= filterStartDate);
            if (filterEndDate.HasValue)
                query = query.Where(l => l.Timestamp <= filterEndDate);
            var logs = await query.OrderByDescending(n => n.Timestamp).ToListAsync();

            using (var ms = new System.IO.MemoryStream())
            {
                var document = new PdfSharpCore.Pdf.PdfDocument();
                var page = document.AddPage();
                var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
                var fontHeader = new PdfSharpCore.Drawing.XFont("Arial", 10, PdfSharpCore.Drawing.XFontStyle.Bold);
                var fontRow = new PdfSharpCore.Drawing.XFont("Arial", 8);
                var fontTitle = new PdfSharpCore.Drawing.XFont("Arial", 14, PdfSharpCore.Drawing.XFontStyle.Bold);
                double margin = 20;
                double y = margin;
                double rowHeight = 16;
                double[] colWidths = { 30, 60, 60, 60, 60, 60, 60, 40, 50, 60, 60, 40, 40, 50, 60, 60, 60, 60, 60, 60, 40, 60, 60, 60, 40, 60, 60 };
                string[] headers = new[] { "Id", "Timestamp", "Institution", "DeviceName", "DeviceType", "LogSource", "EventType", "Severity", "Username", "SrcIp", "DstIp", "SrcPort", "DstPort", "Protocol", "ActionTaken", "NatSrcIp", "NatDstIp", "Hostname", "Notes", "RuleType", "LivePcap", "Message", "LogOccurrence", "EventType2", "Severity2", "UserAccount", "ActionTaken2" };

                // Title
                gfx.DrawString("Event Logs", fontTitle, PdfSharpCore.Drawing.XBrushes.Black, new PdfSharpCore.Drawing.XRect(0, y, page.Width, rowHeight), PdfSharpCore.Drawing.XStringFormats.TopCenter);
                y += rowHeight + 10;

                // Draw header background
                gfx.DrawRectangle(PdfSharpCore.Drawing.XBrushes.LightGray, margin, y, colWidths.Sum(), rowHeight);
                double x = margin;
                for (int i = 0; i < headers.Length; i++)
                {
                    gfx.DrawString(headers[i], fontHeader, PdfSharpCore.Drawing.XBrushes.Black, new PdfSharpCore.Drawing.XRect(x + 2, y + 2, colWidths[i], rowHeight), PdfSharpCore.Drawing.XStringFormats.TopLeft);
                    x += colWidths[i];
                }
                y += rowHeight;

                // Draw rows
                for (int idx = 0; idx < logs.Count; idx++)
                {
                    var l = logs[idx];
                    x = margin;
                    var brush = idx % 2 == 0 ? PdfSharpCore.Drawing.XBrushes.White : PdfSharpCore.Drawing.XBrushes.LightYellow;
                    gfx.DrawRectangle(brush, margin, y, colWidths.Sum(), rowHeight);
                    string[] values = new[] {
                        l.Id.ToString(), l.Timestamp.ToString(), l.Institution, l.DeviceName, l.DeviceType, l.LogSource, l.EventType, l.Severity, l.Username, l.SrcIp, l.DstIp, l.SrcPort?.ToString(), l.DstPort?.ToString(), l.Protocol, l.ActionTaken, l.NatSrcIp, l.NatDstIp, l.Hostname, l.Notes, l.RuleType, l.LivePcap, l.Message, l.LogOccurrence, l.EventType2, l.Severity2, l.UserAccount, l.ActionTaken2
                    };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        gfx.DrawString(values[i], fontRow, PdfSharpCore.Drawing.XBrushes.Black, new PdfSharpCore.Drawing.XRect(x + 2, y + 2, colWidths[i], rowHeight), PdfSharpCore.Drawing.XStringFormats.TopLeft);
                        x += colWidths[i];
                    }
                    y += rowHeight;
                    if (y > page.Height - margin - rowHeight)
                    {
                        page = document.AddPage();
                        gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
                        y = margin;
                        // Redraw header on new page
                        gfx.DrawRectangle(PdfSharpCore.Drawing.XBrushes.LightGray, margin, y, colWidths.Sum(), rowHeight);
                        x = margin;
                        for (int i = 0; i < headers.Length; i++)
                        {
                            gfx.DrawString(headers[i], fontHeader, PdfSharpCore.Drawing.XBrushes.Black, new PdfSharpCore.Drawing.XRect(x + 2, y + 2, colWidths[i], rowHeight), PdfSharpCore.Drawing.XStringFormats.TopLeft);
                            x += colWidths[i];
                        }
                        y += rowHeight;
                    }
                }
                document.Save(ms, false);
                return File(ms.ToArray(), "application/pdf", "EventLogs.pdf");
            }
        }
    }

    // Simple view model for the index view
    public class EventLogsViewModel
    {
        public List<EventLog> Logs { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public string FilterIpAddress { get; set; }
        public string FilterProtocol { get; set; }
        public DateTime? FilterStartDate { get; set; }
        public DateTime? FilterEndDate { get; set; }
    }
} 
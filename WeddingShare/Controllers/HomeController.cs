using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WeddingShare.Helpers;
using WeddingShare.Helpers.Database;
using WeddingShare.Models;

namespace WeddingShare.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IConfigHelper _config;
        private readonly IDatabaseHelper _database;
        private readonly IGalleryHelper _gallery;
        private readonly IDeviceDetector _deviceDetector;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<Lang.Translations> _localizer;

        public HomeController(IConfigHelper config, IDatabaseHelper database, IGalleryHelper gallery, IDeviceDetector deviceDetector, ILogger<HomeController> logger, IStringLocalizer<Lang.Translations> localizer)
        {
            _config = config;
            _database = database;
            _gallery = gallery;
            _deviceDetector = deviceDetector;
            _logger = logger;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new Views.Home.IndexModel();

            try
            {
                var deviceType = HttpContext.Session.GetString(SessionKey.DeviceType);
                if (string.IsNullOrWhiteSpace(deviceType))
                {
                    deviceType = (await _deviceDetector.ParseDeviceType(Request.Headers["User-Agent"].ToString())).ToString();
                    HttpContext.Session.SetString(SessionKey.DeviceType, deviceType ?? "Desktop");
                }

                if (_config.GetOrDefault("Settings:Single_Gallery_Mode", false))
                {
                    var key = await _gallery.GetSecretKey("default");
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        return RedirectToAction("Index", "Gallery");
                    }
                }

                model.GalleryNames = _config.GetOrDefault("Settings:Gallery_Selector:Dropdown", false) ? await _database.GetGalleryNames() : new List<string>() { "default" };
                if (_config.GetOrDefault("Settings:Gallery_Selector:Hide_Default_Option", false))
                {
                    model.GalleryNames = model.GalleryNames.Where(x => !x.Equals("default", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Homepage_Load_Error"].Value} - {ex?.Message}");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult SetIdentity(string name) 
        {
            try
            {
                if (Regex.IsMatch(name, @"^[a-zA-Z-\s\-\']+$", RegexOptions.Compiled))
                {
                    HttpContext.Session.SetString(SessionKey.ViewerIdentity, name);

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_localizer["Identity_Session_Error"].Value}: '{name}'");
            }

            return Json(new { success = false });
        }
    }
}
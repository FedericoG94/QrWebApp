using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QrWebApp.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace QrWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GenerateQr(string link, IFormFile logo)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                ViewBag.Error = "Inserisci un link valido.";
                return View("Index");
            }

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(20);

            // Se non c'è logo, mostra direttamente il QR
            if (logo == null || logo.Length == 0)
            {
                var qrBase64 = Convert.ToBase64String(qrBytes);
                ViewBag.QrCode = "data:image/png;base64," + qrBase64;
                ViewBag.Link = link;
                return View("Index");
            }

            // Altrimenti, sovrapponi il logo
            using var qrStream = new MemoryStream(qrBytes);
            using var qrBitmapIndexed = new Bitmap(qrStream);
            using var qrBitmap = new Bitmap(qrBitmapIndexed.Width, qrBitmapIndexed.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(qrBitmap))
            {
                g.DrawImage(qrBitmapIndexed, 0, 0);
                using var logoStream = new MemoryStream();
                logo.CopyTo(logoStream);
                using var logoBitmap = new Bitmap(logoStream);

                // Ridimensiona il logo se troppo grande
                int logoSize = Math.Min(qrBitmap.Width, qrBitmap.Height) / 4;
                using var resizedLogo = new Bitmap(logoBitmap, new Size(logoSize, logoSize));

                int x = (qrBitmap.Width - logoSize) / 2;
                int y = (qrBitmap.Height - logoSize) / 2;
                g.DrawImage(resizedLogo, x, y, logoSize, logoSize);
            }

            using var ms = new MemoryStream();
            qrBitmap.Save(ms, ImageFormat.Png);
            var qrWithLogoBytes = ms.ToArray();
            var qrWithLogoBase64 = Convert.ToBase64String(qrWithLogoBytes);
            ViewBag.QrCode = "data:image/png;base64," + qrWithLogoBase64;
            ViewBag.Link = link;
            return View("Index");
        }

        public IActionResult Archivio()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

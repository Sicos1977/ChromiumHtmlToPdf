using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ChromeHtmlToPdfLib;
using ChromeHtmlToPdfLib.Settings;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Net.Http.Headers;

namespace PDFConverter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly ILogger<ReportController> _logger;

        public ReportController(ILogger<ReportController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using (var converter = new Converter())
            {
                converter.ConvertToPdf(new ConvertUri("http://www.google.nl"), @".\Output\google.pdf", new PageSettings());
            }

            const int bufferSize = 4096;
            var stream = new FileStream(@".\Output\google.pdf", FileMode.Open, FileAccess.Read,
                FileShare.None, bufferSize, FileOptions.DeleteOnClose);

            return File(stream, "application/pdf", "google-output.pdf");
        }
    }
}

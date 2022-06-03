using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication.Pages
{
    [Authorize]
    public class SecurityModel : PageModel
    {
        private readonly ILogger<SecurityModel> _logger;

        public SecurityModel(ILogger<SecurityModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
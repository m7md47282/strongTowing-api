using Dal;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using towing_services.Hubs;
using towing_services.Models;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace towing_services.Controllers
{
    public class HomeController : Controller
    {
        public readonly Towing_Collection db;
        private readonly UserManager<Driver> _userManager;
        private readonly SignInManager<Driver> _signInManager;

        private readonly UserManager<Admin> _adminUserManager;

        private readonly SignInManager<Admin> _adminSignInManager;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;  // تعريف التخزين المؤقت
        private static readonly HashSet<WebSocket> _connectedSockets = new HashSet<WebSocket>();
        private readonly IHubContext<OrderHub> _hubContext;

        private const int MaxAttempts = 3;
        private const int OtpValiditySeconds = 60;
        private const int LockoutDurationMinutes = 10;
        private readonly PasswordHasher<Dal.HR> _passwordHasher = new();
        // تخزين التوكنات مؤقتًا في الذاكرة (Key: email)
        private static readonly Dictionary<string, PasswordResetTokenInfo> HrPasswordResetTokens = new();


        public HomeController(Towing_Collection info, IEmailSender emailSender, UserManager<Driver> userManager, SignInManager<Driver> signInManager, UserManager<Admin> adminUserManager,
        SignInManager<Admin> adminSignInManager, IMemoryCache cache, IHubContext<OrderHub> hubContext)
        {
            this.db = info;
            _emailSender = emailSender;
            _userManager = userManager;
            _signInManager = signInManager;
            _adminUserManager = adminUserManager;
            _adminSignInManager = adminSignInManager;
            _cache = cache;
            _hubContext = hubContext;
            _passwordHasher = new PasswordHasher<Dal.HR>();


        }
        public IActionResult NotFound1()
        {
            return View();
        }

        public IActionResult Home()
        {
            

            return View();
        }


        public IActionResult About()
        {
            return View();
        }
        public IActionResult Offers()
        {
            return View();
        }

        public IActionResult SERvices()
        {
            return View();
        }

       
    public IActionResult roadside_assistance_services()
        {
            return View();
        }

        public IActionResult ather_services()
        {
            return View();
        }
        public IActionResult pricing()
        {
            return View();
        }

        public IActionResult blog()
        {
            return View();
        }

        public IActionResult Singleblog()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }





        private string GenerateTrackingNumber()
        {
            var random = new Random();
            return $"TRK-{random.Next(100000, 999999)}";
        }



        [HttpGet]
        public IActionResult Order_Req()
        {
           
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> Order_Req(Customer customer, Order order)
        {
            if (ModelState.IsValid)
            {
                order.Status = "New Request";

                db.Customer_table.Add(customer);
                await db.SaveChangesAsync();

                HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);

                order.TrackingNumber = GenerateTrackingNumber();
                db.Order_table.Add(order);
                await db.SaveChangesAsync();

                var customerOrder = new Customer_order
                {
                    CustomerId = customer.CustomerId,
                    OrderId = order.OrderId
                };
                db.Customer_order_table.Add(customerOrder);
                await db.SaveChangesAsync();

                int totalNewRequests = db.Order_table.Count(o => o.Status == "New Request");

                await _hubContext.Clients.All.SendAsync("ReceiveNewOrderNotification", totalNewRequests, "New order received!");

                return RedirectToAction("TrackOrder", new { id = order.OrderId });
            }

            ViewBag.Order = order;
            return View();
        }


     


        private int? GetCurrentCustomerId()
        {
            return HttpContext.Session.GetInt32("CustomerId");
        }




        public IActionResult TrackOrder(int id)
        {
            var order = db.Order_table
          .Include(o => o.Customer_orders)
          .ThenInclude(co => co.Customers)
          .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return RedirectToAction("Order_Req");
            }
            var currentCustomerId = GetCurrentCustomerId();
            var orderCustomerId = order.Customer_orders.FirstOrDefault()?.CustomerId;

            if (orderCustomerId != currentCustomerId)
            {
                return RedirectToAction("Order_Req");
            }

            var customerName = order.Customer_orders.FirstOrDefault()?.Customers?.Name ?? "Unknown";

            var viewModel = new OrderTrackingViewModel
            {
                Order = order,
                CustomerName = customerName,


            };
            return View(viewModel);
        }



        [HttpGet]
        public IActionResult Show_services()
        {

 
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {

            return View();
        }

        [HttpPost]
        public IActionResult Contact(Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Customer_table.Add(customer);
                db.SaveChanges();

                ViewData["SuccessMessage"] = "Thank you for contacting us. We will get in touch with you as soon as possible.!";
                ModelState.Clear();
                return View();
            }

            return View();
        }



        //.................................................Register...................................................
        private string GenerateOTP()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private async Task SendEmail(string email, string otpCode)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Admin", "admin@strongtowing.services"));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = "OTP Verification Code";
            message.Body = new TextPart("plain") { Text = $"Your OTP Code is: {otpCode}" };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.zoho.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync("admin@strongtowing.services", "g96i VU01 xVY2 ");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private string GenerateTrackingDriver()
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var trackingDriver = new StringBuilder();

            for (int i = 0; i < 7; i++)
            {
                trackingDriver.Append(chars[random.Next(chars.Length)]);
            }

            return trackingDriver.ToString();
        }


        [HttpGet]

        public IActionResult Register()
        {
            HttpContext.Session.Clear();
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Register(RegisterDriverViewModel model, IFormFile profilePicture, IFormFile licensePicture, IFormFile workPermitPicture)
        {
            if (!IsImageValid(profilePicture))
            {
                ModelState.AddModelError("ProfilePicture", "Only .jpg, .jpeg, and .png files are allowed for the profile picture.");
            }

            if (!IsImageValid(licensePicture))
            {
                ModelState.AddModelError("LicensePicture", "Only .jpg, .jpeg, and .png files are allowed for the license picture.");
            }

            if (!IsImageValid(workPermitPicture))
            {
                ModelState.AddModelError("WorkPermitPicture", "Only .jpg, .jpeg, and .png files are allowed for the work permit picture.");
            }

            if (!ModelState.IsValid)
                return View(model);

            // البحث في كلا الجدولين
            var existingDriver = await db.Driver_table.FirstOrDefaultAsync(d => d.Email == model.Email || d.PhoneNumber == model.Phone || d.UserName == model.Name);
            var existingAdmin = await db.Admin_table.FirstOrDefaultAsync(a => a.Email == model.Email || a.PhoneNumber == model.Phone || a.UserName == model.Name);

            if (existingDriver != null || existingAdmin != null)
            {
                if (existingDriver != null && existingDriver.Email == model.Email)
                {
                    ModelState.AddModelError("Email", "This email is already ");
                }
                if (existingAdmin != null && existingAdmin.Email == model.Email)
                {
                    ModelState.AddModelError("Email", "This email is already ");
                }
                if (existingDriver != null && existingDriver.PhoneNumber == model.Phone)
                {
                    ModelState.AddModelError("Phone", "This phone number is already");
                }
                if (existingAdmin != null && existingAdmin.PhoneNumber == model.Phone)
                {
                    ModelState.AddModelError("Phone", "This phone number is already ");
                }
                if (existingDriver != null && existingDriver.UserName == model.Name)
                {
                    ModelState.AddModelError("Name", "This name is already");
                }
                if (existingAdmin != null && existingAdmin.UserName == model.Name)
                {
                    ModelState.AddModelError("Name", "This name is already ");
                }

                return View(model);
            }

            string trackingDriver = GenerateTrackingDriver();
            HttpContext.Session.SetString("TrackingDriver", trackingDriver);

            HttpContext.Session.SetString("DriverName", model.Name);
            HttpContext.Session.SetString("DriverEmail", model.Email);
            HttpContext.Session.SetString("DriverPhone", model.Phone);
            HttpContext.Session.SetString("VehicleType", model.VehicleType);
            HttpContext.Session.SetString("ProfilePicture", Convert.ToBase64String(await ConvertFileToByteArray(profilePicture)));
            HttpContext.Session.SetString("LicensePicture", Convert.ToBase64String(await ConvertFileToByteArray(licensePicture)));
            HttpContext.Session.SetString("WorkPermitPicture", Convert.ToBase64String(await ConvertFileToByteArray(workPermitPicture)));

            HttpContext.Session.SetString("Password", model.Password);

            return RedirectToAction("VerifyOtp");
        }



        [HttpGet]
        public IActionResult VerifyOtp()
        {
            string email = HttpContext.Session.GetString("DriverEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");

            if (!HttpContext.Session.Keys.Contains("OtpSent"))
            {
                GenerateAndSendOtp(email);

                HttpContext.Session.SetString("OtpSent", "true");

                HttpContext.Session.SetString("OtpGeneratedTime", DateTime.Now.ToString("o"));
            }

            int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;
            ViewBag.RemainingOtpRequests = remainingOtpRequests;

            return View();
        }



        [HttpPost]

        public async Task<IActionResult> VerifyOtp(string enteredOtp)
        {
            string email = HttpContext.Session.GetString("DriverEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Session expired. Please register again.";
                return RedirectToAction("Register");
            }

            string expectedOtp = HttpContext.Session.GetString("OTPCode");
            string generatedTimeString = HttpContext.Session.GetString("OtpGeneratedTime");

            Console.WriteLine($"Expected OTP: {expectedOtp}, Entered OTP: {enteredOtp}");

            if (string.IsNullOrEmpty(expectedOtp) || string.IsNullOrEmpty(generatedTimeString))
            {
                TempData["ErrorMessage"] = "The OTP has expired. Please request a new one.";
                return RedirectToAction("VerifyOtp");
            }

            DateTime generatedTime = DateTime.Parse(generatedTimeString);
            double remainingTime = 60 - (DateTime.Now - generatedTime).TotalSeconds;

            Console.WriteLine($"Generated Time: {generatedTime}, Remaining Time: {remainingTime}");

            if (remainingTime <= 0)
            {
                TempData["ErrorMessage"] = "The OTP has expired. Please request a new one.";
                HttpContext.Session.Remove("OTPCode");
                HttpContext.Session.Remove("OtpGeneratedTime");
                return RedirectToAction("VerifyOtp");
            }

            enteredOtp = enteredOtp.Trim();

            if (enteredOtp == expectedOtp)
            {
                var existingDriver = await _userManager.FindByEmailAsync(email);
                if (existingDriver != null)
                {
                    TempData["ErrorMessage"] = "This email is already registered. Please log in.";
                    return RedirectToAction("Register");
                }

                Driver driver = await CreateDriverAccount();
                if (driver != null)
                {
                    //var driver = await _userManager.FindByEmailAsync(email);
                 
                    //if (driver != null)
                    //{
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            Expires = DateTime.Now.AddMinutes(60)
                        };

                        Console.WriteLine($"Creating cookie for Driver ID: {driver.Id}");
                    await _signInManager.SignInAsync(driver, isPersistent: false);

                    //Response.Cookies.Append("DriverAuth", $"{driver.Id}|verified", cookieOptions);

                        return RedirectToAction("PendingApproval", new { id = driver.Id });
                    //}
                }

                return RedirectToAction("Register");
            }

            TempData["ErrorMessage"] = "Invalid OTP. Please try again.";
            return RedirectToAction("VerifyOtp");
        }





        [HttpPost]

        public IActionResult ResendOtp()
        {
            string email = HttpContext.Session.GetString("DriverEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Session expired. Please log in again." });

            string generatedTimeString = HttpContext.Session.GetString("OtpGeneratedTime");
            DateTime? generatedTime = string.IsNullOrEmpty(generatedTimeString) ? (DateTime?)null : DateTime.Parse(generatedTimeString);

            if (generatedTime != null && (DateTime.Now - generatedTime.Value).TotalSeconds < 60)
            {
                return Json(new { success = false, message = "The OTP is still valid. Please wait before requesting a new one." });
            }

            string cooldownTimeString = HttpContext.Session.GetString("CooldownTime");
            DateTime? cooldownTime = string.IsNullOrEmpty(cooldownTimeString) ? (DateTime?)null : DateTime.Parse(cooldownTimeString);

            if (cooldownTime.HasValue && cooldownTime <= DateTime.Now)
            {
                HttpContext.Session.SetInt32("RemainingOtpRequests", 2);
                HttpContext.Session.Remove("CooldownTime");
            }

            int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;

            if (remainingOtpRequests > 0)
            {
                GenerateAndSendOtp(email);

                HttpContext.Session.SetString("OtpGeneratedTime", DateTime.Now.ToString("o"));

                remainingOtpRequests--;
                HttpContext.Session.SetInt32("RemainingOtpRequests", remainingOtpRequests);

                if (remainingOtpRequests <= 0)
                {
                    HttpContext.Session.SetString("CooldownTime", DateTime.Now.AddMinutes(5).ToString("o"));
                }

                return Json(new { success = true, message = "A new OTP has been sent.", remainingOtpRequests });
            }

            if (cooldownTime.HasValue && cooldownTime > DateTime.Now)
            {
                TimeSpan remainingCooldown = cooldownTime.Value - DateTime.Now;
                return Json(new { success = false, message = $"Please wait {remainingCooldown.Minutes:D2}:{remainingCooldown.Seconds:D2} before resending." });
            }

            return Json(new { success = false, message = "No attempts left. Please try again later." });
        }



        [HttpGet]
        public IActionResult GetOtpStatus()
        {
            string email = HttpContext.Session.GetString("DriverEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Register");

            double remainingOtpValidity = 0;
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("OtpGeneratedTime")))
            {
                DateTime? generatedTime = DateTime.Parse(HttpContext.Session.GetString("OtpGeneratedTime"));
                remainingOtpValidity = (generatedTime != null) ? 60 - (DateTime.Now - generatedTime.Value).TotalSeconds : 0;
            }

            int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;

            string cooldownTimeString = HttpContext.Session.GetString("CooldownTime");
            DateTime? cooldownTime = string.IsNullOrEmpty(cooldownTimeString) ? (DateTime?)null : DateTime.Parse(cooldownTimeString);

            if (cooldownTime.HasValue && cooldownTime <= DateTime.Now)
            {
                remainingOtpRequests = 2;
                HttpContext.Session.SetInt32("RemainingOtpRequests", remainingOtpRequests);
                HttpContext.Session.Remove("CooldownTime");
            }

            TimeSpan remainingCooldown = cooldownTime.HasValue && cooldownTime > DateTime.Now
                ? cooldownTime.Value - DateTime.Now
                : TimeSpan.Zero;

            return Json(new
            {
                remainingOtpValidity = remainingOtpValidity > 0 ? remainingOtpValidity : 0,
                remainingOtpRequests,
                remainingCooldownMinutes = remainingCooldown.Minutes,
                remainingCooldownSeconds = remainingCooldown.Seconds
            });

        }

        private void GenerateAndSendOtp(string email)
        {
            string generatedTimeString = HttpContext.Session.GetString("OtpGeneratedTime");
            if (!string.IsNullOrEmpty(generatedTimeString))
            {
                DateTime generatedTime = DateTime.Parse(generatedTimeString);
                double elapsedTimeInSeconds = (DateTime.Now - generatedTime).TotalSeconds;

                if (elapsedTimeInSeconds < 60)
                {
                    Console.WriteLine("The OTP is still valid. Please wait before generating a new one.");
                    return; 
                }
            }

            string otpCode = GenerateOTP();
            Console.WriteLine($"Generated OTP: {otpCode}");

            HttpContext.Session.SetString("OTPCode", otpCode);
            HttpContext.Session.SetString("OtpGeneratedTime", DateTime.Now.ToString("o"));
            _ = SendEmail(email, otpCode);
        }



        private async Task<Driver> CreateDriverAccount()
        {

            string password = HttpContext.Session.GetString("Password");
            string profilePictureBase64 = HttpContext.Session.GetString("ProfilePicture");
            string licensePictureBase64 = HttpContext.Session.GetString("LicensePicture");
            string workPermitPictureBase64 = HttpContext.Session.GetString("WorkPermitPicture"); // Adding work permit image

            byte[] profilePictureBytes = Convert.FromBase64String(profilePictureBase64);
            byte[] licensePictureBytes = Convert.FromBase64String(licensePictureBase64);
            byte[] workPermitPictureBytes = Convert.FromBase64String(workPermitPictureBase64); // Converting work permit to bytes

            try
            {
                string profilePicturePath = await SaveFileToDisk(profilePictureBytes, "drivers", ".jpg");
                string licensePicturePath = await SaveFileToDisk(licensePictureBytes, "licenses", ".jpg");
                string workPermitPicturePath = await SaveFileToDisk(workPermitPictureBytes, "workpermits", ".jpg"); // Saving work permit image

                var driver = new Driver
                {
                    UserName = HttpContext.Session.GetString("DriverName"),
                    Email = HttpContext.Session.GetString("DriverEmail"),
                    PhoneNumber = HttpContext.Session.GetString("DriverPhone"),
                    VehicleType = HttpContext.Session.GetString("VehicleType"),
                    ProfilePicture = profilePicturePath,
                    LicensePicture = licensePicturePath,
                    workPermitPicture = workPermitPicturePath, // Adding work permit
                    IsAvailable = false,
                    IsNewDriver = true,
                    TrackingDriver = HttpContext.Session.GetString("TrackingDriver")
                };

                var passwordHasher = new PasswordHasher<Driver>();
                driver.PasswordHash = passwordHasher.HashPassword(driver, password);

                var result = await _userManager.CreateAsync(driver, password);
                if (result.Succeeded)
                {     
                    return driver;
                }
                else
                {
                    var errorDetails = string.Join(", ", result.Errors.Select(e => e.Description)); // جمع الأخطاء مع الفواصل
                    TempData["ErrorMessage"] = "There was an issue saving the driver account. Please try again later. " + errorDetails;
                    Console.WriteLine("Error creating driver account: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                    return null;
                }              

            }


           
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred while saving the driver account. Please try again later.{ex.Message}";
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return null;
            }
        }





        private async Task<string> SaveFileToDisk(byte[] fileData, string folderName, string fileExtension)
        {
            var allowedExtensions = new List<string> { ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(fileExtension.ToLower()))
            {
                throw new InvalidOperationException("This file type is not allowed.");
            }

            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            string filePath = Path.Combine(directoryPath, $"{Guid.NewGuid()}{fileExtension}");

            await System.IO.File.WriteAllBytesAsync(filePath, fileData);

            return Path.Combine("uploads", folderName, Path.GetFileName(filePath));
        }


        private async Task<byte[]> ConvertFileToByteArray(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private bool IsImageValid(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            return allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower());
        }

        /***/
        public async Task<IActionResult> PendingApproval(int id)
        {
          
            var driver = await _userManager.GetUserAsync(User);

            if (driver == null || driver.Id != id)
            {
                return RedirectToAction("Home", "Home");
            }

            var driverFromDb = db.Driver_table.FirstOrDefault(d => d.Id == id);
            if (driverFromDb == null)
            {
                return NotFound();
            }
            return View(driver);
        }


        [HttpGet]
        public IActionResult Login()
        {
            var model = new LoginDriverViewModel();

            if (User.Identity.IsAuthenticated)
            {
                model.RememberMe = true;
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDriverViewModel model, string returnUrl = null)
        {
            Console.WriteLine($"[POST Login] RememberMe Value: {model.RememberMe}");

            if (ModelState.IsValid)
            {
                // التحقق من السائق
                var driver = await _userManager.FindByEmailAsync(model.Email);
                if (driver != null)
                {
                    // التحقق من كلمة المرور
                    if (await _userManager.CheckPasswordAsync(driver, model.Password))
                    {
                        // التحقق من حالة الموافقة
                        if (driver.IsApproved == null)
                        {
                            ModelState.AddModelError("", "Your account has not been approved by the admin yet.");
                            return View(model);
                        }

                        if (driver.IsDriver)
                        {
                            HttpContext.Session.SetString("IsDriver", "true");
                            HttpContext.Session.SetString("IsAdmin", "false");
                            ViewBag.IsDriver = true;
                            ViewBag.IsAdmin = false;
                            await _signInManager.SignInAsync(driver, model.RememberMe);

                            return RedirectToAction("Dashboard", "DriverDash");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Incorrect password.");
                        return View(model);
                    }
                }

                var admin = await _adminUserManager.FindByEmailAsync(model.Email);
                if (admin != null)
                {
                    if (await _adminUserManager.CheckPasswordAsync(admin, model.Password))
                    {
                        if (admin.IsAdmin)
                        {
                            HttpContext.Session.SetString("IsDriver", "false");
                            HttpContext.Session.SetString("IsAdmin", "true");
                            ViewBag.IsDriver = false;
                            ViewBag.IsAdmin = true;
                            await _adminSignInManager.SignInAsync(admin, model.RememberMe);

                            return RedirectToAction("prof", "admin1");
                        }
                    }


                    else
                    {
                        ModelState.AddModelError("", "Incorrect password.");
                        return View(model);
                    }
                }


                var hr = await db.HRs.FirstOrDefaultAsync(h => h.Email == model.Email);
                if (hr != null)
                {
                    var result = _passwordHasher.VerifyHashedPassword(hr, hr.PasswordHash, model.Password);
                    if (result == PasswordVerificationResult.Success)
                    {
                        if (hr.IsHR)
                        {
                            HttpContext.Session.SetString("IsHR", "True");
                            HttpContext.Session.SetInt32("HRId", hr.Id);
                            HttpContext.Session.SetString("HRName", hr.FullName);
                            return RedirectToAction("show_driver", "HRS");
                        }
                        else
                        {
                            ModelState.AddModelError("", "You are not registered as an HR in the system.");
                            return View(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Incorrect password.");
                        return View(model);
                    }
                }



                ModelState.AddModelError("", "Email not found.");
            }
            else
            {
                ModelState.AddModelError("", "Please ensure all fields are filled in correctly.");
            }

            return View(model);
        }

       


        /*******************************/
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Home");
        }

        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

      
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var driver = await _userManager.FindByEmailAsync(model.Email);
            var admin = await _adminUserManager.FindByEmailAsync(model.Email);
            var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == model.Email);

            if (driver == null && admin == null && hrUser == null)
            {
                ModelState.AddModelError("", "This email is not registered in our system.");
                return View(model);
            }

            string userType = driver != null ? "Driver" : admin != null ? "Admin" : "HR";

            var now = DateTime.UtcNow;
            var existingOtp = await db.OTPEntries
                .Where(o => o.Email == model.Email && !o.IsUsed && o.ExpireAt > now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            OTPEntry otpEntry;

            if (existingOtp == null)
            {
                var otp = new Random().Next(100000, 999999).ToString();
                otpEntry = new OTPEntry
                {
                    Email = model.Email,
                    UserType = userType,
                    OTPCode = otp,
                    CreatedAt = now,
                    ExpireAt = now.AddMinutes(5),
                    IsUsed = false,
                    AttemptsCount = 0,
                    ResendCount = 0,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };
                db.OTPEntries.Add(otpEntry);
                await db.SaveChangesAsync();

            }
            else
            {
                otpEntry = existingOtp;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    string subject = "Your OTP Code";
                    string message = $"Your OTP code is: <strong>{otpEntry.OTPCode}</strong>. It will expire in 5 minutes.";
                    await _emailSender.SendEmailAsync(otpEntry.Email, subject, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SEND OTP ERROR] {ex.Message}");
                }
            });
            // **حفظ البيانات في Session لاستخدامها في صفحة التحقق**
            HttpContext.Session.SetString("Email", otpEntry.Email);
            HttpContext.Session.SetInt32("OtpId", otpEntry.Id);

            var otpModel = new OtpVerificationViewModel
            {
                Email = otpEntry.Email,
                ExpireAtUtc = otpEntry.ExpireAt,
                ResendAttemptsLeft = Math.Max(0, 2 - otpEntry.ResendCount),
                ResendLockedUntil = otpEntry.ResendLockedUntil,
                IsResendEnabled = Math.Max(0, 2 - otpEntry.ResendCount) > 0,
                IsVerifyEnabled = otpEntry.ExpireAt > now,
                MaxAttempts = 2
            };

            return View("EnterOtp", otpModel);
        }
        [HttpGet]
        public async Task<IActionResult> EnterOtp()
        {
            var email = HttpContext.Session.GetString("Email");
            var otpId = HttpContext.Session.GetInt32("OtpId");

            if (string.IsNullOrEmpty(email) || otpId == null)
                return RedirectToAction("ForgotPassword");

            var now = DateTime.UtcNow;
            var otpEntry = await db.OTPEntries.FindAsync(otpId);

            if (otpEntry == null || otpEntry.Email != email || otpEntry.IsUsed || otpEntry.ExpireAt <= now)
            {
                TempData["ErrorMessage"] = "OTP not found, used, or expired.";
                return RedirectToAction("ForgotPassword");
            }

            int maxAttempts = 2;
            var resendAttemptsLeft = Math.Max(0, maxAttempts - otpEntry.ResendCount);
            var isResendEnabled = resendAttemptsLeft > 0 && (otpEntry.ResendLockedUntil == null || now >= otpEntry.ResendLockedUntil);

            var model = new OtpVerificationViewModel
            {
                Email = email,
                ExpireAtUtc = otpEntry.ExpireAt,
                ResendAttemptsLeft = resendAttemptsLeft,
                ResendLockedUntil = otpEntry.ResendLockedUntil,
                IsResendEnabled = isResendEnabled,
                IsVerifyEnabled = otpEntry.ExpireAt > now,
                MaxAttempts = maxAttempts
            };

            return View(model);
        }

      

        [HttpPost]
        public async Task<IActionResult> VerifyOtppass(OtpVerificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return await ReloadOtpViewWithModel(model);
            }

            var currentNow = DateTime.UtcNow;
            var otpValidEntry = await db.OTPEntries
                .Where(o => o.Email == model.Email && !o.IsUsed && o.ExpireAt > currentNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpValidEntry == null)
            {
                ModelState.AddModelError("", "OTP not found or expired.");
                return await ReloadOtpViewWithModel(model);
            }

            if (otpValidEntry.OTPCode != model.OTPCode)
            {
                ModelState.AddModelError("", "Invalid OTP code.");
                return await ReloadOtpViewWithModel(model);
            }

            otpValidEntry.IsUsed = true;
            otpValidEntry.AttemptsCount += 1;
            await db.SaveChangesAsync();

            TempData["EmailForReset"] = model.Email;
            return RedirectToAction("ResetPasswordWithOtp");
        }

        // Helper method to reload the OTP view with fresh data from DB
        private async Task<IActionResult> ReloadOtpViewWithModel(OtpVerificationViewModel model)
        {
            var now = DateTime.UtcNow;
            var otpEntry = await db.OTPEntries
                .Where(o => o.Email == model.Email && !o.IsUsed && o.ExpireAt > now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpEntry == null)
            {
                ModelState.AddModelError("", "OTP not found or expired.");
                return View("EnterOtp", model);
            }

            int maxAttempts = 2;
            var resendAttemptsLeft = Math.Max(0, maxAttempts - otpEntry.ResendCount);
            var isResendEnabled = resendAttemptsLeft > 0;

            if (!isResendEnabled && otpEntry.ResendLockedUntil != null && now < otpEntry.ResendLockedUntil)
            {
                isResendEnabled = false;
            }

            var fullModel = new OtpVerificationViewModel
            {
                Email = model.Email,
                OTPCode = model.OTPCode,
                ExpireAtUtc = otpEntry.ExpireAt,
                ResendAttemptsLeft = resendAttemptsLeft,
                ResendLockedUntil = otpEntry.ResendLockedUntil,
                IsResendEnabled = isResendEnabled,
                IsVerifyEnabled = otpEntry.ExpireAt > now,
                MaxAttempts = maxAttempts
            };

            return View("EnterOtp", fullModel);
        }


        [HttpPost]
        public async Task<IActionResult> ResendOtppass(OtpVerificationViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email))
                return RedirectToAction("ForgotPassword");

            var now = DateTime.UtcNow;

            var latestOtp = await db.OTPEntries
                .Where(o => o.Email == model.Email && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestOtp == null)
            {
                ModelState.AddModelError("", "No OTP found to resend.");
                return View("EnterOtp", model);
            }

            // ✅ Reset lockout if expired
            if (latestOtp.ResendLockedUntil != null && now >= latestOtp.ResendLockedUntil)
            {
                latestOtp.ResendCount = 0;
                latestOtp.ResendLockedUntil = null;
                await db.SaveChangesAsync();
            }

            // Check if still locked
            if (latestOtp.ResendLockedUntil != null && now < latestOtp.ResendLockedUntil)
            {
                var minutesLeft = (latestOtp.ResendLockedUntil.Value - now).Minutes;
                ModelState.AddModelError("", $"You have exceeded the allowed limit. Please wait {minutesLeft} minute(s).");

                model.ResendAttemptsLeft = 0;
                model.ResendLockedUntil = latestOtp.ResendLockedUntil;
                model.IsResendEnabled = false;
                return View("EnterOtp", model);
            }

            // If exceeded max allowed attempts
            if (latestOtp.ResendCount >= model.MaxAttempts)
            {
                latestOtp.ResendLockedUntil = now.AddMinutes(1); // Lock for 10 minutes
                await db.SaveChangesAsync();

                ModelState.AddModelError("", "You have exceeded the number of resend attempts. Please try again after 10 minutes.");
                model.ResendAttemptsLeft = 0;
                model.ResendLockedUntil = latestOtp.ResendLockedUntil;
                model.IsResendEnabled = false;
                return View("EnterOtp", model);
            }

            // Generate new OTP
            var newOtp = new Random().Next(100000, 999999).ToString();
            latestOtp.OTPCode = newOtp;
            latestOtp.CreatedAt = now;
            latestOtp.ExpireAt = now.AddMinutes(5);
            latestOtp.ResendCount += 1;

            if (latestOtp.ResendCount == model.MaxAttempts)
                latestOtp.ResendLockedUntil = now.AddMinutes(10); // Lock after second attempt

            await db.SaveChangesAsync();

            // Send OTP via email
            _ = Task.Run(async () =>
            {
                try
                {
                    string subject = "OTP Resent";
                    string message = $"Your new OTP is: <strong>{newOtp}</strong>. It is valid for 5 minutes.";
                    await _emailSender.SendEmailAsync(model.Email, subject, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RESEND OTP ERROR] {ex.Message}");
                }
            });

            // Update model values
            model.OTPCode = string.Empty;
            model.ExpireAtUtc = latestOtp.ExpireAt;
            model.ResendAttemptsLeft = Math.Max(0, model.MaxAttempts - latestOtp.ResendCount);
            model.ResendLockedUntil = latestOtp.ResendLockedUntil;
            model.IsResendEnabled = model.ResendAttemptsLeft > 0 && (model.ResendLockedUntil == null || now >= model.ResendLockedUntil);
            model.IsVerifyEnabled = true;

            ViewBag.SuccessMessage = "OTP has been resent successfully.";

            return View("EnterOtp", model);
        }





        [HttpGet]
        public IActionResult ResetPasswordWithOtp()
        {
            if (TempData["EmailForReset"] == null)
                return RedirectToAction("ForgotPassword");

            TempData.Keep("EmailForReset"); // حتى يبقى في POST
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordWithOtp(ResetPasswordWithOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = TempData["EmailForReset"] as string;
            if (email == null)
                return RedirectToAction("ForgotPassword");

            // تحقق من نوع المستخدم
            var driver = await _userManager.FindByEmailAsync(email);
            var admin = await _adminUserManager.FindByEmailAsync(email);
            var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == email);

            if (driver != null)
            {
                var hash = _userManager.PasswordHasher.HashPassword(driver, model.NewPassword);
                driver.PasswordHash = hash;
                await _userManager.UpdateAsync(driver);
            }
            else if (admin != null)
            {
                var hash = _adminUserManager.PasswordHasher.HashPassword(admin, model.NewPassword);
                admin.PasswordHash = hash;
                await _adminUserManager.UpdateAsync(admin);
            }
            else if (hrUser != null)
            {
                // تشفير كلمة مرور HR بنفس طريقة Identity
                var hasher = new PasswordHasher<HR>();
                var hashedPassword = hasher.HashPassword(hrUser, model.NewPassword);
                hrUser.PasswordHash = hashedPassword;

                db.HRs.Update(hrUser);
                await db.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            TempData.Clear();
            return RedirectToAction("Login", "Home");
        }
















        public IActionResult Errors()
        {
            var errorMessage = TempData["ErrorMessage"] as string;
            Console.WriteLine("Error message: " + errorMessage);
            return View("Errors", model: errorMessage);
        }


        public IActionResult menu_page()
        {
         
            return View();
        }








        //[HttpPost]
        //public IActionResult CheckOffer(string zipCode)
        //{
        //    try
        //    {
        //        // البحث عن ZIP في قاعدة البيانات
        //        var zipEntry = db.ZipOffer.FirstOrDefault(z => z.ZipCode == zipCode);

        //        // إذا لم يوجد عرض لهذا ZIP
        //        if (zipEntry == null)
        //        {
        //            return Json(new
        //            {
        //                result = "😔 Sorry, no offers available for this area right now."
        //            });
        //        }

        //        // زيادة عدد الزيارات لهذا ZIP
        //        zipEntry.VisitCount++;
        //        db.SaveChanges();

        //        // البحث عن أكثر ZIP تمت زيارته
        //        var topZip = db.ZipOffer
        //                       .OrderByDescending(z => z.VisitCount)
        //                       .FirstOrDefault();

        //        // تحديد نوع العرض
        //        string message = (topZip != null && topZip.ZipCode == zipEntry.ZipCode)
        //            ? $"🔥 Special Offer for Your Area! {zipEntry.OfferDescription} + Extra 50% Off!"
        //            : $"🎉 Offer Available: {zipEntry.OfferDescription}";

        //        return Json(new { result = message });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Server Exception: " + ex.Message);  // في حال الـ console موجود
        //        return Json(new
        //        {
        //            error = "Internal Server Error",
        //            message = ex.Message
        //        });
        //    }

        //}

        public class ZipCodeRequest
        {
            public string ZipCode { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckOffer([FromBody] ZipCodeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ZipCode))
            {
                return Json(new { error = "ZIP code cannot be empty." });
            }

            try
            {
                var zipEntry = db.ZipOffer.FirstOrDefault(z => z.ZipCode == request.ZipCode);

                if (zipEntry == null)
                {
                    return Json(new
                    {
                        result = "😔 Sorry, no offers available for this area right now."
                    });
                }

                zipEntry.VisitCount++;
                db.SaveChanges();

                var topZip = db.ZipOffer
                               .OrderByDescending(z => z.VisitCount)
                               .FirstOrDefault();

                string message = (topZip != null && topZip.ZipCode == zipEntry.ZipCode)
                    ? $"🔥 Special Offer for Your Area! {zipEntry.OfferDescription} + Extra 50% Off!"
                    : $"🎉 Offer Available: {zipEntry.OfferDescription}";

                return Json(new { result = message });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server Exception: " + ex.Message);
                return Json(new
                {
                    error = "Internal Server Error",
                    message = ex.Message
                });
            }
        }




        /*PROVI*/
        [HttpGet]
        public IActionResult Create_PROV()
        {
            var vm = new Providersmodel
            {
                Provider = new Dal.Provider()
            };
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            return View(vm);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create_PROV(Providersmodel model)
        //{
        //    var zipFile = Request.Form.Files["zipFile"];

        //    if (zipFile == null || zipFile.Length == 0)
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "Please upload a ZIP file.");
        //        return View(model);
        //    }

        //    if (Path.GetExtension(zipFile.FileName).ToLower() != ".zip")
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "Only ZIP files are allowed.");
        //        return View(model);
        //    }

        //    if (zipFile.Length > 2 * 1024 * 1024) // 2 ميجابايت كحد أقصى
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "File size must be 2MB or less.");
        //        return View(model);
        //    }

        //    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "zipfils");
        //    if (!Directory.Exists(uploadsFolder))
        //        Directory.CreateDirectory(uploadsFolder);

        //    var uniqueFileName = Guid.NewGuid().ToString() + ".zip";
        //    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //    using (var stream = new FileStream(filePath, FileMode.Create))
        //    {
        //        zipFile.CopyTo(stream);
        //    }

        //    // تخزين مسار الملف النسبي في قاعدة البيانات
        //    model.Provider.RequestedTerritory = "zipfils/" + uniqueFileName;

        //    if (ModelState.IsValid)
        //    {
        //        db.Provider.Add(model.Provider);
        //        db.SaveChanges();

        //        TempData["SuccessMessage"] = "Provider data saved successfully.";
        //        return RedirectToAction("Create_PROV");
        //    }

        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create_PROV(Providersmodel model)
        //{
        //    var zipFile = Request.Form.Files["zipFile"];

        //    // تحقق من رفع الملف
        //    if (zipFile == null || zipFile.Length == 0)
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "Please upload a ZIP file.");
        //        return View(model);
        //    }

        //    // تحقق من نوع الملف
        //    if (Path.GetExtension(zipFile.FileName).ToLower() != ".zip")
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "Only ZIP files are allowed.");
        //        return View(model);
        //    }

        //    // تحقق من حجم الملف
        //    if (zipFile.Length > 2 * 1024 * 1024) // 2 ميجابايت
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "File size must be 2MB or less.");
        //        return View(model);
        //    }

        //    // استخراج خدمات المستخدم من الفورم (checkboxes)
        //    var selectedServices = Request.Form["Provider.SelectedServices"].ToArray();

        //    if (selectedServices == null || selectedServices.Length == 0)
        //    {
        //        ModelState.AddModelError("Provider.SelectedServices", "Please select at least one service.");
        //        return View(model);
        //    }

        //    // رفع الملف
        //    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "zipfils");
        //    if (!Directory.Exists(uploadsFolder))
        //        Directory.CreateDirectory(uploadsFolder);

        //    var uniqueFileName = Guid.NewGuid().ToString() + ".zip";
        //    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //    using (var stream = new FileStream(filePath, FileMode.Create))
        //    {
        //        zipFile.CopyTo(stream);
        //    }

        //    // حفظ مسار الملف في الخاصية RequestedTerritory
        //    model.Provider.RequestedTerritory = "zipfils/" + uniqueFileName;

        //    // حفظ الخدمات المختارة كنص مفصول بفواصل
        //    model.Provider.SelectedServices = string.Join(",", selectedServices);

        //    if (ModelState.IsValid)
        //    {
        //        db.Provider.Add(model.Provider);
        //        db.SaveChanges();

        //        TempData["SuccessMessage"] = "Provider data saved successfully.";
        //        return RedirectToAction("Create_PROV");
        //    }

        //    return View(model);
        //}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create_PROV(Providersmodel model)
        {
            var zipFile = Request.Form.Files["zipFile"];
            var w9File = Request.Form.Files["w9File"];
            var backgroundFile = Request.Form.Files["backgroundFile"];
            var certificateFile = Request.Form.Files["certificateFile"];

            // تحقق رفع وفحص zipFile
            if (zipFile == null || zipFile.Length == 0)
            {
                ModelState.AddModelError("Provider.RequestedTerritory", "Please upload a ZIP file.");
                return View(model);
            }
            if (Path.GetExtension(zipFile.FileName).ToLower() != ".zip")
            {
                ModelState.AddModelError("Provider.RequestedTerritory", "Only ZIP files are allowed.");
                return View(model);
            }
            if (zipFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("Provider.RequestedTerritory", "File size must be 2MB or less.");
                return View(model);
            }

            // تحقق رفع وفحص w9File
            if (w9File == null || w9File.Length == 0)
            {
                ModelState.AddModelError("Provider.W9FilePath", "Please upload a ZIP file for W-9.");
                return View(model);
            }
            if (Path.GetExtension(w9File.FileName).ToLower() != ".zip")
            {
                ModelState.AddModelError("Provider.W9FilePath", "Only ZIP files are allowed for W-9.");
                return View(model);
            }
            if (w9File.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("Provider.W9FilePath", "W-9 ZIP file size must be 2MB or less.");
                return View(model);
            }

            // تحقق رفع وفحص backgroundFile
            if (backgroundFile == null || backgroundFile.Length == 0)
            {
                ModelState.AddModelError("Provider.BackgroundFilesPaths", "Please upload a ZIP file for background check.");
                return View(model);
            }
            if (Path.GetExtension(backgroundFile.FileName).ToLower() != ".zip")
            {
                ModelState.AddModelError("Provider.BackgroundFilesPaths", "Only ZIP files are allowed for background check.");
                return View(model);
            }
            if (backgroundFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("Provider.BackgroundFilesPaths", "Background check ZIP file size must be 2MB or less.");
                return View(model);
            }

            // تحقق رفع وفحص certificateFile
            if (certificateFile == null || certificateFile.Length == 0)
            {
                ModelState.AddModelError("Provider.CertificateInsuranceFilePath", "Please upload your Certificate of Insurance.");
                return View(model);
            }
            if (Path.GetExtension(certificateFile.FileName).ToLower() != ".zip")
            {
                ModelState.AddModelError("Provider.CertificateInsuranceFilePath", "Only ZIP files are allowed for Certificate of Insurance.");
                return View(model);
            }
            if (certificateFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("Provider.CertificateInsuranceFilePath", "Certificate ZIP file size must be 2MB or less.");
                return View(model);
            }

            // تحقق اختيار خدمات (checkboxes)
            var selectedServices = Request.Form["Provider.SelectedServices"].ToArray();
            if (selectedServices == null || selectedServices.Length == 0)
            {
                ModelState.AddModelError("Provider.SelectedServices", "Please select at least one service.");
                return View(model);
            }

            // تحقق من تعليب checkbox الخاص بالشهادة (IsNotSubjectToBackupWithholding)
            if (!model.Provider.IsNotSubjectToBackupWithholding)
            {
                ModelState.AddModelError("Provider.IsNotSubjectToBackupWithholding", "You must certify that you are not subject to backup withholding.");
                return View(model);
            }

            // رفع zipFile في مجلد zipfils
            var zipfilsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "zipfils");
            if (!Directory.Exists(zipfilsFolder))
                Directory.CreateDirectory(zipfilsFolder);

            var zipFileName = Guid.NewGuid().ToString() + ".zip";
            var zipFilePath = Path.Combine(zipfilsFolder, zipFileName);
            using (var stream = new FileStream(zipFilePath, FileMode.Create))
            {
                zipFile.CopyTo(stream);
            }
            model.Provider.RequestedTerritory = "zipfils/" + zipFileName;

            // رفع w9File في مجلد w9files
            var w9Folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "w9files");
            if (!Directory.Exists(w9Folder))
                Directory.CreateDirectory(w9Folder);

            var w9FileName = Guid.NewGuid().ToString() + ".zip";
            var w9FilePath = Path.Combine(w9Folder, w9FileName);
            using (var stream = new FileStream(w9FilePath, FileMode.Create))
            {
                w9File.CopyTo(stream);
            }
            model.Provider.W9FilePath = "w9files/" + w9FileName;

            // رفع backgroundFile في مجلد backgroundfiles
            var backgroundFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "backgroundfiles");
            if (!Directory.Exists(backgroundFolder))
                Directory.CreateDirectory(backgroundFolder);

            var backgroundFileName = Guid.NewGuid().ToString() + ".zip";
            var backgroundFilePath = Path.Combine(backgroundFolder, backgroundFileName);
            using (var stream = new FileStream(backgroundFilePath, FileMode.Create))
            {
                backgroundFile.CopyTo(stream);
            }
            model.Provider.BackgroundFilesPaths = "backgroundfiles/" + backgroundFileName;

            // رفع certificateFile في مجلد certificatefiles
            var certFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "certificatefiles");
            if (!Directory.Exists(certFolder))
                Directory.CreateDirectory(certFolder);

            var certFileName = Guid.NewGuid().ToString() + ".zip";
            var certFilePath = Path.Combine(certFolder, certFileName);
            using (var stream = new FileStream(certFilePath, FileMode.Create))
            {
                certificateFile.CopyTo(stream);
            }
            model.Provider.CertificateInsuranceFilePath = "certificatefiles/" + certFileName;

            // حفظ الخدمات المختارة كنص مفصول بفواصل
            model.Provider.SelectedServices = string.Join(",", selectedServices);


       
            // حفظ BusinessTypes (غير إلزامي)
            var selectedBusinessTypes = Request.Form["Provider.BusinessTypes"].ToArray();
            model.Provider.BusinessTypes = string.Join(",", selectedBusinessTypes);

            if (ModelState.IsValid)
            {
                db.Provider.Add(model.Provider);
                db.SaveChanges();

                TempData["SuccessMessage"] = "  Thank you for submitting your information!           Your data has been received and will be processed as soon as possible.";
                return RedirectToAction("Create_PROV");
            }

            return View(model);
        }







        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create_PROV(Providersmodel model)
        //{
        //    var zipFile = Request.Form.Files["zipFile"];
        //    var w9File = Request.Form.Files["w9File"];
        //    var backgroundFile = Request.Form.Files["backgroundFile"];

        //    // التحقق من رفع وفحص zipFile
        //    if (zipFile == null || zipFile.Length == 0)
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "Please upload a ZIP file.");
        //        return View(model);
        //    }
        //    if (Path.GetExtension(zipFile.FileName).ToLower() != ".zip")
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "Only ZIP files are allowed.");
        //        return View(model);
        //    }
        //    if (zipFile.Length > 2 * 1024 * 1024)
        //    {
        //        ModelState.AddModelError("Provider.RequestedTerritory", "File size must be 2MB or less.");
        //        return View(model);
        //    }

        //    // التحقق من رفع وفحص w9File
        //    if (w9File == null || w9File.Length == 0)
        //    {
        //        ModelState.AddModelError("Provider.W9FilePath", "Please upload a ZIP file for W-9.");
        //        return View(model);
        //    }
        //    if (Path.GetExtension(w9File.FileName).ToLower() != ".zip")
        //    {
        //        ModelState.AddModelError("Provider.W9FilePath", "Only ZIP files are allowed for W-9.");
        //        return View(model);
        //    }
        //    if (w9File.Length > 2 * 1024 * 1024)
        //    {
        //        ModelState.AddModelError("Provider.W9FilePath", "W-9 ZIP file size must be 2MB or less.");
        //        return View(model);
        //    }

        //    // التحقق من رفع وفحص backgroundFile
        //    if (backgroundFile == null || backgroundFile.Length == 0)
        //    {
        //        ModelState.AddModelError("Provider.BackgroundFilesPaths", "Please upload a ZIP file for background check.");
        //        return View(model);
        //    }
        //    if (Path.GetExtension(backgroundFile.FileName).ToLower() != ".zip")
        //    {
        //        ModelState.AddModelError("Provider.BackgroundFilesPaths", "Only ZIP files are allowed for background check.");
        //        return View(model);
        //    }
        //    if (backgroundFile.Length > 2 * 1024 * 1024)
        //    {
        //        ModelState.AddModelError("Provider.BackgroundFilesPaths", "Background check ZIP file size must be 2MB or less.");
        //        return View(model);
        //    }

        //    // استخراج خدمات المستخدم من الفورم (checkboxes)
        //    var selectedServices = Request.Form["Provider.SelectedServices"].ToArray();
        //    if (selectedServices == null || selectedServices.Length == 0)
        //    {
        //        ModelState.AddModelError("Provider.SelectedServices", "Please select at least one service.");
        //        return View(model);
        //    }

        //    // رفع zipFile في مجلد zipfils
        //    var zipfilsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "zipfils");
        //    if (!Directory.Exists(zipfilsFolder))
        //        Directory.CreateDirectory(zipfilsFolder);

        //    var zipFileName = Guid.NewGuid().ToString() + ".zip";
        //    var zipFilePath = Path.Combine(zipfilsFolder, zipFileName);
        //    using (var stream = new FileStream(zipFilePath, FileMode.Create))
        //    {
        //        zipFile.CopyTo(stream);
        //    }
        //    model.Provider.RequestedTerritory = "zipfils/" + zipFileName;

        //    // رفع w9File في مجلد w9files
        //    var w9Folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "w9files");
        //    if (!Directory.Exists(w9Folder))
        //        Directory.CreateDirectory(w9Folder);

        //    var w9FileName = Guid.NewGuid().ToString() + ".zip";
        //    var w9FilePath = Path.Combine(w9Folder, w9FileName);
        //    using (var stream = new FileStream(w9FilePath, FileMode.Create))
        //    {
        //        w9File.CopyTo(stream);
        //    }
        //    model.Provider.W9FilePath = "w9files/" + w9FileName;

        //    // رفع backgroundFile في مجلد backgroundfiles
        //    var backgroundFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "backgroundfiles");
        //    if (!Directory.Exists(backgroundFolder))
        //        Directory.CreateDirectory(backgroundFolder);

        //    var backgroundFileName = Guid.NewGuid().ToString() + ".zip";
        //    var backgroundFilePath = Path.Combine(backgroundFolder, backgroundFileName);
        //    using (var stream = new FileStream(backgroundFilePath, FileMode.Create))
        //    {
        //        backgroundFile.CopyTo(stream);
        //    }
        //    model.Provider.BackgroundFilesPaths = "backgroundfiles/" + backgroundFileName;

        //    // حفظ الخدمات المختارة كنص مفصول بفواصل
        //    model.Provider.SelectedServices = string.Join(",", selectedServices);

        //    if (ModelState.IsValid)
        //    {
        //        db.Provider.Add(model.Provider);
        //        db.SaveChanges();

        //        TempData["SuccessMessage"] = "Provider data saved successfully.";
        //        return RedirectToAction("Create_PROV");
        //    }

        //    return View(model);
        //}






































        //[HttpPost]
        //public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);
        //        var admin = await _adminUserManager.FindByEmailAsync(model.Email);
        //        //HR
        //        var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == model.Email);
        //        //

        //        if (driver == null && admin == null && hrUser == null)
        //        {
        //            ModelState.AddModelError("", "This email is not registered in our system.");
        //            return View(model);
        //        }

        //        ViewBag.Message = "A password reset email has been sent to your email address.";

        //        if (driver != null)
        //        {
        //            await _userManager.UpdateSecurityStampAsync(driver);

        //            var token = await _userManager.GeneratePasswordResetTokenAsync(driver);
        //            var resetLink = Url.Action("ResetPassword", "Home", new { token, email = model.Email }, Request.Scheme);

        //            _ = _emailSender.SendEmailAsync(model.Email, "Password Reset",
        //                $"Please reset your password by clicking <a href='{resetLink}'>here</a>");
        //        }
        //        else if (admin != null)
        //        {
        //            // تحديث Security Stamp للإدمن لضمان انتهاء صلاحية الروابط القديمة
        //            await _adminUserManager.UpdateSecurityStampAsync(admin);

        //            var secretKey = Guid.NewGuid().ToString();
        //            var resetLink = Url.Action("ResetPassword", "Home", new { secretKey, email = model.Email, stamp = admin.SecurityStamp }, Request.Scheme);

        //            _ = _emailSender.SendEmailAsync(model.Email, "Admin Password Reset",
        //                $"Please reset your admin password by clicking <a href='{resetLink}'>here</a>");
        //        }


        //        //HR
        //        else if (hrUser != null)
        //        {
        //            // أنشئ توكن فريد
        //            var token = Guid.NewGuid().ToString();

        //            // احفظ التوكن مع وقت انتهاء الصلاحية (ساعة واحدة)
        //            lock (HrPasswordResetTokens) // تأمين من التزامن
        //            {
        //                HrPasswordResetTokens[hrUser.Email] = new PasswordResetTokenInfo
        //                {
        //                    Token = token,
        //                    Expiration = DateTime.UtcNow.AddHours(1)
        //                };
        //            }

        //            var resetLink = Url.Action("ResetPassword", "Home", new { token, email = model.Email }, Request.Scheme);

        //            _ = _emailSender.SendEmailAsync(model.Email, "HR Password Reset",
        //                $"Please reset your HR password by clicking <a href='{resetLink}'>here</a>");
        //        }
        //        ////
        //        return View(model);
        //    }

        //    return View(model);
        //}


        //////////        [HttpGet]
        //////////        public async Task<IActionResult> ResetPassword(string email, string token, string stamp)
        //////////        {
        //////////            if (string.IsNullOrEmpty(email))
        //////////            {
        //////////                TempData["ErrorMessage"] = "Oops! It seems like the link has expired or is no longer valid. Please request a new password reset link.";
        //////////                return RedirectToAction("Errors", "Home");
        //////////            }

        //////////            var driver = await _userManager.FindByEmailAsync(email);
        //////////            var admin = await _adminUserManager.FindByEmailAsync(email);
        //////////            //HR
        //////////            var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == email);
        ////////////
        //////////            if (driver == null && admin == null && hrUser == null)
        //////////            {
        //////////                TempData["ErrorMessage"] = "User not found. Please check your email address and try again.";
        //////////                return RedirectToAction("Errors", "Home");
        //////////            }

        //////////            if (driver != null)
        //////////            {

        //////////                return View(new ResetPasswordViewModel { Email = email, Token = token });
        //////////            }
        //////////            else if (admin != null)
        //////////            {


        //////////                return View(new ResetPasswordViewModel { Email = email });
        //////////            }
        //////////            //HR
        //////////            else if (hrUser != null)
        //////////            {
        //////////                lock (HrPasswordResetTokens)
        //////////                {

        //////////                }
        //////////                return View(new ResetPasswordViewModel { Email = email, Token = token });
        //////////            }
        ////////////


        //////////            TempData["ErrorMessage"] = "User not found. Please check your email address and try again.";
        //////////            return RedirectToAction("Errors", "Home");
        //////////        }


        //////////        [HttpPost]
        //////////        [ValidateAntiForgeryToken]
        //////////        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        //////////        {
        //////////            if (ModelState.IsValid)
        //////////            {
        //////////                var driver = await _userManager.FindByEmailAsync(model.Email);
        //////////                var admin = await _adminUserManager.FindByEmailAsync(model.Email);
        //////////                //HR
        //////////                var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == model.Email);
        ////////////
        //////////                if (driver != null)
        //////////                {
        //////////                    var isTokenValid = await _userManager.VerifyUserTokenAsync(driver, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", model.Token);

        //////////                    if (!isTokenValid)
        //////////                    {
        //////////                        ModelState.AddModelError("", "The password reset token has expired or is invalid. Please request a new token.");
        //////////                        return View(model);
        //////////                    }

        //////////                    var result = await _userManager.ResetPasswordAsync(driver, model.Token, model.NewPassword);

        //////////                    if (result.Succeeded)
        //////////                    {
        //////////                        var emailSubject = "Password Reset Successful";
        //////////                        var emailBody = "Your password has been successfully changed. If you didn't request this change, please contact us immediately.";
        //////////                        await _emailSender.SendEmailAsync(driver.Email, emailSubject, emailBody);

        //////////                        return RedirectToAction("Login");
        //////////                    }

        //////////                    foreach (var error in result.Errors)
        //////////                    {
        //////////                        ModelState.AddModelError("", error.Description);
        //////////                    }
        //////////                }
        //////////                else if (admin != null)
        //////////                {
        //////////                    var passwordHasher = _adminUserManager.PasswordHasher;
        //////////                    var hashedPassword = passwordHasher.HashPassword(admin, model.NewPassword);
        //////////                    admin.PasswordHash = hashedPassword;

        //////////                    var result = await _adminUserManager.UpdateAsync(admin);

        //////////                    if (result.Succeeded)
        //////////                    {
        //////////                        var emailSubject = "Password Reset Successful";
        //////////                        var emailBody = "Your password has been successfully changed. If you didn't request this change, please contact us immediately.";
        //////////                        await _emailSender.SendEmailAsync(admin.Email, emailSubject, emailBody);

        //////////                        ViewBag.Message = "Your password has been updated successfully.";
        //////////                        return RedirectToAction("Login");
        //////////                    }
        //////////                    else
        //////////                    {
        //////////                        ModelState.AddModelError("", "An error occurred while updating your password.");
        //////////                    }
        //////////                }


        //////////                //HR
        //////////                else if (hrUser != null)
        //////////                {
        //////////                    // تحقق من التوكن في الذاكرة وصلاحيته
        //////////                    lock (HrPasswordResetTokens)
        //////////                    {
        //////////                        if (!HrPasswordResetTokens.TryGetValue(model.Email, out var tokenInfo) || tokenInfo.Token != model.Token || tokenInfo.Expiration < DateTime.UtcNow)
        //////////                        {
        //////////                            ModelState.AddModelError("", "The password reset token has expired or is invalid. Please request a new token.");
        //////////                            return View(model);
        //////////                        }

        //////////                        // حذف التوكن بعد الاستخدام
        //////////                        HrPasswordResetTokens.Remove(model.Email);
        //////////                    }

        //////////                    // تحديث الباسورد مشفر
        //////////                    hrUser.PasswordHash = _passwordHasher.HashPassword(hrUser, model.NewPassword);
        //////////                    db.HRs.Update(hrUser);
        //////////                    await db.SaveChangesAsync();

        //////////                    await _emailSender.SendEmailAsync(hrUser.Email, "Password Reset Successful",
        //////////                        "Your password has been successfully changed. If you didn't request this change, please contact us immediately.");

        //////////                    return RedirectToAction("Login");
        //////////                }
        //////////                //

        //////////                ModelState.AddModelError("", "User not found.");
        //////////            }

        //////////            return View(model);
        //////////        }


        //[HttpGet]
        //public async Task<IActionResult> ResetPassword(string email, string token, string stamp)
        //{
        //    if (string.IsNullOrEmpty(email))
        //    {
        //        TempData["ErrorMessage"] = "Oops! It seems like the link has expired or is no longer valid. Please request a new password reset link.";
        //        return RedirectToAction("Errors", "Home");
        //    }

        //    var driver = await _userManager.FindByEmailAsync(email);
        //    var admin = await _adminUserManager.FindByEmailAsync(email);
        //    var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == email);

        //    if (driver == null && admin == null && hrUser == null)
        //    {
        //        TempData["ErrorMessage"] = "User not found. Please check your email address and try again.";
        //        return RedirectToAction("Errors", "Home");
        //    }

        //    if (driver != null)
        //    {
        //        return View(new ResetPasswordViewModel { Email = email, Token = token });
        //    }
        //    else if (admin != null)
        //    {
        //        return View(new ResetPasswordViewModel { Email = email, Token = token });
        //    }
        //    else if (hrUser != null)
        //    {
        //        return View(new ResetPasswordViewModel { Email = email, Token = token });
        //    }

        //    TempData["ErrorMessage"] = "User not found. Please check your email address and try again.";
        //    return RedirectToAction("Errors", "Home");
        //}



        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);
        //        var admin = await _adminUserManager.FindByEmailAsync(model.Email);
        //        var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == model.Email);

        //        if (driver != null)
        //        {
        //            var result = await _userManager.RemovePasswordAsync(driver);
        //            if (!result.Succeeded)
        //            {
        //                foreach (var error in result.Errors)
        //                    ModelState.AddModelError("", error.Description);
        //                return View(model);
        //            }

        //            var addPassResult = await _userManager.AddPasswordAsync(driver, model.NewPassword);
        //            if (addPassResult.Succeeded)
        //            {
        //                await _emailSender.SendEmailAsync(driver.Email, "Password Reset Successful",
        //                    "Your password has been successfully changed. If you didn't request this change, please contact us immediately.");
        //                return RedirectToAction("Login");
        //            }
        //            else
        //            {
        //                foreach (var error in addPassResult.Errors)
        //                    ModelState.AddModelError("", error.Description);
        //                return View(model);
        //            }
        //        }
        //        else if (admin != null)
        //        {
        //            admin.PasswordHash = _adminUserManager.PasswordHasher.HashPassword(admin, model.NewPassword);
        //            var result = await _adminUserManager.UpdateAsync(admin);

        //            if (result.Succeeded)
        //            {
        //                await _emailSender.SendEmailAsync(admin.Email, "Password Reset Successful",
        //                    "Your password has been successfully changed. If you didn't request this change, please contact us immediately.");
        //                return RedirectToAction("Login");
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("", "An error occurred while updating your password.");
        //                return View(model);
        //            }
        //        }
        //        else if (hrUser != null)
        //        {
        //            hrUser.PasswordHash = _passwordHasher.HashPassword(hrUser, model.NewPassword);
        //            db.HRs.Update(hrUser);
        //            await db.SaveChangesAsync();

        //            await _emailSender.SendEmailAsync(hrUser.Email, "Password Reset Successful",
        //                "Your password has been successfully changed. If you didn't request this change, please contact us immediately.");

        //            return RedirectToAction("Login");
        //        }

        //        ModelState.AddModelError("", "User not found.");
        //    }

        //    return View(model);
        //}



        ////////[HttpPost]
        ////////public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        ////////{
        ////////    if (ModelState.IsValid)
        ////////    {
        ////////        var driver = await _userManager.FindByEmailAsync(model.Email);
        ////////        var admin = await _adminUserManager.FindByEmailAsync(model.Email);
        ////////        var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == model.Email);

        ////////        if (driver == null && admin == null && hrUser == null)
        ////////        {
        ////////            ModelState.AddModelError("", "This email is not registered in our system.");
        ////////            return View(model);
        ////////        }

        ////////        string userType = driver != null ? "Driver" : admin != null ? "Admin" : "HR";

        ////////        // إنشاء كود OTP
        ////////        var otp = new Random().Next(100000, 999999).ToString();
        ////////        var now = DateTime.UtcNow;

        ////////        // إنشاء سجل جديد في جدول OTPEntry
        ////////        var otpEntry = new OTPEntry
        ////////        {
        ////////            Email = model.Email,
        ////////            UserType = userType,
        ////////            OTPCode = otp,
        ////////            CreatedAt = now,
        ////////            ExpireAt = now.AddMinutes(5),
        ////////            IsUsed = false,
        ////////            AttemptsCount = 0,
        ////////            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        ////////            UserAgent = Request.Headers["User-Agent"].ToString()
        ////////        };

        ////////        db.OTPEntries.Add(otpEntry);
        ////////        await db.SaveChangesAsync();

        ////////        // إرسال البريد الإلكتروني
        ////////        string subject = "Password Reset OTP";
        ////////        string message = $"Your OTP for password reset is: <strong>{otp}</strong>. It is valid for 5 minutes.";
        ////////        await _emailSender.SendEmailAsync(model.Email, subject, message);

        ////////        TempData["Email"] = model.Email;  // للإبقاء على الإيميل للجلسة القادمة
        ////////        return RedirectToAction("EnterOtp");
        ////////    }

        ////////    return View(model);
        ////////}



        ////////[HttpGet]
        ////////public async Task<IActionResult> EnterOtp()
        ////////{
        ////////    if (TempData["Email"] == null)
        ////////        return RedirectToAction("ForgotPassword");

        ////////    TempData.Keep("Email");

        ////////    var email = TempData["Email"] as string;

        ////////    // جلب آخر OTP غير مستخدم للبريد
        ////////    var otpEntry = await db.OTPEntries
        ////////        .Where(o => o.Email == email && !o.IsUsed)
        ////////        .OrderByDescending(o => o.CreatedAt)
        ////////        .FirstOrDefaultAsync();

        ////////    if (otpEntry == null)
        ////////    {
        ////////        TempData["ErrorMessage"] = "OTP entry not found or already used.";
        ////////        return RedirectToAction("ForgotPassword");
        ////////    }

        ////////    // تمرير وقت انتهاء الصلاحية إلى ال View
        ////////    ViewBag.ExpireAtUtc = otpEntry.ExpireAt;

        ////////    return View();
        ////////}


        ////////[HttpPost]
        ////////public async Task<IActionResult> VerifyOtppass(OtpVerificationViewModel model)
        ////////{
        ////////    if (!ModelState.IsValid)
        ////////    {
        ////////        return View("EnterOtp", model);
        ////////    }

        ////////    var email = TempData["Email"] as string;
        ////////    if (email == null)
        ////////    {
        ////////        ModelState.AddModelError("", "Session expired. Please request a new OTP.");
        ////////        return RedirectToAction("ForgotPassword");
        ////////    }

        ////////    var now = DateTime.UtcNow;

        ////////    var otpEntry = await db.OTPEntries
        ////////        .Where(o => o.Email == email && o.OTPCode == model.OTPCode)
        ////////        .OrderByDescending(o => o.CreatedAt)
        ////////        .FirstOrDefaultAsync();

        ////////    if (otpEntry == null)
        ////////    {
        ////////        ModelState.AddModelError("", "Invalid OTP.");
        ////////        return View("EnterOtp", model);
        ////////    }

        ////////    if (otpEntry.IsUsed)
        ////////    {
        ////////        ModelState.AddModelError("", "This OTP has already been used.");
        ////////        return View("EnterOtp", model);
        ////////    }

        ////////    if (otpEntry.ExpireAt < now)
        ////////    {
        ////////        ModelState.AddModelError("", "This OTP has expired.");
        ////////        return View("EnterOtp", model);
        ////////    }

        ////////    // زيادة عدد المحاولات إذا كانت المحاولة خاطئة (اختياري حسب سياستك)
        ////////    otpEntry.AttemptsCount += 1;

        ////////    // التحقق نجح، ضع IsUsed = true
        ////////    otpEntry.IsUsed = true;
        ////////    await db.SaveChangesAsync();

        ////////    // توجيه المستخدم لتغيير كلمة المرور
        ////////    TempData["EmailForReset"] = email;
        ////////    return RedirectToAction("ResetPasswordWithOtp");
        ////////}



        ////////[HttpGet]
        ////////public async Task<IActionResult> EnterOtp()
        ////////{
        ////////    if (TempData["Email"] == null)
        ////////        return RedirectToAction("ForgotPassword");

        ////////    TempData.Keep("Email");

        ////////    var email = TempData["Email"] as string;

        ////////    var otpEntry = await db.OTPEntries
        ////////        .Where(o => o.Email == email && !o.IsUsed)
        ////////        .OrderByDescending(o => o.CreatedAt)
        ////////        .FirstOrDefaultAsync();

        ////////    if (otpEntry == null)
        ////////    {
        ////////        TempData["ErrorMessage"] = "OTP entry not found or already used.";
        ////////        return RedirectToAction("ForgotPassword");
        ////////    }

        ////////    ViewBag.ExpireAtUtc = otpEntry.ExpireAt;
        ////////    return View();
        ////////}
        //        [HttpGet]
        //public async Task<IActionResult> EnterOtp()
        //{
        //    if (TempData["Email"] == null)
        //        return RedirectToAction("ForgotPassword");

        //    TempData.Keep("Email");
        //    var email = TempData["Email"] as string;
        //    var now = DateTime.UtcNow;

        //    var otpEntry = await db.OTPEntries
        //        .Where(o => o.Email == email && !o.IsUsed)
        //        .OrderByDescending(o => o.CreatedAt)
        //        .FirstOrDefaultAsync();

        //    if (otpEntry == null)
        //    {
        //        TempData["ErrorMessage"] = "OTP entry not found or already used.";
        //        return RedirectToAction("ForgotPassword");
        //    }

        //    ViewBag.ExpireAtUtc = otpEntry.ExpireAt;

        //    // حالة تفعيل زر Verify: مفعل فقط إذا الرمز ما زال صالحاً
        //    ViewBag.IsVerifyEnabled = otpEntry.ExpireAt > now;

        //    // حالة تفعيل زر Resend:
        //    bool canResend = true;

        //    // لو عدد الإعادة >= 2 والقفل ما زال فعال
        //    if (otpEntry.ResendCount >= 2 && otpEntry.ResendLockedUntil != null && now < otpEntry.ResendLockedUntil)
        //    {
        //        canResend = false;
        //        ViewBag.ResendLockedUntil = otpEntry.ResendLockedUntil;
        //    }

        //    // لو رمز منتهي الصلاحية في الوقت الحالي، نسمح بإعادة الإرسال
        //    if (otpEntry.ExpireAt <= now)
        //    {
        //        canResend = true;
        //    }

        //    ViewBag.IsResendEnabled = canResend;

        //    return View();
        //}


        //[HttpPost]
        //public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    var driver = await _userManager.FindByEmailAsync(model.Email);
        //    var admin = await _adminUserManager.FindByEmailAsync(model.Email);
        //    var hrUser = await db.HRs.FirstOrDefaultAsync(h => h.Email == model.Email);

        //    if (driver == null && admin == null && hrUser == null)
        //    {
        //        ModelState.AddModelError("", "This email is not registered in our system.");
        //        return View(model);
        //    }

        //    string userType = driver != null ? "Driver" : admin != null ? "Admin" : "HR";
        //    var now = DateTime.UtcNow;

        //    // تحقق إذا فيه رمز OTP غير مستخدم وغير منتهي
        //    var existingOtp = await db.OTPEntries
        //        .Where(o => o.Email == model.Email && !o.IsUsed && o.ExpireAt > now)
        //        .OrderByDescending(o => o.CreatedAt)
        //        .FirstOrDefaultAsync();

        //    if (existingOtp == null)
        //    {
        //        var otp = new Random().Next(100000, 999999).ToString();

        //        var otpEntry = new OTPEntry
        //        {
        //            Email = model.Email,
        //            UserType = userType,
        //            OTPCode = otp,
        //            CreatedAt = now,
        //            ExpireAt = now.AddMinutes(5),
        //            IsUsed = false,
        //            AttemptsCount = 0,
        //            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        //            UserAgent = Request.Headers["User-Agent"].ToString()
        //        };

        //        db.OTPEntries.Add(otpEntry);
        //        await db.SaveChangesAsync();

        //        // إرسال البريد في الخلفية (بدون انتظار)
        //        _ = Task.Run(async () =>
        //        {
        //            try
        //            {
        //                string subject = "Password Reset OTP";
        //                string message = $"Your OTP for password reset is: <strong>{otp}</strong>. It is valid for 5 minutes.";
        //                await _emailSender.SendEmailAsync(model.Email, subject, message);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"[OTP ERROR] Failed to send OTP to {model.Email}. Error: {ex.Message}");
        //            }
        //        });
        //    }

        //    TempData["Email"] = model.Email;
        //    return RedirectToAction("EnterOtp");
        //}
        //[HttpGet]
        //public async Task<IActionResult> EnterOtp()
        //{
        //    if (TempData["Email"] == null || TempData["OtpId"] == null)
        //        return RedirectToAction("ForgotPassword");

        //    TempData.Keep("Email");
        //    TempData.Keep("OtpId");

        //    var email = TempData["Email"] as string;
        //    int otpId = Convert.ToInt32(TempData["OtpId"]);
        //    var now = DateTime.UtcNow;

        //    var otpEntry = await db.OTPEntries.FindAsync(otpId);

        //    if (otpEntry == null || otpEntry.Email != email || otpEntry.IsUsed)
        //    {
        //        TempData["ErrorMessage"] = "OTP entry not found or already used.";
        //        return RedirectToAction("ForgotPassword");
        //    }

        //    ViewBag.ExpireAtUtc = otpEntry.ExpireAt;
        //    ViewBag.IsVerifyEnabled = otpEntry.ExpireAt > now;

        //    int remaining = Math.Max(0, 2 - otpEntry.ResendCount);
        //    ViewBag.RemainingAttempts = remaining;

        //    bool canResend = true;
        //    if (remaining <= 0 && otpEntry.ResendLockedUntil != null && now < otpEntry.ResendLockedUntil)
        //    {
        //        canResend = false;
        //        ViewBag.ResendLockedUntil = otpEntry.ResendLockedUntil;
        //    }

        //    ViewBag.IsResendEnabled = canResend;

        //    return View();
        //}



        //[HttpPost]
        //public async Task<IActionResult> VerifyOtppass(OtpVerificationViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        TempData.Keep("Email");
        //        return View("EnterOtp", model);
        //    }

        //    var email = TempData["Email"] as string;
        //    if (email == null)
        //    {
        //        return RedirectToAction("ForgotPassword");
        //    }

        //    TempData.Keep("Email");

        //    var now = DateTime.UtcNow;

        //    var otpEntry = await db.OTPEntries
        //        .Where(o => o.Email == email && !o.IsUsed && o.ExpireAt > now)
        //        .OrderByDescending(o => o.CreatedAt)
        //        .FirstOrDefaultAsync();

        //    if (otpEntry == null || otpEntry.OTPCode != model.OTPCode)
        //    {
        //        ModelState.AddModelError("", "Invalid or expired OTP.");
        //        return View("EnterOtp", model);
        //    }

        //    otpEntry.IsUsed = true;
        //    otpEntry.AttemptsCount += 1;
        //    await db.SaveChangesAsync();

        //    TempData["EmailForReset"] = email;
        //    return RedirectToAction("ResetPasswordWithOtp");
        //}

        //[HttpPost]
        //public async Task<IActionResult> ResendOtppass()
        //{
        //    var email = TempData["Email"] as string;
        //    if (string.IsNullOrEmpty(email))
        //        return RedirectToAction("ForgotPassword");

        //    TempData.Keep("Email");
        //    var now = DateTime.UtcNow;

        //    var latestOtp = await db.OTPEntries
        //        .Where(o => o.Email == email && !o.IsUsed)
        //        .OrderByDescending(o => o.CreatedAt)
        //        .FirstOrDefaultAsync();

        //    if (latestOtp == null)
        //    {
        //        TempData["ErrorMessage"] = "No OTP found to resend.";
        //        return RedirectToAction("EnterOtp");
        //    }

        //    // إذا كان القفل مفعل
        //    if (latestOtp.ResendLockedUntil != null && now < latestOtp.ResendLockedUntil)
        //    {
        //        TempData["ErrorMessage"] = "You have reached the resend limit. Please wait 10 minutes.";
        //        return RedirectToAction("EnterOtp");
        //    }

        //    // إذا وصلت للحد الأقصى من المحاولات
        //    if (latestOtp.ResendCount >= 2)
        //    {
        //        latestOtp.ResendLockedUntil = now.AddMinutes(10);
        //        await db.SaveChangesAsync();

        //        TempData["ErrorMessage"] = "You have exceeded the resend limit. Try again after 10 minutes.";
        //        return RedirectToAction("EnterOtp");
        //    }

        //    // إرسال OTP جديد
        //    var newOtp = new Random().Next(100000, 999999).ToString();
        //    latestOtp.OTPCode = newOtp;
        //    latestOtp.CreatedAt = now;
        //    latestOtp.ExpireAt = now.AddMinutes(5);
        //    latestOtp.ResendCount += 1;

        //    await db.SaveChangesAsync();

        //    _ = Task.Run(async () =>
        //    {
        //        try
        //        {
        //            string subject = "Password Reset OTP (Resent)";
        //            string message = $"Your new OTP is: <strong>{newOtp}</strong>. It is valid for 5 minutes.";
        //            await _emailSender.SendEmailAsync(email, subject, message);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"[RESEND OTP ERROR] {ex.Message}");
        //        }
        //    });

        //    TempData["SuccessMessage"] = "OTP resent successfully.";
        //    return RedirectToAction("EnterOtp");
        //}
        // POST: ForgotPassword












        //public IActionResult GetDriver()
        //{
        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }

        //    try
        //    {
        //        // جلب البيانات من الكاش أو تحديثها
        //        var cachedDrivers = _cache.Get<List<DriverViewModel>>("drivers");

        //        if (cachedDrivers == null)
        //        {
        //            Console.WriteLine("Cache miss - Fetching data from database.");
        //            cachedDrivers = db.Driver_table
        //                              .Select(d => new DriverViewModel
        //                              {
        //                                  DriverId = d.Id,
        //                                  Name = d.UserName,
        //                                  Email = d.Email,
        //                                  phone = d.PhoneNumber,
        //                                  VehicleType = d.VehicleType,
        //                                  TrackingDriver = d.TrackingDriver,
        //                                  LicensePicture=d.LicensePicture,
        //                                  ProfilePicture=d.ProfilePicture,
        //                                  workPermitPicture=d.workPermitPicture
        //                              }).ToList();

        //            _cache.Set("drivers", cachedDrivers, TimeSpan.FromMinutes(5));
        //            Console.WriteLine("Data cached successfully for 5 minutes.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Cache hit - Data retrieved from cache.");
        //        }

        //        return PartialView("GetDriver", cachedDrivers);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Error.WriteLine($"An error occurred: {ex.Message}");
        //        return StatusCode(500, "Internal server error");
        //    }
        //}





        //public async Task<IActionResult> show_driver()
        //{
        //    var drivers = await _userManager.Users
        //                          .OrderByDescending(d => d.IsNewDriver)
        //                          .ToListAsync();


        //    return View(drivers);
        //}

        //public async Task<IActionResult> ApproveDriver(string id)
        //{
        //    var driver = await _userManager.FindByIdAsync(id);
        //    if (driver == null)
        //    {
        //        return NotFound();
        //    }

        //    if (!driver.IsDriver)
        //    {
        //        driver.IsDriver = true;
        //    }

        //    driver.IsApproved = true;

        //    var result = await _userManager.UpdateAsync(driver);

        //    if (!result.Succeeded)
        //    {
        //        ModelState.AddModelError(string.Empty, "An error occurred while updating the driver.");
        //        return View(driver);
        //    }

        //    await _emailSender.SendEmailAsync(driver.Email, "Your account has been approved",
        //        "Congratulations! Your application to become a driver has been approved.");

        //    return RedirectToAction(nameof(show_driver));
        //}



        //public async Task<IActionResult> RejectDriver(string id)
        //{
        //    var driver = await _userManager.FindByIdAsync(id);
        //    if (driver == null)
        //    {
        //        return NotFound();
        //    }

        //    driver.IsApproved = false;

        //    var result = await _userManager.UpdateAsync(driver);
        //    if (!result.Succeeded)
        //    {
        //    }

        //    await _emailSender.SendEmailAsync(driver.Email, "Your account has been rejected",
        //        "Sorry, your application to become a driver in the system has been rejected. Thank you for your understanding.");

        //    return RedirectToAction(nameof(show_driver));
        //}

        //[HttpGet]
        //public IActionResult AddDriver()
        //{
        //    return RedirectToAction("Register");  
        //}

        //[HttpPost]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var driver = db.Driver_table.Find(id);

        //    if (driver == null)
        //    {
        //        return NotFound();
        //    }

        //    // مسار الصور على القرص
        //    string profilePicturePath = driver.ProfilePicture;
        //    string licensePicturePath = driver.LicensePicture;
        //    string workPermitPicturePath = driver.workPermitPicture;

        //    try
        //    {
        //        // حذف السائق من قاعدة البيانات
        //        db.Driver_table.Remove(driver);
        //        await db.SaveChangesAsync();

        //        // حذف الصور من القرص الصلب إذا كانت موجودة
        //        DeleteFileFromDisk(profilePicturePath);
        //        DeleteFileFromDisk(licensePicturePath);
        //        DeleteFileFromDisk(workPermitPicturePath);

        //        // إزالة الكاش القديم
        //        _cache.Remove("drivers");

        //        // تحديث الكاش بعد الحذف
        //        var updatedDrivers = db.Driver_table
        //                               .Select(d => new DriverViewModel
        //                               {
        //                                   DriverId = d.Id,
        //                                   Name = d.UserName,
        //                                   Email = d.Email,
        //                                   phone = d.PhoneNumber,
        //                                   VehicleType = d.VehicleType,
        //                                   TrackingDriver = d.TrackingDriver,
        //                                   LicensePicture = d.LicensePicture,
        //                                   ProfilePicture = d.ProfilePicture,
        //                                   workPermitPicture = d.workPermitPicture
        //                               }).ToList();

        //        _cache.Set("drivers", updatedDrivers, TimeSpan.FromMinutes(5));

        //        // إعادة البيانات بعد الحذف
        //        return PartialView("GetDriver", updatedDrivers);
        //    }
        //    catch (Exception ex)
        //    {
        //        // تسجيل الخطأ
        //        Console.Error.WriteLine($"Error during deletion: {ex.Message}");
        //        return StatusCode(500, "Internal server error");
        //    }
        //}

        //// دالة لحذف الملفات من القرص
        //private void DeleteFileFromDisk(string filePath)
        //{
        //    if (!string.IsNullOrEmpty(filePath))
        //    {
        //        string fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);
        //        if (System.IO.File.Exists(fullFilePath))
        //        {
        //            try
        //            {
        //                System.IO.File.Delete(fullFilePath);
        //                Console.WriteLine($"File {filePath} deleted successfully.");
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
        //            }
        //        }
        //    }
        //}

        //[HttpGet]
        //public IActionResult Create_services()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public IActionResult Create_services(Service a)
        //{
        //    if (ModelState.IsValid)
        //    {


        //        db.Service_table.Add(a);
        //        db.SaveChanges();
        //        return RedirectToAction("Page_serv");


        //    }

        //    return View();
        //}

        //[HttpGet]
        //public ActionResult Page_serv()
        //{

        //    var list = db.Service_table.Select(x => new Info_services
        //    {
        //        ServiceId = x.ServiceId,

        //        Name = x.Name,
        //        Description = x.Description,



        //    }).ToList();
        //    return View(list);


        //}



        //[HttpPost]
        //public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        //{
        //    if (profilePicture != null && profilePicture.Length > 0)
        //    {
        //        string fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();

        //        if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
        //        {
        //            TempData["ErrorMessage"] = "Invalid file type. Please upload a .jpg, .jpeg, or .png file.";
        //            return RedirectToAction("UpdateProfile");
        //        }

        //        // تحديد مجلد التحميل
        //        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        //        if (!Directory.Exists(directoryPath))
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }

        //        string subFolder = "drivers"; 
        //        string subFolderPath = Path.Combine(directoryPath, subFolder);
        //        if (!Directory.Exists(subFolderPath))
        //        {
        //            Directory.CreateDirectory(subFolderPath);
        //        }

        //        // حذف الصورة القديمة إذا كانت موجودة
        //        var driver = await _userManager.GetUserAsync(User);
        //        if (driver == null) return RedirectToAction("Register");

        //        if (!string.IsNullOrEmpty(driver.ProfilePicture))
        //        {
        //            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", driver.ProfilePicture);
        //            if (System.IO.File.Exists(oldFilePath))
        //            {
        //                System.IO.File.Delete(oldFilePath);
        //            }
        //        }

        //        string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await profilePicture.CopyToAsync(stream);
        //        }

        //        driver.ProfilePicture = Path.Combine("uploads", "drivers", Path.GetFileName(filePath));

        //        var result = await _userManager.UpdateAsync(driver);

        //        if (result.Succeeded)
        //        {
        //            TempData["SuccessMessage"] = "Profile picture updated successfully.";
        //            return RedirectToAction("UpdateProfile");
        //        }

        //        TempData["ErrorMessage"] = "An error occurred while updating your profile picture.";
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "No file selected.";
        //    }

        //    return RedirectToAction("UpdateProfile");
        //}




        //[HttpPost]
        //public async Task<IActionResult> UpdateLicensePicture(IFormFile licensePicture)
        //{
        //    if (licensePicture != null && licensePicture.Length > 0)
        //    {
        //        string fileExtension = Path.GetExtension(licensePicture.FileName).ToLower();

        //        if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
        //        {
        //            TempData["ErrorMessage"] = "Invalid file type. Please upload a .jpg, .jpeg, or .png file.";
        //            return RedirectToAction("UpdateProfile");
        //        }

        //        // تحديد مجلد التحميل
        //        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        //        if (!Directory.Exists(directoryPath))
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }

        //        string subFolder = "licenses"; 
        //        string subFolderPath = Path.Combine(directoryPath, subFolder);
        //        if (!Directory.Exists(subFolderPath))
        //        {
        //            Directory.CreateDirectory(subFolderPath);
        //        }

        //        var driver = await _userManager.GetUserAsync(User);
        //        if (driver == null) return RedirectToAction("Register");

        //        if (!string.IsNullOrEmpty(driver.LicensePicture))
        //        {
        //            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", driver.LicensePicture);
        //            if (System.IO.File.Exists(oldFilePath))
        //            {
        //                System.IO.File.Delete(oldFilePath);
        //            }
        //        }

        //        // إنشاء مسار الملف الجديد
        //        string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

        //        // حفظ الملف الجديد على الخادم
        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await licensePicture.CopyToAsync(stream);
        //        }

        //        // حفظ مسار الصورة في الـ Driver
        //        driver.LicensePicture = Path.Combine("uploads", "licenses", Path.GetFileName(filePath));

        //        var result = await _userManager.UpdateAsync(driver);

        //        if (result.Succeeded)
        //        {
        //            TempData["SuccessMessage"] = "License picture updated successfully.";
        //            return RedirectToAction("UpdateProfile");
        //        }

        //        TempData["ErrorMessage"] = "An error occurred while updating your license picture.";
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "No file selected.";
        //    }

        //    return RedirectToAction("UpdateProfile");
        //}























        //[HttpPost]
        //public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        //{
        //    if (profilePicture != null && profilePicture.Length > 0)
        //    {
        //        string fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();

        //        if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
        //        {
        //            TempData["ErrorMessage"] = "Invalid file type. Please upload a .jpg, .jpeg, or .png file.";
        //            return RedirectToAction("UpdateProfile");
        //        }

        //        // تحديد مجلد التحميل
        //        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        //        if (!Directory.Exists(directoryPath))
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }

        //        // تحديد المجلد الفرعي (إما 'drivers' أو 'licenses')
        //        string subFolder = "drivers"; // لأننا هنا نتعامل مع صورة الملف الشخصي

        //        string subFolderPath = Path.Combine(directoryPath, subFolder);
        //        if (!Directory.Exists(subFolderPath))
        //        {
        //            Directory.CreateDirectory(subFolderPath);
        //        }

        //        // إنشاء مسار الملف
        //        string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

        //        // حفظ الملف على الخادم
        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await profilePicture.CopyToAsync(stream);
        //        }

        //        // حفظ مسار الصورة في الـ Driver
        //        var driver = await _userManager.GetUserAsync(User);
        //        if (driver == null) return RedirectToAction("Register");

        //        driver.ProfilePicture = Path.Combine("uploads", "drivers", Path.GetFileName(filePath));

        //        var result = await _userManager.UpdateAsync(driver);

        //        if (result.Succeeded)
        //        {
        //            TempData["SuccessMessage"] = "Profile picture updated successfully.";
        //            return RedirectToAction("UpdateProfile");
        //        }

        //        TempData["ErrorMessage"] = "An error occurred while updating your profile picture.";
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "No file selected.";
        //    }

        //    return RedirectToAction("UpdateProfile");
        //}
        //[HttpPost]
        //public async Task<IActionResult> UpdateLicensePicture(IFormFile licensePicture)
        //{
        //    if (licensePicture != null && licensePicture.Length > 0)
        //    {
        //        string fileExtension = Path.GetExtension(licensePicture.FileName).ToLower();

        //        if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
        //        {
        //            TempData["ErrorMessage"] = "Invalid file type. Please upload a .jpg, .jpeg, or .png file.";
        //            return RedirectToAction("UpdateProfile");
        //        }

        //        // تحديد مجلد التحميل
        //        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        //        if (!Directory.Exists(directoryPath))
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }

        //        // تحديد المجلد الفرعي (إما 'drivers' أو 'licenses')
        //        string subFolder = "licenses"; // لأننا هنا نتعامل مع صورة الرخصة

        //        string subFolderPath = Path.Combine(directoryPath, subFolder);
        //        if (!Directory.Exists(subFolderPath))
        //        {
        //            Directory.CreateDirectory(subFolderPath);
        //        }

        //        // إنشاء مسار الملف
        //        string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

        //        // حفظ الملف على الخادم
        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await licensePicture.CopyToAsync(stream);
        //        }

        //        // حفظ مسار الصورة في الـ Driver
        //        var driver = await _userManager.GetUserAsync(User);
        //        if (driver == null) return RedirectToAction("Register");

        //        driver.LicensePicture = Path.Combine("uploads", "licenses", Path.GetFileName(filePath));

        //        var result = await _userManager.UpdateAsync(driver);

        //        if (result.Succeeded)
        //        {
        //            TempData["SuccessMessage"] = "License picture updated successfully.";
        //            return RedirectToAction("UpdateProfile");
        //        }

        //        TempData["ErrorMessage"] = "An error occurred while updating your license picture.";
        //    }
        //    else
        //    {
        //        TempData["ErrorMessage"] = "No file selected.";
        //    }

        //    return RedirectToAction("UpdateProfile");
        //}








        //[HttpPost]
        //public async Task<IActionResult> UpdateProfile(DriverViewModel model)
        //{
        //    var request = HttpContext.Request;

        //    // تأكد من وجود ملفات مرفوعة
        //    if (request.Form.Files.Count > 0)
        //    {
        //        foreach (var file in request.Form.Files)
        //        {
        //            if (file.Length > 0)
        //            {
        //                // التحقق من اسم الملف ونوعه
        //                string fileExtension = Path.GetExtension(file.FileName).ToLower();
        //                if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
        //                {
        //                    Console.WriteLine("Error: Invalid file type.");
        //                    TempData["ErrorMessage"] = "Invalid file type. Please upload a .jpg, .jpeg, or .png file.";
        //                    return View(model);  // يجب إظهار رسالة خطأ للمستخدم
        //                }

        //                // تحديد مجلد التحميل
        //                string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        //                if (!Directory.Exists(directoryPath))
        //                {
        //                    Directory.CreateDirectory(directoryPath);
        //                }

        //                // تحديد المجلد الفرعي (إما 'drivers' أو 'licenses')
        //                string subFolder = file.Name == "profilePicture" ? "drivers" : "licenses";

        //                string subFolderPath = Path.Combine(directoryPath, subFolder);
        //                if (!Directory.Exists(subFolderPath))
        //                {
        //                    Directory.CreateDirectory(subFolderPath);
        //                }

        //                // إنشاء مسار الملف
        //                string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

        //                // حفظ الملف على الخادم
        //                using (var stream = new FileStream(filePath, FileMode.Create))
        //                {
        //                    await file.CopyToAsync(stream);
        //                }

        //                // عرض رسالة في الكونسول للتحقق
        //                Console.WriteLine($"File uploaded: {file.FileName} -> {filePath}");

        //                // إذا كانت الصورة هي Profile Picture أو License Picture
        //                if (file.Name == "profilePicture")
        //                {
        //                    model.ProfilePicture = Path.Combine("uploads", "drivers", Path.GetFileName(filePath));  // حفظ مسار الملف في الـ model
        //                }
        //                else if (file.Name == "licensePicture")
        //                {
        //                    model.LicensePicture = Path.Combine("uploads", "licenses", Path.GetFileName(filePath));  // حفظ مسار الملف في الـ model
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // إذا لم يتم رفع أي ملف
        //        Console.WriteLine("No files uploaded.");
        //    }

        //    // تحديث البيانات النصية
        //    var driver = await _userManager.GetUserAsync(User);
        //    if (driver == null) return RedirectToAction("Register");

        //    driver.UserName = model.Name;
        //    driver.Email = model.Email;
        //    driver.PhoneNumber = model.phone;
        //    driver.VehicleType = model.VehicleType;

        //    // تحديث الصور في قاعدة البيانات
        //    driver.ProfilePicture = model.ProfilePicture;
        //    driver.LicensePicture = model.LicensePicture;

        //    var result = await _userManager.UpdateAsync(driver);

        //    if (result.Succeeded)
        //    {
        //        TempData["SuccessMessage"] = "Your profile has been updated successfully.";
        //        Console.WriteLine("Profile updated successfully.");
        //        return RedirectToAction("Dashboard");
        //    }

        //    TempData["ErrorMessage"] = "An error occurred while updating your profile.";
        //    return View(model);
        //}



        //    var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    if (string.IsNullOrEmpty(driverId))
        //    {
        //        return Unauthorized();
        //    }

        //    var driver = await db.Driver_table.FindAsync(int.Parse(driverId));
        //    if (driver == null)
        //    {
        //        return NotFound("Driver not found."); // 404
        //    }

        //    return View(driver);
        //}

        ////[HttpPost]
        ////public async Task<IActionResult> UpdateProfile(Driver driver)
        ////{
        ////    if (!ModelState.IsValid)
        ////    {
        ////        return View(driver);
        ////    }

        ////    var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        ////    if (string.IsNullOrEmpty(driverId))
        ////    {
        ////        return Unauthorized();
        ////    }

        ////    var existingDriver = await db.Driver_table.FirstOrDefaultAsync(d => d.Id == int.Parse(driverId));

        ////    if (existingDriver == null)
        ////    {
        ////        return NotFound("Driver not found.");//404
        ////    }

        ////    existingDriver.UserName = driver.UserName;
        ////    existingDriver.Email = driver.Email;
        ////    existingDriver.PhoneNumber = driver.PhoneNumber;
        ////    existingDriver.VehicleType = driver.VehicleType;
        ////    existingDriver.ProfilePicture = driver.ProfilePicture;
        ////    existingDriver.LicensePicture = driver.LicensePicture;

        ////    existingDriver.CurrentLocation = driver.CurrentLocation;
        ////    existingDriver.Latitude = driver.Latitude;
        ////    existingDriver.Longitude = driver.Longitude;

        ////    try
        ////    {
        ////        db.Update(existingDriver);
        ////        await db.SaveChangesAsync();
        ////        TempData["SuccessMessage"] = "Profile updated successfully!";
        ////        return RedirectToAction("Dashboard");
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        ModelState.AddModelError(string.Empty, "Error saving data: " + ex.Message);//@Html.ValidationSummary in html
        ////        return View(driver);
        ////    }



        //[HttpGet]
        //public IActionResult VerifyOtp()
        //{
        //    string email = HttpContext.Session.GetString("DriverEmail");
        //    if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");

        //    if (!HttpContext.Session.Keys.Contains("OTPCode"))
        //    {
        //        GenerateAndSendOtp(email);
        //    }

        //    int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;
        //    ViewBag.RemainingOtpRequests = remainingOtpRequests;

        //    return View();
        //}
        //[HttpGet]
        //public IActionResult VerifyOtp()
        //{
        //    string email = HttpContext.Session.GetString("DriverEmail");
        //    if (string.IsNullOrEmpty(email)) return RedirectToAction("Register");

        //    // تحقق إذا كانت هذه أول مرة يتم فيها إرسال OTP
        //    if (!HttpContext.Session.Keys.Contains("OtpSent"))
        //    {
        //        // إنشاء وإرسال OTP
        //        GenerateAndSendOtp(email);

        //        // تحديد علامة أن OTP تم إرساله
        //        HttpContext.Session.SetString("OtpSent", "true");

        //        // تخزين وقت إنشاء OTP (اختياري)
        //        HttpContext.Session.SetString("OtpGeneratedTime", DateTime.Now.ToString("o"));
        //    }

        //    int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;
        //    ViewBag.RemainingOtpRequests = remainingOtpRequests;

        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> VerifyOtp(string enteredOtp)
        //{
        //    // استرجاع البريد الإلكتروني من الجلسة
        //    string email = HttpContext.Session.GetString("DriverEmail");
        //    if (string.IsNullOrEmpty(email))
        //    {
        //        TempData["ErrorMessage"] = "Session expired. Please register again.";
        //        return RedirectToAction("Register");
        //    }

        //    // استرجاع الـ OTP المخزن ووقت التوليد
        //    string expectedOtp = HttpContext.Session.GetString("OTPCode");
        //    string generatedTimeString = HttpContext.Session.GetString("OtpGeneratedTime");

        //    // سجل المعلومات
        //    Console.WriteLine($"Expected OTP: {expectedOtp}, Entered OTP: {enteredOtp}");

        //    if (string.IsNullOrEmpty(expectedOtp) || string.IsNullOrEmpty(generatedTimeString))
        //    {
        //        TempData["ErrorMessage"] = "The OTP has expired. Please request a new one.";
        //              return RedirectToAction("VerifyOtp");
        //    }

        //    DateTime generatedTime = DateTime.Parse(generatedTimeString);
        //    double remainingTime = 60 - (DateTime.Now - generatedTime).TotalSeconds;

        //    Console.WriteLine($"Generated Time: {generatedTime}, Remaining Time: {remainingTime}");

        //    if (remainingTime <= 0)
        //    {
        //        TempData["ErrorMessage"] = "The OTP has expired. Please request a new one.";
        //        return RedirectToAction("VerifyOtp");
        //    }

        //    enteredOtp = enteredOtp.Trim();

        //    if (enteredOtp == expectedOtp)
        //    {
        //        var existingDriver = await _userManager.FindByEmailAsync(email);
        //        if (existingDriver != null)
        //        {
        //            TempData["ErrorMessage"] = "This email is already registered. Please log in.";
        //            return RedirectToAction("Register");
        //        }

        //        bool accountCreated = await CreateDriverAccount();
        //        if (accountCreated)
        //        {
        //            var driver = await _userManager.FindByEmailAsync(email);
        //            if (driver != null)
        //            {
        //                var cookieOptions = new CookieOptions
        //                {
        //                    HttpOnly = true,
        //                    Secure = true,
        //                    Expires = DateTime.Now.AddMinutes(10)
        //                };

        //                Console.WriteLine($"Creating cookie for Driver ID: {driver.Id}");
        //                Response.Cookies.Append("DriverAuth", $"{driver.Id}|verified", cookieOptions);

        //                return RedirectToAction("PendingApproval", new { id = driver.Id });
        //            }
        //        }

        //        TempData["ErrorMessage"] = "Error occurred while creating the driver.";
        //        return RedirectToAction("VerifyOtp");
        //    }

        //    TempData["ErrorMessage"] = "Invalid OTP. Please try again.";
        //    return RedirectToAction("VerifyOtp");
        //}


        //[HttpPost]
        //public IActionResult ResendOtp()
        //{
        //    string email = HttpContext.Session.GetString("DriverEmail");
        //    if (string.IsNullOrEmpty(email))
        //        return Json(new { success = false, message = "Session expired. Please log in again." });

        //    // التحقق من وقت إنشاء OTP وصلاحيته
        //    string generatedTimeString = HttpContext.Session.GetString("OtpGeneratedTime");
        //    DateTime? generatedTime = string.IsNullOrEmpty(generatedTimeString) ? (DateTime?)null : DateTime.Parse(generatedTimeString);

        //    // التحقق إذا كان الـ OTP ما زال صالحًا
        //    if (generatedTime != null && (DateTime.Now - generatedTime.Value).TotalSeconds < 60)
        //    {
        //        // إذا كان الـ OTP ما زال صالحًا، إظهار رسالة بأن الانتظار مطلوب
        //        return Json(new { success = false, message = "The OTP is still valid. Please wait before requesting a new one." });
        //    }

        //    // التحقق من وقت الـ cooldown وإذا انتهت المدة يتم إعادة المحاولات
        //    string cooldownTimeString = HttpContext.Session.GetString("CooldownTime");
        //    DateTime? cooldownTime = string.IsNullOrEmpty(cooldownTimeString) ? (DateTime?)null : DateTime.Parse(cooldownTimeString);

        //    if (cooldownTime.HasValue && cooldownTime <= DateTime.Now)
        //    {
        //        // إذا انتهت فترة الـ cooldown، إعادة تعيين المحاولات المتبقية
        //        HttpContext.Session.SetInt32("RemainingOtpRequests", 2);
        //        HttpContext.Session.Remove("CooldownTime");
        //    }

        //    int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;

        //    if (remainingOtpRequests > 0)
        //    {
        //        // إنشاء وإرسال OTP جديد
        //        GenerateAndSendOtp(email);

        //        // تحديث وقت إنشاء OTP في الجلسة
        //        HttpContext.Session.SetString("OtpGeneratedTime", DateTime.Now.ToString("o"));

        //        // تقليل المحاولات المتبقية
        //        remainingOtpRequests--;
        //        HttpContext.Session.SetInt32("RemainingOtpRequests", remainingOtpRequests);

        //        if (remainingOtpRequests <= 0)
        //        {
        //            // إذا تم استنفاد المحاولات، بدء فترة الـ cooldown
        //            HttpContext.Session.SetString("CooldownTime", DateTime.Now.AddMinutes(5).ToString("o"));
        //        }

        //        return Json(new { success = true, message = "A new OTP has been sent.", remainingOtpRequests });
        //    }

        //    // إذا كانت فترة الـ cooldown لا تزال سارية، إظهار الوقت المتبقي
        //    if (cooldownTime.HasValue && cooldownTime > DateTime.Now)
        //    {
        //        TimeSpan remainingCooldown = cooldownTime.Value - DateTime.Now;
        //        return Json(new { success = false, message = $"Please wait {remainingCooldown.Minutes:D2}:{remainingCooldown.Seconds:D2} before resending." });
        //    }

        //    return Json(new { success = false, message = "No attempts left. Please try again later." });
        //}


        /****/
        //[HttpPost]
        //public IActionResult ResendOtp()
        //{
        //    string email = HttpContext.Session.GetString("DriverEmail");
        //    if (string.IsNullOrEmpty(email))
        //        return Json(new { success = false, message = "Session expired. Please log in again." });

        //    // التحقق من وقت إنشاء OTP وصلاحيته
        //    string generatedTimeString = HttpContext.Session.GetString("OtpGeneratedTime");
        //    DateTime? generatedTime = string.IsNullOrEmpty(generatedTimeString) ? (DateTime?)null : DateTime.Parse(generatedTimeString);

        //    if (generatedTime != null && (DateTime.Now - generatedTime.Value).TotalSeconds < 60)
        //    {
        //        return Json(new { success = false, message = "The OTP is still valid. Please wait before requesting a new one." });
        //    }

        //    // التحقق من وقت الـ cooldown وإذا انتهت المدة يتم إعادة المحاولات
        //    string cooldownTimeString = HttpContext.Session.GetString("CooldownTime");
        //    DateTime? cooldownTime = string.IsNullOrEmpty(cooldownTimeString) ? (DateTime?)null : DateTime.Parse(cooldownTimeString);

        //    if (cooldownTime.HasValue && cooldownTime <= DateTime.Now)
        //    {
        //        // إذا انتهت فترة الـ cooldown، إعادة تعيين المحاولات المتبقية
        //        HttpContext.Session.SetInt32("RemainingOtpRequests", 2);
        //        HttpContext.Session.Remove("CooldownTime");
        //    }

        //    int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;

        //    if (remainingOtpRequests > 0)
        //    {
        //        // إنشاء وإرسال OTP جديد
        //        GenerateAndSendOtp(email);

        //        // تقليل المحاولات المتبقية
        //        remainingOtpRequests--;
        //        HttpContext.Session.SetInt32("RemainingOtpRequests", remainingOtpRequests);

        //        if (remainingOtpRequests <= 0)
        //        {
        //            // إذا تم استنفاد المحاولات، بدء فترة الـ cooldown
        //            HttpContext.Session.SetString("CooldownTime", DateTime.Now.AddMinutes(10).ToString("o"));
        //        }

        //        return Json(new { success = true, message = "A new OTP has been sent.", remainingOtpRequests });
        //    }

        //    // إذا كانت فترة الـ cooldown لا تزال سارية، إظهار الوقت المتبقي
        //    if (cooldownTime.HasValue && cooldownTime > DateTime.Now)
        //    {
        //        TimeSpan remainingCooldown = cooldownTime.Value - DateTime.Now;
        //        return Json(new { success = false, message = $"Please wait {remainingCooldown.Minutes:D2}:{remainingCooldown.Seconds:D2} (mm:ss) before resending." });
        //    }

        //    return Json(new { success = false, message = "No attempts left. Please try again later." });
        //}



        //[HttpPost]
        //public IActionResult ResendOtp()
        //{
        //    string email = HttpContext.Session.GetString("DriverEmail");
        //    if (string.IsNullOrEmpty(email))
        //        return Json(new { success = false, message = "Session expired. Please log in again." });

        //    string cooldownTimeString = HttpContext.Session.GetString("CooldownTime");
        //    DateTime? cooldownTime = string.IsNullOrEmpty(cooldownTimeString) ? (DateTime?)null : DateTime.Parse(cooldownTimeString);

        //    if (cooldownTime.HasValue && cooldownTime <= DateTime.Now)
        //    {
        //        HttpContext.Session.SetInt32("RemainingOtpRequests", 2);
        //        HttpContext.Session.Remove("CooldownTime");
        //    }

        //    int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;

        //    if (remainingOtpRequests > 0)
        //    {
        //        GenerateAndSendOtp(email);

        //        remainingOtpRequests--;
        //        HttpContext.Session.SetInt32("RemainingOtpRequests", remainingOtpRequests);

        //        if (remainingOtpRequests <= 0)
        //        {
        //            HttpContext.Session.SetString("CooldownTime", DateTime.Now.AddSeconds(10).ToString("o"));
        //        }

        //        return Json(new { success = true, message = "A new OTP has been sent.", remainingOtpRequests });
        //    }

        //    if (cooldownTime.HasValue && cooldownTime > DateTime.Now)
        //    {
        //        double remainingCooldown = (cooldownTime.Value - DateTime.Now).TotalSeconds;
        //        return Json(new { success = false, message = $"Please wait {remainingCooldown:F0} seconds before resending." });
        //    }

        //    return Json(new { success = false, message = "No attempts left. Please try again later." });
        //}

        //[HttpGet]
        //public IActionResult GetOtpStatus()
        //{
        //    double remainingOtpValidity = 0;
        //    if (!string.IsNullOrEmpty(HttpContext.Session.GetString("OtpGeneratedTime")))
        //    {
        //        DateTime? generatedTime = DateTime.Parse(HttpContext.Session.GetString("OtpGeneratedTime"));
        //        remainingOtpValidity = (generatedTime != null) ? 60 - (DateTime.Now - generatedTime.Value).TotalSeconds : 0;
        //    }

        //    int remainingOtpRequests = HttpContext.Session.GetInt32("RemainingOtpRequests") ?? 2;

        //    string cooldownTimeString = HttpContext.Session.GetString("CooldownTime");
        //    DateTime? cooldownTime = string.IsNullOrEmpty(cooldownTimeString) ? (DateTime?)null : DateTime.Parse(cooldownTimeString);

        //    if (cooldownTime.HasValue && cooldownTime <= DateTime.Now)
        //    {
        //        remainingOtpRequests = 2;
        //        HttpContext.Session.SetInt32("RemainingOtpRequests", remainingOtpRequests);
        //        HttpContext.Session.Remove("CooldownTime");
        //    }

        //    double remainingCooldown = cooldownTime.HasValue && cooldownTime > DateTime.Now ? (cooldownTime.Value - DateTime.Now).TotalSeconds : 0;

        //    return Json(new
        //    {
        //        remainingOtpValidity = remainingOtpValidity > 0 ? remainingOtpValidity : 0,
        //        remainingOtpRequests,
        //        remainingCooldown = remainingCooldown > 0 ? remainingCooldown : 0
        //    });
        //}




        //[HttpPost]
        //public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);

        //        if (driver == null)
        //        {
        //            ModelState.AddModelError("", "This email is not registered in our system.");
        //            return View(model);
        //        }

        //        // إرسال البريد الإلكتروني مباشرة
        //        var token = await _userManager.GeneratePasswordResetTokenAsync(driver);
        //        var resetLink = Url.Action("ResetPassword", "Home", new { token, email = model.Email }, Request.Scheme);

        //        await _emailSender.SendEmailAsync(model.Email, "Password Reset",
        //            $"Please reset your password by clicking <a href='{resetLink}'>here</a>");

        //        // إظهار رسالة تم إرسال البريد الإلكتروني بنجاح في نفس الصفحة
        //        ViewBag.Message = "A password reset email has been sent to your email address.";

        //        return View(model); // عرض نفس الصفحة مع الرسالة
        //    }

        //    return View(model);
        //}
        //[HttpPost]
        //public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // البحث عن السائق باستخدام البريد الإلكتروني
        //        var driver = await _userManager.FindByEmailAsync(model.Email);

        //        // البحث عن الادمن باستخدام البريد الإلكتروني
        //        var admin = await _adminUserManager.FindByEmailAsync(model.Email);

        //        // إذا لم يتم العثور على المستخدم في أي من النوعين، نعرض رسالة خطأ
        //        if (driver == null && admin == null)
        //        {
        //            ModelState.AddModelError("", "This email is not registered in our system.");
        //            return View(model);
        //        }

        //        // إذا كان المستخدم من نوع Driver
        //        if (driver != null)
        //        {
        //            // إرسال البريد الإلكتروني مع رابط إعادة تعيين كلمة المرور باستخدام التوكن
        //            var token = await _userManager.GeneratePasswordResetTokenAsync(driver);
        //            var resetLink = Url.Action("ResetPassword", "Home", new { token, email = model.Email }, Request.Scheme);

        //            await _emailSender.SendEmailAsync(model.Email, "Password Reset",
        //                $"Please reset your password by clicking <a href='{resetLink}'>here</a>");

        //            ViewBag.Message = "A password reset email has been sent to your email address.";
        //        }
        //        // إذا كان المستخدم من نوع Admin
        //        else if (admin != null)
        //        {
        //            // تحويل الإداري إلى صفحة تعديل كلمة المرور مباشرة بدون توكن
        //            return RedirectToAction("ResetPassword", new { email = model.Email });
        //        }

        //        return View(model); // عرض نفس الصفحة مع الرسالة
        //    }

        //    return View(model);
        //}
        //[HttpPost]
        //public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);
        //        var admin = await _adminUserManager.FindByEmailAsync(model.Email);

        //        if (driver == null && admin == null)
        //        {
        //            ModelState.AddModelError("", "This email is not registered in our system.");
        //            return View(model);
        //        }

        //        if (driver != null)
        //        {
        //            // تحديث Security Stamp للسائق لضمان انتهاء صلاحية التوكنات القديمة
        //            await _userManager.UpdateSecurityStampAsync(driver);

        //            // توليد التوكن الجديد
        //            var token = await _userManager.GeneratePasswordResetTokenAsync(driver);
        //            var resetLink = Url.Action("ResetPassword", "Home", new { token, email = model.Email }, Request.Scheme);

        //            await _emailSender.SendEmailAsync(model.Email, "Password Reset",
        //                $"Please reset your password by clicking <a href='{resetLink}'>here</a>");

        //            ViewBag.Message = "A password reset email has been sent to your email address.";
        //        }
        //        else if (admin != null)
        //        {
        //            return RedirectToAction("ResetPassword", new { email = model.Email });
        //        }

        //        return View(model);
        //    }

        //    return View(model);
        //}

        //[HttpPost]
        //public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);
        //        var admin = await _adminUserManager.FindByEmailAsync(model.Email);

        //        if (driver == null && admin == null)
        //        {
        //            ModelState.AddModelError("", "This email is not registered in our system.");
        //            return View(model);
        //        }

        //        if (driver != null)
        //        {
        //            // تحديث Security Stamp للسائق لضمان انتهاء صلاحية التوكنات القديمة
        //            await _userManager.UpdateSecurityStampAsync(driver);

        //            var token = await _userManager.GeneratePasswordResetTokenAsync(driver);
        //            var resetLink = Url.Action("ResetPassword", "Home", new { token, email = model.Email }, Request.Scheme);

        //            await _emailSender.SendEmailAsync(model.Email, "Password Reset",
        //                $"Please reset your password by clicking <a href='{resetLink}'>here</a>");

        //            ViewBag.Message = "A password reset email has been sent to your email address.";
        //        }
        //        else if (admin != null)
        //        {
        //            // إنشاء رابط سري باستخدام GUID
        //            var secretKey = Guid.NewGuid().ToString();
        //            TempData["SecretKey"] = secretKey; // تخزين المفتاح مؤقتًا للتحقق لاحقًا

        //            var resetLink = Url.Action("ResetPassword", "Home", new { secretKey, email = model.Email }, Request.Scheme);
        //            await _emailSender.SendEmailAsync(model.Email, "Admin Password Reset",
        //                $"Please reset your admin password by clicking <a href='{resetLink}'>here</a>");

        //            ViewBag.Message = "A password reset email has been sent to your email address.";
        //        }

        //        return View(model);
        //    }

        //    return View(model);
        //}


        //[HttpGet]


        //public async Task<IActionResult> ResetPassword(string email, string token)
        //{
        //    if (string.IsNullOrEmpty(email))
        //    {
        //        TempData["ErrorMessage"] = "Oops! It seems like the link has expired or is no longer valid. Please request a new password reset link.";
        //        return RedirectToAction("Errors", "Home");
        //    }

        //    var driver = await _userManager.FindByEmailAsync(email);
        //    var admin = await _adminUserManager.FindByEmailAsync(email);

        //    if (driver == null && admin == null)
        //    {
        //        TempData["ErrorMessage"] = "User not found. Please check your email address and try again.";
        //        return RedirectToAction("Errors", "Home");
        //    }

        //    if (driver != null)
        //    {
        //        var isTokenValid = await _userManager.VerifyUserTokenAsync(driver, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token);

        //        if (!isTokenValid)
        //        {
        //            TempData["ErrorMessage"] = "Oops! It seems like the link has expired or is no longer valid. Please request a new password reset link.";
        //            return RedirectToAction("Errors", "Home");
        //        }

        //        return View(new ResetPasswordViewModel { Email = email, Token = token });
        //    }
        //    else if (admin != null)
        //    {
        //        // Admin doesn't need a token. Allow direct password reset.
        //        return View(new ResetPasswordViewModel { Email = email });
        //    }

        //    // If no user is found, redirect to error page.
        //    TempData["ErrorMessage"] = "User not found. Please check your email address and try again.";
        //    return RedirectToAction("Errors", "Home");
        //}


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);
        //        var admin = await _adminUserManager.FindByEmailAsync(model.Email);

        //        if (driver != null)
        //        {
        //            // If it's a driver, verify the token before resetting password.
        //            var isTokenValid = await _userManager.VerifyUserTokenAsync(driver, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", model.Token);

        //            if (!isTokenValid)
        //            {
        //                ModelState.AddModelError("", "The password reset token has expired or is invalid. Please request a new token.");
        //                return View(model);
        //            }

        //            var result = await _userManager.ResetPasswordAsync(driver, model.Token, model.NewPassword);

        //            if (result.Succeeded)
        //            {
        //                var emailSubject = "Password Reset Successful";
        //                var emailBody = "Your password has been successfully changed. If you didn't request this change, please contact us immediately.";
        //                await _emailSender.SendEmailAsync(driver.Email, emailSubject, emailBody);

        //                return RedirectToAction("Login");
        //            }

        //            foreach (var error in result.Errors)
        //            {
        //                ModelState.AddModelError("", error.Description);
        //            }
        //        }
        //        else if (admin != null)
        //        {
        //            // If it's an admin, reset the password directly without a token.
        //            var passwordHasher = _adminUserManager.PasswordHasher;
        //            var hashedPassword = passwordHasher.HashPassword(admin, model.NewPassword);
        //            admin.PasswordHash = hashedPassword;

        //            var result = await _adminUserManager.UpdateAsync(admin);

        //            if (result.Succeeded)
        //            {
        //                ViewBag.Message = "Your password has been updated successfully.";
        //                return RedirectToAction("Login");
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("", "An error occurred while updating your password.");
        //            }
        //        }

        //        ModelState.AddModelError("", "User not found.");
        //    }

        //    return View(model);
        //}
        //[HttpPost]
        //public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);
        //        var admin = await _adminUserManager.FindByEmailAsync(model.Email);

        //        if (driver == null && admin == null)
        //        {
        //            ModelState.AddModelError("", "This email is not registered in our system.");
        //            return View(model);
        //        }

        //        if (driver != null)
        //        {
        //            // تحديث Security Stamp للسائق لضمان انتهاء صلاحية التوكنات القديمة
        //            await _userManager.UpdateSecurityStampAsync(driver);

        //            var token = await _userManager.GeneratePasswordResetTokenAsync(driver);
        //            var resetLink = Url.Action("ResetPassword", "Home", new { token, email = model.Email }, Request.Scheme);

        //            await _emailSender.SendEmailAsync(model.Email, "Password Reset",
        //                $"Please reset your password by clicking <a href='{resetLink}'>here</a>");

        //            ViewBag.Message = "A password reset email has been sent to your email address.";
        //        }
        //        else if (admin != null)
        //        {
        //            // تحديث Security Stamp للإدمن لضمان انتهاء صلاحية الروابط القديمة
        //            await _adminUserManager.UpdateSecurityStampAsync(admin);

        //            // إنشاء رابط يحتوي على Security Stamp
        //            var secretKey = Guid.NewGuid().ToString();
        //            var resetLink = Url.Action("ResetPassword", "Home", new { secretKey, email = model.Email, stamp = admin.SecurityStamp }, Request.Scheme);

        //            await _emailSender.SendEmailAsync(model.Email, "Admin Password Reset",
        //                $"Please reset your admin password by clicking <a href='{resetLink}'>here</a>");

        //            ViewBag.Message = "A password reset email has been sent to your email address.";
        //        }

        //        return View(model);
        //    }

        //    return View(model);
        //}


        //public async Task<IActionResult> ResetPassword(string email, string token)
        //{
        //    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        //    {
        //        TempData["ErrorMessage"] = "Oops! It seems like the link has expired or is no longer valid. Please request a new password reset link.";
        //        return RedirectToAction("Errors", "Home");
        //    }

        //    var driver = await _userManager.FindByEmailAsync(email);
        //    if (driver == null)
        //    {
        //        TempData["ErrorMessage"] = "User not found. Please check your email address and try again.";
        //        return RedirectToAction("Errors", "Home");
        //    }

        //    var isTokenValid = await _userManager.VerifyUserTokenAsync(driver, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token);

        //    if (!isTokenValid)
        //    {
        //        TempData["ErrorMessage"] = "Oops! It seems like the link has expired or is no longer valid. Please request a new password reset link.";
        //        return RedirectToAction("Errors", "Home");
        //    }

        //    return View(new ResetPasswordViewModel { Email = email, Token = token });
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);

        //        if (driver != null)
        //        {
        //            var isTokenValid = await _userManager.VerifyUserTokenAsync(driver, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", model.Token);

        //            if (!isTokenValid)
        //            {
        //                ModelState.AddModelError("", "The password reset token has expired or is invalid. Please request a new token.");
        //                return RedirectToAction("Errors", "Home");
        //            }

        //            var result = await _userManager.ResetPasswordAsync(driver, model.Token, model.NewPassword);

        //            if (result.Succeeded)
        //            {
        //                var emailSubject = "Password Reset Successful";
        //                var emailBody = "Your password has been successfully changed. If you didn't request this change, please contact us immediately.";
        //                await _emailSender.SendEmailAsync(driver.Email, emailSubject, emailBody);

        //                return RedirectToAction("Login");
        //            }

        //            foreach (var error in result.Errors)
        //            {
        //                ModelState.AddModelError("", error.Description);
        //            }
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", "Email not found.");
        //        }
        //    }

        //    return View(model);
        //}

        /***********************/



        //public async Task<IActionResult> GetCustomerDetails(int orderId)
        //{
        //    var order = await db.Order_table
        //        .Include(o => o.Customer_orders)
        //        .ThenInclude(co => co.Customers)
        //        .FirstOrDefaultAsync(o => o.OrderId == orderId);

        //    if (order == null || order.Customer_orders.Count == 0)
        //        return NotFound("Customer details not found.");//404

        //    var customer = order.Customer_orders.FirstOrDefault()?.Customers;
        //    return PartialView("GetCustomerDetails", customer);
        //}






        //[HttpPost]
        //public IActionResult UpdateAvailability(bool isAvailable)
        //{
        //    var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var driver = db.Driver_table.FirstOrDefault(d => d.Id == int.Parse(driverId));

        //    if (driver != null)
        //    {
        //        driver.IsAvailable = isAvailable;
        //        db.SaveChanges();
        //    }

        //    return RedirectToAction("Dashboard", "Home");
        //}





        /*******************/
        //[HttpPost]
        //public async Task<IActionResult> MarkOrderOnTheWay(int orderId)
        //{
        //    var orderDriver = await db.Order_Driver_table
        //        .Include(od => od.Orders)
        //        .Include(od => od.Drivers)
        //        .FirstOrDefaultAsync(od => od.OrderId == orderId);

        //    if (orderDriver == null)
        //        return NotFound("Order not found.");//404

        //    orderDriver.Orders.Status = "On the Way";

        //    orderDriver.Drivers.Status = "Busy";

        //    db.Update(orderDriver);
        //    await db.SaveChangesAsync();

        //    return RedirectToAction("Dashboard");
        //}


        //[HttpPost]
        //public async Task<IActionResult> MarkOrderCompleted(int orderId)
        //{
        //    var orderDriver = await db.Order_Driver_table
        //        .Include(od => od.Orders)
        //        .Include(od => od.Drivers)
        //        .FirstOrDefaultAsync(od => od.OrderId == orderId);

        //    if (orderDriver == null)
        //        return NotFound("Order not found.");//404

        //    orderDriver.Orders.Status = "Completed";

        //    orderDriver.Drivers.Status = "I'm available to take another order";

        //    db.Update(orderDriver);
        //    await db.SaveChangesAsync();

        //    return RedirectToAction("Dashboard");
        //}



        /******************************************/

        //[HttpGet]
        //public async Task<IActionResult> UpdateProfile()
        //{
        //    var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    if (string.IsNullOrEmpty(driverId))
        //    {
        //        return Unauthorized();
        //    }

        //    var driver = await db.Driver_table.FindAsync(int.Parse(driverId));
        //    if (driver == null)
        //    {
        //        return NotFound("Driver not found."); // 404
        //    }

        //    // تحويل كائن Driver إلى DriverViewModel
        //    var model = new DriverViewModel
        //    {
        //        Name = driver.UserName,
        //        Email = driver.Email,
        //        phone = driver.PhoneNumber,
        //        VehicleType = driver.VehicleType,
        //        ProfilePicture = driver.ProfilePicture,
        //        LicensePicture = driver.LicensePicture
        //    };

        //    return View(model);
        //}
        /// <summary>
        /// //////tyui
        /// </summary>
        /// <returns></returns>


        //[HttpGet]
        //public async Task<IActionResult> UpdateProfile()
        //{

        //    var driver = await _userManager.GetUserAsync(User); 
        //    if (driver == null) return NotFound();

        //    var model = new DriverViewModel
        //    {
        //        Name = driver.UserName,
        //        Email = driver.Email,
        //        phone = driver.PhoneNumber,
        //        VehicleType = driver.VehicleType,
        //    CurrentLocation=driver.CurrentLocation,
        //        Latitude = driver.Latitude,              
        //        Longitude = driver.Longitude,
        //        ProfilePicture =driver.ProfilePicture,
        //    LicensePicture=driver.LicensePicture
        //    };

        //    return View(model);
        //}
        //[HttpPost]
        //public async Task<IActionResult> UpdateProfile(DriverViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model); 
        //    }

        //    var driver = await _userManager.GetUserAsync(User); 
        //    if (driver == null) return NotFound();

        //    driver.UserName = model.Name;
        //    driver.Email = model.Email;
        //    driver.PhoneNumber = model.phone;
        //    driver.VehicleType = model.VehicleType;
        //    driver.CurrentLocation = model.CurrentLocation;
        //    driver.Latitude = model.Latitude;               
        //    driver.Longitude = model.Longitude;
        //    var result = await _userManager.UpdateAsync(driver);

        //    if (result.Succeeded)
        //    {
        //        return RedirectToAction("Dashboard");
        //    }

        //    foreach (var error in result.Errors)
        //    {
        //        ModelState.AddModelError(string.Empty, error.Description);
        //    }

        //    return View(model); 
        //}
        /*********************/



        //[HttpPost]
        //public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        //{
        //    if (profilePicture != null && profilePicture.Length > 0)
        //    {
        //        string fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();
        //        if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
        //        {
        //            TempData["ErrorMessage"] = "Invalid file type. Please upload a .jpg, .jpeg, or .png file.";
        //            return Json(new { success = false, message = "Invalid file type" });
        //        }

        //        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        //        if (!Directory.Exists(directoryPath))
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }

        //        string subFolder = "drivers";
        //        string subFolderPath = Path.Combine(directoryPath, subFolder);
        //        if (!Directory.Exists(subFolderPath))
        //        {
        //            Directory.CreateDirectory(subFolderPath);
        //        }

        //        var driver = await _userManager.GetUserAsync(User);
        //        if (driver == null) return Json(new { success = false, message = "User not found" });

        //        if (!string.IsNullOrEmpty(driver.ProfilePicture))
        //        {
        //            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", driver.ProfilePicture);
        //            if (System.IO.File.Exists(oldFilePath))
        //            {
        //                System.IO.File.Delete(oldFilePath);
        //            }
        //        }

        //        string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");
        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await profilePicture.CopyToAsync(stream);
        //        }

        //        driver.ProfilePicture = Path.Combine("uploads", "drivers", Path.GetFileName(filePath));

        //        var result = await _userManager.UpdateAsync(driver);

        //        if (result.Succeeded)
        //        {
        //            return Json(new { success = true, imageUrl = "/" + driver.ProfilePicture });
        //        }
        //        return Json(new { success = false, message = "An error occurred while updating your profile picture." });
        //    }

        //    return Json(new { success = false, message = "No file selected." });
        //}
        //[HttpPost]
        //public async Task<IActionResult> UpdateLicensePicture(IFormFile licensePicture)
        //{
        //    if (licensePicture != null && licensePicture.Length > 0)
        //    {
        //        string fileExtension = Path.GetExtension(licensePicture.FileName).ToLower();

        //        if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
        //        {
        //            return Json(new { success = false, message = "Invalid file type. Please upload a .jpg, .jpeg, or .png file." });
        //        }

        //        // تحديد مجلد التحميل
        //        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        //        if (!Directory.Exists(directoryPath))
        //        {
        //            Directory.CreateDirectory(directoryPath);
        //        }

        //        string subFolder = "licenses";
        //        string subFolderPath = Path.Combine(directoryPath, subFolder);
        //        if (!Directory.Exists(subFolderPath))
        //        {
        //            Directory.CreateDirectory(subFolderPath);
        //        }

        //        var driver = await _userManager.GetUserAsync(User);
        //        if (driver == null)
        //        {
        //            return Json(new { success = false, message = "Driver not found." });
        //        }

        //        if (!string.IsNullOrEmpty(driver.LicensePicture))
        //        {
        //            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", driver.LicensePicture);
        //            if (System.IO.File.Exists(oldFilePath))
        //            {
        //                System.IO.File.Delete(oldFilePath);
        //            }
        //        }

        //        // إنشاء مسار الملف الجديد
        //        string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

        //        // حفظ الملف الجديد على الخادم
        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await licensePicture.CopyToAsync(stream);
        //        }

        //        // حفظ مسار الصورة في الـ Driver
        //        driver.LicensePicture = Path.Combine("uploads", "licenses", Path.GetFileName(filePath));

        //        var result = await _userManager.UpdateAsync(driver);

        //        if (result.Succeeded)
        //        {
        //            return Json(new { success = true, imageUrl = "/" + driver.LicensePicture });
        //        }

        //        return Json(new { success = false, message = "An error occurred while updating your license picture." });
        //    }
        //    else
        //    {
        //        return Json(new { success = false, message = "No file selected." });
        //    }
        //}







        ///***/
        //[HttpPost]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var driver = db.Driver_table.Find(id);

        //    if (driver == null)
        //    {
        //        return NotFound();
        //    }

        //    // حذف السائق من قاعدة البيانات
        //    db.Driver_table.Remove(driver);
        //    await db.SaveChangesAsync();

        //    // تحديث الكاش
        //    var updatedDrivers = db.Driver_table
        //                           .Select(d => new DriverViewModel
        //                           {
        //                               DriverId = d.Id,
        //                               Name = d.UserName,
        //                               Email = d.Email,
        //                               phone = d.PhoneNumber,
        //                               VehicleType = d.VehicleType,
        //                               TrackingDriver = d.TrackingDriver,
        //                               LicensePicture = d.LicensePicture,
        //                               ProfilePicture = d.ProfilePicture,
        //                               workPermitPicture = d.workPermitPicture
        //                           }).ToList();

        //    _cache.Set("drivers", updatedDrivers, TimeSpan.FromMinutes(5));

        //    // إعادة البيانات بعد الحذف
        //    return PartialView("GetDriver", updatedDrivers);
        //}







        //[HttpPost]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var driver = db.Driver_table.Find(id);

        //    if (driver == null)
        //    {
        //        return NotFound();
        //    }

        //    // مسار الصور على القرص
        //    string profilePicturePath = driver.ProfilePicture;
        //    string licensePicturePath = driver.LicensePicture;
        //    string workPermitPicturePath = driver.workPermitPicture;

        //    // حذف السائق من قاعدة البيانات
        //    db.Driver_table.Remove(driver);
        //    await db.SaveChangesAsync();

        //    // حذف الصور من القرص الصلب إذا كانت موجودة
        //    DeleteFileFromDisk(profilePicturePath);
        //    DeleteFileFromDisk(licensePicturePath);
        //    DeleteFileFromDisk(workPermitPicturePath);

        //    // تحديث الكاش
        //    var updatedDrivers = db.Driver_table
        //                           .Select(d => new DriverViewModel
        //                           {
        //                               DriverId = d.Id,
        //                               Name = d.UserName,
        //                               Email = d.Email,
        //                               phone = d.PhoneNumber,
        //                               VehicleType = d.VehicleType,
        //                               TrackingDriver = d.TrackingDriver,
        //                               LicensePicture = d.LicensePicture,
        //                               ProfilePicture = d.ProfilePicture,
        //                               workPermitPicture = d.workPermitPicture
        //                           }).ToList();

        //    _cache.Set("drivers", updatedDrivers, TimeSpan.FromMinutes(5));

        //    // إعادة البيانات بعد الحذف
        //    return PartialView("GetDriver", updatedDrivers);
        //}






        //[HttpGet]
        //public async Task<IActionResult> Dashboard()
        //{
        //    var driverIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    if (string.IsNullOrEmpty(driverIdString))
        //    {
        //        // إذا كانت القيمة فارغة أو null
        //        return RedirectToAction("Login", "Home");
        //    }

        //    int driverId;
        //    if (!int.TryParse(driverIdString, out driverId))
        //    {
        //        // إذا لم تتمكن من تحويل القيمة إلى int
        //        return RedirectToAction("Login", "Home");
        //    }

        //    var driver = await db.Driver_table
        //        .Include(d => d.Order_Drivers)
        //            .ThenInclude(od => od.Orders)
        //            .ThenInclude(o => o.Customer_orders)
        //            .ThenInclude(co => co.Customers)
        //        .Where(d => d.Id == driverId)
        //        .FirstOrDefaultAsync();

        //    if (driver == null)
        //    {
        //        return NotFound("Driver not found.");
        //    }

        //    if (!driver.IsDriver)
        //    {
        //        return Unauthorized("You are not an approved driver.");
        //    }

        //    ViewBag.IsDriver = true;
        //    ViewBag.IsAdmin = false;
        //    return View(driver);
        //}

        //public async Task<IActionResult> Dashboard()


        //{

        //    var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        //    var driver = await db.Driver_table
        //        .Include(d => d.Order_Drivers)
        //            .ThenInclude(od => od.Orders)
        //            .ThenInclude(o => o.Customer_orders)
        //            .ThenInclude(co => co.Customers)
        //        .Where(d => d.Id == driverId)
        //        .FirstOrDefaultAsync();

        //    if (driver == null)
        //    {
        //        return NotFound("Driver not found.");
        //    }

        //    if (!driver.IsDriver)
        //    {
        //        return Unauthorized("You are not an approved driver.");
        //    }
        //    ViewBag.IsDriver = true;
        //    ViewBag.IsAdmin = false;
        //    return View(driver);
        //}




        //[HttpPost]
        //public IActionResult SaveETA(int orderId, string eta)
        //{
        //    var order = db.Order_table.FirstOrDefault(o => o.OrderId == orderId);
        //    if (order == null)
        //    {
        //        return NotFound("Order not found.");
        //    }

        //    if (string.IsNullOrEmpty(eta))
        //    {
        //        return BadRequest("ETA cannot be empty.");
        //    }

        //    order.ETA = eta; 
        //    db.SaveChanges(); 

        //    return RedirectToAction("DashboardAdmin"); 
        //}
        //[HttpGet]
        //public IActionResult IndexPartialView(string status, string trackingNumber)
        //{


        //    //var referer = Request.Headers["Referer"].ToString();
        //    //if (!referer.Contains("/DashboardAdmin"))
        //    //{
        //    //    return RedirectToAction("DashboardAdmin", "Home");
        //    //}
        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }
        //    var ordersQuery = db.Order_table.Include(o => o.Customer_orders)
        //                                     .ThenInclude(co => co.Customers)
        //                                     .AsQueryable();

        //    ViewBag.NewRequestCount = ordersQuery.Count(o => o.Status == "New Request");

        //    if (string.IsNullOrEmpty(status) || status == "All")
        //    {

        //    }
        //    else
        //    {
        //        ordersQuery = ordersQuery.Where(o => o.Status == status);
        //    }
        //    if (!string.IsNullOrEmpty(trackingNumber) && trackingNumber != "TRK-")
        //    {
        //        ordersQuery = ordersQuery.Where(o => o.TrackingNumber == trackingNumber);
        //    }


        //    var orders = ordersQuery.ToList();

        //    return PartialView("IndexPartialView", orders);
        //}

        //[HttpPost]
        //public IActionResult SaveETA(int orderId, string eta)
        //{
        //    var order = db.Order_table.FirstOrDefault(o => o.OrderId == orderId);
        //    if (order == null)
        //    {
        //        return Json(new { success = false, message = "Order not found." });
        //    }

        //    if (string.IsNullOrEmpty(eta))
        //    {
        //        return Json(new { success = false, message = "ETA cannot be empty." });
        //    }

        //    order.ETA = eta;
        //    db.SaveChanges();

        //    return Json(new { success = true, orderId = orderId, eta = eta });
        //}




        //[HttpGet]

        //public async Task<IActionResult> DashboardAdmin(string section = "default")
        //{
        //    if (!string.IsNullOrEmpty(section))
        //    {
        //        ViewBag.Section = section;
        //    }
        //    var adminUser = await _adminUserManager.GetUserAsync(User);

        //    if (adminUser == null)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }

        //    ViewBag.AdminName = adminUser.UserName;

        //    ViewBag.IsDriver = false;
        //    ViewBag.IsAdmin = true;


        //    return View();
        //}


        //[HttpGet]
        //public IActionResult GetPartialView(string section)
        //{

        //    switch (section)
        //    {
        //        case "drivers-management":
        //            return RedirectToAction("GetDriver");
        //        case "order-management":
        //            var ordersQuery = db.Order_table.Include(o => o.Customer_orders)
        //                                             .ThenInclude(co => co.Customers)
        //                                             .AsQueryable();

        //            ViewBag.NewRequestCount = ordersQuery.Count(o => o.Status == "New Request");
        //            var orders = ordersQuery.ToList();

        //            return PartialView("IndexPartialView", orders);
        //        default:
        //            return PartialView("_Profile");
        //    }


        //}
        /****/


        //[HttpGet]
        //public IActionResult AssignDriver(int orderId)
        //{


        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }
        //    var order = db.Order_table.FirstOrDefault(o => o.OrderId == orderId);

        //    if (order == null)
        //    {
        //        ViewData["ErrorMessage"] = "Order not found.";
        //        return RedirectToAction("Index");
        //    }

        //    // عرض جميع السائقين المتوافقين مع نوع المركبة
        //    var availableDrivers = db.Driver_table
        //                              .Where(d => d.VehicleType == order.VehicleType)
        //                              .Select(d => new DriverViewModel
        //                              {
        //                                  DriverId = d.Id,
        //                                  Name = d.UserName,
        //                                  Email = d.Email,
        //                                  phone = d.PhoneNumber,
        //                                  VehicleType = d.VehicleType,
        //                                  isAvailable = d.IsAvailable,
        //                                  ProfilePicture = d.ProfilePicture,
        //                                  LicensePicture = d.LicensePicture
        //                              })
        //                              .ToList();

        //    ViewBag.OrderId = orderId;

        //    return View(availableDrivers);
        //}
        //[HttpPost]
        //public async Task<IActionResult> AssignDriver(int orderId, string driverId)
        //{
        //    if (string.IsNullOrEmpty(driverId) || !int.TryParse(driverId, out int driverIdInt))
        //    {
        //        return BadRequest("Invalid driver ID.");
        //    }

        //    var order = db.Order_table.FirstOrDefault(o => o.OrderId == orderId);
        //    if (order == null)
        //    {
        //        return NotFound("Order not found.");
        //    }

        //    var driver = await _userManager.FindByIdAsync(driverId);
        //    if (driver == null)
        //    {
        //        return NotFound("Driver not found.");
        //    }

        //    order.Status = "In Progress";
        //    db.SaveChanges();

        //    var orderDriver = new Order_Driver
        //    {
        //        OrderId = orderId,
        //        DriverId = driverIdInt
        //    };

        //    db.Order_Driver_table.Add(orderDriver);
        //    db.SaveChanges();


        //    return RedirectToAction("Details", new { orderId = order.OrderId });
        //}



        //public IActionResult Details(int orderId)
        //{


        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }

        //    var order = db.Order_table
        //                  .Include(o => o.Customer_orders)
        //                  .ThenInclude(co => co.Customers)
        //                  .Include(o => o.Order_Drivers)
        //                  .ThenInclude(od => od.Drivers)
        //                  .FirstOrDefault(o => o.OrderId == orderId);

        //    if (order == null)
        //    {
        //        return NotFound("Order not found.");
        //    }

        //    return View(order);
        //}


        //private async Task<bool> CreateDriverAccount()
        //{
        //    string password = HttpContext.Session.GetString("Password");
        //    string profilePictureBase64 = HttpContext.Session.GetString("ProfilePicture");
        //    string licensePictureBase64 = HttpContext.Session.GetString("LicensePicture");
        //    string workPermitPictureBase64 = HttpContext.Session.GetString("WorkPermitPicture"); // إضافة صورة رخصة العمل


        //    byte[] profilePictureBytes = Convert.FromBase64String(profilePictureBase64);
        //    byte[] licensePictureBytes = Convert.FromBase64String(licensePictureBase64);
        //    byte[] workPermitPictureBytes = Convert.FromBase64String(workPermitPictureBase64); // تحويل رخصة العمل إلى بايتات

        //    string profilePicturePath = await SaveFileToDisk(profilePictureBytes, "drivers", ".jpg");
        //    string licensePicturePath = await SaveFileToDisk(licensePictureBytes, "licenses", ".jpg");
        //    string workPermitPicturePath = await SaveFileToDisk(workPermitPictureBytes, "workpermits", ".jpg"); // حفظ رخصة العمل


        //    var driver = new Driver
        //    {
        //        UserName = HttpContext.Session.GetString("DriverName"),
        //        Email = HttpContext.Session.GetString("DriverEmail"),
        //        PhoneNumber = HttpContext.Session.GetString("DriverPhone"),
        //        VehicleType = HttpContext.Session.GetString("VehicleType"),
        //        ProfilePicture = profilePicturePath,
        //        LicensePicture = licensePicturePath,
        //        workPermitPicture = workPermitPicturePath, // إضافة رخصة العمل
        //        IsAvailable = false,
        //        IsNewDriver = true,
        //        TrackingDriver = HttpContext.Session.GetString("TrackingDriver")
        //    };

        //    var passwordHasher = new PasswordHasher<Driver>();
        //    driver.PasswordHash = passwordHasher.HashPassword(driver, password);

        //    var result = await _userManager.CreateAsync(driver, password);
        //    return result.Succeeded;
        //}



        /***/



        //[HttpPost]
        //public async Task<IActionResult> Login(LoginDriverViewModel model, string returnUrl = null)
        //{
        //    Console.WriteLine($"[POST Login] RememberMe Value: {model.RememberMe}");

        //    if (ModelState.IsValid)
        //    {
        //        var driver = await _userManager.FindByEmailAsync(model.Email);
        //        if (driver != null)
        //        {
        //            if (await _userManager.CheckPasswordAsync(driver, model.Password))
        //            {
        //                if (!driver.IsApproved)
        //                {
        //                    ModelState.AddModelError("", "Your account has not been approved by the admin yet.");
        //                    return View(model);
        //                }

        //                if (driver != null && driver.IsDriver)
        //                {
        //                    HttpContext.Session.SetString("IsDriver", "true");
        //                    HttpContext.Session.SetString("IsAdmin", "false");
        //                    ViewBag.IsDriver = true;
        //                    ViewBag.IsAdmin = false;
        //                    await _signInManager.SignInAsync(driver, model.RememberMe);

        //                    return RedirectToAction("Dashboard", "DriverDash");
        //                }
        //            }
        //        }
        //        else
        //        {
        //            var admin = await _adminUserManager.FindByEmailAsync(model.Email);
        //            if (admin != null && await _adminUserManager.CheckPasswordAsync(admin, model.Password))
        //            {
        //                if (admin.IsAdmin)
        //                {
        //                    HttpContext.Session.SetString("IsDriver", "false");
        //                    HttpContext.Session.SetString("IsAdmin", "true");
        //                    ViewBag.IsDriver = false;
        //                    ViewBag.IsAdmin = true;
        //                    await _adminSignInManager.SignInAsync(admin, model.RememberMe);
        //                    return RedirectToAction("IIndexPartialView", "admin1");
        //                }
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("", "Invalid email or password.");
        //            }
        //        }
        //    }

        //    return View(model);
        //}










        // وظيفة لإنشاء حساب السائق
    }
}





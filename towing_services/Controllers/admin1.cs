using Dal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading.Tasks;
using towing_services.Hubs;
using towing_services.Models;

namespace towing_services.Controllers
{

    public class admin1 : Controller
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

        private readonly PasswordHasher<Dal.HR> _passwordHasher = new();




        public admin1(Towing_Collection info, IEmailSender emailSender, UserManager<Driver> userManager, SignInManager<Driver> signInManager, UserManager<Admin> adminUserManager,
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

       
        [HttpGet]
        public IActionResult IIndexPartialView(int page = 1, int pageInProgress = 1, string trackingNumber = "")
        {
            var user = _adminUserManager.GetUserAsync(User).Result;
            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Home", "Home");
            }

            var newRequestsQuery = db.Order_table
                                     .Include(o => o.Customer_orders)
                                     .ThenInclude(co => co.Customers)
                                     .Where(o => o.Status == "New Request")
                                     .AsQueryable();

            var inProgressRequestsQuery = db.Order_table
                                            .Include(o => o.Customer_orders)
                                            .ThenInclude(co => co.Customers)
                                            .Where(o => o.Status == "In Progress")
                                            .AsQueryable();

            if (!string.IsNullOrEmpty(trackingNumber) && trackingNumber != "TRK-")
            {
                newRequestsQuery = newRequestsQuery.Where(o => o.TrackingNumber == trackingNumber);
                inProgressRequestsQuery = inProgressRequestsQuery.Where(o => o.TrackingNumber == trackingNumber);
            }

            int pageSize = 3;
            int totalNewRequests = newRequestsQuery.Count();


            int totalInProgressRequests = inProgressRequestsQuery.Count();

            int totalPages = (int)Math.Ceiling((double)totalNewRequests / pageSize);
            int totalInProgressPages = (int)Math.Ceiling((double)totalInProgressRequests / pageSize);

            var newRequests = newRequestsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var inProgressRequests = inProgressRequestsQuery.Skip((pageInProgress - 1) * pageSize).Take(pageSize).ToList();


            ViewBag.NewRequestCount = totalNewRequests;
            ViewBag.InProgressCount = totalInProgressRequests;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPageInProgress = pageInProgress;
            ViewBag.TotalPagesInProgress = totalInProgressPages;

            var viewModel = new OrdersModel
            {
                NewRequests = newRequests,
                InProgressRequests = inProgressRequests
            };


            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("orders", viewModel);
            }

            return View(viewModel);
        }




        [HttpGet]
        public  IActionResult prof()
        {
            
            return View();
        }







        [HttpPost]
        public IActionResult SaveETA(int orderId, string eta)
        {
            var order = db.Order_table.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            if (string.IsNullOrEmpty(eta))
            {
                return Json(new { success = false, message = "ETA cannot be empty." });
            }

            order.ETA = eta;
            db.SaveChanges();

            return Json(new { success = true, orderId = orderId, eta = eta });
        }



        public async Task<IActionResult> GetCustomerDetails(int orderId)
        {
            var order = await db.Order_table
                .Include(o => o.Customer_orders)
                .ThenInclude(co => co.Customers)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null || order.Customer_orders.Count == 0)
                return NotFound("Customer details not found.");

            var customer = order.Customer_orders.FirstOrDefault()?.Customers;
            return PartialView("GetCustomerDetails", customer);
        }

        [HttpGet]
        public IActionResult AssignDriver(int orderId)
        {


            var user = _adminUserManager.GetUserAsync(User).Result;

            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Home", "Home");
            }
            var order = db.Order_table.FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                ViewData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Index");
            }

            var availableDrivers = db.Driver_table
                                      .Where(d => d.IsAvailable == true)
                                      .Select(d => new DriverViewModel
                                      {
                                          DriverId = d.Id,
                                          Name = d.UserName,
                                          Email = d.Email,
                                          phone = d.PhoneNumber,
                                          VehicleType = d.VehicleType,
                                          isAvailable = d.IsAvailable,
                                          ProfilePicture = d.ProfilePicture,
                                          LicensePicture = d.LicensePicture
                                      })
                                      .ToList();

            ViewBag.OrderId = orderId;

            return View(availableDrivers);
        }
        [HttpPost]
        public async Task<IActionResult> AssignDriver(int orderId, string driverId)
        {
            if (string.IsNullOrEmpty(driverId) || !int.TryParse(driverId, out int driverIdInt))
            {
                return BadRequest("Invalid driver ID.");
            }

            var order = db.Order_table.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            var driver = await _userManager.FindByIdAsync(driverId);
            if (driver == null)
            {
                return NotFound("Driver not found.");
            }

            order.Status = "In Progress";
            db.SaveChanges();

            var orderDriver = new Order_Driver
            {
                OrderId = orderId,
                DriverId = driverIdInt
            };

            db.Order_Driver_table.Add(orderDriver);
            await db.SaveChangesAsync();  


            //return RedirectToAction("Details", new { orderId = order.OrderId });
            return Json(new { success = true, message = "Driver assigned successfully." });

        }



        public IActionResult Details(int orderId)
        {


            var user = _adminUserManager.GetUserAsync(User).Result;

            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Home", "Home");
            }

            var order = db.Order_table
                          .Include(o => o.Customer_orders)
                          .ThenInclude(co => co.Customers)
                          .Include(o => o.Order_Drivers)
                          .ThenInclude(od => od.Drivers)
                          .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound("Order not found.");
            }

            return View(order);
        }



        ///



        public IActionResult GetDriver(int page = 1)
        {
            //var user = _adminUserManager.GetUserAsync(User).Result;

            //if (user == null || !user.IsAdmin)
            //{
            //    return RedirectToAction("Home", "Home");
            //}
            var isHr = HttpContext.Session.GetString("IsHR");
            if (isHr != "True")
            {
                return RedirectToAction("Home", "Home"); // أو أي صفحة تريد تحويل المستخدم إليها
            }

            try
            {
                var drivers = db.Driver_table
                    .Where(d => d.IsDriver == true)
                    .Select(d => new DriverViewModel
                    {
                        DriverId = d.Id,
                        Name = d.UserName,
                        Email = d.Email,
                        phone = d.PhoneNumber,
                        VehicleType = d.VehicleType,
                        TrackingDriver = d.TrackingDriver,
                        LicensePicture = d.LicensePicture,
                        ProfilePicture = d.ProfilePicture,
                        workPermitPicture = d.workPermitPicture,
                    }).ToList();

                int pageSize = 3; 

                int totalItems = drivers.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var paginatedDrivers = drivers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var viewModel = new PaginatedDriversViewModel
                {
                    Drivers = paginatedDrivers,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return PartialView("GetDriver", viewModel);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }




        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var isHr = HttpContext.Session.GetString("IsHR");
            if (isHr != "True")
            {
                return RedirectToAction("Home", "Home"); 
            }

            try
            {
                var driver = await db.Driver_table.FindAsync(id);

                if (driver == null)
                {
                    return Json(new { success = false, message = "Driver not found." });
                }

                string profilePicturePath = driver.ProfilePicture;
                string licensePicturePath = driver.LicensePicture;
                string workPermitPicturePath = driver.workPermitPicture;

                db.Driver_table.Remove(driver);
                await db.SaveChangesAsync();

                DeleteFileFromDisk(profilePicturePath);
                DeleteFileFromDisk(licensePicturePath);
                DeleteFileFromDisk(workPermitPicturePath);

                _cache.Remove("drivers");
                var updatedDrivers = db.Driver_table
                                       .Select(d => new DriverViewModel
                                       {
                                           DriverId = d.Id,
                                           Name = d.UserName,
                                           Email = d.Email,
                                           phone = d.PhoneNumber,
                                           VehicleType = d.VehicleType,
                                           TrackingDriver = d.TrackingDriver,
                                           LicensePicture = d.LicensePicture,
                                           ProfilePicture = d.ProfilePicture,
                                           workPermitPicture = d.workPermitPicture
                                       }).ToList();

                _cache.Set("drivers", updatedDrivers, TimeSpan.FromMinutes(5));

                Console.WriteLine($"Deleted Driver ID: {id}");
                Console.WriteLine($"Profile: {profilePicturePath}, License: {licensePicturePath}, WorkPermit: {workPermitPicturePath}");

                return Json(new { success = true, message = "Driver deleted successfully!" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during deletion: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the driver." });
            }
        }


        private void DeleteFileFromDisk(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                string fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);
                if (System.IO.File.Exists(fullFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(fullFilePath);
                        Console.WriteLine($"File {filePath} deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                    }
                }
            }
        }




        //new
        public IActionResult show_driver(int page = 1)
        {
            //var user = _adminUserManager.GetUserAsync(User).Result;

            //if (user == null || !user.IsAdmin)
            //{
            //    return RedirectToAction("Home", "Home");
            //}


            var isHr = HttpContext.Session.GetString("IsHR");

            if (isHr != "True")
            {
                return RedirectToAction("Home", "Home");
            }

            try
            {
                var drivers = db.Driver_table
                    .Where(d => d.IsNewDriver) // التأكد من السائقين الجدد
                    .Where(d => d.IsDriver == false)
                    .Select(d => new DriverViewModel
                    {
                        DriverId = d.Id,
                        Name = d.UserName,
                        Email = d.Email,
                        phone = d.PhoneNumber,
                        VehicleType = d.VehicleType,
                        TrackingDriver = d.TrackingDriver,
                        LicensePicture = d.LicensePicture,
                        ProfilePicture = d.ProfilePicture,
                        workPermitPicture = d.workPermitPicture
                    }).ToList();

                int pageSize = 4; 
                int totalItems = drivers.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var paginatedDrivers = drivers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var viewModel = new PaginatedDriversViewModel
                {
                    Drivers = paginatedDrivers,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return PartialView("show_driver", viewModel); // اسم العرض الجزئي الصحيح هنا
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }



        public async Task<IActionResult> ApproveDriver(string id)
        {
            var driver = await _userManager.FindByIdAsync(id);
            if (driver == null)
            {
                return NotFound();
            }

            if (!driver.IsDriver)
            {
                driver.IsDriver = true;
            }

            driver.IsApproved = true;

            var result = await _userManager.UpdateAsync(driver);

            if (!result.Succeeded)
            {
                return Json(new { success = false, message = "An error occurred while updating the driver." });
            }

            _ = SendApprovalEmailAsync(driver.Email, driver.UserName); 

            return RedirectToAction(nameof(show_driver));
        }

        private async Task SendApprovalEmailAsync(string email, string driverName)
        {
            var subject = "Your account has been approved";
            var body = $@"
    <html>
    <body>
        <h2 style='color: #4CAF50;'>Congratulations {driverName}!</h2>
        <p style='font-size: 16px;'>We're pleased to inform you that your application to become a driver has been approved. You're now part of our driver community.</p>
        <p style='font-size: 16px;'>Best regards,</p>
        <p style='font-size: 16px;'><strong>The Driver Management Team</strong></p>
    </body>
    </html>";

            await _emailSender.SendEmailAsync(email, subject, body);
        }


      


        public async Task<IActionResult> RejectDriver(string id)
        {
            try
            {
                var driver = await _userManager.FindByIdAsync(id);
                if (driver == null)
                {
                    return NotFound();
                }

                driver.IsApproved = false;
                var result = await _userManager.UpdateAsync(driver);
                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = "Error updating driver status." });
                }

                _ = SendRejectionEmailAsync(driver.Email);

                var driverToDelete = await db.Driver_table.FindAsync(driver.Id);
                if (driverToDelete != null)
                {
                    string profilePicturePath = driverToDelete.ProfilePicture;
                    string licensePicturePath = driverToDelete.LicensePicture;
                    string workPermitPicturePath = driverToDelete.workPermitPicture;

                    db.Driver_table.Remove(driverToDelete);
                    await db.SaveChangesAsync();

                    var deleteFileTasks = new List<Task>
            {
                Task.Run(() => DeleteFileFromDisk(profilePicturePath)),
                Task.Run(() => DeleteFileFromDisk(licensePicturePath)),
                Task.Run(() => DeleteFileFromDisk(workPermitPicturePath))
            };

                    await Task.WhenAll(deleteFileTasks);  
                }

                _cache.Remove("drivers");
                var updatedDrivers = db.Driver_table
                    .Select(d => new DriverViewModel
                    {
                        DriverId = d.Id,
                        Name = d.UserName,
                        Email = d.Email,
                        phone = d.PhoneNumber,
                        VehicleType = d.VehicleType,
                        TrackingDriver = d.TrackingDriver,
                        LicensePicture = d.LicensePicture,
                        ProfilePicture = d.ProfilePicture,
                        workPermitPicture = d.workPermitPicture
                    }).ToList();

                _cache.Set("drivers", updatedDrivers, TimeSpan.FromMinutes(5));

                return RedirectToAction(nameof(show_driver));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during rejection and deletion: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while rejecting and deleting the driver." });
            }
        }
        private async Task SendRejectionEmailAsync(string email)
        {
            var subject = "Your account has been rejected";
            var body = @"
    <html>
    <body>
        <h2>We're sorry to inform you</h2>
        <p>Dear driver,</p>
        <p>Your application to become a driver in the system has been rejected. We appreciate your understanding.</p>
        <p>If you have any questions, feel free to contact us.</p>
        <p>Best regards,</p>
        <p><strong>The Driver Management Team</strong></p>
        <footer>
            <p style='font-size: 12px; color: gray;'>For further assistance, please contact stroingtowing@gmail.com</p>
        </footer>
    </body>
    </html>";

            await _emailSender.SendEmailAsync(email, subject, body);
        }





        /// <summary>
        /// //drveradd
        /// </summary>
        /// <returns></returns>





        [HttpGet]
        public IActionResult AddDriver()
        {
            return PartialView("AddfirstDriver");
        }

        [HttpPost]
        public async Task<IActionResult> AddDriver(RegisterDriverViewModel model, IFormFile profilePicture, IFormFile licensePicture, IFormFile workPermitPicture)
        {
            // ✅ التحقق من صحة الصور المرفوعة
            if (!IsImageValid(profilePicture))
            {
                ModelState.AddModelError("ProfilePicture", "Only .jpg, .jpeg, and .png files are allowed for the profile picture, and file size must be less than 5MB.");
            }

            if (!IsImageValid(licensePicture))
            {
                ModelState.AddModelError("LicensePicture", "Only .jpg, .jpeg, and .png files are allowed for the license picture, and file size must be less than 5MB.");
            }

            if (!IsImageValid(workPermitPicture))
            {
                ModelState.AddModelError("WorkPermitPicture", "Only .jpg, .jpeg, and .png files are allowed for the work permit picture, and file size must be less than 5MB.");
            }

            // ✅ إذا كانت هناك أخطاء في النموذج
            if (!ModelState.IsValid)
                return PartialView("AddfirstDriver", model);

            // ✅ التحقق من وجود السائق أو المسؤول بنفس البيانات
            var existingDriver = await db.Driver_table.FirstOrDefaultAsync(d => d.Email == model.Email || d.PhoneNumber == model.Phone || d.UserName == model.Name);
            var existingAdmin = await db.Admin_table.FirstOrDefaultAsync(a => a.Email == model.Email || a.PhoneNumber == model.Phone || a.UserName == model.Name);

            if (existingDriver != null || existingAdmin != null)
            {
                if (existingDriver?.Email == model.Email)
                {
                    ModelState.AddModelError("Email", "This email is already in use by a driver.");
                }
                if (existingAdmin?.Email == model.Email)
                {
                    ModelState.AddModelError("Email", "This email is already in use by an admin.");
                }
                if (existingDriver?.PhoneNumber == model.Phone)
                {
                    ModelState.AddModelError("Phone", "This phone number is already in use by a driver.");
                }
                if (existingAdmin?.PhoneNumber == model.Phone)
                {
                    ModelState.AddModelError("Phone", "This phone number is already in use by an admin.");
                }
                if (existingDriver?.UserName == model.Name)
                {
                    ModelState.AddModelError("Name", "This name is already in use by a driver.");
                }
                if (existingAdmin?.UserName == model.Name)
                {
                    ModelState.AddModelError("Name", "This name is already in use by an admin.");
                }
                return PartialView("AddfirstDriver", model);
            }

            // ✅ إنشاء السائق الجديد
            var driver = new Driver
            {
                UserName = model.Name,
                Email = model.Email,
                PhoneNumber = model.Phone,
                VehicleType = model.VehicleType,
                ProfilePicture = await SaveFileToDisk(await ConvertFileToByteArray(profilePicture), "profilePictures", Path.GetExtension(profilePicture.FileName)),
                LicensePicture = await SaveFileToDisk(await ConvertFileToByteArray(licensePicture), "licensePictures", Path.GetExtension(licensePicture.FileName)),
                workPermitPicture = await SaveFileToDisk(await ConvertFileToByteArray(workPermitPicture), "workPermitPictures", Path.GetExtension(workPermitPicture.FileName)),
                IsAvailable = false,
                IsNewDriver = true,
                IsApproved = true,
                IsDriver = true
            };

            var passwordHasher = new PasswordHasher<Driver>();
            driver.PasswordHash = passwordHasher.HashPassword(driver, model.Password);

            // ✅ حفظ السائق في قاعدة البيانات
            var result = await _userManager.CreateAsync(driver, model.Password);

            if (result.Succeeded)
            {
                // ✅ تحميل السائقين بعد الإضافة
                var drivers = db.Driver_table
                    .Where(d => d.IsDriver == true)
                    .Select(d => new DriverViewModel
                    {
                        DriverId = d.Id,
                        Name = d.UserName,
                        Email = d.Email,
                        phone = d.PhoneNumber,
                        VehicleType = d.VehicleType,
                        TrackingDriver = d.TrackingDriver,
                        LicensePicture = d.LicensePicture,
                        ProfilePicture = d.ProfilePicture,
                        workPermitPicture = d.workPermitPicture,
                    }).ToList();

                int pageSize = 3;
                int totalItems = drivers.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var paginatedDrivers = drivers.Take(pageSize).ToList();

                var viewModel = new PaginatedDriversViewModel
                {
                    Drivers = paginatedDrivers,
                    CurrentPage = 1,
                    TotalPages = totalPages
                };

                return PartialView("GetDriver", viewModel);
            }

            ModelState.AddModelError("", "Failed to create driver account.");
            return PartialView("AddfirstDriver", model);
        }


        // التحقق من امتداد وحجم الصورة
        private bool IsImageValid(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // تحديد الامتدادات المسموح بها
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            bool isValidExtension = allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower());

            // تحديد الحجم الأقصى (مثلاً 5 ميجابايت)
            long maxFileSize = 5 * 1024 * 1024; // 5MB

            // التحقق من امتداد الصورة والحجم
            if (!isValidExtension || file.Length > maxFileSize)
            {
                return false;
            }

            return true;
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


        // ADD  HR 
        [HttpGet]
        public IActionResult CreateHR()
        {
            var user = _adminUserManager.GetUserAsync(User).Result;

            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Home", "Home");
            }
            return View();
        }

       
        [HttpPost]
        public async Task<IActionResult> CreateHR(CreateHRViewModel model)
        {
            

            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, errorsHtml = errors });
            }

            var existingHR = await db.HRs
                .FirstOrDefaultAsync(h => h.Email == model.Email || h.PhoneNumber == model.PhoneNumber);
            var existingDriver = await db.Driver_table
                .FirstOrDefaultAsync(d => d.Email == model.Email || d.PhoneNumber == model.PhoneNumber);
            var existingAdmin = await db.Admin_table
                .FirstOrDefaultAsync(a => a.Email == model.Email || a.PhoneNumber == model.PhoneNumber);

            if (existingHR != null || existingDriver != null || existingAdmin != null)
            {
                var errorsList = new List<string>();
                if ((existingHR?.Email == model.Email) || (existingDriver?.Email == model.Email) || (existingAdmin?.Email == model.Email))
                    errorsList.Add("This email is already in use.");
                if ((existingHR?.PhoneNumber == model.PhoneNumber) || (existingDriver?.PhoneNumber == model.PhoneNumber) || (existingAdmin?.PhoneNumber == model.PhoneNumber))
                    errorsList.Add("This phone number is already in use.");

                return Json(new { success = false, errorsHtml = string.Join("<br/>", errorsList) });
            }

            var hr = new Dal.HR
            {
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                IsHR = true
            };
            hr.PasswordHash = _passwordHasher.HashPassword(hr, model.Password);

            db.HRs.Add(hr);
            await db.SaveChangesAsync();

            return Json(new { success = true });
        }

        //list hr 


        public async Task<IActionResult> _HRTable(int page = 1)
        {
            var user = _adminUserManager.GetUserAsync(User).Result;

            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Home", "Home");
            }
            int pageSize = 4;
            var totalCount = await db.HRs.CountAsync();
            var data = await db.HRs
                .OrderBy(h => h.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.CurrentPage = page;

            return PartialView("_HRTable", data);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteHR(int id)
        {
            var user = _adminUserManager.GetUserAsync(User).Result;

            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Home", "Home");
            }

            var hr = await db.HRs.FindAsync(id);
            if (hr != null)
            {
                db.HRs.Remove(hr);
                await db.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHR(UpdateHRViewModel model)
        {
            var user = _adminUserManager.GetUserAsync(User).Result;

            if (user == null || !user.IsAdmin)
            {
                return RedirectToAction("Home", "Home");
            }
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, errorsHtml = errors });
            }

            var hr = await db.HRs.FindAsync(model.Id);
            if (hr == null)
            {
                return Json(new { success = false, errorsHtml = "HR not found." });
            }

            // فحص البريد والهاتف مع استثناء السجل الحالي
            var duplicateEmail = await db.HRs.AnyAsync(h => h.Email == model.Email && h.Id != model.Id)
                             || await db.Driver_table.AnyAsync(d => d.Email == model.Email)
                             || await db.Admin_table.AnyAsync(a => a.Email == model.Email);

            var duplicatePhone = await db.HRs.AnyAsync(h => h.PhoneNumber == model.PhoneNumber && h.Id != model.Id)
                             || await db.Driver_table.AnyAsync(d => d.PhoneNumber == model.PhoneNumber)
                             || await db.Admin_table.AnyAsync(a => a.PhoneNumber == model.PhoneNumber);

            var errorsList = new List<string>();
            if (duplicateEmail)
                errorsList.Add("This email is already in use.");
            if (duplicatePhone)
                errorsList.Add("This phone number is already in use.");

            if (errorsList.Any())
                return Json(new { success = false, errorsHtml = string.Join("<br/>", errorsList) });

            // التحديث
            hr.FullName = model.FullName;
            hr.Email = model.Email;
            hr.PhoneNumber = model.PhoneNumber;

            await db.SaveChangesAsync();

            return Json(new { success = true });
        }



































        //[HttpGet]
        // public IActionResult AddDriver()
        // {
        //     return PartialView("AddfirstDriver");
        // }

        // [HttpPost]
        // public async Task<IActionResult> AddDriver(RegisterDriverViewModel model, IFormFile profilePicture, IFormFile licensePicture, IFormFile workPermitPicture)
        // {
        //     // التحقق من صحة الصور المرفوعة
        //     if (!IsImageValid(profilePicture))
        //     {
        //         ModelState.AddModelError("ProfilePicture", "Only .jpg, .jpeg, and .png files are allowed for the profile picture, and file size must be less than 5MB.");
        //     }

        //     if (!IsImageValid(licensePicture))
        //     {
        //         ModelState.AddModelError("LicensePicture", "Only .jpg, .jpeg, and .png files are allowed for the license picture, and file size must be less than 5MB.");
        //     }

        //     if (!IsImageValid(workPermitPicture))
        //     {
        //         ModelState.AddModelError("WorkPermitPicture", "Only .jpg, .jpeg, and .png files are allowed for the work permit picture, and file size must be less than 5MB.");
        //     }

        //     // إذا كانت هناك أخطاء في النموذج، العودة إلى نفس الصفحة
        //     if (!ModelState.IsValid)
        //         return PartialView("AddfirstDriver", model);

        //     // تحقق من وجود السائق أو المسؤول الذي يحمل نفس البريد أو الهاتف أو الاسم
        //     var existingDriver = await db.Driver_table.FirstOrDefaultAsync(d => d.Email == model.Email || d.PhoneNumber == model.Phone || d.UserName == model.Name);
        //     var existingAdmin = await db.Admin_table.FirstOrDefaultAsync(a => a.Email == model.Email || a.PhoneNumber == model.Phone || a.UserName == model.Name);

        //     if (existingDriver != null || existingAdmin != null)
        //     {
        //         if (existingDriver != null && existingDriver.Email == model.Email)
        //         {
        //             ModelState.AddModelError("Email", "This email is already in use by a driver.");
        //         }
        //         if (existingAdmin != null && existingAdmin.Email == model.Email)
        //         {
        //             ModelState.AddModelError("Email", "This email is already in use by an admin.");
        //         }
        //         if (existingDriver != null && existingDriver.PhoneNumber == model.Phone)
        //         {
        //             ModelState.AddModelError("Phone", "This phone number is already in use by a driver.");
        //         }
        //         if (existingAdmin != null && existingAdmin.PhoneNumber == model.Phone)
        //         {
        //             ModelState.AddModelError("Phone", "This phone number is already in use by an admin.");
        //         }
        //         if (existingDriver != null && existingDriver.UserName == model.Name)
        //         {
        //             ModelState.AddModelError("Name", "This name is already in use by a driver.");
        //         }
        //         if (existingAdmin != null && existingAdmin.UserName == model.Name)
        //         {
        //             ModelState.AddModelError("Name", "This name is already in use by an admin.");
        //         }
        //         return PartialView("AddfirstDriver", model);
        //     }

        //     // إنشاء السائق
        //     var driver = new Driver
        //     {
        //         UserName = model.Name,
        //         Email = model.Email,
        //         PhoneNumber = model.Phone,
        //         VehicleType = model.VehicleType,
        //         ProfilePicture = await SaveFileToDisk(await ConvertFileToByteArray(profilePicture), "profilePictures", Path.GetExtension(profilePicture.FileName)),
        //         LicensePicture = await SaveFileToDisk(await ConvertFileToByteArray(licensePicture), "licensePictures", Path.GetExtension(licensePicture.FileName)),
        //         workPermitPicture = await SaveFileToDisk(await ConvertFileToByteArray(workPermitPicture), "workPermitPictures", Path.GetExtension(workPermitPicture.FileName)),
        //         IsAvailable = false,
        //         IsNewDriver = true,
        //         IsApproved = true,
        //         IsDriver = true
        //     };

        //     var passwordHasher = new PasswordHasher<Driver>();
        //     driver.PasswordHash = passwordHasher.HashPassword(driver, model.Password);

        //     // حفظ السائق في قاعدة البيانات
        //     var result = await _userManager.CreateAsync(driver, model.Password);

        //     if (result.Succeeded)
        //     {

        //         return PartialView("GetDriver");
        //     }

        //     ModelState.AddModelError("", "Failed to create driver account.");
        //     return PartialView("AddfirstDriver", model);
        // }


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
        //public IActionResult IIndexPartialView(string status, string trackingNumber)
        //{

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

        //    return View(orders);
        //}





        //[HttpGet]
        //public IActionResult IIndexPartialView(int page = 1, string trackingNumber = "")
        //{
        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }

        //    var ordersQuery = db.Order_table.Include(o => o.Customer_orders)
        //                                     .ThenInclude(co => co.Customers)
        //                                     .AsQueryable();

        //    ViewBag.NewRequestCount = ordersQuery.Count(o => o.Status == "New Request");

        //    // البحث عن رقم التتبع إذا كان موجودًا
        //    if (!string.IsNullOrEmpty(trackingNumber) && trackingNumber != "TRK-")
        //    {
        //        ordersQuery = ordersQuery.Where(o => o.TrackingNumber == trackingNumber);
        //    }

        //    int pageSize = 6; // عدد الطلبات في كل صفحة
        //    int totalOrders = ordersQuery.Count(); // العدد الكلي للطلبات
        //    int totalPages = (int)Math.Ceiling((double)totalOrders / pageSize); // حساب عدد الصفحات

        //    var orders = ordersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        //    ViewBag.CurrentPage = page;
        //    ViewBag.TotalPages = totalPages;

        //    // إرجاع جزئية فقط عند استدعاء AJAX
        //    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //    {
        //        return PartialView("orders", orders);
        //    }

        //    return View(orders); // إرجاع الصفحة الرئيسية
        //}

        //[HttpGet]
        //public IActionResult IIndexPartialView(int page = 1, string trackingNumber = "")
        //{
        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }

        //    var ordersQuery = db.Order_table
        //                         .Include(o => o.Customer_orders)
        //                         .ThenInclude(co => co.Customers)
        //                         .Where(o => o.Status == "New Request") // عرض الطلبات الجديدة فقط
        //                         .AsQueryable();

        //    ViewBag.NewRequestCount = ordersQuery.Count();

        //    // البحث عن رقم التتبع إذا كان موجودًا
        //    if (!string.IsNullOrEmpty(trackingNumber) && trackingNumber != "TRK-")
        //    {
        //        ordersQuery = ordersQuery.Where(o => o.TrackingNumber == trackingNumber);
        //    }

        //    int pageSize = 3; // عدد الطلبات في كل صفحة
        //    int totalOrders = ordersQuery.Count(); // العدد الكلي للطلبات الجديدة فقط
        //    int totalPages = (int)Math.Ceiling((double)totalOrders / pageSize); // حساب عدد الصفحات

        //    var orders = ordersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        //    ViewBag.CurrentPage = page;
        //    ViewBag.TotalPages = totalPages;

        //    // إرجاع جزئية فقط عند استدعاء AJAX
        //    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //    {
        //        return PartialView("orders", orders);
        //    }

        //    return View(orders); 
        //}

        //[HttpGet]
        //public IActionResult IIndexPartialView(int page = 1, string trackingNumber = "")
        //{
        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }

        //    var newRequestsQuery = db.Order_table
        //                             .Include(o => o.Customer_orders)
        //                             .ThenInclude(co => co.Customers)
        //                             .Where(o => o.Status == "New Request")
        //                             .AsQueryable();

        //    var inProgressRequestsQuery = db.Order_table
        //                                    .Include(o => o.Customer_orders)
        //                                    .ThenInclude(co => co.Customers)
        //                                    .Where(o => o.Status == "In Progress")
        //                                    .AsQueryable();

        //    ViewBag.NewRequestCount = newRequestsQuery.Count();
        //    ViewBag.InProgressCount = inProgressRequestsQuery.Count();

        //    // تصفية الطلبات برقم التتبع إذا كان موجودًا
        //    if (!string.IsNullOrEmpty(trackingNumber) && trackingNumber != "TRK-")
        //    {
        //        newRequestsQuery = newRequestsQuery.Where(o => o.TrackingNumber == trackingNumber);
        //        inProgressRequestsQuery = inProgressRequestsQuery.Where(o => o.TrackingNumber == trackingNumber);
        //    }

        //    int pageSize = 3; // عدد الطلبات في كل صفحة
        //    int totalNewRequests = newRequestsQuery.Count();
        //    int totalPages = (int)Math.Ceiling((double)totalNewRequests / pageSize);

        //    var newRequests = newRequestsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        //    var inProgressRequests = inProgressRequestsQuery.ToList(); // إحضار جميع الطلبات قيد التنفيذ

        //    ViewBag.CurrentPage = page;
        //    ViewBag.TotalPages = totalPages;

        //    var viewModel = new OrdersModel
        //    {
        //        NewRequests = newRequests,
        //        InProgressRequests = inProgressRequests
        //    };

        //    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //    {
        //        return PartialView("orders", viewModel);
        //    }

        //    return View(viewModel);
        //}






        //[HttpPost]
        //public async Task<IActionResult> CreateHR(CreateHRViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //        return View(model);

        //    // 🔍 فحص إذا الإيميل أو رقم الهاتف موجودين في HR
        //    var existingHR = await db.HRs
        //        .FirstOrDefaultAsync(h => h.Email == model.Email || h.PhoneNumber == model.PhoneNumber);

        //    // 🔍 فحص إذا الإيميل أو رقم الهاتف موجودين في Driver
        //    var existingDriver = await db.Driver_table
        //        .FirstOrDefaultAsync(d => d.Email == model.Email || d.PhoneNumber == model.PhoneNumber);

        //    // 🔍 فحص إذا الإيميل أو رقم الهاتف موجودين في Admin
        //    var existingAdmin = await db.Admin_table
        //        .FirstOrDefaultAsync(a => a.Email == model.Email || a.PhoneNumber == model.PhoneNumber);

        //    if (existingHR != null || existingDriver != null || existingAdmin != null)
        //    {
        //        if ((existingHR?.Email == model.Email) || (existingDriver?.Email == model.Email) || (existingAdmin?.Email == model.Email))
        //            ModelState.AddModelError("Email", "This email is already in use.");

        //        if ((existingHR?.PhoneNumber == model.PhoneNumber) || (existingDriver?.PhoneNumber == model.PhoneNumber) || (existingAdmin?.PhoneNumber == model.PhoneNumber))
        //            ModelState.AddModelError("PhoneNumber", "This phone number is already in use.");

        //        return View(model);
        //    }

        //    var hr = new Dal.HR
        //    {
        //        UserName = model.UserName,
        //        Email = model.Email,
        //        FullName = model.FullName,
        //        PhoneNumber = model.PhoneNumber,
        //        IsHR = true
        //    };

        //    // 🔐 تشفير الباسورد
        //    hr.PasswordHash = _passwordHasher.HashPassword(hr, model.Password);

        //    db.HRs.Add(hr);
        //    await db.SaveChangesAsync();

        //    return RedirectToAction("Home", "Home");
        //}


        //public IActionResult GetDriver(int page = 1)
        //{



        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }

        //    try
        //    {
        //        var cachedDrivers = _cache.Get<List<DriverViewModel>>("drivers");

        //        if (cachedDrivers == null || page == 1)
        //        {
        //            Console.WriteLine("Cache miss - Fetching data from database.");
        //            cachedDrivers = db.Driver_table
        //                .Where(d => d.IsDriver == true)
        //                                .Select(d => new DriverViewModel
        //                                {
        //                                    DriverId = d.Id,
        //                                    Name = d.UserName,
        //                                    Email = d.Email,
        //                                    phone = d.PhoneNumber,
        //                                    VehicleType = d.VehicleType,
        //                                    TrackingDriver = d.TrackingDriver,
        //                                    LicensePicture = d.LicensePicture,
        //                                    ProfilePicture = d.ProfilePicture,
        //                                    workPermitPicture = d.workPermitPicture
        //                                }).ToList();

        //            _cache.Set("drivers", cachedDrivers, TimeSpan.FromMinutes(5));
        //            Console.WriteLine("Data cached successfully for 5 minutes.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Cache hit - Data retrieved from cache.");
        //        }

        //        int pageSize = 1; // يمكنك تعديله إلى 10 مثلاً حسب الحاجة

        //        int totalItems = cachedDrivers.Count;
        //        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        //        var paginatedDrivers = cachedDrivers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        //        var viewModel = new PaginatedDriversViewModel
        //        {
        //            Drivers = paginatedDrivers,
        //            CurrentPage = page,
        //            TotalPages = totalPages
        //        };

        //        return PartialView("GetDriver", viewModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Error.WriteLine($"An error occurred: {ex.Message}");
        //        return StatusCode(500, "Internal server error");
        //    }
        //}
        //public IActionResult GetDriver()
        //{
        //    var user = _adminUserManager.GetUserAsync(User).Result;

        //    if (user == null || !user.IsAdmin)
        //    {
        //        return RedirectToAction("Home", "Home");
        //    }

        //    try
        //    {
        //        جلب البيانات من الكاش أو تحديثها
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
        //                                  LicensePicture = d.LicensePicture,
        //                                  ProfilePicture = d.ProfilePicture,
        //                                  workPermitPicture = d.workPermitPicture
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

        ////public async Task<IActionResult> ApproveDriver(string id)
        ////{
        ////    var driver = await _userManager.FindByIdAsync(id);
        ////    if (driver == null)
        ////    {
        ////        return NotFound();
        ////    }

        ////    if (!driver.IsDriver)
        ////    {
        ////        driver.IsDriver = true;
        ////    }

        ////    driver.IsApproved = true;

        ////    var result = await _userManager.UpdateAsync(driver);

        ////    if (!result.Succeeded)
        ////    {
        ////        ModelState.AddModelError(string.Empty, "An error occurred while updating the driver.");
        ////        return View(driver);
        ////    }

        ////    await _emailSender.SendEmailAsync(driver.Email, "Your account has been approved",
        ////        "Congratulations! Your application to become a driver has been approved.");

        ////    return RedirectToAction(nameof(show_driver));
        ////}

        ////public async Task<IActionResult> RejectDriver(string id)
        ////{
        ////    var driver = await _userManager.FindByIdAsync(id);
        ////    if (driver == null)
        ////    {
        ////        return NotFound();
        ////    }

        ////    driver.IsApproved = false;

        ////    var result = await _userManager.UpdateAsync(driver);
        ////    if (!result.Succeeded)
        ////    {
        ////    }

        ////    await _emailSender.SendEmailAsync(driver.Email, "Your account has been rejected",
        ////        "Sorry, your application to become a driver in the system has been rejected. Thank you for your understanding.");

        ////    return RedirectToAction(nameof(show_driver));
        ////}


    }
}

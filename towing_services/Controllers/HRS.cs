using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using towing_services.Models;
using Dal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Net.WebSockets;
using towing_services.Hubs;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace towing_services.Controllers
{

    public class HRS : Controller
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


        public HRS(Towing_Collection info, IEmailSender emailSender, UserManager<Driver> userManager, SignInManager<Driver> signInManager, UserManager<Admin> adminUserManager,
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
        public IActionResult DashboardHR()
        {
            return View();
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

























        public IActionResult show_driver(int page = 1)
        {
            


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
        /// 
        /// </summary>
        /// <returns></returns>



        public IActionResult GetDriver(int page = 1)
        {
           
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


































        [HttpGet]
        public IActionResult AddDriver()
        {
            var isHr = HttpContext.Session.GetString("IsHR");
            if (isHr != "True")
            {
                return RedirectToAction("Home", "Home");
            }
            return PartialView("AddfirstDriver");
        }

        [HttpPost]
        public async Task<IActionResult> AddDriver(RegisterDriverViewModel model, IFormFile profilePicture, IFormFile licensePicture, IFormFile workPermitPicture)
        {
            var isHr = HttpContext.Session.GetString("IsHR");
            if (isHr != "True")
            {
                return RedirectToAction("Home", "Home");
            }
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
            var existingHR = await db.HRs
             .FirstOrDefaultAsync(h => h.Email == model.Email || h.PhoneNumber == model.Phone);

            if (existingDriver != null || existingAdmin != null || existingHR!=null)
            {
                if (existingDriver?.Email == model.Email)
                {
                    ModelState.AddModelError("Email", "This email is already in use by a driver.");
                }
                if (existingAdmin?.Email == model.Email)
                {
                    ModelState.AddModelError("Email", "This email is already in use by an admin.");
                }
                if (existingHR?.Email == model.Email)
                {
                    ModelState.AddModelError("Email", "This email is already in use by an HR.");
                }
                if (existingDriver?.PhoneNumber == model.Phone)
                {
                    ModelState.AddModelError("Phone", "This phone number is already in use by a driver.");
                }
                if (existingAdmin?.PhoneNumber == model.Phone)
                {
                    ModelState.AddModelError("Phone", "This phone number is already in use by an admin.");
                }
                if (existingHR?.PhoneNumber == model.Phone)
                {
                    ModelState.AddModelError("Phone", "This phone number is already in use by an HR.");
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




        //edi prof  driver
        [HttpGet]
        public async Task<IActionResult> UpdateProfiledriver(string id)
        {
            var isHr = HttpContext.Session.GetString("IsHR");
            if (isHr != "True")
            {
                return RedirectToAction("Home", "Home");
            }

            var driver = await _userManager.FindByIdAsync(id);
            if (driver == null) return NotFound();

            var model = new DriverViewModel
            {
                DriverId = driver.Id,
                Name = driver.UserName,
                Email = driver.Email,
                phone = driver.PhoneNumber,
                VehicleType = driver.VehicleType,
                CurrentLocation = driver.CurrentLocation,
                Latitude = driver.Latitude,
                Longitude = driver.Longitude,
                ProfilePicture = driver.ProfilePicture,
                LicensePicture = driver.LicensePicture,
                workPermitPicture = driver.workPermitPicture
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateProfiledriver(string id, DriverViewModel model)
        {
            var isHr = HttpContext.Session.GetString("IsHR");
            if (isHr != "True")
            {
                return RedirectToAction("Home", "Home");
            }

            // تحقق من صحة النموذج
            if (!ModelState.IsValid)
            {
                // إذا فشل التحقق، نعيد تحميل الصور القديمة بناءً على السائق المطلوب تعديله
                var driverForPics = await _userManager.FindByIdAsync(id);
                if (driverForPics != null)
                {
                    model.ProfilePicture = driverForPics.ProfilePicture;
                    model.LicensePicture = driverForPics.LicensePicture;
                    model.workPermitPicture = driverForPics.workPermitPicture;
                }
                return View(model);
            }

            // جلب السائق الحالي المراد تحديثه بناءً على الـ id
            var driverToUpdate = await _userManager.FindByIdAsync(id);
            if (driverToUpdate == null)
            {
                return NotFound();
            }

            // فحص إن كانت البيانات مكررة في الجداول الأخرى (مع استثناء السائق نفسه)
            var existingDriver = await db.Driver_table
                .FirstOrDefaultAsync(d => d.Id != driverToUpdate.Id &&
                    (d.Email == model.Email || d.PhoneNumber == model.phone || d.UserName == model.Name));

            var existingAdmin = await db.Admin_table
                .FirstOrDefaultAsync(a =>
                    a.Email == model.Email || a.PhoneNumber == model.phone || a.UserName == model.Name);

            var existingHR = await db.HRs
                .FirstOrDefaultAsync(h =>
                    h.Email == model.Email || h.PhoneNumber == model.phone);

            // التحقق من التكرار وإضافة الأخطاء للنموذج
            if (existingDriver != null || existingAdmin != null || existingHR != null)
            {
                if (existingDriver?.Email == model.Email)
                    ModelState.AddModelError("Email", "This email is already in use by a driver.");

                if (existingAdmin?.Email == model.Email)
                    ModelState.AddModelError("Email", "This email is already in use by an admin.");

                if (existingHR?.Email == model.Email)
                    ModelState.AddModelError("Email", "This email is already in use by an HR.");

                if (existingDriver?.PhoneNumber == model.phone)
                    ModelState.AddModelError("phone", "This phone number is already in use by a driver.");

                if (existingAdmin?.PhoneNumber == model.phone)
                    ModelState.AddModelError("phone", "This phone number is already in use by an admin.");

                if (existingHR?.PhoneNumber == model.phone)
                    ModelState.AddModelError("phone", "This phone number is already in use by an HR.");

                if (existingDriver?.UserName == model.Name)
                    ModelState.AddModelError("Name", "This username is already in use by a driver.");

                if (existingAdmin?.UserName == model.Name)
                    ModelState.AddModelError("Name", "This username is already in use by an admin.");

                // إعادة تحميل الصور القديمة للسائق
                model.ProfilePicture = driverToUpdate.ProfilePicture;
                model.LicensePicture = driverToUpdate.LicensePicture;
                model.workPermitPicture = driverToUpdate.workPermitPicture;

                return View(model);
            }

            // تحديث بيانات السائق
            driverToUpdate.UserName = model.Name;
            driverToUpdate.Email = model.Email;
            driverToUpdate.PhoneNumber = model.phone;
            driverToUpdate.VehicleType = model.VehicleType;
            driverToUpdate.CurrentLocation = model.CurrentLocation;
            driverToUpdate.Latitude = model.Latitude;
            driverToUpdate.Longitude = model.Longitude;

            var result = await _userManager.UpdateAsync(driverToUpdate);

            if (result.Succeeded)
            {
                return RedirectToAction("GetDriver");
            }

            // في حالة فشل التحديث
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.ProfilePicture = driverToUpdate.ProfilePicture;
            model.LicensePicture = driverToUpdate.LicensePicture;
            model.workPermitPicture = driverToUpdate.workPermitPicture;

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(string driverId, IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                string fileExtension = Path.GetExtension(profilePicture.FileName).ToLower();
                if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                {
                    TempData["ErrorMessage"] = "Invalid file type. Please upload a .jpg, .jpeg, or .png file.";
                    return Json(new { success = false, message = "Invalid file type" });
                }

                string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string subFolder = "drivers";
                string subFolderPath = Path.Combine(directoryPath, subFolder);
                if (!Directory.Exists(subFolderPath))
                {
                    Directory.CreateDirectory(subFolderPath);
                }

                // تعديل: جلب السائق باستخدام الـ driverId بدلاً من المستخدم الحالي
                var driver = await _userManager.FindByIdAsync(driverId);
                if (driver == null) return Json(new { success = false, message = "Driver not found" });

                if (!string.IsNullOrEmpty(driver.ProfilePicture))
                {
                    string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", driver.ProfilePicture);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                driver.ProfilePicture = Path.Combine("uploads", "drivers", Path.GetFileName(filePath));

                var result = await _userManager.UpdateAsync(driver);

                if (result.Succeeded)
                {
                    return Json(new { success = true, imageUrl = "/" + driver.ProfilePicture });
                }
                return Json(new { success = false, message = "An error occurred while updating the profile picture." });
            }

            return Json(new { success = false, message = "No file selected." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLicensePicture(string driverId, IFormFile licensePicture)
        {
            if (licensePicture != null && licensePicture.Length > 0)
            {
                string fileExtension = Path.GetExtension(licensePicture.FileName).ToLower();

                if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                {
                    return Json(new { success = false, message = "Invalid file type. Please upload a .jpg, .jpeg, or .png file." });
                }

                string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string subFolder = "licenses";
                string subFolderPath = Path.Combine(directoryPath, subFolder);
                if (!Directory.Exists(subFolderPath))
                {
                    Directory.CreateDirectory(subFolderPath);
                }

                // تعديل: جلب السائق باستخدام الـ driverId
                var driver = await _userManager.FindByIdAsync(driverId);
                if (driver == null)
                {
                    return Json(new { success = false, message = "Driver not found." });
                }

                if (!string.IsNullOrEmpty(driver.LicensePicture))
                {
                    string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", driver.LicensePicture);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await licensePicture.CopyToAsync(stream);
                }

                driver.LicensePicture = Path.Combine("uploads", "licenses", Path.GetFileName(filePath));

                var result = await _userManager.UpdateAsync(driver);

                if (result.Succeeded)
                {
                    return Json(new { success = true, imageUrl = "/" + driver.LicensePicture });
                }

                return Json(new { success = false, message = "An error occurred while updating the license picture." });
            }
            else
            {
                return Json(new { success = false, message = "No file selected." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWorkPermitPicture(string driverId, IFormFile workPermitPicture)
        {
            if (workPermitPicture != null && workPermitPicture.Length > 0)
            {
                string fileExtension = Path.GetExtension(workPermitPicture.FileName).ToLower();

                if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                {
                    return Json(new { success = false, message = "Invalid file type. Please upload a .jpg, .jpeg, or .png file." });
                }

                string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string subFolder = "workpermits";
                string subFolderPath = Path.Combine(directoryPath, subFolder);
                if (!Directory.Exists(subFolderPath))
                {
                    Directory.CreateDirectory(subFolderPath);
                }

                // تعديل: جلب السائق باستخدام الـ driverId
                var driver = await _userManager.FindByIdAsync(driverId);
                if (driver == null)
                {
                    return Json(new { success = false, message = "Driver not found." });
                }

                if (!string.IsNullOrEmpty(driver.workPermitPicture))
                {
                    string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", driver.workPermitPicture);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await workPermitPicture.CopyToAsync(stream);
                }

                driver.workPermitPicture = Path.Combine("uploads", "workpermits", Path.GetFileName(filePath));

                var result = await _userManager.UpdateAsync(driver);

                if (result.Succeeded)
                {
                    return Json(new { success = true, imageUrl = "/" + driver.workPermitPicture });
                }

                return Json(new { success = false, message = "An error occurred while updating the work permit picture." });
            }
            else
            {
                return Json(new { success = false, message = "No file selected." });
            }
        }





        //










    }
}

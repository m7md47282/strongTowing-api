using Dal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using towing_services.Models;

namespace towing_services.Controllers
{

    public class DriverDash : Controller
    {
        public readonly Towing_Collection db;
        private readonly UserManager<Driver> _userManager;
        private readonly SignInManager<Driver> _signInManager;

        private readonly UserManager<Admin> _adminUserManager;

        private readonly SignInManager<Admin> _adminSignInManager;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;  // تعريف التخزين المؤقت

        public DriverDash(Towing_Collection info, IEmailSender emailSender, UserManager<Driver> userManager, SignInManager<Driver> signInManager, UserManager<Admin> adminUserManager,
     SignInManager<Admin> adminSignInManager, IMemoryCache cache)
        {
            this.db = info;
            _emailSender = emailSender;
            _userManager = userManager;
            _signInManager = signInManager;
            _adminUserManager = adminUserManager;
            _adminSignInManager = adminSignInManager;
            _cache = cache;


        }


        [HttpGet]
        public async Task<IActionResult> Dashboard(int? page = null, int pageSize = 10)
        {
            var driverIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(driverIdString) || !int.TryParse(driverIdString, out int driverId))
            {
                return RedirectToAction("Home", "Home");
            }

            var driver = await db.Driver_table
                .Include(d => d.Order_Drivers)
                    .ThenInclude(od => od.Orders)
                    .ThenInclude(o => o.Customer_orders)
                    .ThenInclude(co => co.Customers)
                .Where(d => d.Id == driverId)
                .FirstOrDefaultAsync();

            if (driver == null)
            {
                return NotFound("Driver not found.");
            }

            if (!driver.IsDriver)
            {
                return Unauthorized("You are not an approved driver.");
            }

            // حساب عدد السجلات الكلي
            var totalOrders = driver.Order_Drivers
                .SelectMany(od => od.Orders.Customer_orders)
                .Count();

            // حساب عدد الصفحات المتاحة بناءً على حجم الصفحة
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            // التحقق من الصفحة المطلوبة
            if (page.HasValue && page.Value >= totalPages)
            {
                // إذا كانت الصفحة المطلوبة أكبر من أو تساوي العدد الإجمالي للصفحات المتاحة، قم بإرجاع الصفحة الأخيرة
                page = totalPages - 1;
            }

            if (page.HasValue)
            {
                // جلب البيانات جزئيًا عند طلب صفحة معينة
                var orders = driver.Order_Drivers
                    .SelectMany(od => od.Orders.Customer_orders)
                    .Where(co => co.Orders.Status == "In Progress") // عرض الطلبات في حالتي "In Progress" و "On the Way"
                    .OrderBy(co => co.Orders.OrderId)
                    .Skip(page.Value * pageSize)
                    .Take(pageSize)
                    .Select(co => new
                    {
                        orderId = co.Orders.OrderId,
                        status = co.Orders.Status,
                        customerName = co.Customers.Name,
                        dropoffLocation = co.Orders.DropoffLocation,
                        pickupLocation = co.Orders.PickupLocation,
                        vehicleType = co.Orders.VehicleType,
                        eta = co.Orders.ETA,
                        track = co.Orders.TrackingNumber
                    })
                    .ToList();
            Console.WriteLine($"Orders: {string.Join(", ", orders.Select(o => o.orderId))}");

                Console.WriteLine($"Total Orders: {totalOrders}, Requested Page: {page.Value}, Skip: {page.Value * pageSize}");

                return Json(new { totalCount = totalOrders, orders }); // إعادة بيانات JSON عند طلب الصفحات
            }


            ViewBag.IsDriver = true;
            ViewBag.IsAdmin = false;
            return View(driver);
        }





        public async Task<IActionResult> Ordercompletedsuccessfully(int? page = null, int pageSize = 10)
        {
            var driverIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(driverIdString) || !int.TryParse(driverIdString, out int driverId))
            {
                return RedirectToAction("Home", "Home");
            }

            var driver = await db.Driver_table
                .Include(d => d.Order_Drivers)
                    .ThenInclude(od => od.Orders)
                    .ThenInclude(o => o.Customer_orders)
                    .ThenInclude(co => co.Customers)
                .Where(d => d.Id == driverId)
                .FirstOrDefaultAsync();

            if (driver == null)
            {
                return NotFound("Driver not found.");
            }

            if (!driver.IsDriver)
            {
                return Unauthorized("You are not an approved driver.");
            }

            if (page.HasValue)
            {
                var completedOrders = driver.Order_Drivers
                    .SelectMany(od => od.Orders.Customer_orders)
                    .Where(co => co.Orders.Status == "Completed") // تصفية الطلبات المكتملة فقط
                    .OrderBy(co => co.Orders.OrderId);

                var totalCompletedOrders = completedOrders.Count();

                if (page.Value * pageSize >= totalCompletedOrders)
                {
                    return Json(new List<object>());
                }

                var orders = completedOrders
                    .Skip(page.Value * pageSize)
                    .Take(pageSize)
                    .Select(co => new
                    {
                        orderId = co.Orders.OrderId,
                        status = co.Orders.Status,
                        customerName = co.Customers.Name,
                        dropoffLocation = co.Orders.DropoffLocation,
                        pickupLocation = co.Orders.PickupLocation,
                        vehicleType = co.Orders.VehicleType,
                        eta = co.Orders.ETA,
                        track = co.Orders.TrackingNumber
                    })
                    .ToList();

                return Json(new { totalCount = totalCompletedOrders, orders }); // إعادة بيانات JSON عند طلب الصفحات
            }

            ViewBag.IsDriver = true;
            ViewBag.IsAdmin = false;
            return View(driver); 
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAvailability(bool isAvailable)
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var driver = db.Driver_table.FirstOrDefault(d => d.Id == int.Parse(driverId));

            if (driver != null)
            {
                driver.IsAvailable = isAvailable;
                db.SaveChanges();
            }

            return Json(new { success = true });
        }



      

        [HttpPost]
        public async Task<IActionResult> MarkOrderOnTheWay(int orderId)
        {
            var order = await db.Order_table
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Json(new { success = false, message = "Order not found." });

            order.Status = "On the Way";

            var driver = await db.Driver_table
                .FirstOrDefaultAsync(d => d.Id == order.OrderId);

            if (driver != null)
            {
                driver.Status = "Busy";
                await db.SaveChangesAsync();
            }

            await db.SaveChangesAsync();

            return Json(new { success = true, message = "Order marked as on the way." });
        }

        [HttpPost]
        public async Task<IActionResult> MarkOrderCompleted(int orderId)
        {
            var order = await db.Order_table
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Json(new { success = false, message = "Order not found." });

            order.Status = "Completed";

            var driver = await db.Driver_table
                .FirstOrDefaultAsync(d => d.Id == order.OrderId);

            if (driver != null)
            {
                driver.Status = "I'm available to take another order";
                await db.SaveChangesAsync();
            }

            await db.SaveChangesAsync();

            return Json(new { success = true, message = "Order marked as completed." });
        }



        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {

            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return NotFound();

            var model = new DriverViewModel
            {
                Name = driver.UserName,
                Email = driver.Email,
                phone = driver.PhoneNumber,
                VehicleType = driver.VehicleType,
                CurrentLocation = driver.CurrentLocation,
                Latitude = driver.Latitude,
                Longitude = driver.Longitude,
                ProfilePicture = driver.ProfilePicture,
                LicensePicture = driver.LicensePicture,
                workPermitPicture=driver.workPermitPicture
            };

            return View(model);
        }


        //[HttpPost]
        //public async Task<IActionResult> UpdateProfile(DriverViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var driver = await _userManager.GetUserAsync(User);
        //        if (driver != null)
        //        {
        //            model.ProfilePicture = driver.ProfilePicture;
        //            model.LicensePicture = driver.LicensePicture;
        //            model.workPermitPicture = driver.workPermitPicture;
        //        }

        //        return View(model);
        //    }

        //    var driverToUpdate = await _userManager.GetUserAsync(User);
        //    if (driverToUpdate == null) return NotFound();

        //    var existingEmail = await _userManager.FindByEmailAsync(model.Email);
        //    if (existingEmail != null && existingEmail.Id != driverToUpdate.Id)
        //    {
        //        ModelState.AddModelError("Email", "The email is already in use.");
        //        return View(model);
        //    }

        //    var existingUserName = await _userManager.FindByNameAsync(model.Name);
        //    if (existingUserName != null && existingUserName.Id != driverToUpdate.Id)
        //    {
        //        ModelState.AddModelError("Name", "The username is already in use.");
        //        return View(model);
        //    }

        //    var existingPhone = await _userManager.Users
        //        .FirstOrDefaultAsync(u => u.PhoneNumber == model.phone);
        //    if (existingPhone != null && existingPhone.Id != driverToUpdate.Id)
        //    {
        //        ModelState.AddModelError("phone", "The phone number is already in use.");
        //        return View(model);
        //    }

        //    driverToUpdate.UserName = model.Name;
        //    driverToUpdate.Email = model.Email;
        //    driverToUpdate.PhoneNumber = model.phone;
        //    driverToUpdate.VehicleType = model.VehicleType;
        //    driverToUpdate.CurrentLocation = model.CurrentLocation;
        //    driverToUpdate.Latitude = model.Latitude;
        //    driverToUpdate.Longitude = model.Longitude;

        //    var result = await _userManager.UpdateAsync(driverToUpdate);

        //    if (result.Succeeded)
        //    {
        //        return RedirectToAction("Dashboard");
        //    }

        //    foreach (var error in result.Errors)
        //    {
        //        ModelState.AddModelError(string.Empty, error.Description);
        //    }

        //    model.ProfilePicture = driverToUpdate.ProfilePicture;
        //    model.LicensePicture = driverToUpdate.LicensePicture;
        //    model.workPermitPicture = driverToUpdate.workPermitPicture;

        //    return View(model);
        //}


        [HttpPost]
        public async Task<IActionResult> UpdateProfile(DriverViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var driver = await _userManager.GetUserAsync(User);
                if (driver != null)
                {
                    model.ProfilePicture = driver.ProfilePicture;
                    model.LicensePicture = driver.LicensePicture;
                    model.workPermitPicture = driver.workPermitPicture;
                }

                return View(model);
            }

            var driverToUpdate = await _userManager.GetUserAsync(User);
            if (driverToUpdate == null)
                return NotFound();

            // فحص إذا الإيميل أو الاسم أو رقم الهاتف موجود في جداول أخرى غير السائق الحالي
            var existingDriver = await db.Driver_table
                .FirstOrDefaultAsync(d => d.Id != driverToUpdate.Id &&
                    (d.Email == model.Email || d.PhoneNumber == model.phone || d.UserName == model.Name));

            var existingAdmin = await db.Admin_table
                .FirstOrDefaultAsync(a =>
                    a.Email == model.Email || a.PhoneNumber == model.phone || a.UserName == model.Name);

            var existingHR = await db.HRs
                .FirstOrDefaultAsync(h =>
                    h.Email == model.Email || h.PhoneNumber == model.phone);

            // إذا وجد تطابقات نضيف الأخطاء
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

                // إذا كان هناك أي خطأ، نرجع النموذج مع البيانات
                if (!ModelState.IsValid)
                {
                    model.ProfilePicture = driverToUpdate.ProfilePicture;
                    model.LicensePicture = driverToUpdate.LicensePicture;
                    model.workPermitPicture = driverToUpdate.workPermitPicture;

                    return View(model);
                }
            }

            // التحديث بعد التأكد من أن البيانات غير مكررة
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
                return RedirectToAction("Dashboard");
            }

            // لو في أخطاء من Identity
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
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
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

                var driver = await _userManager.GetUserAsync(User);
                if (driver == null) return Json(new { success = false, message = "User not found" });

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
                return Json(new { success = false, message = "An error occurred while updating your profile picture." });
            }

            return Json(new { success = false, message = "No file selected." });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateLicensePicture(IFormFile licensePicture)
        {
            if (licensePicture != null && licensePicture.Length > 0)
            {
                string fileExtension = Path.GetExtension(licensePicture.FileName).ToLower();

                if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                {
                    return Json(new { success = false, message = "Invalid file type. Please upload a .jpg, .jpeg, or .png file." });
                }

                // تحديد مجلد التحميل
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

                var driver = await _userManager.GetUserAsync(User);
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

                // إنشاء مسار الملف الجديد
                string filePath = Path.Combine(subFolderPath, $"{Guid.NewGuid()}{fileExtension}");

                // حفظ الملف الجديد على الخادم
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await licensePicture.CopyToAsync(stream);
                }

                // حفظ مسار الصورة في الـ Driver
                driver.LicensePicture = Path.Combine("uploads", "licenses", Path.GetFileName(filePath));

                var result = await _userManager.UpdateAsync(driver);

                if (result.Succeeded)
                {
                    return Json(new { success = true, imageUrl = "/" + driver.LicensePicture });
                }

                return Json(new { success = false, message = "An error occurred while updating your license picture." });
            }
            else
            {
                return Json(new { success = false, message = "No file selected." });
            }
        }






        [HttpPost]
        public async Task<IActionResult> UpdateWorkPermitPicture(IFormFile workPermitPicture)
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

                var driver = await _userManager.GetUserAsync(User);
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

                return Json(new { success = false, message = "An error occurred while updating your work permit picture." });
            }
            else
            {
                return Json(new { success = false, message = "No file selected." });
            }
        }


    }
}
//[HttpPost]
//public async Task<IActionResult> MarkOrderOnTheWay(int orderId)
//{
//    var order = await db.Order_table
//        .FirstOrDefaultAsync(o => o.OrderId == orderId);

//    if (order == null)
//        return NotFound("Order not found.");

//    order.Status = "On the Way";

//    var driver = await db.Driver_table
//        .FirstOrDefaultAsync(d => d.Id == order.OrderId);

//    if (driver != null)
//    {
//        driver.Status = "Busy";
//        await db.SaveChangesAsync();
//    }

//    await db.SaveChangesAsync();

//    return RedirectToAction("Dashboard");
//}

//[HttpPost]
//public async Task<IActionResult> MarkOrderCompleted(int orderId)
//{
//    var order = await db.Order_table
//        .FirstOrDefaultAsync(o => o.OrderId == orderId);

//    if (order == null)
//        return NotFound("Order not found.");

//    order.Status = "Completed";

//    var driver = await db.Driver_table
//        .FirstOrDefaultAsync(d => d.Id == order.OrderId);

//    if (driver != null)
//    {
//        driver.Status = "I'm available to take another order";
//        await db.SaveChangesAsync();
//    }

//    await db.SaveChangesAsync();

//    return RedirectToAction("Dashboard");
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
//[HttpPost]
//public async Task<IActionResult> UpdateProfile(DriverViewModel model)
//{
//    if (!ModelState.IsValid)
//    {

//        return View(model);
//    }

//    var driver = await _userManager.GetUserAsync(User);
//    if (driver == null) return NotFound();

//    // Check if email already exists
//    var existingEmail = await _userManager.FindByEmailAsync(model.Email);
//    if (existingEmail != null && existingEmail.Id != driver.Id)
//    {
//        ModelState.AddModelError("Email", "The email is already in use.");
//        return View(model);
//    }

//    // Check if username already exists
//    var existingUserName = await _userManager.FindByNameAsync(model.Name);
//    if (existingUserName != null && existingUserName.Id != driver.Id)
//    {
//        ModelState.AddModelError("Name", "The username is already in use.");
//        return View(model);
//    }

//    // Check if phone number already exists
//    var existingPhone = await _userManager.Users
//        .FirstOrDefaultAsync(u => u.PhoneNumber == model.phone);
//    if (existingPhone != null && existingPhone.Id != driver.Id)
//    {
//        ModelState.AddModelError("phone", "The phone number is already in use.");
//        return View(model);
//    }

//    // Update driver details
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


//  public async Task<IActionResult> Dashboard(int? page = null, int pageSize = 10)
//  {
//      var driverIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

//      if (string.IsNullOrEmpty(driverIdString) || !int.TryParse(driverIdString, out int driverId))
//      {
//          return RedirectToAction("Home", "Home");
//      }

//      var driver = await db.Driver_table
//          .Include(d => d.Order_Drivers)
//              .ThenInclude(od => od.Orders)
//              .ThenInclude(o => o.Customer_orders)
//              .ThenInclude(co => co.Customers)
//          .Where(d => d.Id == driverId)
//          .FirstOrDefaultAsync();

//      if (driver == null)
//      {
//          return NotFound("Driver not found.");
//      }

//      if (!driver.IsDriver)
//      {
//          return Unauthorized("You are not an approved driver.");
//      }

//      if (page.HasValue)
//      {
//          // حساب عدد السجلات الكلي
//          var totalOrders = driver.Order_Drivers
//              .SelectMany(od => od.Orders.Customer_orders)
//              .Count();

//          if (page.Value * pageSize >= totalOrders)
//          {
//              return Json(new { totalCount = totalOrders, orders = new List<object>() });
//          }

//          var orders = driver.Order_Drivers
//.SelectMany(od => od.Orders.Customer_orders)  // ربط الطلبات مع العملاء
//.OrderBy(co => co.Orders.OrderId)  // ترتيب النتائج حسب OrderId من Order
//.Skip(page.Value * pageSize)  // تخطي العناصر بناءً على الصفحة والحجم
//.Take(pageSize)  // أخذ عدد معين من العناصر بناءً على الحجم
//.Select(co => new
//{
//    orderId = co.Orders.OrderId,  // استخدام OrderId من Orders
//        status = co.Orders.Status,  // استخدام Status من Orders
//        customerName = co.Customers.Name,  // استخدام اسم العميل
//        dropoffLocation = co.Orders.DropoffLocation,  // مكان التسليم
//        pickupLocation = co.Orders.PickupLocation,  // مكان الاستلام
//        vehicleType = co.Orders.VehicleType,  // نوع المركبة
//        TrackingNumber = co.Orders.TrackingNumber,  // رقم التتبع
//        Distance = co.Orders.Distance,
//    ETA = co.Orders.ETA  // الوقت المقدر للوصول
//    })
//.ToList();
//          // رسالة طباعة لعرض القيم
//          foreach (var order in orders)
//          {
//              Console.WriteLine($"Order ID: {order.orderId}, ETA: {order.ETA}, TrackingNumber: {order.TrackingNumber}");
//          }
//          return Json(new { totalCount = totalOrders, orders });
//      }

//      ViewBag.IsDriver = true;
//      ViewBag.IsAdmin = false;
//      return View(driver); // إعادة العرض عند فتح الصفحة الرئيسية
//  }



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

//    return RedirectToAction("Dashboard", "DriverDash");
//}
using Dal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
   

    public class Towing_Collection : IdentityDbContext<Admin, IdentityRole<int>, int>
    {
        public Towing_Collection(DbContextOptions<Towing_Collection> options) : base(options)
        {
        }

        public DbSet<Admin> Admin_table { get; set; }
        public DbSet<Driver> Driver_table { get; set; }
        public DbSet<Customer> Customer_table { get; set; }
        public DbSet<Order> Order_table { get; set; }
        public DbSet<Service> Service_table { get; set; }
        public DbSet<Customer_driver> Customer_driver_table { get; set; }
        public DbSet<Customer_order> Customer_order_table { get; set; }
        public DbSet<Order_Driver> Order_Driver_table { get; set; }
        public DbSet<ZipOffer> ZipOffer { get; set; }

        public DbSet<HR> HRs { get; set; }
        public DbSet<Dispatcher> Dispatchers { get; set; }

        public DbSet<OTPEntry> OTPEntries { get; set; }

        public DbSet<Provider> Provider { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تكوين العلاقات بين الجداول الخاصة بك
            modelBuilder.Entity<Customer_driver>()
                .HasOne(cd => cd.Drivers)
                .WithMany(d => d.Customer_drivers)
                .HasForeignKey(cd => cd.DriverId);

            modelBuilder.Entity<Order_Driver>()
                .HasKey(od => new { od.DriverId, od.OrderId });

            modelBuilder.Entity<Order_Driver>()
                .HasOne(od => od.Drivers)
                .WithMany(d => d.Order_Drivers)
                .HasForeignKey(od => od.DriverId);

            modelBuilder.Entity<Order_Driver>()
                .HasOne(od => od.Orders)
                .WithMany(o => o.Order_Drivers)
                .HasForeignKey(od => od.OrderId);
      //      modelBuilder.Entity<Order>()
      //.Property(o => o.TotalCost)
      //.HasColumnType("decimal(18,2)"); // 18 رقم كحد أقصى مع 2 أرقام بعد الفاصلة العشرية

      //      // تحديد دقة و مقياس العمود DID
      //      modelBuilder.Entity<Order>()
      //          .Property(o => o.DID)
      //          .HasColumnType("decimal(18,2)");
      //      modelBuilder.Entity<Customer>()
      //       .Property(c => c.Message_or_inquiry)
      //       .HasMaxLength(200);  // تحديد الحد الأقصى للطول 200 حرف

            modelBuilder.Entity<Order_Driver>()
     .Property(od => od.Order_DriverId)
     .ValueGeneratedOnAdd();  // التأكد

        modelBuilder.Entity<ZipOffer>().HasData(
         new ZipOffer { Id = 1, ZipCode = "20176", AreaName = "Leesburg", OfferDescription = "🚗 10% off on towing services in Leesburg" },
         new ZipOffer { Id = 2, ZipCode = "22030", AreaName = "Fairfax", OfferDescription = "🚨 Free emergency towing up to 5 miles in Fairfax" },
         new ZipOffer { Id = 3, ZipCode = "23464", AreaName = "Virginia Beach", OfferDescription = "🛠️ 15% off vehicle recovery services in Virginia Beach" },
         new ZipOffer { Id = 4, ZipCode = "24541", AreaName = "Danville", OfferDescription = "🚛 Buy 1 tow, get 50% off your next in Danville" },
         new ZipOffer { Id = 5, ZipCode = "23320", AreaName = "Chesapeake", OfferDescription = "🔧 20% discount on long-distance towing in Chesapeake" },
         new ZipOffer { Id = 6, ZipCode = "20147", AreaName = "Ashburn", OfferDescription = "🎁 Free battery jumpstart with every tow in Ashburn" },
         new ZipOffer { Id = 7, ZipCode = "22903", AreaName = "Charlottesville", OfferDescription = "🆘 First-time customers get 25% off towing in Charlottesville" },
         new ZipOffer { Id = 8, ZipCode = "24153", AreaName = "Salem", OfferDescription = "⛓️ Free lockout service with towing in Salem" },
         new ZipOffer { Id = 9, ZipCode = "23666", AreaName = "Hampton", OfferDescription = "📦 Free tire change with towing in Hampton" },
         new ZipOffer { Id = 10, ZipCode = "23223", AreaName = "Richmond", OfferDescription = "🔥 Special offer: 30% off towing this week in Richmond" }
     );


                   





        }

    }
}

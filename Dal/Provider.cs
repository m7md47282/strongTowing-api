using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal
{
   public class Provider
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Display(Name = "Owner's First Name")]
        public string OwnerFirstName { get; set; }

        [Required]
        [Display(Name = "Owner's Last Name")]
        public string OwnerLastName { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required]
        [Display(Name = "Have you previously applied or been in-network with us?")]
        public string PreviouslyApplied { get; set; }

        [Required]
        [Display(Name = "Contact Phone Number")]
        public string ContactPhoneNumber { get; set; }

        [Required]
        [Display(Name = "Is this a mobile phone number?")]
        public string IsMobilePhone { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; }

        [Required]
        [Display(Name = "Who has access to this inbox?")]
        public string InboxAccess { get; set; }

        [Required]
        [Display(Name = "Facility Address")]
        public string FacilityAddress { get; set; }

        [Display(Name = "Facility Address Line 2")]
        public string FacilityAddressLine2 { get; set; }

        [Required]
        [Display(Name = "Facility City")]
        public string FacilityCity { get; set; }

        [Required]
        [Display(Name = "Facility State")]
        public string FacilityState { get; set; }

        [Required]
        [Display(Name = "Facility Zip")]
        public string FacilityZip { get; set; }

        [Required]
        [Display(Name = "Is your billing address the same as your facility address?")]
        public string BillingSameAsFacility { get; set; }

        /*2*/
        [Required]

        [Display(Name = "Please select all services you would like to perform with us")]
        public string SelectedServices { get; set; }
       
        [Required]

        [Display(Name = "Are you open 24/7?")]
        public string IsOpen247 { get; set; }

        [Display(Name = "Requested territory")]
        public string RequestedTerritory { get; set; }

        [Display(Name = "If you'd prefer to type in your zip code list, please do so here")]
        public string ZipCodeList { get; set; }
        [Required]

        [Display(Name = "Will you be providing any towing services in either Nevada or Texas?")]
        public string WillProvideTowingInNVorTX { get; set; }


        /*3*/

        [Display(Name = "Please upload your accurate, complete and signed W-9")]
        public string W9FilePath { get; set; }

        [Required(ErrorMessage = "The address on your W-9 form is required.")]
        [Display(Name = "The address on my W-9 form is")]
        public string W9Address { get; set; }

        [Required(ErrorMessage = "You must certify that you are not subject to backup withholding.")]
        [Display(Name = "I certify that I am not subject to backup withholding")]

        public bool IsNotSubjectToBackupWithholding { get; set; }

        [Required(ErrorMessage = "Electronic Signature is required.")]
        [Display(Name = "Electronic Signature")]
        public string ElectronicSignature { get; set; }

        [Required(ErrorMessage = "Please enter the number of employees.")]
        [Display(Name = "Number of Employees (including yourself)")]
        public int NumberOfEmployees { get; set; }

        [Display(Name = "Please upload background check documentation (ZIP file)")]
        public string BackgroundFilesPaths { get; set; }

        /*4*/
        [Display(Name = "Certificate of Insurance")]
        public string CertificateInsuranceFilePath { get; set; }

        /*5*/
        [Required(ErrorMessage = "Please specify how long your company has been in business.")]
        [Display(Name = "How long has your company been in business?")]
        public string CompanyBusinessDuration { get; set; } // نص أو يمكن تغيره إلى enum

        [Required(ErrorMessage = "Please specify if you have experience handling Electric Vehicles.")]
        [Display(Name = "Do you have experience handling Electric Vehicles?")]
        public string HasElectricVehicleExperience { get; set; }

        [Required(ErrorMessage = "Please specify if you currently use digital dispatch software.")]
        [Display(Name = "Do you currently use digital dispatch software?")]
        public string UsesDigitalDispatchSoftware { get; set; }

        [Display(Name = "Do you consider your business to be any of the below?")]
        public string BusinessTypes { get; set; } // ممكن نص لتخزين القيم المفصولة بفواصل


    }
}

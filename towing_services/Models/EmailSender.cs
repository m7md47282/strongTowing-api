using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using MimeKit;  // إضافة this using statement
using MailKit.Net.Smtp;  // إضافة this using statement
using Microsoft.Extensions.Options;

namespace towing_services.Models
{

    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string recipientEmail, string subject, string body);
    }
    public class EmailSender : IEmailSender
    {
        private readonly string _smtpServer = "smtp.zoho.com";
        private readonly int _smtpPort = 587;
        private readonly string _senderEmail = "admin@strongtowing.services";
        private readonly string _senderPassword = "g96i VU01 xVY2";

        public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
        {
            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("Towing Services", _senderEmail));
                emailMessage.To.Add(new MailboxAddress("Admin", recipientEmail));
                emailMessage.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                using (var smtpClient = new MailKit.Net.Smtp.SmtpClient())
                {
                    await smtpClient.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await smtpClient.AuthenticateAsync(_senderEmail, _senderPassword);
                    await smtpClient.SendAsync(emailMessage);
                    await smtpClient.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                // سجل الخطأ لمراجعة السبب الحقيقي
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

    }
}

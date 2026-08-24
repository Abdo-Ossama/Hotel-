using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Hotel_Booking.Utilities
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {

                EnableSsl = true,
                UseDefaultCredentials = false ,
                Credentials = new NetworkCredential("abdoosama24101975@gmail.com" , "hbvc fqft kepe kepe")
                

            };
            return client.SendMailAsync(
                new MailMessage(from: "abdoosama24101975@gmail.com",
                to: email,
                subject,
                htmlMessage)
                );
                
                

        }
    }
}

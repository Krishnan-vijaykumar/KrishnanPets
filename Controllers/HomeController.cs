using DemoWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using MimeKit;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Mail;

namespace DemoWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }


        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Main()
        {
            var sessionKey = HttpContext.Session.GetString("MySessionKey");
            ViewBag.SessionKey = sessionKey;
            return View();
        }
        public IActionResult Mail()
        {
            return View();
        }
        [HttpPost]
        public JsonResult CheckLogin(string username, string password, string confirmPassword, string Flag)
        {
            try
            {
                confirmPassword = confirmPassword ?? "";
                var connectionString = _configuration.AsEnumerable().Where(wh=>wh.Key==("ConnectionString")).First().Value.ToString();
                ViewData["ConnectionString"] = connectionString;
                DataTable dataTable = new DataTable();
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var cmd = new SqlCommand("UserLogin", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);
                        cmd.Parameters.AddWithValue("@Flag", Flag);

                        if (Flag == "SELECT")
                        {
                            using (var reader = new SqlDataAdapter(cmd))
                            {
                                reader.Fill(dataTable);
                            }
                        }
                        else
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    // Process the results
                                    var res=reader["ColumnName"].ToString();
                                }
                            }
                        }

                    }
                }
                if (dataTable.Rows.Count > 0 )
                {
                    if(Flag != "SELECT")
                    { }
                    var SessionValue = dataTable.Rows[0][2].ToString();
                    HttpContext.Session.SetString("MySessionKey", SessionValue);
                    string redirectUrl = Url.Action("Main", "Home");

                    return Json(new { success = true, redirectUrl = redirectUrl });
                }
                else if (Flag != "SELECT")
                {
                    string redirectUrl = Url.Action("Index", "Home");

                    return Json(new { success = true, redirectUrl = redirectUrl });
                }
                else
                    return Json(new { success = false, MESSAGE = "No Data Match" });
            }
            catch (Exception Ex)
            {

                return Json(new { success = false, MESSAGE = Ex });
            }
        }


        [HttpPost]
        public JsonResult SendEmail(string from, string to, string subject, string body, string useSsl)
        {
            try
            {

                var connectionString = _configuration.AsEnumerable().Where(wh => wh.Key == ("ConnectionString")).First().Value.ToString();
    
                DataTable dataTable = new DataTable();
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var cmd = new SqlCommand("sp_MailDetails", con))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@from", from);
                        cmd.Parameters.AddWithValue("@Flag", "SELECT");

                        
                            using (var reader = new SqlDataAdapter(cmd))
                            {
                                reader.Fill(dataTable);
                            }
                        

                    }
                }
                if (dataTable.Rows.Count > 0)
                {
                    var userName = dataTable.Rows[0][0].ToString();
                    var Password = dataTable.Rows[0][1].ToString();

                    var message = new MimeMessage();

                    message.From.Add(new MailboxAddress(from, from));
                    message.To.Add(new MailboxAddress(to, to));
                    message.Subject = subject??"How you doin'?";
                    message.Body = new TextPart("plain") { Text = body };
                    try
                    {

                   
                    using (var client = new MailKit.Net.Smtp.SmtpClient())
                    {
                        client.Connect("smtp.gmail.com", 587);

                        client.Authenticate(userName, Password);

                        client.Send(message);
                        client.Disconnect(true);
                    }
                    }
                    catch (Exception Ex)
                    {
                        return Json(new { success = false, MESSAGE = Ex.Message});
                    }

                    return Json(new { success = true, MESSAGE = "Mail Send successfully" });
                }
                else
                    return Json(new { success = false, MESSAGE = "No Data Match" });
            }
            catch (Exception Ex)
            {

                return Json(new { success = false, MESSAGE = Ex });
            }
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult RedirectBasedOnSession()
        {
            var route = HttpContext.Session.GetString("MySessionKey");

            if (route !="")
            {
                return RedirectToAction("Home", "HomeController");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }
    }

}

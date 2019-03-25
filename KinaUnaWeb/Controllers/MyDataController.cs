using System;
using System.Drawing;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace KinaUnaWeb.Controllers
{
    public class MyDataController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;

        public MyDataController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ChildSpreadSheet(int progenyId)
        {
            Progeny prog = await _progenyHttpClient.GetProgeny(progenyId);
            var stream = new System.IO.MemoryStream();
            using (ExcelPackage package = new ExcelPackage(stream))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Profile");

                worksheet.Cells[1, 1].Value = Constants.AppName + " DATA for " + prog.NickName;

                using (ExcelRange titleRange = worksheet.Cells[1, 1, 1, 6])
                {
                    titleRange.Merge = true;
                    titleRange.Style.Font.SetFromFont(new Font("Arial", 28, FontStyle.Bold));
                    titleRange.Style.Font.Color.SetColor(Color.White);
                    titleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(75, 0, 100));
                }

                worksheet.Cells[2, 1].Value = "ID";
                worksheet.Cells[3, 1].Value = prog.Id;
                worksheet.Cells[2, 2].Value = "Display Name";
                worksheet.Cells[3, 2].Value = prog.NickName;
                worksheet.Cells[2, 3].Value = "Name";
                worksheet.Cells[3, 3].Value = prog.Name;
                worksheet.Cells[2, 4].Value = "Birthday";
                if (prog.BirthDay.HasValue)
                {
                    worksheet.Cells[3, 4].Value = prog.BirthDay.Value.ToString("dd-MMM-yyyy HH:mm");
                }
                worksheet.Cells[2, 5].Value = "Administrators";
                worksheet.Cells[3, 5].Value = prog.Admins;
                worksheet.Cells[2, 6].Value = "Timezone";
                worksheet.Cells[3, 6].Value = prog.TimeZone;

                using (ExcelRange titleRange = worksheet.Cells[2, 1, 2, 6])
                {
                    titleRange.Style.Font.SetFromFont(new Font("Arial", 12, FontStyle.Bold));
                    titleRange.Style.Font.Color.SetColor(Color.DarkBlue);
                    titleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(Color.MediumPurple);
                }

                worksheet.Row(1).Height = 45.0;
                worksheet.Row(2).Style.Font.Bold = true;
                worksheet.Row(2).Height = 30.0;
                worksheet.Column(1).Width = 100;
                worksheet.Column(2).Width = 250;
                worksheet.Column(3).Width = 350;
                worksheet.Column(4).Width = 200;
                worksheet.Column(5).Width = 350;
                worksheet.Column(6).Width = 300;

                package.Save();
            }

            string fileName = Constants.AppName + "_Data_" + prog.NickName + ".xlsx";
            string fileType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            stream.Position = 0;

            return File(stream, fileType, fileName);
        }
    }
}
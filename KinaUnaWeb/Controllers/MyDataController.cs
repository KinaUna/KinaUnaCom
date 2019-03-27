using System;
using System.Collections.Generic;
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
        private readonly ImageStore _imageStore;

        public MyDataController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<FileResult> ChildSpreadSheet(int progenyId)
        {
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? Constants.DefaultTimezone;
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = Constants.DefaultTimezone;
            }

            Progeny prog = await _progenyHttpClient.GetProgeny(progenyId);

            var stream = new System.IO.MemoryStream();
            using (ExcelPackage package = new ExcelPackage(stream))
            {
                // Profile sheet
                ExcelWorksheet profileWorksheet = package.Workbook.Worksheets.Add("Profile");

                profileWorksheet.Cells[1, 1].Value = Constants.AppName + " Profile DATA for " + prog.NickName;

                using (ExcelRange titleRange = profileWorksheet.Cells[1, 1, 1, 6])
                {
                    titleRange.Merge = true;
                    titleRange.Style.Font.SetFromFont(new Font("Arial", 28, FontStyle.Bold));
                    titleRange.Style.Font.Color.SetColor(Color.White);
                    titleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(75, 0, 100));
                }

                profileWorksheet.Cells[2, 1].Value = "ID";
                profileWorksheet.Cells[3, 1].Value = prog.Id;
                profileWorksheet.Cells[2, 2].Value = "Display Name";
                profileWorksheet.Cells[3, 2].Value = prog.NickName;
                profileWorksheet.Cells[2, 3].Value = "Name";
                profileWorksheet.Cells[3, 3].Value = prog.Name;
                profileWorksheet.Cells[2, 4].Value = "Birthday";
                if (prog.BirthDay.HasValue)
                {
                    profileWorksheet.Cells[3, 4].Value = prog.BirthDay.Value.ToString("dd-MMM-yyyy HH:mm");
                }
                profileWorksheet.Cells[2, 5].Value = "Administrators";
                profileWorksheet.Cells[3, 5].Value = prog.Admins;
                profileWorksheet.Cells[2, 6].Value = "Timezone";
                profileWorksheet.Cells[3, 6].Value = prog.TimeZone;

                using (ExcelRange headerRange = profileWorksheet.Cells[2, 1, 2, 6])
                {
                    headerRange.Style.Font.SetFromFont(new Font("Arial", 11, FontStyle.Bold));
                    headerRange.Style.Font.Color.SetColor(Color.DarkBlue);
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.MediumPurple);
                }

                profileWorksheet.Row(1).Height = 45.0;
                profileWorksheet.Row(2).Style.Font.Bold = true;
                profileWorksheet.Row(2).Height = 30.0;
                profileWorksheet.Column(1).Width = 10;
                profileWorksheet.Column(2).Width = 35;
                profileWorksheet.Column(3).Width = 50;
                profileWorksheet.Column(4).Width = 25;
                profileWorksheet.Column(5).Width = 70;
                profileWorksheet.Column(6).Width = 30;

                // Access List sheet
                ExcelWorksheet accessListWorksheet = package.Workbook.Worksheets.Add("Access List");

                accessListWorksheet.Cells[1, 1].Value = Constants.AppName + " Access DATA for " + prog.NickName;

                using (ExcelRange titleRange = accessListWorksheet.Cells[1, 1, 1, 4])
                {
                    titleRange.Merge = true;
                    titleRange.Style.Font.SetFromFont(new Font("Arial", 28, FontStyle.Bold));
                    titleRange.Style.Font.Color.SetColor(Color.White);
                    titleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(75, 0, 100));
                }

                accessListWorksheet.Cells[2, 1].Value = "User Name";
                accessListWorksheet.Cells[2, 2].Value = "Full Name";
                accessListWorksheet.Cells[2, 3].Value = "Email";
                accessListWorksheet.Cells[2, 4].Value = "Access Level";

                using (ExcelRange headerRange = accessListWorksheet.Cells[2, 1, 2, 4])
                {
                    headerRange.Style.Font.SetFromFont(new Font("Arial", 11, FontStyle.Bold));
                    headerRange.Style.Font.Color.SetColor(Color.DarkBlue);
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.MediumPurple);
                }

                accessListWorksheet.Row(1).Height = 45.0;
                accessListWorksheet.Row(2).Style.Font.Bold = true;
                accessListWorksheet.Row(2).Height = 30.0;
                accessListWorksheet.Column(1).Width = 35;
                accessListWorksheet.Column(2).Width = 35;
                accessListWorksheet.Column(3).Width = 35;
                accessListWorksheet.Column(4).Width = 20;

                List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(progenyId);
                int acccessRowNumber = 3;
                foreach (UserAccess access in accessList)
                {
                    accessListWorksheet.Cells[acccessRowNumber, 1].Value = access.User.UserName;
                    string fullName = access.User.FirstName + " " + access.User.MiddleName + " " + access.User.LastName;
                    accessListWorksheet.Cells[acccessRowNumber, 2].Value = fullName.Trim();
                    accessListWorksheet.Cells[acccessRowNumber, 3].Value = access.User.Email;
                    accessListWorksheet.Cells[acccessRowNumber, 4].Value = access.AccessLevel;

                    acccessRowNumber++;
                }

                // Photos sheet
                ExcelWorksheet photosWorksheet = package.Workbook.Worksheets.Add("Photos");

                photosWorksheet.Cells[1, 1].Value = Constants.AppName + " Photo DATA for " + prog.NickName;

                using (ExcelRange titleRange = photosWorksheet.Cells[1, 1, 1, 12])
                {
                    titleRange.Merge = true;
                    titleRange.Style.Font.SetFromFont(new Font("Arial", 28, FontStyle.Bold));
                    titleRange.Style.Font.Color.SetColor(Color.White);
                    titleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(75, 0, 100));
                }

                photosWorksheet.Cells[2, 1].Value = "Photo ID";
                photosWorksheet.Cells[2, 2].Value = "Date and Time";
                photosWorksheet.Cells[2, 3].Value = "Access Level";
                photosWorksheet.Cells[2, 4].Value = "Tags";
                photosWorksheet.Cells[2, 5].Value = "Link";
                photosWorksheet.Cells[2, 6].Value = "Width";
                photosWorksheet.Cells[2, 7].Value = "Height";
                photosWorksheet.Cells[2, 8].Value = "Rotation";
                photosWorksheet.Cells[2, 9].Value = "Location";
                photosWorksheet.Cells[2, 10].Value = "Longitude";
                photosWorksheet.Cells[2, 11].Value = "Latitude";
                photosWorksheet.Cells[2, 12].Value = "Altitude";

                using (ExcelRange headerRange = photosWorksheet.Cells[2, 1, 2, 12])
                {
                    headerRange.Style.Font.SetFromFont(new Font("Arial", 11, FontStyle.Bold));
                    headerRange.Style.Font.Color.SetColor(Color.DarkBlue);
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.MediumPurple);
                }

                photosWorksheet.Row(1).Height = 45.0;
                photosWorksheet.Row(2).Style.Font.Bold = true;
                photosWorksheet.Row(2).Height = 30.0;
                photosWorksheet.Column(1).Width = 10;
                photosWorksheet.Column(2).Width = 24;
                photosWorksheet.Column(3).Width = 15;
                photosWorksheet.Column(4).Width = 50;
                photosWorksheet.Column(5).Width = 55;
                photosWorksheet.Column(6).Width = 10;
                photosWorksheet.Column(7).Width = 10;
                photosWorksheet.Column(8).Width = 10;
                photosWorksheet.Column(9).Width = 30;
                photosWorksheet.Column(10).Width = 20;
                photosWorksheet.Column(11).Width = 20;
                photosWorksheet.Column(12).Width = 20;

                List<Picture> photoList = await _mediaHttpClient.GetPictureList(progenyId, 0, userTimeZone);
                int photoRowNumber = 3;
                foreach (Picture pic in photoList)
                {
                    photosWorksheet.Cells[photoRowNumber, 1].Value = pic.PictureId;
                    photosWorksheet.Cells[photoRowNumber, 2].Value = pic.PictureTime.HasValue ? pic.PictureTime.Value.ToString("dd-MMM-yyyy HH:mm:ss") : "";
                    photosWorksheet.Cells[photoRowNumber, 3].Value = pic.AccessLevel;
                    photosWorksheet.Cells[photoRowNumber, 4].Value = pic.Tags;
                    photosWorksheet.Cells[photoRowNumber, 5].Value = Constants.WebAppUrl + "/Pictures/Picture/" + pic.PictureId;
                    photosWorksheet.Cells[photoRowNumber, 6].Value = pic.PictureWidth;
                    photosWorksheet.Cells[photoRowNumber, 7].Value = pic.PictureHeight;
                    photosWorksheet.Cells[photoRowNumber, 8].Value = pic.PictureRotation;
                    photosWorksheet.Cells[photoRowNumber, 9].Value = pic.Location;
                    photosWorksheet.Cells[photoRowNumber, 10].Value = pic.Longtitude;
                    photosWorksheet.Cells[photoRowNumber, 11].Value = pic.Latitude;
                    photosWorksheet.Cells[photoRowNumber, 12].Value = pic.Altitude;
                    photoRowNumber++;
                }

                // Videos sheet
                ExcelWorksheet videosWorksheet = package.Workbook.Worksheets.Add("Videos");

                videosWorksheet.Cells[1, 1].Value = Constants.AppName + " Video DATA for " + prog.NickName;

                using (ExcelRange titleRange = videosWorksheet.Cells[1, 1, 1, 12])
                {
                    titleRange.Merge = true;
                    titleRange.Style.Font.SetFromFont(new Font("Arial", 28, FontStyle.Bold));
                    titleRange.Style.Font.Color.SetColor(Color.White);
                    titleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(75, 0, 100));
                }

                videosWorksheet.Cells[2, 1].Value = "Video ID";
                videosWorksheet.Cells[2, 2].Value = "Date and Time";
                videosWorksheet.Cells[2, 3].Value = "Access Level";
                videosWorksheet.Cells[2, 4].Value = "Tags";
                videosWorksheet.Cells[2, 5].Value = "Link";
                videosWorksheet.Cells[2, 6].Value = "Duration";
                videosWorksheet.Cells[2, 7].Value = "Type";
                videosWorksheet.Cells[2, 8].Value = "Thumbnail Link";
                videosWorksheet.Cells[2, 9].Value = "Location";
                videosWorksheet.Cells[2, 10].Value = "Longitude";
                videosWorksheet.Cells[2, 11].Value = "Latitude";
                videosWorksheet.Cells[2, 12].Value = "Altitude";

                using (ExcelRange headerRange = videosWorksheet.Cells[2, 1, 2, 12])
                {
                    headerRange.Style.Font.SetFromFont(new Font("Arial", 11, FontStyle.Bold));
                    headerRange.Style.Font.Color.SetColor(Color.DarkBlue);
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.MediumPurple);
                }

                videosWorksheet.Row(1).Height = 45.0;
                videosWorksheet.Row(2).Style.Font.Bold = true;
                videosWorksheet.Row(2).Height = 30.0;
                videosWorksheet.Column(1).Width = 10;
                videosWorksheet.Column(2).Width = 24;
                videosWorksheet.Column(3).Width = 15;
                videosWorksheet.Column(4).Width = 50;
                videosWorksheet.Column(5).Width = 55;
                videosWorksheet.Column(6).Width = 10;
                videosWorksheet.Column(7).Width = 10;
                videosWorksheet.Column(8).Width = 55;
                videosWorksheet.Column(9).Width = 30;
                videosWorksheet.Column(10).Width = 20;
                videosWorksheet.Column(11).Width = 20;
                videosWorksheet.Column(12).Width = 20;

                List<Video> videoList = await _mediaHttpClient.GetVideoList(progenyId, 0, userTimeZone);
                int videoRowNumber = 3;
                foreach (Video vid in videoList)
                {
                    videosWorksheet.Cells[videoRowNumber, 1].Value = vid.VideoId;
                    videosWorksheet.Cells[videoRowNumber, 2].Value = vid.VideoTime.HasValue ? vid.VideoTime.Value.ToString("dd-MMM-yyyy HH:mm:ss") : "";
                    videosWorksheet.Cells[videoRowNumber, 3].Value = vid.AccessLevel;
                    videosWorksheet.Cells[videoRowNumber, 4].Value = vid.Tags;
                    videosWorksheet.Cells[videoRowNumber, 5].Value = Constants.WebAppUrl + "/Videos/Video/" + vid.VideoId;
                    videosWorksheet.Cells[videoRowNumber, 6].Value = vid.Duration.HasValue ? vid.Duration.Value.ToString("g") : "";
                    videosWorksheet.Cells[videoRowNumber, 7].Value = vid.VideoType;
                    videosWorksheet.Cells[videoRowNumber, 8].Value = vid.ThumbLink;
                    videosWorksheet.Cells[videoRowNumber, 9].Value = vid.Location;
                    videosWorksheet.Cells[videoRowNumber, 10].Value = vid.Longtitude;
                    videosWorksheet.Cells[videoRowNumber, 11].Value = vid.Latitude;
                    videosWorksheet.Cells[videoRowNumber, 12].Value = vid.Altitude;
                    videoRowNumber++;
                }


                // Calendar sheet
                ExcelWorksheet calendarWorksheet = package.Workbook.Worksheets.Add("Calendar");

                calendarWorksheet.Cells[1, 1].Value = Constants.AppName + " Calendar DATA for " + prog.NickName;

                using (ExcelRange titleRange = calendarWorksheet.Cells[1, 1, 1, 9])
                {
                    titleRange.Merge = true;
                    titleRange.Style.Font.SetFromFont(new Font("Arial", 28, FontStyle.Bold));
                    titleRange.Style.Font.Color.SetColor(Color.White);
                    titleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(75, 0, 100));
                }

                calendarWorksheet.Cells[2, 1].Value = "Event ID";
                calendarWorksheet.Cells[2, 2].Value = "Access Level";
                calendarWorksheet.Cells[2, 3].Value = "Start Date and Time";
                calendarWorksheet.Cells[2, 4].Value = "End Date and Time";
                calendarWorksheet.Cells[2, 5].Value = "Title";
                calendarWorksheet.Cells[2, 6].Value = "Notes";
                calendarWorksheet.Cells[2, 7].Value = "Location";
                calendarWorksheet.Cells[2, 8].Value = "Context";
                calendarWorksheet.Cells[2, 9].Value = "All Day";


                using (ExcelRange headerRange = calendarWorksheet.Cells[2, 1, 2, 9])
                {
                    headerRange.Style.Font.SetFromFont(new Font("Arial", 11, FontStyle.Bold));
                    headerRange.Style.Font.Color.SetColor(Color.DarkBlue);
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.MediumPurple);
                }

                calendarWorksheet.Row(1).Height = 45.0;
                calendarWorksheet.Row(2).Style.Font.Bold = true;
                calendarWorksheet.Row(2).Height = 30.0;
                calendarWorksheet.Column(1).Width = 10;
                calendarWorksheet.Column(2).Width = 10;
                calendarWorksheet.Column(3).Width = 25;
                calendarWorksheet.Column(4).Width = 25;
                calendarWorksheet.Column(5).Width = 30;
                calendarWorksheet.Column(6).Width = 40;
                calendarWorksheet.Column(7).Width = 25;
                calendarWorksheet.Column(8).Width = 15;
                calendarWorksheet.Column(9).Width = 10;

                List<CalendarItem> calendarItems = await _progenyHttpClient.GetCalendarList(progenyId, 0);
                int calendarRowNumber = 3;
                foreach (CalendarItem evt in calendarItems)
                {
                    calendarWorksheet.Cells[calendarRowNumber, 1].Value = evt.EventId;
                    calendarWorksheet.Cells[calendarRowNumber, 2].Value = evt.AccessLevel;
                    if (evt.StartTime.HasValue)
                    {
                        evt.StartTime = TimeZoneInfo.ConvertTimeFromUtc(evt.StartTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                        calendarWorksheet.Cells[calendarRowNumber, 3].Value = evt.StartTime.Value.ToString("dd-MMM-yyyy HH:mm");
                    }
                    if (evt.EndTime.HasValue)
                    {
                        evt.EndTime = TimeZoneInfo.ConvertTimeFromUtc(evt.EndTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                        calendarWorksheet.Cells[calendarRowNumber, 4].Value = evt.EndTime.Value.ToString("dd-MMM-yyyy HH:mm");
                    }
                    calendarWorksheet.Cells[calendarRowNumber, 5].Value = evt.Title;
                    calendarWorksheet.Cells[calendarRowNumber, 6].Value = evt.Notes;
                    calendarWorksheet.Cells[calendarRowNumber, 7].Value = evt.Location;
                    calendarWorksheet.Cells[calendarRowNumber, 8].Value = evt.Context;
                    calendarWorksheet.Cells[calendarRowNumber, 9].Value = evt.AllDay;
                    calendarRowNumber++;
                }

                // Locations sheet
                ExcelWorksheet locationsWorksheet = package.Workbook.Worksheets.Add("Locations");

                locationsWorksheet.Cells[1, 1].Value = Constants.AppName + " Location DATA for " + prog.NickName;

                using (ExcelRange titleRange = locationsWorksheet.Cells[1, 1, 1, 16])
                {
                    titleRange.Merge = true;
                    titleRange.Style.Font.SetFromFont(new Font("Arial", 28, FontStyle.Bold));
                    titleRange.Style.Font.Color.SetColor(Color.White);
                    titleRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(75, 0, 100));
                }

                locationsWorksheet.Cells[2, 1].Value = "Location ID";
                locationsWorksheet.Cells[2, 2].Value = "Access Level";
                locationsWorksheet.Cells[2, 3].Value = "Name";
                locationsWorksheet.Cells[2, 4].Value = "Longitude";
                locationsWorksheet.Cells[2, 5].Value = "Latitude";
                locationsWorksheet.Cells[2, 6].Value = "Date";
                locationsWorksheet.Cells[2, 7].Value = "Street";
                locationsWorksheet.Cells[2, 8].Value = "House Number";
                locationsWorksheet.Cells[2, 9].Value = "Postal Code";
                locationsWorksheet.Cells[2, 10].Value = "City";
                locationsWorksheet.Cells[2, 11].Value = "District";
                locationsWorksheet.Cells[2, 12].Value = "County";
                locationsWorksheet.Cells[2, 13].Value = "State";
                locationsWorksheet.Cells[2, 14].Value = "Country";
                locationsWorksheet.Cells[2, 15].Value = "Notes";
                locationsWorksheet.Cells[2, 16].Value = "Tags";

                using (ExcelRange headerRange = locationsWorksheet.Cells[2, 1, 2, 16])
                {
                    headerRange.Style.Font.SetFromFont(new Font("Arial", 11, FontStyle.Bold));
                    headerRange.Style.Font.Color.SetColor(Color.DarkBlue);
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.MediumPurple);
                }

                locationsWorksheet.Row(1).Height = 45.0;
                locationsWorksheet.Row(2).Style.Font.Bold = true;
                locationsWorksheet.Row(2).Height = 30.0;
                locationsWorksheet.Column(1).Width = 10;
                locationsWorksheet.Column(2).Width = 10;
                locationsWorksheet.Column(3).Width = 35;
                locationsWorksheet.Column(4).Width = 15;
                locationsWorksheet.Column(5).Width = 15;
                locationsWorksheet.Column(6).Width = 15;
                locationsWorksheet.Column(7).Width = 35;
                locationsWorksheet.Column(8).Width = 10;
                locationsWorksheet.Column(9).Width = 15;
                locationsWorksheet.Column(10).Width = 25;
                locationsWorksheet.Column(11).Width = 25;
                locationsWorksheet.Column(12).Width = 25;
                locationsWorksheet.Column(13).Width = 25;
                locationsWorksheet.Column(14).Width = 25;
                locationsWorksheet.Column(15).Width = 35;
                locationsWorksheet.Column(16).Width = 25;

                List<Location> locationItems = await _progenyHttpClient.GetLocationsList(progenyId, 0);
                int locationsRowNumber = 3;
                foreach (Location loc in locationItems)
                {
                    locationsWorksheet.Cells[locationsRowNumber, 1].Value = loc.LocationId;
                    locationsWorksheet.Cells[locationsRowNumber, 2].Value = loc.AccessLevel;
                    locationsWorksheet.Cells[locationsRowNumber, 3].Value = loc.Name;
                    locationsWorksheet.Cells[locationsRowNumber, 4].Value = loc.Longitude;
                    locationsWorksheet.Cells[locationsRowNumber, 5].Value = loc.Latitude;
                    if (loc.Date.HasValue)
                    {
                        locationsWorksheet.Cells[locationsRowNumber, 6].Value = loc.Date.Value.ToString("dd-MMM-yyyy");
                    }
                    locationsWorksheet.Cells[locationsRowNumber, 7].Value = loc.StreetName;
                    locationsWorksheet.Cells[locationsRowNumber, 8].Value = loc.HouseNumber;
                    locationsWorksheet.Cells[locationsRowNumber, 9].Value = loc.PostalCode;
                    locationsWorksheet.Cells[locationsRowNumber, 10].Value = loc.City;
                    locationsWorksheet.Cells[locationsRowNumber, 11].Value = loc.District;
                    locationsWorksheet.Cells[locationsRowNumber, 12].Value = loc.County;
                    locationsWorksheet.Cells[locationsRowNumber, 13].Value = loc.State;
                    locationsWorksheet.Cells[locationsRowNumber, 14].Value = loc.Country;
                    locationsWorksheet.Cells[locationsRowNumber, 15].Value = loc.Notes;
                    locationsWorksheet.Cells[locationsRowNumber, 16].Value = loc.Tags;
                    locationsRowNumber++;
                }

                // Vocabulary sheet

                // Skills sheet

                // Friends sheet

                // Measurements sheet

                // Sleep sheet

                // Contacts sheet

                // Vaccinations sheet
                package.Save();
            }

            string fileName = Constants.AppName + "_Data_" + prog.NickName + ".xlsx";
            string fileType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            stream.Position = 0;

            return File(stream, fileType, fileName);
        }
    }
}
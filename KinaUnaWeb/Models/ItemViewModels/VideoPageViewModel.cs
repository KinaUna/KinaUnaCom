﻿using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VideoPageViewModel
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int SortBy { get; set; }
        public List<Video> VideosList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }
        public string TagFilter { get; set; }
        public string TagsList { get; set; }
    }
}

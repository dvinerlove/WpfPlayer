using System;
using System.Collections.Generic;
using System.Text;
using WpfPlayer.Services;

namespace WpfPlayer.Models
{
    [Serializable]
    public class AudioS
    {
        public string Author { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; }
        public string Url { get; set; }
        public string Thumbnail { get; set; }



        public AudioS(string Author = "", string Title = "", string Description = "", string Duration = "", string Url = "", string Thumbnail = "")
        {
            this.Author = Author;
            this.Title = Title;
            this.Description = Description;
            this.Duration = Duration;
            this.Url = Url;
            this.Thumbnail = Thumbnail;
        }
    }
}

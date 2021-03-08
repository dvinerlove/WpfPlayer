using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfPlayer.Models
{
    public class Audio
    {
        public int id { get; set; }
        public string Title { get; set; }
        public string DirectoryName { get; set; }
        public string Singer { get; set; }
        public string Album { get; set; }
        public bool isFavorite { get; set; }
        public DateTime lastPlay { get; set; }
        public int countPlay { get; set; }
        public TimeSpan Duration { get; set; }
        public string DurationString { get { return Duration.ToString(@"mm\:ss") /*Duration.Minutes + ":" + Duration.Seconds*/; } set { d = value; } }
        string d;
        public string Fav
        {
            get
            {
                if (isFavorite)
                {
                    return "Убрать из избраного";
                }
                else
                {
                    return "Добавить в избраное";
                }
            }
        }
        public string TT
        {

            get
            {
                if (!string.IsNullOrEmpty(Singer))
                {
                    string tmp = Singer + " - " + Title;
                    return tmp;
                }
                else
                {
                    string tmp = Title;
                    return tmp;
                }
            }

           
        }
        
        //[ForeignKey("PlaylistId")]
        //public int PlaylistId { get; set; }
        //public Playlist Playlist { get; set; }
        public virtual ICollection<Playlist> Playlist { get; set; }

        public Audio()
        {
            Playlist = new List<Playlist>();
        }


        //public Audio(  string Title , string DirectoryName , string Singer  , string Album   , TimeSpan Duration  )
        //{
        //    this.Title = Title;
        //    this.DirectoryName = DirectoryName;
        //    this.Singer = Singer;
        //    this.Album = Album;
        //    this.Duration = Duration;
        //    isFavorite = false;
        //    lastPlay = DateTime.Now;
        //    countPlay = 0;

        //    if (String.IsNullOrEmpty(this.Album))
        //    {
        //        this.Album = "Неизвестный альбом";
        //    } 

        //    if (String.IsNullOrEmpty(this.Singer))
        //    {
        //        this.Singer = "Неизвестный исполнитель";
        //    }
        //}
    }
}

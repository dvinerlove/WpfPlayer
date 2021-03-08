using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfPlayer.Models
{
    public class Playlist
    {
        [Key]
        public int id { get; set; }
        [Required]
        public string Name { get; set; }
        public int AudioTime { get; set; }
        public byte[] Image { get; set; }
        //public virtual ICollection<Audio> Audios { get; set; }
        //public List<Audio> AudioList { get; set; } = new List<Audio>();

        public virtual ICollection<Audio> AudioList { get; set; }
        public Playlist()
        {
            AudioList = new List<Audio>();
        }

        //public Playlist(string Name1, List<Audio> audios, byte[] image)
        //{
        //    this.Name = Name1;
        //    this.AudioList = audios;
        //    this.Image = image;
        //    GetTime();
        //}

        public void GetTime()
        {
            if (AudioList != null)
            {
                AudioTime = 0;
                foreach (Audio val in AudioList)
                {
                    AudioTime += val.Duration.Minutes;

                }

            }
        }
    }
}

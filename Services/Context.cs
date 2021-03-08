using System;
using System.Collections.Generic;
using System.Data.Entity;
using WpfPlayer.Models;

namespace WpfPlayer.Services
{

    public class MyContextInitializer : DropCreateDatabaseAlways<Context>
    {
        protected override void Seed(Context db)
        {
            Playlist p2 = new Playlist { Name = "dp" };

           
            db.Playlists.Add(p2);
            db.SaveChanges();
        }
    }

    public class Context : DbContext
    {
        static Context()
        { 
        }
        public Context()
            : base("DbConnection")
        { }
        public DbSet<Audio> Audios { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
    }
    //class MyContextInitializer : DropCreateDatabaseAlways<Context>
    //{
    //    //protected override void Seed(MobileContext db)
    //    //{
    //    //    Phone p1 = new Phone { Name = "Samsung Galaxy S5", Price = 14000 };
    //    //    Phone p2 = new Phone { Name = "Nokia Lumia 630", Price = 8000 };

    //    //    db.Phones.Add(p1);
    //    //    db.Phones.Add(p2);
    //    //    db.SaveChanges();
    //    //}
    //}

}

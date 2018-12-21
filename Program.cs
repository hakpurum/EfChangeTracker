using LiteDB;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EfChangeTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            //NewPerson();
            UpdatePerson();
        }

        private static void NewPerson()
        {
            using (var db = new SampleContext())
            {
                db.Person.Add(new Person { FirstName = "Hasan", LastName = "Akpürüm" });
                Commit(db);
            }
        }

        private static void UpdatePerson()
        {
            using (var db = new SampleContext())
            {
                var detail = db.Person.FirstOrDefault(f => f.Id == 1);
                detail.LastName = "güncellendi";
                Commit(db);
            }
        }

        private static void Commit(SampleContext db)
        {
            if (db.ChangeTracker.HasChanges())//DbContext üzerinde insert,update,delete yapılacak bir işlem var mı ?
            {
                //Update,Delete işlemlerini SaveChanges den önce yakalayabiliriz
                DisplayTrackedEntities(db.ChangeTracker);

                db.SaveChanges();

                //Insert işlemlerini SaveChanges den önce yakalarsan PrimaryKeyler 0 geleceği için işimize yaramaz o yüzden
                //SaveChanges işleminden sonra yakalarız.
                DisplayTrackedEntities(db.ChangeTracker);
            }
        }

        private static void DisplayTrackedEntities(DbChangeTracker changeTracker)
        {
            Debug.WriteLine("---------------------------------------------------------------------------------------------------------------------");

            var entries = changeTracker.Entries();
            foreach (var entry in entries)
            {
                Debug.WriteLine($"Entity Name: {entry.Entity.GetType().FullName}");
                Debug.WriteLine($"Status: {entry.State}");

                if (entry.State != EntityState.Detached)//Durumu Detached dışındaki işlemler
                {
                    LiteDbSync(entry);
                }
            }
            Debug.WriteLine("");
            Debug.WriteLine("---------------------------------------------------------------------------------------------------------------------");
        }

        /// <summary>
        /// Db üzerinde yapılan her işlemi litedb ye sonkron bir şekilde yansıtır.
        /// </summary>
        /// <param name="entry"></param>
        private static void LiteDbSync(DbEntityEntry entry)
        {
            try
            {
                //Gelen nesneyi Bson'a çevirdik
                var t = BsonMapper.Global.ToDocument(entry.Entity);
                var pk = t["_id"];//PrimaryKey Alanımız
                if (pk.AsString != "0")//PrimaryKey değerimiz 0 ise daya savechange yapılmamış işlem yapma.
                {
                    //Lite Db Veritabanımızın barınacağı dizin
                    //DataBase1.db isminde bir db yok ise otomatik oluşturur.
                    using (var db = new LiteDatabase(@"c:/temp/DataBase1.db"))
                    {
                        if (entry.State == EntityState.Deleted)
                        {
                            db.GetCollection(entry.Entity.GetType().Name).Delete(pk);
                        }
                        else if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Unchanged)
                        {
                            //Pk değerine göre veri aynı ise update eder değil ise insert işlemini gerçekleştirir
                            db.GetCollection(entry.Entity.GetType().Name).Upsert(t);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}

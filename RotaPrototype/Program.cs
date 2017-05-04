using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RotaPrototype
{
    class Program
    {
        static void Main(string[] args)
        {
            GovanhillRota rota = new GovanhillRota(new DateTime(2016, 1, 1), new DateTime(2016, 12, 31));

            rota.AddPerson(PersonFactory.CreateKevin());
            rota.AddPerson(PersonFactory.CreateJulie());
            rota.AddPerson(PersonFactory.CreateChris());          
            rota.AddPerson(PersonFactory.CreateAileen());
            rota.AddPerson(PersonFactory.CreateGordon());

            Console.WriteLine("Reading annual leave file...");

            rota.ReadAnnualLeave("leave.txt");

            //AnnualLeaveHelper.GenerateRandomLeave(rota);

            DeclarePreferredCover(rota);

            Console.WriteLine("Generating rota...");
            rota.PopulateRota();

            //rota.PrintOnCallRota();
            //rota.PrintAllOnCallCounts();

            rota.PrintOnCallRota(DayOfWeek.Friday, MorningOrAfternoon.afternoon);
            rota.PrintOnCallCounts(DayOfWeek.Friday, MorningOrAfternoon.afternoon);

            rota.WriteRotaCsv("test.csv", DayOfWeek.Friday, MorningOrAfternoon.afternoon);

            Console.WriteLine("Complete");
            Console.WriteLine("Press Any Key to Close....");
            Console.ReadLine();


        }

        private static void DeclarePreferredCover(GovanhillRota rota)
        {
            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.morning),
                rota.PeopleInRota.Find(x => x.Name == "Chris"));

            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.afternoon),
             rota.PeopleInRota.Find(x => x.Name == "Kevin"));


            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Tuesday, MorningOrAfternoon.morning),
              rota.PeopleInRota.Find(x => x.Name == "Julie"));

            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Tuesday, MorningOrAfternoon.afternoon),
             rota.PeopleInRota.Find(x => x.Name == "Kevin"));

            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.morning),
              rota.PeopleInRota.Find(x => x.Name == "Chris"));

            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.afternoon),
             rota.PeopleInRota.Find(x => x.Name == "Julie"));

            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Thursday, MorningOrAfternoon.morning),
              rota.PeopleInRota.Find(x => x.Name == "Aileen"));

            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Thursday, MorningOrAfternoon.afternoon),
             rota.PeopleInRota.Find(x => x.Name == "Kevin"));

            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.morning),
            rota.PeopleInRota.Find(x => x.Name == "Aileen"));


            rota.AddPreferredOnCallRotaCover(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.afternoon),
            rota.PeopleInRota.Find(x => x.Name == "Gordon"));




        }


    }




}

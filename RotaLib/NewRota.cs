using RotaPrototype;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RotaLib
{
    public class SimpleFridayRota
    {

        public ObservableCollection<SimpleRotaPerson> RotaPersons { get; private set; }

        public Dictionary<DateTime, SimpleRotaPerson> OnCallRota;
        public Dictionary<DateTime, SimpleRotaPerson> SurgeryRota;
        public Dictionary<DateTime, SimpleRotaPerson> ProtectedRota;
        public Dictionary<DateTime, SimpleRotaPerson> OnCallRegistrarCoverRota;



        public SimpleFridayRota()
        {

            RotaPersons = new ObservableCollection<SimpleRotaPerson>();

            OnCallRota = new Dictionary<DateTime, SimpleRotaPerson>();
            SurgeryRota = new Dictionary<DateTime, SimpleRotaPerson>();
            ProtectedRota = new Dictionary<DateTime, SimpleRotaPerson>();
            OnCallRegistrarCoverRota = new Dictionary<DateTime, SimpleRotaPerson>();

        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void AddPersonToRota(SimpleRotaPerson p)
        {
            RotaPersons.Add(p);
        }

        public void SetRotaForDate(Dictionary<DateTime, SimpleRotaPerson> rota, DateTime value, SimpleRotaPerson p)
        {
            if (rota.ContainsKey(value))
                rota[value] = p;
            else
                rota.Add(value, p);
        }

        public void ClearRotaForDate(Dictionary<DateTime, SimpleRotaPerson> rota, DateTime value)
        {
            if (rota.ContainsKey(value))
                rota.Remove(value);

        }

        public void MakeRota()
        {

            ClearForwardRotas();

            //we'll attempt to make a fair rota from here on out, given what we know about 
            //past rota 

            if (RotaPersons.Count == 0)
                return;

            //find our registrar
            var registrar = RotaPersons.First(x => x.IsRegistrar);

            if (registrar == null)
                return;

            DateTime currentDate = DateTime.Now;
            currentDate = currentDate.Date;
            DateTime endDate = currentDate.AddYears(1);

            var fridays = GetFridays(currentDate, endDate).ToList();


            foreach (DateTime friday in fridays)
            {
                //is Registrar available?
                if (registrar.IsAvailable(friday))
                {
                    OnCallRota.Add(friday, registrar);

                    //next do the on call cover.
                    DetermineBestAvailable(friday, OnCallRegistrarCoverRota, OnCallRota);

                    //next do surgery, don't clash with on-call cover
                    DetermineBestAvailable(friday, SurgeryRota, OnCallRota, OnCallRegistrarCoverRota);

                    //next do protected, don't clash with on-call cover and surgery
                    DetermineBestAvailable(friday, ProtectedRota, OnCallRota, OnCallRegistrarCoverRota, SurgeryRota);


                }
                else
                {

                    DetermineBestAvailable(friday, OnCallRota);

                    //next do surgery, don't clash with on-call cover
                    DetermineBestAvailable(friday, SurgeryRota, OnCallRota, OnCallRegistrarCoverRota);

                    //next do protected, don't clash with on-call cover and surgery
                    DetermineBestAvailable(friday, ProtectedRota, OnCallRota, OnCallRegistrarCoverRota, SurgeryRota);
                }

            }



        }

        private void ClearForwardRotas()
        {
            ClearForwardDateFromRota(OnCallRota);
            ClearForwardDateFromRota(SurgeryRota);
            ClearForwardDateFromRota(ProtectedRota);
            ClearForwardDateFromRota(OnCallRegistrarCoverRota);


        }

        private void ClearForwardDateFromRota(Dictionary<DateTime, SimpleRotaPerson> rota)
        {
            var keyList = rota.Keys.ToList();
            for (int i = 0; i < keyList.Count; i++)
            {
                DateTime friday = keyList[i];

                //future date - remove it
                if (friday > DateTime.Now.Date)
                {
                    rota.Remove(friday);
                }
            }
        }

        private void DetermineBestAvailable(DateTime friday, Dictionary<DateTime, SimpleRotaPerson> rotaToFill, params Dictionary<DateTime, SimpleRotaPerson>[] rotasToAvoidClash)
        {

            SimpleRotaPerson leastOnCall = null;
            int onCall = int.MaxValue;

            foreach (SimpleRotaPerson person in RotaPersons)
            {
                if (!person.IsAvailable(friday))
                    continue;

                bool clash = false;
                foreach (Dictionary<DateTime, SimpleRotaPerson> clashRota in rotasToAvoidClash)
                {
                    if (clashRota.ContainsKey(friday))
                        if (clashRota[friday] == person)
                            clash = true;
                }

                if (clash)
                    continue;

                //find the number of sessions this person already has
                var matchingSessions = GetRotaCount(person, rotaToFill);

                if (matchingSessions < onCall)
                {
                    leastOnCall = person;
                    onCall = matchingSessions;
                }

            }

            if (leastOnCall != null)
            {
                rotaToFill[friday] = leastOnCall;
            }

        }

        private int GetRotaCount(SimpleRotaPerson person, Dictionary<DateTime, SimpleRotaPerson> rotaToFill)
        {
            return rotaToFill.Count(x => x.Value == person);
        }

        static IEnumerable<DateTime> GetFridays(DateTime startdate, DateTime enddate)
        {
            // step forward to the first friday
            while (startdate.DayOfWeek != DayOfWeek.Friday)
                startdate = startdate.AddDays(1);

            while (startdate < enddate)
            {
                yield return startdate;
                startdate = startdate.AddDays(7);
            }
        }

        public string GetFullRotaPrintOut()
        {
            StringBuilder s = new StringBuilder();

            DateTime currentDate = DateTime.Now;
            currentDate = currentDate.Date;
            DateTime endDate = currentDate.AddYears(1);

            var fridays = GetFridays(currentDate, endDate).ToList();

            foreach (DateTime friday in fridays)
            {

                string onCallName = "TBD";
                string coverName = "TBD";
                string surgeryName = "TBD";
                string protectedName = "TBD";

                if (OnCallRota.ContainsKey(friday))
                    onCallName = OnCallRota[friday].Name;
                if (OnCallRegistrarCoverRota.ContainsKey(friday))
                    coverName = OnCallRegistrarCoverRota[friday].Name;
                if (SurgeryRota.ContainsKey(friday))
                    surgeryName = SurgeryRota[friday].Name;
                if (ProtectedRota.ContainsKey(friday))
                    protectedName = ProtectedRota[friday].Name;


                s.AppendLine(friday.ToShortDateString() + ": "
                    + "On Call - " + onCallName + "     "
                    + "Cover - " + coverName + "     "
                    + "Surgery - " + surgeryName + "     "
                    + "Protected - " + protectedName + "     "
                    );


            }

            return s.ToString();

        }

        public string GetRotaCountPrintOut()
        {
            StringBuilder s = new StringBuilder();

            foreach (SimpleRotaPerson p in RotaPersons)
            {
                int onCallCount = GetRotaCount(p, OnCallRota);
                int coverCount = GetRotaCount(p, OnCallRegistrarCoverRota);
                int surgeryCount = GetRotaCount(p, SurgeryRota);
                int protectedCount = GetRotaCount(p, ProtectedRota);

                s.AppendLine(p.Name + ": " + "On Call - " + onCallCount + "     "
                    + "Cover - " + coverCount + "     "
                    + "Surgery - " + surgeryCount + "     "
                    + "Protected - " + protectedCount + "     "
                    );

            }
            return s.ToString();
        }


        public void SerializeHistoricalRota()
        {
            SaveRota(OnCallRota, "OnCallRota.txt");
            SaveRota(OnCallRegistrarCoverRota, "CoverRota.txt");
            SaveRota(SurgeryRota, "SurgeryRota.txt");
            SaveRota(ProtectedRota, "ProtectedRota.txt");

        }

        private void SaveRota(Dictionary<DateTime,SimpleRotaPerson> rota, string fileName)
        {
            StringBuilder s = new StringBuilder();
            foreach (DateTime d in rota.Keys)
            {
                if (d.Date < DateTime.Now.Date)
                {
                    //we want to serialize
                    s.AppendLine(d.ToShortDateString() + "," + rota[d].Name);
                }
            }
            File.WriteAllText(fileName, s.ToString());
        }

        public void DeserializeHistoricalRota()
        {
            LoadRota(OnCallRota, "OnCallRota.txt");
            LoadRota(OnCallRegistrarCoverRota, "CoverRota.txt");
            LoadRota(SurgeryRota, "SurgeryRota.txt");
            LoadRota(ProtectedRota, "ProtectedRota.txt");

        }

        private void LoadRota(Dictionary<DateTime, SimpleRotaPerson> rota, string fileName)
        {

            if (!File.Exists(fileName))
                return;

            string[] rotaLines = File.ReadAllLines(fileName);

            foreach (string line in rotaLines)
            {
                if (String.IsNullOrWhiteSpace(line))
                    continue;

                string[] lineSplit = line.Split(',');
                DateTime d = DateTime.Parse(lineSplit[0]);
                string name = lineSplit[1];

                rota.Add(d, RotaPersons.First(x => x.Name == name));
            }
        }
    }

    [Serializable()]
    [System.Xml.Serialization.XmlRoot("RotaPersonCollection")]
    public class RotaPersonCollection
    {
        [XmlArray("SimpleRotaPerson")]
        [XmlArrayItem("SimpleRotaPerson", typeof(SimpleRotaPerson))]
        public SimpleRotaPerson[] RotaPeople { get; set; }
    }

    public class SimpleRotaPerson
    {

        public string Name { get; set; }

        [XmlIgnore]
        public SimpleFridayRota rota;
        public ObservableCollection<DateTime> AnnualLeaveDates;
        public bool IsRegistrar { get; set; }


        public SimpleRotaPerson()
        {

        }

        public SimpleRotaPerson(string name, SimpleFridayRota rota)
        {
            this.rota = rota;
            Name = name;
            AnnualLeaveDates = new ObservableCollection<DateTime>();
        }

        public void AddAnnualLeave(DateTime date)
        {
            if (!AnnualLeaveDates.Contains(date))
                AnnualLeaveDates.Add(date);
        }

        public bool IsAvailable(DateTime s)
        {
            return !AnnualLeaveDates.Contains(s);
        }

        public override string ToString()
        {
            return Name;
        }

    }
}

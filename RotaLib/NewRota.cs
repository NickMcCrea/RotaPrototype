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
    public struct RotaDate
    {
        public DateTime DateTime { get; set; }
        public bool Locked { get; set; }

        public RotaDate(DateTime date)
        {
            DateTime = date;
            Locked = false;
        }

 
    }

    public class RotaResult
    {
        public string OnCall { get; set; }
        public string Surgery { get; set; }
        public string Protected { get; set; }
        public string Cover { get; set; }
        public DateTime Date { get; set; }
        public bool Locked { get; set; }

        public RotaResult(DateTime date)
        {
            Date = date;
        }
    }

    public class SimpleFridayRota
    {

        public ObservableCollection<SimpleRotaPerson> RotaPersons { get; private set; }
        public ObservableCollection<RotaResult> RotaResults { get; set; }
        public Dictionary<RotaDate, SimpleRotaPerson> OnCallRota;
        public Dictionary<RotaDate, SimpleRotaPerson> SurgeryRota;
        public Dictionary<RotaDate, SimpleRotaPerson> ProtectedRota;
        public Dictionary<RotaDate, SimpleRotaPerson> OnCallRegistrarCoverRota;



        public SimpleFridayRota()
        {

            RotaPersons = new ObservableCollection<SimpleRotaPerson>();

            OnCallRota = new Dictionary<RotaDate, SimpleRotaPerson>();
            SurgeryRota = new Dictionary<RotaDate, SimpleRotaPerson>();
            ProtectedRota = new Dictionary<RotaDate, SimpleRotaPerson>();
            OnCallRegistrarCoverRota = new Dictionary<RotaDate, SimpleRotaPerson>();
            RotaResults = new ObservableCollection<RotaResult>();

        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void AddPersonToRota(SimpleRotaPerson p)
        {
            RotaPersons.Add(p);
        }

        public void SetRotaForDate(Dictionary<RotaDate, SimpleRotaPerson> rota, RotaDate value, SimpleRotaPerson p)
        {
            if (rota.ContainsKey(value))
                rota[value] = p;
            else
                rota.Add(value, p);
        }

        public void ClearRotaForDate(Dictionary<RotaDate, SimpleRotaPerson> rota, RotaDate value)
        {
            if (rota.ContainsKey(value))
                rota.Remove(value);

        }

        public void MakeRota()
        {
            RotaResults.Clear();
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


            foreach (RotaDate friday in fridays)
            {
                RotaResults.Add(new RotaResult(friday.DateTime));

                //is Registrar available?
                if (registrar.IsAvailable(friday))
                {
                    
                    OnCallRota.Add(friday, registrar);
                    RotaResults.First(x => x.Date == friday.DateTime).OnCall = registrar.Name;

                    //next do the on call cover.
                    DetermineBestAvailable(friday, OnCallRegistrarCoverRota, OnCallRota);

                    //next do surgery, don't clash with on-call cover
                    DetermineBestAvailable(friday, SurgeryRota, OnCallRota, OnCallRegistrarCoverRota);

                    //next do protected, don't clash with on-call cover and surgery
                    DetermineBestAvailable(friday, ProtectedRota, OnCallRota, OnCallRegistrarCoverRota, SurgeryRota);


                }
                else
                {
                    //registrar unavailable - look for best fit
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

        private void ClearForwardDateFromRota(Dictionary<RotaDate, SimpleRotaPerson> rota)
        {
            var keyList = rota.Keys.ToList();
            for (int i = 0; i < keyList.Count; i++)
            {
                RotaDate r = keyList[i];
                DateTime friday = keyList[i].DateTime;

                //future date - remove it
                if (friday > DateTime.Now.Date)
                {
                    rota.Remove(r);
                }
            }
        }

        private void DetermineBestAvailable(RotaDate friday, Dictionary<RotaDate, SimpleRotaPerson> rotaToFill, params Dictionary<RotaDate, SimpleRotaPerson>[] rotasToAvoidClash)
        {
            var rotaResult = RotaResults.First(x => x.Date == friday.DateTime);

            SimpleRotaPerson leastOnCall = null;
            int onCall = int.MaxValue;

            foreach (SimpleRotaPerson person in RotaPersons)
            {
                if (!person.IsAvailable(friday))
                    continue;

                bool clash = false;
                foreach (Dictionary<RotaDate, SimpleRotaPerson> clashRota in rotasToAvoidClash)
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
                if (rotaToFill == OnCallRota)
                    rotaResult.OnCall = leastOnCall.Name;
                if (rotaToFill == SurgeryRota)
                    rotaResult.Surgery = leastOnCall.Name;
                if (rotaToFill == OnCallRegistrarCoverRota)
                    rotaResult.Cover = leastOnCall.Name;
                if (rotaToFill == ProtectedRota)
                    rotaResult.Protected = leastOnCall.Name;
            }

        }

        private int GetRotaCount(SimpleRotaPerson person, Dictionary<RotaDate, SimpleRotaPerson> rotaToFill)
        {
            return rotaToFill.Count(x => x.Value == person);
        }

        static IEnumerable<RotaDate> GetFridays(DateTime startdate, DateTime enddate)
        {
            // step forward to the first friday
            while (startdate.DayOfWeek != DayOfWeek.Friday)
                startdate = startdate.AddDays(1);

            while (startdate < enddate)
            {
                yield return new RotaDate(startdate);
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

            foreach (RotaDate friday in fridays)
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


                s.AppendLine(friday.DateTime.ToShortDateString() + ": "
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

        private void SaveRota(Dictionary<RotaDate, SimpleRotaPerson> rota, string fileName)
        {
            StringBuilder s = new StringBuilder();
            foreach (RotaDate d in rota.Keys)
            {
                if (d.DateTime.Date < DateTime.Now.Date)
                {
                    //we want to serialize
                    s.AppendLine(d.DateTime.ToShortDateString() + "," + rota[d].Name);
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

        private void LoadRota(Dictionary<RotaDate, SimpleRotaPerson> rota, string fileName)
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

                rota.Add(new RotaDate(d), RotaPersons.First(x => x.Name == name));
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

        public bool IsAvailable(RotaDate s)
        {
            return !AnnualLeaveDates.Contains(s.DateTime);
        }

        public override string ToString()
        {
            return Name;
        }

    }
}

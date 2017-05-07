using RotaPrototype;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
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

        public static bool operator ==(RotaDate o1, RotaDate o2)
        {

            return (o1.DateTime == o2.DateTime && o1.Locked == o2.Locked);
        }
        public static bool operator !=(RotaDate o1, RotaDate o2)
        {

            return (o1.DateTime != o2.DateTime || o1.Locked != o2.Locked);
        }


    }

    public struct RotaCell
    {
        public bool UserOverride { get; set; }
        public string Value { get; set; }

    }

    public class RotaRow
    {
        char delimiter = '|';
        public string Date { get { return rotaDate.DateTime.ToShortDateString(); } }
        public string OnCall { get { return onCall.Value; } set { onCall.Value = value; } }
        public string Surgery { get { return surgery.Value; } set { surgery.Value = value; } }
        public string Protected { get { return protectedTime.Value; } set { protectedTime.Value = value; } }
        public string Cover { get { return cover.Value; } set { cover.Value = value; } }
        public string OnLeave { get { return onLeave.Value; } set { onLeave.Value = value; } }
        public bool Locked { get { return rotaDate.Locked; } set { rotaDate.Locked = value; } }
        public RotaDate rotaDate;
        public RotaCell onCall;
        public RotaCell surgery;
        public RotaCell protectedTime;
        public RotaCell cover;
        public RotaCell onLeave;


        public RotaRow(RotaDate date)
        {
            this.rotaDate = date;
        }

        public RotaRow(string deserializedString, CultureInfo ci)
        {
            string[] split = deserializedString.Split(delimiter);


            rotaDate = new RotaDate(DateTime.Parse(split[0], ci));
            rotaDate.Locked = bool.Parse(split[6]);

            OnCall = split[1];
            Surgery = split[2];
            Protected = split[3];
            Cover = split[4];
            OnLeave = split[5];

        }

        public void SetUserFlag(int col_index)
        {
            switch (col_index)
            {
                case (1):
                    onCall.UserOverride = true;
                    break;
                case (2):
                    surgery.UserOverride = true;
                    break;
                case (3):
                    protectedTime.UserOverride = true;
                    break;
                case (4):
                    cover.UserOverride = true;
                    break;
            }
        }

        public override string ToString()
        {

            return Date + delimiter + OnCall + delimiter + Surgery + delimiter + Protected
                + delimiter + Cover + delimiter + OnLeave + delimiter + Locked;
        }


    }

    public class RotaPersonCollection<SimpleRotaPerson> : ObservableCollection<SimpleRotaPerson> { }

    public class SimpleFridayRota
    {
        string fileName = "rota.txt";
        public RotaPersonCollection<SimpleRotaPerson> RotaPersons
        {
            get;
            set;
        }
        public ObservableCollection<RotaRow> RotaResults { get; set; }
        public DateTime? startTime;
        public DateTime? endTime;


        public SimpleFridayRota()
        {

            RotaPersons = new RotaPersonCollection<SimpleRotaPerson>();


            RotaResults = new ObservableCollection<RotaRow>();

        }

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

        public void GenerateRota()
        {
            if (RotaPersons.Count == 0)
                return;

            //what we basically want here is to let the user cherry pick bits of the rota, which 
            //will then be worked around as best we can. So we want to fill the gaps the user has left.

            //We want a concept of 'user derived' and 'generated' data. Generated data is produced
            //during the rota generation process. User derived data is kept as is and worked around.






            //find our registrar
            var registrar = RotaPersons.First(x => x.IsRegistrar);

            if (registrar == null)
                return;



            var fridays = GetFridays(startTime.Value.Date, endTime.Value.Date).ToList();

            foreach (RotaDate friday in fridays)
            {

            }


            //foreach (RotaDate friday in fridays)
            //{
            //    RotaResults.Add(new RotaResult(friday));

            //    //is Registrar available?
            //    if (registrar.IsAvailable(friday))
            //    {

            //        OnCallRota.Add(friday, registrar);
            //        RotaResults.First(x => x.rotaDate == friday).OnCall = registrar.Name;

            //        //next do the on call cover.
            //        DetermineBestAvailable(friday, OnCallRegistrarCoverRota, OnCallRota);

            //        //next do surgery, don't clash with on-call cover
            //        DetermineBestAvailable(friday, SurgeryRota, OnCallRota, OnCallRegistrarCoverRota);

            //        //next do protected, don't clash with on-call cover and surgery
            //        DetermineBestAvailable(friday, ProtectedRota, OnCallRota, OnCallRegistrarCoverRota, SurgeryRota);


            //    }
            //    else
            //    {
            //        //registrar unavailable - look for best fit
            //        DetermineBestAvailable(friday, OnCallRota);

            //        //next do surgery, don't clash with on-call cover
            //        DetermineBestAvailable(friday, SurgeryRota, OnCallRota, OnCallRegistrarCoverRota);

            //        //next do protected, don't clash with on-call cover and surgery
            //        DetermineBestAvailable(friday, ProtectedRota, OnCallRota, OnCallRegistrarCoverRota, SurgeryRota);
            //    }

            //}



        }


        private void DetermineBestAvailable(RotaDate friday, Dictionary<RotaDate, SimpleRotaPerson> rotaToFill, params Dictionary<RotaDate, SimpleRotaPerson>[] rotasToAvoidClash)
        {
            //var rotaResult = RotaResults.First(x => x.rotaDate == friday);

            //SimpleRotaPerson leastOnCall = null;
            //int onCall = int.MaxValue;

            //foreach (SimpleRotaPerson person in RotaPersons)
            //{
            //    if (!person.IsAvailable(friday))
            //        continue;

            //    bool clash = false;
            //    foreach (Dictionary<RotaDate, SimpleRotaPerson> clashRota in rotasToAvoidClash)
            //    {
            //        if (clashRota.ContainsKey(friday))
            //            if (clashRota[friday] == person)
            //                clash = true;
            //    }

            //    if (clash)
            //        continue;

            //    //find the number of sessions this person already has
            //    var matchingSessions = GetRotaCount(person, rotaToFill);

            //    if (matchingSessions < onCall)
            //    {
            //        leastOnCall = person;
            //        onCall = matchingSessions;
            //    }

            //}

            //if (leastOnCall != null)
            //{
            //    rotaToFill[friday] = leastOnCall;
            //    if (rotaToFill == OnCallRota)
            //        rotaResult.OnCall = leastOnCall.Name;
            //    if (rotaToFill == SurgeryRota)
            //        rotaResult.Surgery = leastOnCall.Name;
            //    if (rotaToFill == OnCallRegistrarCoverRota)
            //        rotaResult.Cover = leastOnCall.Name;
            //    if (rotaToFill == ProtectedRota)
            //        rotaResult.Protected = leastOnCall.Name;
            //}

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

            //DateTime currentDate = DateTime.Now;
            //currentDate = currentDate.Date;
            //DateTime endDate = currentDate.AddYears(1);

            //var fridays = GetFridays(currentDate, endDate).ToList();

            //foreach (RotaDate friday in fridays)
            //{

            //    string onCallName = "TBD";
            //    string coverName = "TBD";
            //    string surgeryName = "TBD";
            //    string protectedName = "TBD";

            //    if (OnCallRota.ContainsKey(friday))
            //        onCallName = OnCallRota[friday].Name;
            //    if (OnCallRegistrarCoverRota.ContainsKey(friday))
            //        coverName = OnCallRegistrarCoverRota[friday].Name;
            //    if (SurgeryRota.ContainsKey(friday))
            //        surgeryName = SurgeryRota[friday].Name;
            //    if (ProtectedRota.ContainsKey(friday))
            //        protectedName = ProtectedRota[friday].Name;


            //    s.AppendLine(friday.DateTime.ToShortDateString() + ": "
            //        + "On Call - " + onCallName + "     "
            //        + "Cover - " + coverName + "     "
            //        + "Surgery - " + surgeryName + "     "
            //        + "Protected - " + protectedName + "     "
            //        );


            //}

            return s.ToString();

        }

        public string GetRotaCountPrintOut()
        {
            StringBuilder s = new StringBuilder();

            //foreach (SimpleRotaPerson p in RotaPersons)
            //{
            //    int onCallCount = GetRotaCount(p, OnCallRota);
            //    int coverCount = GetRotaCount(p, OnCallRegistrarCoverRota);
            //    int surgeryCount = GetRotaCount(p, SurgeryRota);
            //    int protectedCount = GetRotaCount(p, ProtectedRota);

            //    s.AppendLine(p.Name + ": " + "On Call - " + onCallCount + "     "
            //        + "Cover - " + coverCount + "     "
            //        + "Surgery - " + surgeryCount + "     "
            //        + "Protected - " + protectedCount + "     "
            //        );

            //}
            return s.ToString();
        }


        public void SerializeRota()
        {
            //don't try to serialize if we have no results or the user hasn't picked dates.
            if (!startTime.HasValue || !endTime.HasValue)
                return;

            StringBuilder s = new StringBuilder();

            s.AppendLine(startTime.Value.ToShortDateString());
            s.AppendLine(endTime.Value.ToShortDateString());

            foreach (RotaRow r in RotaResults)
            {
                s.AppendLine(r.ToString());
            }


            File.WriteAllText(fileName, s.ToString());
        }


        public void DeserializeRota(CultureInfo ci)
        {

            RotaResults.Clear();
            if (!File.Exists(fileName))
                return;



            string[] rotaLines = File.ReadAllLines(fileName);

            startTime = DateTime.Parse(rotaLines[0], ci);
            endTime = DateTime.Parse(rotaLines[1], ci);

            List<RotaRow> tempResults = new List<RotaRow>();
            for (int i = 2; i < rotaLines.Length; i++)
            {
                tempResults.Add(new RotaRow(rotaLines[i], ci));
            }


            var findAllDatesWithinRange = tempResults.FindAll(x => x.rotaDate.DateTime > startTime.Value && x.rotaDate.DateTime < endTime.Value);

            foreach (RotaRow r in findAllDatesWithinRange)
                RotaResults.Add(r);

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

        public string Name
        {
            get;
            set;
        }
        public bool IsRegistrar
        {
            get;
            set;
        }


        [XmlIgnore]
        public SimpleFridayRota rota;
        public ObservableCollection<DateTime> AnnualLeaveDates;


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

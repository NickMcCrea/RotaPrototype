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
        public override string ToString()
        {
            return Value + ":" + UserOverride.ToString();

        }

        public static RotaCell Parse(string s)
        {
            var sections = s.Split(':');
            RotaCell c = new RotaCell();
            c.Value = sections[0];
            c.UserOverride = bool.Parse(sections[1]);
            return c;
        }

    }

    public class RotaRow
    {
        char delimiter = '|';
        public string Date { get { return rotaDate.DateTime.ToShortDateString(); } }
        public string OnCall { get { return onCall.Value; } set { onCall.Value = value; } }
        public string Cover { get { return cover.Value; } set { cover.Value = value; } }
        public string Surgery { get { return surgery.Value; } set { surgery.Value = value; } }
        public string Protected { get { return protectedTime.Value; } set { protectedTime.Value = value; } }
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
            onCall = new RotaCell();
            surgery = new RotaCell();
            protectedTime = new RotaCell();
            cover = new RotaCell();
            onLeave = new RotaCell();

            OnCall = "";
            Surgery = "";
            Protected = "";
            Cover = "";
            OnLeave = "";
        }

        public RotaRow(string deserializedString, CultureInfo ci)
        {
            string[] split = deserializedString.Split(delimiter);


            rotaDate = new RotaDate(DateTime.Parse(split[0], ci));
            rotaDate.Locked = bool.Parse(split[6]);

            onCall = RotaCell.Parse(split[1]);
            surgery = RotaCell.Parse(split[2]);
            protectedTime = RotaCell.Parse(split[3]);
            cover = RotaCell.Parse(split[4]);
            onLeave = RotaCell.Parse(split[5]);

        }

        public void SetUserFlag(int col_index, bool value)
        {
            switch (col_index)
            {
                case (1):
                    onCall.UserOverride = value;
                    break;
                case (2):
                    cover.UserOverride = value;
                    break;
                case (3):
                    surgery.UserOverride = value;
                    break;
                case (4):
                    protectedTime.UserOverride = value;
                    break;

                case (5):
                    onLeave.UserOverride = value;
                    break;
            }
        }

        public override string ToString()
        {

            return Date + delimiter + onCall.ToString() + delimiter + surgery.ToString() + delimiter + protectedTime.ToString()
                + delimiter + cover.ToString() + delimiter + onLeave.ToString() + delimiter + Locked;
        }

        public void ClearNonUserData()
        {
            if (!onCall.UserOverride)
                onCall.Value = "";
            if (!surgery.UserOverride)
                surgery.Value = "";
            if (!protectedTime.UserOverride)
                protectedTime.Value = "";
            if (!cover.UserOverride)
                cover.Value = "";
            if (!onLeave.UserOverride)
                onLeave.Value = "";
        }


    }


    public class SimpleFridayRota
    {
        string fileName = "rota.txt";
        public ObservableCollection<SimpleRotaPerson> RotaPersons
        {
            get;
            set;
        }
        public ObservableCollection<RotaRow> RotaResults { get; set; }
        public DateTime? startTime;
        public DateTime? endTime;


        public SimpleFridayRota()
        {

            RotaPersons = new ObservableCollection<SimpleRotaPerson>();


            RotaResults = new ObservableCollection<RotaRow>();

        }

        public void AddPersonToRota(SimpleRotaPerson p)
        {
            RotaPersons.Add(p);
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
                //if we don't have stored results from disk, create a new empty row.
                if (RotaResults.ToList().Find(x => x.rotaDate.DateTime == friday.DateTime) == null)
                {
                    RotaResults.Add(new RotaRow(friday));
                }

            }


            ClearNonUserData();

            //now generate our rota
            foreach (RotaRow row in RotaResults)
            {
                if (row.Locked)
                    continue;

                //first, is the registrar available? If so, assign them to on call (unless it's overridden)
                if (!OnLeave(row, registrar))
                {
                    if (!row.onCall.UserOverride)
                    {
                        row.OnCall = registrar.Name;

                        //next, assign on call cover. Who's done the least and isn't on AL?
                        if (!row.cover.UserOverride)
                            row.Cover = FindCover(row);
                    }
                }
                else
                {
                    //registrar off. Pick someone else for on call
                    if (!row.onCall.UserOverride)
                    {
                        row.OnCall = FindOnCall(row);

                    }

                    row.Cover = "N/A";

                }




                //next, assign surgery.
                if (!row.surgery.UserOverride)
                    row.Surgery = FindSurgery(row);

                //then protected time.
                if (!row.protectedTime.UserOverride)
                    row.Protected = FindProtected(row);



            }



        }

        private void ClearNonUserData()
        {
            foreach (RotaRow r in RotaResults)
            {
                if (!r.Locked)
                    r.ClearNonUserData();
            }
        }

        private string FindSurgery(RotaRow row)
        {
            Dictionary<SimpleRotaPerson, int> totals = new Dictionary<SimpleRotaPerson, int>();
            foreach (SimpleRotaPerson p in RotaPersons)
            {
                totals.Add(p, RotaResults.Count(x => x.Surgery == p.Name));
            }


            var list = totals.OrderBy(x => x.Value).ToList();

            list.RemoveAll(x => row.OnCall.Contains(x.Key.Name));
            list.RemoveAll(x => row.Cover.Contains(x.Key.Name));
            list.RemoveAll(x => OnLeave(row, x.Key));

            if (list.Count == 1)
                return list[0].Key.Name;
            if (list.Count > 1)
            {

                if (list[0].Value != list[1].Value)
                    return list[0].Key.Name;
                else
                {
                    Random r = new Random();
                    var dice = r.Next(0, 100);
                    if (dice < 50)
                        return list[0].Key.Name;
                    else
                        return list[1].Key.Name;
                }
            }


            return "TBD";
        }

        private string FindProtected(RotaRow row)
        {
            Dictionary<SimpleRotaPerson, int> totals = new Dictionary<SimpleRotaPerson, int>();
            foreach (SimpleRotaPerson p in RotaPersons)
            {
                totals.Add(p, RotaResults.Count(x => x.Protected == p.Name));
            }


            foreach (var kvp in totals.OrderBy(x => x.Value))
            {
                //don't pick the on call person
                if (row.OnCall.Contains(kvp.Key.Name))
                    continue;

                if (row.Cover.Contains(kvp.Key.Name))
                    continue;

                if (row.Surgery.Contains(kvp.Key.Name))
                    continue;

                if (OnLeave(row, kvp.Key))
                    continue;

                return kvp.Key.Name;
            }
            return "TBD";
        }

        private string FindOnCall(RotaRow row)
        {
            Dictionary<SimpleRotaPerson, int> totals = new Dictionary<SimpleRotaPerson, int>();
            foreach (SimpleRotaPerson p in RotaPersons)
            {
                int onCallTotal = RotaResults.Count(x => x.OnCall == p.Name);
                int coverTotal = RotaResults.Count(x => x.Cover == p.Name);
                totals.Add(p, onCallTotal + coverTotal);
            }

            foreach (var kvp in totals.OrderBy(x => x.Value))
            {

                if (OnLeave(row, kvp.Key))
                    continue;

                return kvp.Key.Name;
            }
            return "TBD";
        }

        private string FindCover(RotaRow row)
        {
            Dictionary<SimpleRotaPerson, int> totals = new Dictionary<SimpleRotaPerson, int>();
            foreach (SimpleRotaPerson p in RotaPersons)
            {
                int onCallTotal = RotaResults.Count(x => x.OnCall == p.Name);
                int coverTotal = RotaResults.Count(x => x.Cover == p.Name);
                totals.Add(p, onCallTotal + coverTotal);
            }


            foreach (var kvp in totals.OrderBy(x => x.Value))
            {
                //don't pick the on call person
                if (row.OnCall.Contains(kvp.Key.Name))
                    continue;

                if (OnLeave(row, kvp.Key))
                    continue;

                return kvp.Key.Name;
            }
            return "TBD";
        }

        private bool OnLeave(RotaRow row, SimpleRotaPerson registrar)
        {
            string onLeave = row.OnLeave;
            if (onLeave.Contains(registrar.Name))
                return true;
            return false;


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

        public string GetRotaCountPrintOut()
        {
            StringBuilder s = new StringBuilder();

            foreach (SimpleRotaPerson p in RotaPersons)
            {

                int onCallTotal = RotaResults.Count(x => x.OnCall == p.Name);
                int coverTotal = RotaResults.Count(x => x.Cover == p.Name);
                int surgeryTotal = RotaResults.Count(x => x.Surgery == p.Name);
                int protectedTotal = RotaResults.Count(x => x.Protected == p.Name);

                s.AppendLine(p.Name + " --- On Call: "
                    + onCallTotal + " Cover: "
                    + coverTotal + " Surgery: "
                    + surgeryTotal + " Protected: "
                    + protectedTotal);


            }



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

            SortRotaResultsByDate();
        }

        private void SortRotaResultsByDate()
        {
            var orderedResults = RotaResults.OrderBy(x => x.rotaDate.DateTime);

            List<RotaRow> results = new List<RotaRow>();
            foreach (var row in orderedResults)
                results.Add(row);

            RotaResults.Clear();
            foreach (RotaRow r in results)
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



        public SimpleRotaPerson()
        {

        }


        public SimpleRotaPerson(string name)
        {
            Name = name;
        }



        public override string ToString()
        {
            return Name;
        }

    }
}

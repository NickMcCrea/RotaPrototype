using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;


namespace RotaPrototype
{
    public class GovanhillRota
    {
        public DateTime StartDate;
        public DateTime EndDate;
        public List<RotaPerson> PeopleInRota;

        public Dictionary<Session, RotaPerson> OnCallRota;
        public Dictionary<Session, RotaPerson> SurgeryRota;
        public Dictionary<Session, RotaPerson> ProtectedRota;
        public Dictionary<Session, RotaPerson> OnCallRegistrarCoverRota;

        public Dictionary<SessionType, RotaPerson> PreferredOnCallRota;


        public GovanhillRota(DateTime start, DateTime end)
        {
            this.StartDate = start;
            this.EndDate = end;
            PeopleInRota = new List<RotaPerson>();

            OnCallRota = new Dictionary<Session, RotaPerson>();
            SurgeryRota = new Dictionary<Session, RotaPerson>();
            ProtectedRota = new Dictionary<Session, RotaPerson>();
            OnCallRegistrarCoverRota = new Dictionary<Session, RotaPerson>();

            PreferredOnCallRota = new Dictionary<SessionType, RotaPerson>();

            for (DateTime date = StartDate; date.Date <= EndDate.Date; date = date.AddDays(1))
            {
                if (IsWeekday(date))
                {
                    AddSession(date, MorningOrAfternoon.morning);
                    AddSession(date, MorningOrAfternoon.afternoon);
                }
            }
        }

        private void AddSession(DateTime date, MorningOrAfternoon amPM)
        {
            Session s = new Session();
            SessionType t = new SessionType(date.DayOfWeek, amPM);
            s.Date = date;
            s.SessionType = t;
            OnCallRota.Add(s, null);
            SurgeryRota.Add(s, null);
            ProtectedRota.Add(s, null);
            OnCallRegistrarCoverRota.Add(s, null);
        }

        public void AddPerson(RotaPerson p)
        {
            PeopleInRota.Add(p);
        }

        public void AddPreferredOnCallRotaCover(SessionType sessionType, RotaPerson rotaPerson)
        {
            PreferredOnCallRota.Add(sessionType, rotaPerson);
        }

        public static bool IsWeekday(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday)
                return false;
            if (date.DayOfWeek == DayOfWeek.Sunday)
                return false;
            return true;
        }

        public void PopulateRota()
        {
            CreateOnCallRota();

            CreateProtectedAndSurgeryTime();
        }

        public void CreateProtectedAndSurgeryTime()
        {
            var surgeryList = SurgeryRota.Keys.ToList();
            for (int i = 0; i < surgeryList.Count; i++)
            {
                Session s = surgeryList[i];

                if (s.Date.DayOfWeek != DayOfWeek.Friday || s.SessionType.MorningOrAfternoon != MorningOrAfternoon.afternoon)
                    continue;

                var person = DetermineAvailableLeastSessionsInSlot(s, SurgeryRota, OnCallRota, ProtectedRota, OnCallRegistrarCoverRota);
                SurgeryRota[s] = person;
            }

            var protectedList = ProtectedRota.Keys.ToList();
            for (int i = 0; i < protectedList.Count; i++)
            {
                Session s = protectedList[i];

                if (s.Date.DayOfWeek != DayOfWeek.Friday || s.SessionType.MorningOrAfternoon != MorningOrAfternoon.afternoon)
                    continue;

                var person = DetermineAvailableLeastSessionsInSlot(s, ProtectedRota, OnCallRota, SurgeryRota, OnCallRegistrarCoverRota);
                ProtectedRota[s] = person;
            }


        }

        public void CreateOnCallRota()
        {
            var onCallList = OnCallRota.Keys.ToList();

            for (int i = 0; i < onCallList.Count; i++)
            {
                Session s = onCallList[i];

                if (PreferredOnCallRota.ContainsKey(s.SessionType))
                {
                    var preferredPerson = PreferredOnCallRota[s.SessionType];
                    if (preferredPerson.IsAvailable(s))
                    {
                        OnCallRota[s] = preferredPerson;
                    }
                    else
                    {
                        //if the usual person isn't available, pick the person with the least sessions in that slot
                        OnCallRota[s] = DetermineAvailableLeastSessionsInSlot(s, OnCallRota);
                    }

                    //we need cover for Gordon
                    if (OnCallRota[s].Name == "Gordon")
                    {
                        RotaPerson supervisor = DetermineAvailableLeastSessionsInSlot(s, OnCallRegistrarCoverRota, OnCallRota);
                        OnCallRegistrarCoverRota[s] = supervisor;
                    }

                }
                
            }
        }

        public int GetRotaCount(RotaPerson person, DayOfWeek day, MorningOrAfternoon amPM, Dictionary<Session, RotaPerson> rota)
        {
            return rota.Count(x => (x.Value == person && x.Key.SessionType.Day == day && x.Key.SessionType.MorningOrAfternoon == amPM));
        }

        public int GetRotaCount(RotaPerson person, Dictionary<Session, RotaPerson> rota)
        {
            return rota.Count(x => (x.Value == person));
        }

        private RotaPerson DetermineAvailableLeastSessionsInSlot(Session s, Dictionary<Session, RotaPerson> rotaWeCareAbout, params Dictionary<Session, RotaPerson>[] rotasToAvoidClashing)
        {
            RotaPerson leastOnCall = null;
            int onCall = int.MaxValue;


            foreach (RotaPerson person in PeopleInRota)
            {
                if (!person.IsAvailable(s) || !person.WorksOnDay(s))
                    continue;

                bool clash = false;
                foreach (Dictionary<Session, RotaPerson> clashRota in rotasToAvoidClashing)
                {
                    if (clashRota.ContainsKey(s))
                        if (clashRota[s] == person)
                            clash = true;
                }

                if (clash)
                    continue;

                var matchingOnCallSessions = GetRotaCount(person, s.SessionType.Day, s.SessionType.MorningOrAfternoon, rotaWeCareAbout);

                if (matchingOnCallSessions < onCall)
                {
                    leastOnCall = person;
                    onCall = matchingOnCallSessions;
                }
            }

            return leastOnCall;
        }

        public void PrintOnCallRota()
        {
            foreach (KeyValuePair<Session, RotaPerson> pair in OnCallRota)
            {
                string rotaPerson = "TBD";
                if (pair.Value != null)
                    rotaPerson = pair.Value.Name;

                Console.WriteLine(pair.Key.Date.ToShortDateString() + " : " + pair.Key.SessionType + " " + rotaPerson);
            }
        }

        public void ReadAnnualLeave(string path)
        {
            var lines = File.ReadAllLines(path);
            RotaPerson currentRotaPerson = null;

            foreach (String line in lines)
            {
                if (String.IsNullOrEmpty(line))
                    continue;

                if (String.IsNullOrWhiteSpace(line))
                    continue;

                if (PeopleInRota.Any(x => x.Name == line))
                {
                    currentRotaPerson = PeopleInRota.Find(x => x.Name == line);
                    continue;
                }

                if (currentRotaPerson != null)
                {

                    var dates = line.Split(' ');

                    if (dates.Length == 1)
                    {
                        AddLeaveForDate(currentRotaPerson, dates[0].Trim(), MorningOrAfternoon.morning);
                        AddLeaveForDate(currentRotaPerson, dates[0].Trim(), MorningOrAfternoon.afternoon);
                    }

                    if (dates.Length == 2)
                    {

                        if (dates[1] == "AM" || dates[1] == "PM")
                        {
                            if (dates[1] == "AM")
                                AddLeaveForDate(currentRotaPerson, dates[0].Trim(), MorningOrAfternoon.morning);
                            else
                                AddLeaveForDate(currentRotaPerson, dates[0].Trim(), MorningOrAfternoon.afternoon);
                        }
                        else
                        {
                            AddLeaveRange(currentRotaPerson, dates[0], dates[1]);
                        }

                    }


                }

            }

        }

        private void AddLeaveRange(RotaPerson currentRotaPerson, string startString, string endString)
        {
            DateTime start = DateTime.Parse(startString);
            DateTime end = DateTime.Parse(endString);

            for (DateTime date = start; date.Date <= end.Date; date = date.AddDays(1))
            {
                if (IsWeekday(date))
                {
                    AddLeaveForDate(currentRotaPerson, date, MorningOrAfternoon.morning);
                    AddLeaveForDate(currentRotaPerson, date, MorningOrAfternoon.afternoon);
                }
            }
        }

        private void AddLeaveForDate(RotaPerson currentRotaPerson, string dateString, MorningOrAfternoon amPM)
        {
            DateTime date = DateTime.Parse(dateString);

            Session s = new Session();
            s.Date = date;
            s.SessionType = new SessionType(date.DayOfWeek, amPM);

            currentRotaPerson.AddAnnualLeave(s);
        }

        private void AddLeaveForDate(RotaPerson currentRotaPerson, DateTime date, MorningOrAfternoon amPM)
        {
            Session s = new Session();
            s.Date = date;
            s.SessionType = new SessionType(date.DayOfWeek, amPM);

            currentRotaPerson.AddAnnualLeave(s);
        }

        public void WriteRotaCsv(string path)
        {
            var csv = new StringBuilder();

            foreach (KeyValuePair<Session, RotaPerson> pair in OnCallRota)
            {
                string rotaPerson = "TBD";

                if (pair.Value != null)
                {
                    rotaPerson = pair.Value.Name;
                }

                var first = pair.Key.Date.ToShortDateString();
                var second = pair.Key.SessionType;
                var third = rotaPerson;
                var newLine = string.Format("{0},{1},{2}", first, second, third);
                csv.Append(newLine);
            }

            File.WriteAllText(path, csv.ToString());
        }

        public void WriteRotaCsv(string path, DayOfWeek day, MorningOrAfternoon amPM)
        {
            var csv = new StringBuilder();

            foreach (KeyValuePair<Session, RotaPerson> pair in OnCallRota)
            {

                if (pair.Key.SessionType.Day != day)
                    continue;
                if (pair.Key.SessionType.MorningOrAfternoon != amPM)
                    continue;

                string rotaPerson = "TBD";

                if (pair.Value != null)
                {
                    rotaPerson = pair.Value.Name;
                }

                var first = pair.Key.Date.ToShortDateString();
                var second = pair.Key.SessionType;
                var third = rotaPerson;
                var newLine = string.Format("{0},{1},{2}", first, second, third);
                csv.Append(newLine);
            }

            File.WriteAllText(path, csv.ToString());
        }

        public void PrintOnCallRota(DayOfWeek day, MorningOrAfternoon amPM)
        {
            foreach (KeyValuePair<Session, RotaPerson> pair in OnCallRota)
            {
                if (pair.Key.SessionType.Day != day)
                    continue;
                if (pair.Key.SessionType.MorningOrAfternoon != amPM)
                    continue;

                string rotaPerson = "TBD";

                if (pair.Value != null)
                {
                    rotaPerson = pair.Value.Name;
                }

                string cover = "N/A";
                if (OnCallRegistrarCoverRota[pair.Key] != null)
                    cover = OnCallRegistrarCoverRota[pair.Key].Name;


                string surgeryProtected = "";

                if (pair.Key.Date.DayOfWeek == DayOfWeek.Friday && pair.Key.SessionType.MorningOrAfternoon == MorningOrAfternoon.afternoon)
                {
                    var prot = "TBD";
                    var surgery = "TBD";

                    if (SurgeryRota[pair.Key] != null)
                        surgery = SurgeryRota[pair.Key].Name;

                    if (ProtectedRota[pair.Key] != null)
                        prot = ProtectedRota[pair.Key].Name;

                    surgeryProtected = "SURGERY: " + surgery + " PROTECTED: " + prot;
                }


                Console.WriteLine(pair.Key.Date.ToShortDateString() + ", " + pair.Key.SessionType + "    -----   " + "ONCALL: " +  rotaPerson + " COVER: " + cover + " " + surgeryProtected);

            }
        }

        public void PrintAllOnCallCounts()
        {
            foreach (RotaPerson person in PeopleInRota)
            {
                var matchingOnCallSessions = GetRotaCount(person, OnCallRota);
                Console.WriteLine(person.Name + " sessions " + matchingOnCallSessions);
            }
        }

        public void PrintOnCallCounts(DayOfWeek day, MorningOrAfternoon amPM)
        {
            foreach (RotaPerson person in PeopleInRota)
            {
                var matchingOnCallSessions = GetRotaCount(person, day, amPM, OnCallRota);
                var surgerySessions = GetRotaCount(person, day, amPM, SurgeryRota);
                var protectedSessions = GetRotaCount(person, day, amPM, ProtectedRota);
                var coverSessions = GetRotaCount(person, day, amPM, OnCallRegistrarCoverRota);

                Console.WriteLine(person.Name + " " + matchingOnCallSessions + " on call, " + coverSessions + " Gordon cover, "  + surgerySessions + " surgery, " + protectedSessions + " protected");
            }
        }


    }
}


using System;
using System.Collections.Generic;
using System.Linq;

namespace RotaPrototype
{
    [Serializable]
    public struct Session
    {
        public SessionType SessionType;
        public RotaPerson RotaPerson;
        public DateTime Date;
    }

    [Serializable]
    public struct SessionType
    {
        public DayOfWeek Day;
        public MorningOrAfternoon MorningOrAfternoon;
        public SessionType(DayOfWeek day, MorningOrAfternoon amPM)
        {
            Day = day;
            MorningOrAfternoon = amPM;
        }
        public override string ToString()
        {
            return Day + " : " + MorningOrAfternoon;
        }
    }

    public enum MorningOrAfternoon
    {
        morning,
        afternoon
    }

    [Serializable]
    public class RotaPerson
    {

        public string Name { get; set; }

        public List<Session> AnnualLeaveDates;
        public List<SessionType> EligibleSessions;
        public bool IsRegistrar = false;

        public RotaPerson(string name)
        {
            Name = name;
            AnnualLeaveDates = new List<Session>();
            EligibleSessions = new List<SessionType>();
        }


        public void AddEligibleSession(SessionType session)
        {
            EligibleSessions.Add(session);
        }

        public void AddAnnualLeave(Session date)
        {
            AnnualLeaveDates.Add(date);
        }

        public bool WorksOnDay(DateTime day)
        {
            return EligibleSessions.Any(x => x.Day == day.DayOfWeek);
        }

        public bool WorksOnDay(Session session)
        {
            return EligibleSessions.Contains(session.SessionType);
        }

        public bool IsAvailable(Session s)
        {
            return !AnnualLeaveDates.Contains(s);
        }

        public override string ToString()
        {
            return Name;
        }

    }

    public class PersonFactory
    {
        public static RotaPerson CreateJulie()
        {
            RotaPerson p = new RotaPerson("Julie");

            p.AddEligibleSession(new SessionType(DayOfWeek.Tuesday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.afternoon));


            return p;
        }

        public static RotaPerson CreateKevin()
        {
            RotaPerson p = new RotaPerson("Kevin");

            p.AddEligibleSession(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Tuesday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Tuesday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Thursday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.afternoon));

            return p;
        }

        public static RotaPerson CreateChris()
        {
            RotaPerson p = new RotaPerson("Chris");

            p.AddEligibleSession(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Tuesday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Thursday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.afternoon));

            return p;
        }

        public static RotaPerson CreateAileen()
        {
            RotaPerson p = new RotaPerson("Aileen");

            p.AddEligibleSession(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Thursday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Thursday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.morning));


            return p;
        }

        public static RotaPerson CreateGordon()
        {
            RotaPerson p = new RotaPerson("Gordon");

            p.AddEligibleSession(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Monday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Tuesday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Tuesday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Wednesday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Thursday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Thursday, MorningOrAfternoon.afternoon));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.morning));
            p.AddEligibleSession(new SessionType(DayOfWeek.Friday, MorningOrAfternoon.afternoon));

            return p;
        }

    }
}
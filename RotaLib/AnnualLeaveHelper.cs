using System;

namespace RotaPrototype
{
    public class AnnualLeaveHelper
    {
        internal static void GenerateRandomLeave(GovanhillRota rota)
        {
            Random r = new Random();

            foreach (RotaPerson person in rota.PeopleInRota)
            {
                var sessionCount = person.EligibleSessions.Count;
                var annualLeave = sessionCount * 4;

                for (DateTime date = rota.StartDate; date.Date <= rota.EndDate.Date; date = date.AddDays(1))
                {
                    if (annualLeave > 0)
                    {
                        if (GovanhillRota.IsWeekday(date))
                        {
                            if (person.WorksOnDay(date))
                            {
                                var diceRoll = r.Next(0, 100);
                                if (diceRoll > 90)
                                {

                                    Session morning = new Session();
                                    morning.Date = date;
                                    morning.SessionType = new SessionType(date.DayOfWeek, MorningOrAfternoon.morning);

                                    Session afternoon = new Session();
                                    afternoon.Date = date;
                                    afternoon.SessionType = new SessionType(date.DayOfWeek, MorningOrAfternoon.afternoon);

                                    person.AddAnnualLeave(morning);
                                    person.AddAnnualLeave(afternoon);

                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
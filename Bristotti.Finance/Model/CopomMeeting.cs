using System;

namespace Bristotti.Finance.Model
{
    public class CopomMeeting : Entity
    {
        public virtual double MeetingNumber { get; set; }
        public virtual DateTime MeetingDate { get; set; }
        public virtual DateTime EffectiveInitialDate { get; set; }
        public virtual DateTime? EffectiveFinalDate { get; set; }
        public virtual double? InterestTarget { get; set; }
        public virtual bool ExtraordinaryMeeting { get; set; }
        public virtual string Observation { get; set; }
    }
}
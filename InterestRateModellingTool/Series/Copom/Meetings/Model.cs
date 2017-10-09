using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Bristotti.Finance.Model;
using InterestRateModellingTool.Annotations;

namespace InterestRateModellingTool.Series.Copom.Meetings
{
    public class Model : INotifyPropertyChanged
    {
        private IList<CopomMeeting> _copomMeetings;
        private DateTime _date;

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        public IList<CopomMeeting> CopomMeetings
        {
            get => _copomMeetings;
            set
            {
                _copomMeetings = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

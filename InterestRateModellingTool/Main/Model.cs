using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Bristotti.Finance.Model;
using InterestRateModellingTool.Annotations;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace InterestRateModellingTool.Main
{
    public class Model : INotifyPropertyChanged
    {
        private ObservableCollection<CopomMeeting> _copomMeetings;
        private DateTime _date;
        private ObservableCollection<DI1> _di1series;
        private Yield[] _yields;
        private PlotModel _plotModel;

        public Model()
        {
            PlotModel = new PlotModel { Title = "Yield Curve" };
            var series = new LineSeries
            {
                MarkerType = MarkerType.Circle
            };

            PlotModel.Series.Add(series);

            PlotModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd"
            });
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CopomMeeting> CopomMeetings
        {
            get => _copomMeetings;
            set
            {
                _copomMeetings = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DI1> DI1Series
        {
            get => _di1series;
            set
            {
                _di1series = value;
                OnPropertyChanged();
            }
        }

        public Yield[] Yields
        {
            get => _yields;
            set { _yields = value; OnPropertyChanged();}
        }

        public ICommand TestCommand { get; set; }

        public ICommand RefreshCommand { get; set; }

        public ICommand BuildCurveCommand { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PlotModel PlotModel
        {
            get => _plotModel;
            set { _plotModel = value; OnPropertyChanged();}
        }
    }
}
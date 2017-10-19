using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Bristotti.Finance;
using Bristotti.Finance.ExcelDataAccess;
using Bristotti.Finance.Model;
using Bristotti.Finance.Repositories;
using InterestRateModellingTool.Infraestructure;
using OxyPlot;
using OxyPlot.Series;

namespace InterestRateModellingTool.Main
{
    public class ViewModel
    {
        private readonly Model _model;
        private readonly MainWindow _view;
        private readonly IYieldRepository _yieldRepository;

        public ViewModel()
        {
            _model = new Model
            {
                Date = DateTime.Today,
                RefreshCommand = new CommandHandler(Refresh, true),
                BuildCurveCommand = new CommandHandler(BuildCurve, true)
            };

            _view = new MainWindow
            {
                DataContext = _model
            };

            _yieldRepository = new YieldRepository("sample.xlsx");
        }

        private void BuildCurve()
        {
            var engine = new YieldCurveEngine();


            var holidays = _yieldRepository.GetHolidays();
            var cdi = _yieldRepository.GetCDI(_model.Date);
            _model.Yields = engine.BuildYield(
                    _model.Date,
                    _model.CopomMeetings.ToArray(),
                    _model.DI1Series.ToArray(),
                    cdi,
                    holidays)
                .ToArray();


            var scatter = (LineSeries) _model.PlotModel.Series[0];
            scatter.Points.Clear();
            scatter.Points.AddRange(_model.Yields.Select(yield => new DataPoint(yield.Term, yield.Spot)));
            _model.PlotModel.InvalidatePlot(true);

            //var plot = new PlotModel { Title = "Yield Curve" };
            //var scatter = new ScatterSeries
            //{
            //    MarkerType = MarkerType.Cross
            //};
            //var points = _model.Yields.Select(yield => new ScatterPoint(yield.Term, yield.Spot)).ToArray();
            //scatter.Points.AddRange(points);
            //_model.PlotModel = plot;
        }

        private void Refresh()
        {
            _model.CopomMeetings = new ObservableCollection<CopomMeeting>(_yieldRepository.GetCopomMeetings(_model.Date));
            _model.DI1Series = new ObservableCollection<DI1>(_yieldRepository.GetDI1s(_model.Date));
        }

        public void Show()
        {
            _view.Show();
        }
    }
}
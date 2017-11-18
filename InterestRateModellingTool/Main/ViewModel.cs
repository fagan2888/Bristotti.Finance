using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Bristotti.Finance;
using Bristotti.Finance.ExcelDataAccess;
using Bristotti.Finance.Model;
using Bristotti.Finance.Repositories;
using InterestRateModellingTool.Infraestructure;
using OxyPlot;
using OxyPlot.Axes;
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
            try
            {
                var engine = new YieldCurveEngine();

                var holidays = _yieldRepository.GetHolidays();
                var cdi = _yieldRepository.GetCDI(_model.Date);
                //_model.Yields = engine.BuildYield(
                //        _model.Date,
                //        _model.CopomMeetings.ToArray(),
                //        _model.DI1Series.ToArray(),
                //        cdi,
                //        holidays)
                //    .ToArray();

                _model.Yields = engine.BuildYield2(
                        _model.Date,
                        _model.CopomMeetings.ToArray(),
                        _model.DI1Series.ToArray(),
                        cdi,
                        holidays)
                    .ToArray();


                var line = (LineSeries)_model.PlotModel.Series[0];
                line.Points.Clear();
                line.Points.AddRange(_model.Yields.Select(yield => new DataPoint(DateTimeAxis.ToDouble(yield.Maturity), yield.Spot)));
                _model.PlotModel.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
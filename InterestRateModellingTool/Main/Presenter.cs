using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Finance.ExcelDataAccess;
using Bristotti.Finance.Model;
using InterestRateModellingTool.Infraestructure;

namespace InterestRateModellingTool.Main
{
    public class Presenter
    {
        private readonly MainWindow _view;
        private readonly Model _model;

        public Presenter()
        {
            _model = new Model
            {
                Date = DateTime.Today,
                TestCommand = new CommandHandler(Test, true),
                RefreshCommand = new CommandHandler(Refresh, true),
                BuildCurveCommand = new CommandHandler(BuildCurve, true)
            };
            _view = new MainWindow
            {
                DataContext = _model
            };
        }

        private void BuildCurve()
        {
            DateTime date;
        }

        private void Refresh()
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.xlsx");

            var copomRepo = new CopomRepository(file);
            var di1Repo = new DI1Repository(file);
            _model.CopomMeetings = new ObservableCollection<CopomMeeting>(copomRepo.GetMeetings());
            _model.DI1Series = new ObservableCollection<DI1>(di1Repo.GetByMarketDate(_model.Date));
        }

        public void Show()
        {
            _view.Show();
        }

        protected virtual void Test()
        {
            //var repo = new CopomRepository("data.xlsx");
            //var meetings = repo.GetMeetings();

            (new Series.Copom.Meetings.Presenter()).Show();
        }
    }
}

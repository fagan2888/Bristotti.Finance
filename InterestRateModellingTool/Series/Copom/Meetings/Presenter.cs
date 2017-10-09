using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bristotti.Finance.ExcelDataAccess;

namespace InterestRateModellingTool.Series.Copom.Meetings
{
    class Presenter
    {
        private readonly CopomMeetingWindow _view;
        private readonly Model _model;

        public Presenter()
        {
            var repo = new CopomRepository("data.xlsx");
            var meetings = repo.GetMeetings();
            _model = new Model
            {
                CopomMeetings = meetings
            };
            _view = new CopomMeetingWindow
            {
                DataContext = _model
            };
        }

        public void Show()
        {
            _view.Show();
        }
    }
}

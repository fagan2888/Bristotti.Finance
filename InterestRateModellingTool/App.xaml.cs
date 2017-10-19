using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using InterestRateModellingTool.Main;

namespace InterestRateModellingTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            BootStrapper.Init();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            (new ViewModel()).Show();
            //(new Series.Copom.Meetings.Presenter()).Show();
        }
    }
}

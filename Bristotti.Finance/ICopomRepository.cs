using System;
using System.Collections.Generic;
using Bristotti.Finance.Model;

namespace Bristotti.Finance
{
    public interface ICopomRepository : IDisposable
    {
        IList<CopomMeeting> GetMeetings();
    }
}
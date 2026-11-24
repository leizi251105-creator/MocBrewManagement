
using System;

namespace MocBrewManagement.ViewModels
{
    public class POSOrderHistoryViewModel
    {
        public long HistoryId { get; set; }

        public int OrderId { get; set; }

        public string OldStatus { get; set; }

        public string NewStatus { get; set; }

        public string Note { get; set; }

        public DateTime ChangedAt { get; set; }

        public string EmployeeName { get; set; }
    }
}

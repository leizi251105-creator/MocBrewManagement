
using System;
using System.Collections.Generic;

namespace MocBrewManagement.ViewModels
{
    public class WorkShiftViewModel
    {
        public long ShiftId { get; set; }

        public string ShiftCode { get; set; }

        public int AccountId { get; set; }

        public string EmployeeName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public decimal OpeningCash { get; set; }

        public decimal ExpectedCash { get; set; }

        public decimal? ActualCash { get; set; }

        public decimal? CashDifference { get; set; }

        public string Status { get; set; }

        public string Note { get; set; }

        public int TotalOrders { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal CashRevenue { get; set; }

        public decimal BankTransferRevenue { get; set; }
    }

    public class WorkShiftCloseViewModel
    {
        public long ShiftId { get; set; }

        public decimal ActualCash { get; set; }

        public string Note { get; set; }
    }
}

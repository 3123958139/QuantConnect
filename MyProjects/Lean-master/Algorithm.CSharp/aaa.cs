using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System;


namespace QuantConnect.Algorithm.CSharp
{
    public class aaa : QCAlgorithm
    {
        private decimal _fastMa;
        private decimal _slowMa;
        private decimal _lastPrice;
        private DateTime _resample;
        private TimeSpan _resamplePeriod;
        private readonly DateTime _startDate = new DateTime(2023, 10, 7);
        private readonly DateTime _endDate = new DateTime(2023, 11, 11);
        private Symbol symbol;

        public override void Initialize()
        {
            SetStartDate(2023, 10, 07);
            SetEndDate(2023, 11, 11);
            SetCash(100000);
            symbol = AddEquity("stk000001", Resolution.Daily).Symbol;
            AddEquity("stk000000", Resolution.Daily);
            SetBenchmark("stk000000");
            var avgCross = new Chart("Strategy Equity");
            var fastMa = new Series("FastMA", SeriesType.Line, 1);
            var slowMa = new Series("SlowMA", SeriesType.Line, 1);
            avgCross.AddSeries(fastMa);
            avgCross.AddSeries(slowMa);
            AddChart(avgCross);
            _resamplePeriod = TimeSpan.FromMinutes((_endDate - _startDate).TotalMinutes / 2000);
        }

        public override void OnData(Slice slice)
        {
            Debug(slice.Bars["stk000001"].Open.ToString("F2"));
            if (!Portfolio.Invested)
            {
                SetHoldings(symbol, 1);
                Debug("Purchased Stock");
            }
            _lastPrice = slice["stk000001"].Close;

            if (_fastMa == 0) _fastMa = _lastPrice;
            if (_slowMa == 0) _slowMa = _lastPrice;

            _fastMa = (0.01m * _lastPrice) + (0.99m * _fastMa);
            _slowMa = (0.001m * _lastPrice) + (0.999m * _slowMa);

            if (Time > _resample)
            {
                _resample = Time.Add(_resamplePeriod);
                Plot("Strategy Equity", "FastMA", _fastMa);
                Plot("Strategy Equity", "SlowMA", _slowMa);
            }
        }
    }
}

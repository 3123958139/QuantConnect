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
            //本地已经生成stk000000~stk000999的1998~2024的日线数据
            SetStartDate(2023, 10, 07);
            SetEndDate(2023, 11, 11);
            SetCash(100000);
            //订阅数据
            symbol = AddEquity("stk000001", Resolution.Daily).Symbol;
            AddEquity("stk000000", Resolution.Daily);
            //设置基础资产，用来算一些指标用到
            SetBenchmark("stk000000");
            //图表初始化
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
            //测试Debug的通信
            Debug(slice.Bars["stk000001"].Open.ToString("F2"));
            //测试Order的通信
            if (!Portfolio.Invested)
            {
                SetHoldings(symbol, 1);
                Debug("Purchased Stock");
            }
            //测试Chart的通信
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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class aaa : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2020, 10, 07);  
            SetEndDate(2023, 10, 11);   
            SetCash(100000);             
            AddEquity("stk000000", Resolution.Daily);
            AddEquity("stk000001", Resolution.Daily);
            SetBenchmark("stk000000");
        }

        public override void OnData(Slice slice)
        {
                Debug(slice.Bars["stk000001"].Open.ToString("F2"));
        }
    }
}

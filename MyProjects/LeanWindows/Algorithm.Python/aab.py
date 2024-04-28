from AlgorithmImports import *


class aab(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2022,1,1)
        self.SetEndDate(2023,1,1)
        self.SetCash(100000)

        stk000001 = self.AddEquity("stk000001", Resolution.Daily).Symbol

        stockPlot = Chart("Trade Plot")
        stockPlot.AddSeries(Series("Buy", SeriesType.Scatter, 0))
        stockPlot.AddSeries(Series("Sell", SeriesType.Scatter, 0))
        stockPlot.AddSeries(Series("Price", SeriesType.Line, 0))
        self.AddChart(stockPlot)

        avgCross = Chart("Average Cross")
        avgCross.AddSeries(Series("FastMA", SeriesType.Line, 0))
        avgCross.AddSeries(Series("SlowMA", SeriesType.Line, 0))
        self.AddChart(avgCross)

        weeklySpyPlot = Chart("Weekly SPY")
        spyCandlesticks = CandlestickSeries("SPY")
        weeklySpyPlot.AddSeries(spyCandlesticks)
        self.AddChart(weeklySpyPlot)

        self.Consolidate(stk000001, Calendar.Weekly, lambda bar: self.Plot("Weekly SPY", "SPY", bar))

        self.fastMA = 0
        self.slowMA = 0
        self.lastPrice = 0
        self.resample = datetime.min
        self.resamplePeriod = (self.EndDate - self.StartDate) / 2000

    def OnData(self, slice):
        if slice["stk000001"] is None: return

        self.lastPrice = slice["stk000001"].Close
        if self.fastMA == 0: self.fastMA = self.lastPrice
        if self.slowMA == 0: self.slowMA = self.lastPrice
        self.fastMA = (0.01 * self.lastPrice) + (0.99 * self.fastMA)
        self.slowMA = (0.001 * self.lastPrice) + (0.999 * self.slowMA)


        if self.Time > self.resample:
            self.resample = self.Time  + self.resamplePeriod
            self.Plot("Average Cross", "FastMA", self.fastMA)
            self.Plot("Average Cross", "SlowMA", self.slowMA)

        # On the 5th days when not invested buy:
        if not self.Portfolio.Invested and self.Time.day % 13 == 0:
        	self.Order("stk000001", (int)(self.Portfolio.MarginRemaining / self.lastPrice))
        	self.Plot("Trade Plot", "Buy", self.lastPrice)
        elif self.Time.day % 21 == 0 and self.Portfolio.Invested:
            self.Plot("Trade Plot", "Sell", self.lastPrice)
            self.Liquidate()

    def OnEndOfDay(self, symbol):
       #Log the end of day prices:
       self.Plot("Trade Plot", "Price", self.lastPrice)

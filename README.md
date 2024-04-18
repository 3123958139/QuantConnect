1. config.json启用`desktop-http-port`和`messaging-handler`，取自带StreamingMessageHandler即可；

   ```json
    "backtesting-desktop": {
      "live-mode": false,
      "desktop-http-port": 33333,
      "messaging-handler": "QuantConnect.Messaging.StreamingMessageHandler",
      "setup-handler": "QuantConnect.Lean.Engine.Setup.BacktestingSetupHandler",
      "result-handler": "QuantConnect.Lean.Engine.Results.BacktestingResultHandler",
      "data-feed-handler": "QuantConnect.Lean.Engine.DataFeeds.FileSystemDataFeed",
      "real-time-handler": "QuantConnect.Lean.Engine.RealTime.BacktestingRealTimeHandler",
      "history-provider": [ "QuantConnect.Lean.Engine.HistoricalData.SubscriptionDataReaderHistoryProvider" ],
      "transaction-handler": "QuantConnect.Lean.Engine.TransactionHandlers.BacktestingTransactionHandler"
    },
   ```

2. `Lean`为后端，`Panoptes.exe`为GUI前端；

3. 策略通过`OnData`打包进`Debug`的`message`进行TCP传递；

4. 


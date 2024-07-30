from datetime import timedelta
from QuantConnect.Data.Custom.Tiingo import *
from AlgorithmImports import *

from env import TradingEnv
from explore import Runner
from agent import TD3

import pandas as pd
import numpy as np

import math


class TwinDelayedDDPG(QCAlgorithm):  # 定义一个名为TwinDelayedDDPG的类
   def Initialize(self):
       """
       初始化函数，设置交易环境的基本参数，包括特征窗口大小、回看周期、测试周期等，并配置交易品种和基准指数。
       该函数还负责创建和配置交易环境、设置模型参数，并初始化交易代理。

       参数:
       self: 表示实例自身的引用。

       返回值:
       无
       """

       # 初始化一些基本参数
       self.FeatureWindow = 10  # 特征窗口大小
       self.LookBack = 100 * 2  # 回看周期
       self.Test = 20 * 2  # 测试周期
       self.LastDataNum = -1  # 最后一个数据的编号

       live = False  # 是否为实盘交易

       # 设置交易开始和结束日期
       self.SetStartDate(2019, 1, 1)
       self.SetEndDate(2019, 1, 31)

       # 初始化股票列表和安全列表
       self.symbolDataBySymbol = {}  # 通过符号存储数据的字典
       self.SymbolList = ['STK000000',
                          'STK000001',
                          'STK000002']  # 股票符号列表

       self.SecurityList = []  # 安全列表

       # 为每个股票符号添加股票并初始化SymbolData
       for symbol in self.SymbolList:
           security = self.AddEquity(symbol, Resolution.Daily)  # 添加股票
           self.SecurityList.append(security.Symbol)  # 添加符号到安全列表
           self.symbolDataBySymbol[security.Symbol] = SymbolData(
               self, security.Symbol, self.FeatureWindow, Resolution.Daily)  # 初始化SymbolData

       self.SetBenchmark("STK000000")  # 设置基准指数

       # 初始化交易环境
       env = TradingEnv(self.FeatureWindow)
       env.reset()
       self.environment = env  # 交易环境

       # 初始化一些运行标志和模型训练标志
       self.observationRun = False
       self.modelIsTraining = False

       # 根据环境设置模型的参数
       state_size = env.observation_space.shape[0]  # 状态空间大小
       action_dim = env.action_space.shape[0]  # 动作空间维度
       max_action = float(env.action_space.high[0])  # 最大动作值
       seed = 0  # 随机种子

       sizes = (state_size, action_dim, max_action, seed)  # 模型参数

       # 初始化运行器和AI交易代理
       self.runnerObj = Runner(self, n_episodes=170, batch_size=5, gamma=0.99, tau=0.005, noise=0.2,
                               noise_clip=0.5, explore_noise=0.1, policy_frequency=2, sizes=sizes)

       self.AI_TradeAgent = TD3(
           self, state_dim=state_size, action_dim=action_dim, max_action=max_action, seed=seed)

       # 如果是实盘交易，则加载回放缓冲区
       if live:
           self.runnerObj.replay_buffer.load(name='ReplayBuff')

       # 开始训练
       self.Train(self.TrainingMethod)

       # 定义每周日6点开始的训练计划
       self.Train(self.DateRules.Every(DayOfWeek.Sunday),
                  self.TimeRules.At(6, 0), self.TrainingMethod)

   def TrainTimeCheck(self):
       """
       检查是否需要训练的时间。

       该方法不接受参数。

       返回值：
       - True：如果上一次记录的数据月份与当前月份不同，且当前月份为偶数月，或上一次记录的月份数据未设置。
       - False：如果上一次记录的数据月份与当前月份相同，且当前月份不是偶数月。
       """

       today = self.Time  # 获取当前时间
       weekNum = today.strftime("%V")  # 获取当前时间的星期数
       dayNum = today.strftime("%e")  # 获取当前时间的日数
       monthNum = today.strftime("%m")  # 获取当前时间的月份

       # 判断是否需要更新训练，即判断当前月份与上一次记录的月份是否不同，且当前月份为偶数，或上一次月份未记录
       if self.LastDataNum == -1:
           self.LastDataNum = monthNum
           return True
       return False

   def HistoricalData(self, lookBack=100):
       """
       获取历史数据的函数，用于提取指定时间段内证券列表的每日收盘价和成交量数据。

       参数:
       lookBack: int, 默认值为100，表示要查看的过去的数据天数。

       返回值:
       pd.DataFrame, 包含证券列表中每个证券的收盘价和成交量数据的DataFrame，其中行表示时间，列表示证券和数据类型（收盘价或成交量）。
       """

       # 从历史数据中获取指定时间段内的每日数据
       historyData = self.History(
           self.SecurityList, lookBack, Resolution.Daily)
       historyData.dropna(inplace=True)  # 删除缺失值

       pricesX = {}  # 存储证券收盘价的字典
       volumeX = {}  # 存储证券成交量的字典

       # 遍历证券列表，提取每个证券的收盘价和成交量
       for symbol in self.SecurityList:
           if not historyData.empty:
               pricesX[symbol.Value] = list(
                   historyData.loc[str(symbol.Value)]['close'])[:-1]
               volumeX[symbol.Value] = list(
                   historyData.loc[str(symbol.Value)]['volume'])[:-1]

           maxValue = len(pricesX[symbol.Value])  # 计算最长数据长度

       dictOfDict = {'close': pricesX, 'volume': volumeX}  # 将收盘价和成交量数据封装成字典

       # 将字典中的数据转换为DataFrame
       dictOfDf = {k: pd.DataFrame(v) for k, v in dictOfDict.items()}

       df = pd.concat(dictOfDf, axis=1)  # 按列合并DataFrame

       # 重新设置DataFrame的列标签，以实现多级索引
       temp1 = df.columns.get_level_values(0)
       v1 = pd.Categorical(df.columns.get_level_values(0),
                           categories=['close', 'volume'],
                           ordered=True)

       temp2 = df.columns.get_level_values(1)
       v2 = pd.Categorical(df.columns.get_level_values(1),
                           categories=self.SymbolList,
                           ordered=True)

       df.columns = pd.MultiIndex.from_arrays([v2, v1])  # 设置多级列索引

       return df.sort_index(axis=1, level=[0, 1])  # 按索引的级别排序DataFrame的列，并返回

   def TrainingMethod(self):
       """
       训练方法：负责执行训练流程。

       无参数和返回值。
       """

       # 检查是否到达训练时间
       train = self.TrainTimeCheck()

       # 如果未到训练时间，则直接返回
       if not train:
           return

       # 设置查看历史数据的长度
       x = self.LookBack
       # 获取历史数据
       df = self.HistoricalData(x)

       # 创建训练环境和测试环境
       trainEnv = TradingEnv(obs_len=self.FeatureWindow,
                             df=df.iloc[:-self.Test])
       testEnv = TradingEnv(obs_len=self.FeatureWindow,
                            df=df.iloc[-self.Test:])

       # 如果还未执行过观察步骤，则对训练环境进行一次观察
       if not self.observationRun:
           self.runnerObj.observe(trainEnv, 1000)
           self.observationRun = True

       # 标记模型为训练中
       self.modelIsTraining = True
       # 进行模型训练
       self.runnerObj.train(testEnv, testEnv)
       # 更新模型训练状态为完成
       self.modelIsTraining = False

   def OnOrderEvent(self, orderEvent):
       """
       处理订单事件的回调函数。

       参数:
       - self: 对象自身的引用。
       - orderEvent: 订单事件对象，包含订单的详细信息。

       返回值:
       - 无。
       """
       self.Debug("{} {}".format(self.Time, orderEvent.ToString()))
       # 在调试模式下输出当前时间及订单事件信息

   def OnEndOfAlgorithm(self):
       """
       在算法结束时执行的操作。

       无参数。
       无返回值。
       """
       # 保存重播缓冲区数据
       self.runnerObj.replay_buffer.save(name='ReplayBuff')
       # 记录当前总 portfolio 值
       self.Log("{} - TotalPortfolioValue: {}".format(self.Time,
                                                      self.Portfolio.TotalPortfolioValue))
       # 记录当前的现金账本
       self.Log("{} - CashBook: {}".format(self.Time, self.Portfolio.CashBook))


class SymbolData:  # 创建一个类，用于存储交易品种的数据
   def __init__(self, algo, symbol, window, resolution):
       """
       初始化函数
       :param algo: 算法实例，用于交易和数据订阅管理
       :param symbol: 交易品种的符号
       :param window: 窗口大小，用于分析数据
       :param resolution: 数据分辨率，如分钟、小时或日
       """

       # 初始化类属性
       self.algo = algo
       self.symbol = symbol
       self.window = window
       self.resolution = resolution

       # 设置交易日志合并器，以天为单位
       self.timeConsolidator = TradeBarConsolidator(timedelta(days=1))
       self.timeConsolidator.DataConsolidated += self.TimeConsolidator  # 当数据合并时触发的事件处理函数
       self.algo.SubscriptionManager.AddConsolidator(
           symbol, self.timeConsolidator)  # 向算法订阅管理器添加合并器

       self.weight_temp = 0  # 临时权重变量

       # 初始化历史数据列表
       self.history_close = []  # 收盘价历史数据
       self.history_volume = []  # 成交量历史数据

       # 计算最大多头仓位比例，基于符号列表长度
       self.max_pos = 1 / len(algo.SymbolList)
       self.max_short_pos = 0.0  # 最大空头仓位比例初始化为0

   def update(self, close, volume, symbol):
       """
       更新价格和成交量历史数据。

       参数:
       close: float - 最新的收盘价。
       volume: int - 最新的成交量。
       symbol: str - 股票代码。

       返回值:
       无
       """
       # 如果历史收盘价列表为空，则从algo中获取20天的历史数据
       if len(self.history_close) == 0:
           hist_df = self.algo.History(
               [self.symbol], timedelta(days=20), self.resolution)

           # 如果数据中没有'close'字段，则返回
           if 'close' not in hist_df.columns:
               return

           # 数据处理：去除NaN值，重置索引，并设置'time'为索引
           hist_df.dropna(inplace=True)
           hist_df.reset_index(level=[0, 1], inplace=True)
           hist_df.set_index('time', inplace=True)
           hist_df.dropna(inplace=True)

           # 保存最后window长度的历史收盘价和成交量
           self.history_close = hist_df.close.values[-self.window:]
           self.history_volume = hist_df.volume.values[-self.window:]

       # 如果历史收盘价列表长度小于window，追加新的收盘价和成交量
       if len(self.history_close) < self.window:
           self.history_close = np.append(self.history_close, close)
           self.history_volume = np.append(self.history_volume, volume)

       # 如果历史收盘价列表长度大于等于window，移除第一个元素并追加新的收盘价和成交量
       else:
           self.history_close = np.append(self.history_close, close)[1:]
           self.history_volume = np.append(self.history_volume, volume)[1:]

   def TimeConsolidator(self, sender, bar):
       '''
       此函数用于整合时间周期内的交易数据，根据AI策略进行交易决策。

       参数:
       - sender: 发送数据的源对象。
       - bar: 包含交易品种的收盘价、成交量等信息的数据对象。

       返回值:
       - 无
       '''

       # 如果模型正在训练，则不执行任何交易操作
       if self.algo.modelIsTraining:
           self.algo.Debug("Retun, model still training")
           return

       # 加载最优平均策略
       self.algo.AI_TradeAgent.load("best_avg")

       # 提取交易品种、收盘价、成交量信息
       symbol = bar.Symbol
       price = bar.Close
       vol = bar.Volume

       # 更新历史价格和成交量数据
       self.update(price, vol, symbol)

       # 如果已经投资于该证券
       if self.algo.Securities[symbol].Invested:
           # 计算当前持仓权重
           currentweight = (
               self.algo.Portfolio[symbol].Quantity * price) / self.algo.Portfolio.TotalPortfolioValue
       else:
           currentweight = 0.0

       # 初始化交易权重
       weight = currentweight

       # 获取新的观测值
       new_obs = self.algo.environment.next_observation(close_window=self.history_close,
                                                        volume_window=self.history_volume)

       # 根据观测值选择行动
       action = self.algo.AI_TradeAgent.select_action(
           np.array(new_obs), noise=0)

       # 如果行动指示买入
       if action > 0.05:
           # 调整权重以反映买入决策
           weight += np.clip(self.algo.environment.normalize(abs(float(action))), 0, 1) * 0.3
           weight = np.clip(round(weight, 4),
                            self.max_short_pos, self.max_pos)

           # 如果新的权重大于之前的最大权重，则更新持仓
           if weight > self.weight_temp:
               self.algo.SetHoldings(symbol, weight, False)
               self.weight_temp = weight

       # 如果行动指示卖出
       elif action < -0.05:
           # 调整权重以反映卖出决策
           weight += - \
               (np.clip(self.algo.environment.normalize(
                   abs(float(action))), 0, 1)) * 0.3
           weight = np.clip(round(weight, 4),
                            self.max_short_pos, self.max_pos)

           # 如果新的权重小于之前的最大权重，则更新持仓
           if weight < self.weight_temp:
               self.algo.SetHoldings(symbol, weight, False)
               self.weight_temp = weight
           else:
               pass

       else:
           # 如果行动指示保持现状，则不做任何操作
           pass


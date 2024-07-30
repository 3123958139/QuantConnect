import numpy as np
import pandas as pd
from collections import deque
import random
from gym import spaces
import math
from scipy.stats import linregress
import random


class TradingEnv(object):  # 创建环境类
   def __init__(self, obs_len=10, df=None):
       """
       初始化函数
       :param obs_len: 观察窗口的长度，默认为10
       :param df: 输入的数据框，默认为None，如果为None，则使用虚拟数据
       """

       self.window = obs_len  # 设置观察窗口长度
       self.data = df  # 存储输入的数据框

       # 如果没有提供数据框，则使用虚拟数据
       if df is None:
           self.data = self.dummy_data()

       # 从数据框的列名中提取唯一符号列表
       x = list(self.data.columns.get_level_values(0))
       x = list(dict.fromkeys(x))

       self.SymbolList = x  # 存储符号列表
       self.CountIter = -1  # 初始化迭代计数器
       self.MaxCount = len(x)  # 计算符号列表的长度
       # 定义动作空间，使用Box空间，范围在-1到+1之间，数据类型为float32
       self.action_space = spaces.Box(-1, +1, (1,), dtype=np.float32)

   def dummy_data(self):
       """
       生成一个包含虚拟股票数据的DataFrame。

       该函数不接受任何参数。

       返回值:
       - df: 一个MultiIndex DataFrame，其中包含三个股票（symbol_1, symbol_2, symbol_3）的收盘价（close）和成交量（volume）数据。
       """

       # 初始化收盘价和成交量数据字典
       x1 = np.zeros(100)  # 创建长度为100的全0数组
       close = {'symbol_1': x1, 'symbol_2': x1, 'symbol_3': x1}
       vol = {'symbol_1': x1, 'symbol_2': x1, 'symbol_3': x1}

       # 将收盘价和成交量数据整合到一个字典中
       y = {'close': close, 'volume': vol}

       # 将字典中的数据转换为DataFrame
       dict_of_df = {k: pd.DataFrame(v) for k, v in y.items()}

       # 将DataFrame合并为一个宽格式的DataFrame
       df = pd.concat(dict_of_df, axis=1)

       # 为DataFrame的列设置多级分类索引
       v = pd.Categorical(df.columns.get_level_values(0),
                          categories=['close', 'volume'],
                          ordered=True)
       v2 = pd.Categorical(df.columns.get_level_values(1),
                           categories=['symbol_1', 'symbol_2', 'symbol_3'],
                           ordered=True)
       df.columns = pd.MultiIndex.from_arrays([v2, v])

       # 按照列的多级索引进行排序
       return df.sort_index(axis=1, level=[0, 1])

   def reset(self, randomIndex=False):
       """
       重置函数，用于初始化或重新设置环境的状态。

       参数:
       - randomIndex: 布尔值，当设置为True时，会随机初始化环境的状态。

       返回值:
       - observations: 环境的当前观察值，用于后续的决策制定。
       """
       # 如果randomIndex为True，随机设置CountIter的值
       if randomIndex:
           self.CountIter = random.randint(0, int(self.MaxCount))

       # 当CountIter加1后大于等于MaxCount时，将CountIter重置为-1
       if self.CountIter + 1 >= self.MaxCount:
           self.CountIter = -1

       self.CountIter += 1  # 更新CountIter的值

       # 根据CountIter获取对应的symbol
       self.sym = self.SymbolList[self.CountIter]

       # 根据symbol从data中获取相应的数据
       df = self.data[self.sym]

       # 提取close、volume和returns数据
       self.close = df['close'].values
       self.volume = df['volume'].values
       self.returns = df['close'].pct_change().values

       # 初始化时间序列索引
       self.ts_index = self.window + 1

       # 调用on_data函数，获取c_window和v_window
       c_window, v_window = self.on_data()
       # 基于c_window和v_window，生成下一个观察值
       observations = self.next_observation(
           close_window=c_window, volume_window=v_window)

       # 设置observation_space的形状和类型
       self.observation_space = spaces.Box(-np.inf, np.inf,
                                           shape=(len(observations),), dtype=np.float32)

       # 初始化策略回报列表
       self.strat_returns = []

       return observations

   def std(self, x):
       """
       计算标准化值。

       该方法接收一个数组x，计算其标准化值，并返回数组最后一个元素的标准化值。
       标准化值是通过将原始值减去该变量的平均值，然后除以该变量的标准差来计算的。

       参数:
       - x: 输入的数组，其中包含了需要进行标准化处理的数值。

       返回值:
       - 返回数组x最后一个元素的标准化值。
       """
       y = (x - x.mean()) / x.std()  # 计算标准化值
       return y[-1]  # 返回最后一个标准化值

   def exponential_regression(self, data):
       """
       计算指数回归的拟合优度。

       参数:
       - data: 用于指数回归的数据集，预期为一个数组或列表。

       返回值:
       - 返回一个表示拟合优度的数值，该数值越大表示拟合效果越好。
       """
       # 对数据取对数
       log_c = np.log(data)
       # 创建一个索引数组，长度与数据集相同
       x = np.arange(len(log_c))
       # 计算对数数据与索引之间的线性回归斜率和相关系数
       slope, _, rvalue, _, _ = linregress(x, log_c)
       # 计算并返回指数回归的拟合优度
       return (1 + slope) * (rvalue ** 2)

   def regression(self, data):
       """
       计算回归分析的调整R平方值。

       参数:
       data: 一维数组，包含需要进行回归分析的数据点。

       返回值:
       返回调整后的R平方值，该值表示拟合优度。
       """
       # 生成等间距的x值，范围与data一致
       x = np.arange(len(data))
       # 计算线性回归的斜率和决定系数rvalue
       slope, _, rvalue, _, _ = linregress(x, data)
       # 计算并返回调整后的R平方值
       return (1 + slope) * (rvalue ** 2)

   def next_observation(self, close_window, volume_window):
       """
       生成下一个观察值，结合了价格和成交量的统计特征。

       :param close_window: 一个包含收盘价的窗口，用于计算线性回归和标准差。
       :param volume_window: 一个包含成交量的窗口，用于计算标准差。
       :return: 返回一个包含三个特征的numpy数组：收盘价的标准差、线性回归值、成交量的标准差。
       """
       # 计算收盘价窗口的线性回归
       lin_reg = self.regression(close_window)

       # 计算收盘价窗口的标准差
       col = self.std(close_window)

       # 计算成交量窗口的标准差
       vol = self.std(volume_window)

       # 将三个特征 concatenate 在一起形成观察值
       obs = np.concatenate(([col], [lin_reg], [vol]), axis=0)

       # 将观察值中的 NaN 值替换为 0
       where_are_NaNs = np.isnan(obs)
       obs[where_are_NaNs] = 0

       return obs

   def on_data(self):
       """
       处理数据的函数。

       该函数不接受任何参数。

       返回:
       close_window : list
           返回一个包含最近 `window` 个收盘价的列表。
       volume_window : list
           返回一个包含最近 `window` 个成交量的列表。
       """
       step = self.ts_index  # 获取当前时间步

       # 获取最近window个收盘价和成交量
       close_window = self.close[step-self.window:step]
       volume_window = self.volume[step-self.window:step]

       return close_window, volume_window

   def get_reward(self, trade=0):
       """
       计算并获取当前时间步的奖励。

       参数:
       - trade: 交易标志，默认为0，表示没有进行交易。如果为非零值，则表示进行了交易。

       返回值:
       - reward: 计算得到的奖励值。如果计算结果不是有限数，则返回0。
       """
       step = self.ts_index  # 当前时间步

       # 计算奖励，根据当前时间步的回报和是否进行交易来确定
       reward = self.returns[step] * trade

       self.strat_returns.append(reward)  # 将计算得到的奖励添加到策略回报列表中

       # 如果计算出的奖励是有限数，则返回该奖励；否则，返回0
       return reward if np.isfinite(reward) else 0

   def normalize(self, x):
       """
       对输入的值进行标准化处理。

       参数:
       - self: 方法的对象引用。
       - x: 需要进行标准化处理的值。

       返回值:
       - 标准化处理后的值，保留3位小数。
       """
       return np.round((1/0.95*x)-0.05264, 3)  # 计算标准化值，并四舍五入至小数点后三位

   def step(self, action):
       """
       执行一步交易操作。

       参数:
       action - 一个数组，表示执行的动作，其中第一个元素被使用来决定交易的大小。

       返回值:
       observations - 表示环境状态的观察值。
       reward - 该步操作得到的奖励。
       done - 表示这一步是否完成了整个交易过程。
       self.ts_index - 当前时间序列的索引。
       """

       done = False  # 初始化交易未完成标志

       action = float(action[0])  # 提取并转换动作的第一个元素为浮点数

       # 根据动作的大小决定交易的大小，小于-0.05做空，大于等于0.05做多，否则不做交易
       if action >= 0.05:
           size = np.clip(self.normalize(abs(action)), 0, 1)
       elif action <= -0.05:
           size = -(np.clip(self.normalize(abs(action)), 0, 1))
       else:
           size = 0  # 如果动作在指定阈值内，则不做交易

       # 检查是否到达交易序列的末尾
       if self.ts_index + 2 >= len(self.close):
           done = True  # 标记交易完成

           reward = self.get_reward(trade=size)  # 计算并获取此步交易的奖励

           c_window, v_window = self.on_data()  # 获取当前的收盘价和成交量窗口

           observations = self.next_observation(
               close_window=c_window, volume_window=v_window)  # 根据当前窗口生成下一个观察值

           return observations, reward, done, self.ts_index  # 返回观察值，奖励，完成标志和当前时间序列索引

       # 如果未到达交易序列末尾，则继续进行下一步交易
       reward = self.get_reward(trade=size)  # 计算并获取此步交易的奖励

       self.ts_index += 1  # 更新时间序列索引

       c_window, v_window = self.on_data()  # 获取更新后的收盘价和成交量窗口

       observations = self.next_observation(
           close_window=c_window, volume_window=v_window)  # 根据更新后的窗口生成下一个观察值

       return observations, reward, done, self.ts_index  # 返回更新后的观察值，奖励，完成标志和当前时间序列索引


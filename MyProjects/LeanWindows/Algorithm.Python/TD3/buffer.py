import numpy as np
import pickle


class ReplayBuffer(object):  # 存储数据
   def __init__(self, algo, max_size=1000000):
       """
       初始化一个对象。

       :param algo: 用于算法的实例或对象。
       :param max_size: 储存空间的最大大小，默认为1000000。
       :return: 无返回值。
       """
       self.algo = algo  # 存储传入的算法实例或对象

       self.storage = []  # 初始化一个空列表用于存储
       self.max_size = max_size  # 设置存储空间的最大大小
       self.ptr = 0  # 初始化指针指向存储列表的起始位置

   def add(self, data):
       """
       向存储结构中添加新数据。

       如果存储结构已达到最大大小，则在循环存储中覆盖最旧的数据；
       如果未达到最大大小，则直接添加数据到末尾。

       参数:
       - data: 需要添加到存储结构的数据。

       返回值:
       - 无
       """
       if len(self.storage) == self.max_size:
           # 当存储结构达到最大容量时，覆盖最旧的数据
           self.storage[int(self.ptr)] = data
           self.ptr = (self.ptr + 1) % self.max_size
       else:
           # 如果未达到最大容量，直接将数据添加到末尾
           self.storage.append(data)

   def save(self, name='ReplayBuff'):
       """
       保存回放缓冲区到对象存储。

       参数:
       - name: 保存的文件名，默认为 'ReplayBuff'。

       返回值:
       - 无
       """
       # 保存回放缓冲区到指定的对象存储
       self.algo.ObjectStore.Save(name, str(self.storage))
       # 记录保存回放缓冲区的时间和大小
       self.algo.Log(
           "{} - Saving Replay Buffer!: {}".format(self.algo.Time, len(self.storage)))

   def load(self, name='ReplayBuff'):
       """
       加载回放缓冲区（Replay Buffer）的数据。

       参数:
       - name: 回放缓冲区的名称，默认为'ReplayBuff'。

       返回值:
       - 无
       """
       # 从算法的对象存储中读取回放缓冲区的数据并保存到self.storage中
       self.storage = eval(self.algo.ObjectStore.ReadBytes("key"))
       # 记录加载回放缓冲区的时间和数据量
       self.algo.Log(
           "{} - Loading Replay Buffer!: {}".format(self.algo.Time, len(self.storage)))

   def sample(self, batch_size):
       """
       从存储器中抽取指定批次大小的样本。

       参数:
       batch_size -- 抽取样本的大小

       返回值:
       states -- 抽取样本的状态数组
       actions -- 抽取样本的动作数组
       next_states -- 抽取样本的下一个状态数组
       rewards -- 抽取样本的奖励数组
       dones -- 抽取样本的完成标志数组
       """
       # 生成随机索引，用于从存储器中选择样本
       ind = np.random.randint(0, len(self.storage), size=batch_size)
       states, actions, next_states, rewards, dones = [], [], [], [], []

       # 遍历随机索引，收集样本数据
       for i in ind:
           s, a, s_, r, d = self.storage[i]
           states.append(np.array(s, copy=False))
           actions.append(np.array(a, copy=False))
           next_states.append(np.array(s_, copy=False))
           rewards.append(np.array(r, copy=False))
           dones.append(np.array(d, copy=False))

       # 将收集到的样本数据转换为numpy数组，并调整形状以便使用
       return np.array(states), np.array(actions), np.array(next_states), np.array(rewards).reshape(-1, 1), np.array(dones).reshape(-1, 1)


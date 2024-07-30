import torch
import torch.nn as nn
from torch.autograd import Variable
import torch.nn.functional as F

def hidden_init(layer):
   """
   初始化隐藏层参数的函数。
   
   参数:
   - layer: 包含权重的层，用于计算初始值的范围。
   
   返回:
   - 一个元组，包含权重初始化的下界和上界。
   """
   # 计算输入神经元的数量（fan_in）
   fan_in = layer.weight.data.size()[0]
   # 根据输入神经元数量计算权重初始化的界限
   lim = 1. / np.sqrt(fan_in)
   return (-lim, lim)

class Actor(nn.Module):
   """
   Actor网络，用于生成动作。
   """
   def __init__(self, state_dim, action_dim, max_action, seed):
       """
       初始化Actor网络。
       
       参数:
       state_dim (int): 状态空间的维度。
       action_dim (int): 动作空间的维度。
       max_action (float): 动作的最大值，用于归一化动作。
       seed (int): 随机种子，用于初始化模型参数，保证实验可复现性。
       """
       # 使用父类的构造方法初始化
       super(Actor, self).__init__()
       # 设置随机种子
       self.seed = torch.manual_seed(seed)

       # 定义网络的三层线性结构
       self.l1 = nn.Linear(state_dim, 400)
       self.l2 = nn.Linear(400, 300)
       self.l3 = nn.Linear(300, action_dim)

       # 保存最大动作值，用于动作的归一化
       self.max_action = max_action


   def forward(self, x):
       """
       实现前向传播过程
       
       参数:
       - x输入数据，待处理的特征向量或张量
       
       返回值:
       - x处理后的数据，经过多层神经网络结构变换后的结果
       """
       # 通过第一层神经网络，并应用ReLU激活函数
       x = F.relu(self.l1(x))
       # 通过第二层神经网络，并应用ReLU激活函数
       x = F.relu(self.l2(x))
       # 通过第三层神经网络，应用tanh激活函数，并乘以最大动作值，用于输出规范化动作
       x = self.max_action * torch.tanh(self.l3(x)) 
       return x



class Critic(nn.Module):
   """
   Critic网络类，用于估计状态-动作值函数Q(s, a)。
   
   参数:
   - state_dim (int): 状态空间的维度。
   - action_dim (int): 动作空间的维度。
   - seed (int): 随机种子，用于初始化模型参数，以确保可复现性。
   """
   
   def __init__(self, state_dim, action_dim, seed):
       super(Critic, self).__init__()
       # 使用给定的种子设置随机数生成器
       self.seed = torch.manual_seed(seed)

       # 定义Q1网络的架构
       self.l1 = nn.Linear(state_dim + action_dim, 400)  # 第一层线性层
       self.l2 = nn.Linear(400, 300)  # 第二层线性层
       self.l3 = nn.Linear(300, 1)  # 输出层，估计Q1值

       # 定义Q2网络的架构，与Q1相似
       self.l4 = nn.Linear(state_dim + action_dim, 400)  # 第一层线性层
       self.l5 = nn.Linear(400, 300)  # 第二层线性层
       self.l6 = nn.Linear(300, 1)  # 输出层，估计Q2值



   def forward(self, x, u):
       """
       前向传播函数：将输入的特征向量x和控制向量u拼接后，通过一系列的卷积或全连接层处理，得到两路输出。
       
       参数:
       - x: 输入的特征向量，shape为(batch_size, feature_dim_x)
       - u: 控制向量，shape为(batch_size, control_dim)
       
       返回值:
       - x1, x2: 两路输出的特征向量，分别经过不同的层处理得到，shape同输入x但维度可能发生变化
       """
       # 将特征向量x和控制向量u在第1维（特征维度）上拼接
       xu = torch.cat([x, u], 1)

       # 第一路处理：通过两个ReLU激活函数和一个线性层
       x1 = F.relu(self.l1(xu))  # 使用ReLU激活函数处理拼接后的输入
       x1 = F.relu(self.l2(x1))  # 再次使用ReLU激活函数处理上一層的输出
       x1 = self.l3(x1)          # 经过一个线性层（全连接层或卷积层）处理

       # 第二路处理：与第一路类似，但使用不同的层
       x2 = F.relu(self.l4(xu))  # 第一个ReLU激活函数处理
       x2 = F.relu(self.l5(x2))  # 第二个ReLU激活函数处理
       x2 = self.l6(x2)          # 经过最后一个线性层处理
       return x1, x2


   def Q1(self, x, u):
       """
       函数Q1：将输入的x和u拼接后通过一系列神经网络层进行处理。
       
       参数:
       - x: 输入特征向量x，torch.Tensor类型。
       - u: 控制输入u，torch.Tensor类型，与x将在维度1上拼接。
       
       返回:
       - x1: 经过处理后的输出特征向量x1，torch.Tensor类型。
       """
       # 将输入x和u在第1维上拼接
       xu = torch.cat([x, u], 1)

       # 通过第一个ReLU激活函数和层l1处理输入
       x1 = F.relu(self.l1(xu))

       # 通过第二个ReLU激活函数和层l2继续处理
       x1 = F.relu(self.l2(x1))

       # 通过层l3得到最终输出
       x1 = self.l3(x1)
       return x1

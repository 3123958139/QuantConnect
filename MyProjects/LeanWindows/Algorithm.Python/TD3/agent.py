import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F
from network import Actor, Critic
import base64
from io import BytesIO
import json

# 定义设备
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")


class TD3(object):  # 定义DDPG算法
   def __init__(self, algo, state_dim, action_dim, max_action, seed):
       """
       初始化DQN代理.

       参数:
       algo: 使用的算法.
       state_dim: 状态空间的维度.
       action_dim: 动作空间的维度.
       max_action: 动作的最大值.
       seed: 随机种子.
       """
       self.algo = algo  # 使用的算法
       self.actor = Actor(state_dim, action_dim, max_action,
                          seed).to(device)  # 创建演员网络并移动到指定设备上
       self.actor_target = Actor(state_dim, action_dim, max_action, seed).to(
           device)  # 创建目标演员网络并移动到指定设备上
       self.actor_target.load_state_dict(
           self.actor.state_dict())  # 初始化目标演员网络的参数为演员网络的参数
       self.actor_optimizer = torch.optim.Adam(
           self.actor.parameters(), lr=1e-3)  # 创建演员网络的优化器

       self.critic = Critic(state_dim, action_dim, seed).to(
           device)  # 创建评论家网络并移动到指定设备上
       self.critic_target = Critic(state_dim, action_dim, seed).to(
           device)  # 创建目标评论家网络并移动到指定设备上
       self.critic_target.load_state_dict(
           self.critic.state_dict())  # 初始化目标评论家网络的参数为评论家网络的参数
       self.critic_optimizer = torch.optim.Adam(
           self.critic.parameters(), lr=1e-3)  # 创建评论家网络的优化器

       self.max_action = max_action  # 动作的最大值

       # np.random.seed(seed=seed)  # 设置随机种子（注释掉的代码，按照要求不进行解释）

   def select_action(self, state, noise=0.1):
       """
       从代理的策略中选择一个合适的动作

       参数:
           state (array): 当前环境的状态
           noise (float): 向动作中添加的噪声大小

       返回值:
           action (float): 被限制在动作范围内的动作
       """

       # 将状态转换为张量并移动到指定的设备上
       state = torch.FloatTensor(state.reshape(1, -1)).to(device)

       # 基于当前状态通过actor网络选择动作
       action = self.actor(state).cpu().data.numpy().flatten()
       # 如果设置了噪声，则向动作中添加噪声
       if noise != 0:
           action = (action + np.random.normal(0, noise, size=1))

       # 限制动作的范围
       return action.clip(-self.max_action, self.max_action)

   def train(self, replay_buffer, iterations, batch_size=100, discount=0.99, tau=0.005, policy_noise=0.2, noise_clip=0.5, policy_freq=2):
       """
       对智能体进行训练，使用DQN算法更新策略网络和价值网络。

       参数:
       - replay_buffer: 经验回放缓冲区，用于存储环境交互过程中的经验。
       - iterations: 训练迭代次数。
       - batch_size: 批量大小，每次从经验回放缓冲区中抽取的经验数量。
       - discount: 折扣因子，用于计算目标Q值。
       - tau: 目标网络参数更新的软更新系数。
       - policy_noise: 策略噪声，用于探索。
       - noise_clip: 噪声截断值，防止噪声过大。
       - policy_freq: 策略更新频率，每隔多少次价值网络更新进行一次策略网络更新。

       返回值:
       无
       """
       for it in range(iterations):
           # 从经验回放缓冲区中采样
           x, y, u, r, d = replay_buffer.sample(batch_size)
           state = torch.FloatTensor(x).to(device)
           action = torch.FloatTensor(u).to(device)
           next_state = torch.FloatTensor(y).to(device)
           done = torch.FloatTensor(1 - d).to(device)
           reward = torch.FloatTensor(r).to(device)

           # 生成策略噪声
           noise = torch.FloatTensor(u).data.normal_(
               0, policy_noise).to(device)
           noise = noise.clamp(-noise_clip, noise_clip)

           # 计算下一个动作和目标Q值
           next_action = (self.actor_target(next_state) +
                          noise).clamp(-self.max_action, self.max_action)
           target_Q1, target_Q2 = self.critic_target(next_state, next_action)
           target_Q = torch.min(target_Q1, target_Q2)
           target_Q = reward + (done * discount * target_Q).detach()

           # 计算当前Q值并更新价值网络
           current_Q1, current_Q2 = self.critic(state, action)
           critic_loss = F.mse_loss(
               current_Q1, target_Q) + F.mse_loss(current_Q2, target_Q)
           self.critic_optimizer.zero_grad()
           critic_loss.backward()
           self.critic_optimizer.step()

           # 按照策略更新频率更新策略网络
           if it % policy_freq == 0:
               actor_loss = -self.critic.Q1(state, self.actor(state)).mean()
               self.actor_optimizer.zero_grad()
               actor_loss.backward()
               self.actor_optimizer.step()

               # 进行软更新，更新目标网络参数
               for param, target_param in zip(self.critic.parameters(), self.critic_target.parameters()):
                   target_param.data.copy_(
                       tau * param.data + (1 - tau) * target_param.data)
               for param, target_param in zip(self.actor.parameters(), self.actor_target.parameters()):
                   target_param.data.copy_(
                       tau * param.data + (1 - tau) * target_param.data)

   def save(self, filename):
       """
       保存模型的状态字典至指定文件。

       参数:
       - filename: 字符串，保存文件的基础文件名。

       返回值:
       - 无
       """
       # 保存actor模型的状态字典
       torch.save(self.actor.state_dict(), '%s_actor.pth' % (filename))
       # 保存critic模型的状态字典
       torch.save(self.critic.state_dict(), '%s_critic.pth' % (filename))

   def load(self, filename):
       """
       加载Actor和Critic的模型状态。

       参数:
       - filename: 字符串，指定加载模型状态的文件名前缀。

       返回值:
       - 无
       """
       # 加载Actor模型的状态
       self.actor.load_state_dict(torch.load('%s_actor.pth' % (filename)))
       # 加载Critic模型的状态
       self.critic.load_state_dict(torch.load('%s_critic.pth' % (filename)))


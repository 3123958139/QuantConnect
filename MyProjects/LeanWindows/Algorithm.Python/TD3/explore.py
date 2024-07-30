from collections import deque
import pandas as pd
import numpy as np
import gym
import sys

from buffer import ReplayBuffer
from agent import TD3


class Runner:  # 运行器
   def __init__(self, algo, n_episodes=100, batch_size=32, gamma=0.99, tau=0.005, noise=0.2, noise_clip=0.5, explore_noise=0.1,
                policy_frequency=2, sizes=None):
       """
       初始化函数
       :param algo: 使用的算法
       :param n_episodes: 训练的回合数，默认为100
       :param batch_size: 批量大小，默认为32
       :param gamma: 折扣因子，默认为0.99
       :param tau: 目标网络的更新参数，默认为0.005
       :param noise: 探索噪声，默认为0.2
       :param noise_clip: 噪声夹断值，默认为0.5
       :param explore_noise: 探索时的噪声大小，默认为0.1
       :param policy_frequency: 政策更新频率，默认为2
       :param sizes: 状态维度、动作维度、最大动作值和随机种子的列表
       """

       self.algo = algo  # 算法
       self.n_episodes = n_episodes  # 训练回合数
       self.batch_size = batch_size  # 批量大小
       self.gamma = gamma  # 折扣因子
       self.tau = tau  # 目标网络更新参数
       self.noise = noise  # 探索噪声
       self.noise_clip = noise_clip  # 噪声夹断值
       self.explore_noise = explore_noise  # 探索时的噪声大小
       self.policy_frequency = policy_frequency  # 政策更新频率
       self.replay_buffer = ReplayBuffer(algo)  # 初始化回放缓冲区

       self.agent = TD3(
           self, state_dim=sizes[0], action_dim=sizes[1], max_action=sizes[2], seed=sizes[3])  # 初始化TD3代理

   def evaluate_policy(self, TestEnv, eval_episodes=1):
       """
       评估策略的性能。

       参数:
       - TestEnv: 测试环境，用于执行策略评估。
       - eval_episodes: 评估的回合数，默认为1。

       返回值:
       - avg_reward: 在评估回合中的平均奖励。
       """

       avg_reward = 0.
       for i in range(eval_episodes):
           obs = TestEnv.reset()  # 重置环境，开始新的回合
           done = False
           while not done:
               action = self.agent.select_action(
                   np.array(obs), noise=0)  # 选择一个动作，无噪声
               obs, reward, done, _ = TestEnv.step(
                   action)  # 执行动作，获取新的观测、奖励和完成状态
               avg_reward += reward  # 累加奖励
               if action <= -0.05:  # 特定条件下的日志记录
                   self.algo.Log("Action {}.".format(action))

       # 计算平均奖励
       avg_reward /= eval_episodes

       return avg_reward

   def observe(self, TrainEnv, observation_steps):
       """
       观察并填充回放缓冲区。

       通过在给定的环境中执行随机动作，并将观察结果存储到回放缓冲区中，来填充缓冲区。
       这个方法主要用于训练前让智能体探索环境，以便后续的强化学习。

       参数:
       TrainEnv: 训练环境，提供了一个接口来执行动作和获取环境状态。
       observation_steps: 观察步数，决定了函数执行和填充缓冲区的次数。

       返回值:
       无
       """

       time_steps = 0
       obs = TrainEnv.reset()  # 重置环境，并获取初始观察结果
       done = False

       while time_steps < observation_steps:
           action = TrainEnv.action_space.sample()  # 从动作空间中随机采样一个动作
           new_obs, reward, done, _ = TrainEnv.step(
               action)  # 执行动作，并获取新的观察结果、奖励等

           self.replay_buffer.add(
               (obs, new_obs, action, reward, done))  # 将观察结果等信息添加到回放缓冲区

           obs = new_obs  # 更新当前观察结果为新的观察结果
           time_steps += 1  # 增加时间步数

           if done:
               obs = TrainEnv.reset()  # 如果当前环境已完成，则重置环境
               done = False  # 重置完成标志

       # self.algo.Log("Populating Buffer {}/{}.".format(time_steps, observation_steps))
       # sys.stdout.flush()

   def train(self, TrainEnv, TestEnv):
       """
       对智能体进行训练。

       参数:
       - TrainEnv: 训练环境，用于智能体的训练过程。
       - TestEnv: 测试环境，用于评估智能体的性能。

       无返回值。
       """

       # 初始化分数列表及相关变量
       scores = []
       scores_avg = []
       scores_window = deque(maxlen=25)

       eval_reward_best = -1000  # 初始化最佳评估奖励

       self.algo.Debug("{} | Training..".format(self.algo.Time))  # 记录开始训练的日志

       # 初始评估
       eval_reward = self.evaluate_policy(TestEnv, int(TestEnv.MaxCount))

       # 如果当前评估分数更好，则保存为最佳模型
       if eval_reward > eval_reward_best:
           eval_reward_best = eval_reward
           self.algo.Debug("Last Model Tested |"+str(eval_reward_best))
           self.agent.save("best_avg")

       # 开始训练循环
       for i_episode in range(1, self.n_episodes+1):

           # 重置训练环境，并初始化分数、完成标志和步数
           obs = TrainEnv.reset()
           score = 0
           done = False
           episode_timesteps = 0

           # 在当前环境中执行动作，直到完成该回合
           while not done:

               # 选择动作，并执行于环境中
               action = self.agent.select_action(
                   np.array(obs), noise=self.explore_noise)
               new_obs, reward, done, _ = TrainEnv.step(action)
               self.replay_buffer.add((obs, new_obs, action, reward, done))
               obs = new_obs
               score += reward

               episode_timesteps += 1

               # 更新分数相关统计
               scores_window.append(score)
               scores.append(score)
               scores_avg.append(np.mean(scores_window))

               # 基于回放缓冲区进行训练
               self.agent.train(self.replay_buffer, episode_timesteps, self.batch_size,
                                self.gamma, self.tau, self.noise, self.noise_clip, self.policy_frequency)

           # 每隔一定回合数进行一次评估，并记录最佳模型
           if i_episode % 1 == 0:
               eval_reward = self.evaluate_policy(
                   TestEnv, int(TestEnv.MaxCount))

               # 如果当前评估分数为最佳，则保存模型
               if eval_reward > eval_reward_best:
                   eval_reward_best = eval_reward
                   self.algo.Debug(
                       str(i_episode)+"| Best Model! |"+str(round(eval_reward_best, 3)))
                   self.agent.save("best_avg")
                   # self.algo.Log("{} {} {} {} {}".format(episode_timesteps, i_episode, score, eval_reward))


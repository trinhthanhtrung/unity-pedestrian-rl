# Unity Pedestrian Reinforcement Learning

This is the implementation of our research in using Unity-ML for pedestrian simulation using reinforcement learning with human cognition. The implementation consists of two parts (more to be updated)

## 1. Pedestrian agent path planning and collision avoidance  

**Abstract**  
_Most microscopic pedestrian navigation models use the concept of “forces” applied to the pedestrian agents to replicate the navigation environment. While the approach could provide believable results in regular situations, it does not always resemble natural pedestrian navigation behaviour in many typical settings.  In our research, we proposed a novel approach using reinforcement learning for simulation of pedestrian agent path planning and collision avoidance problem. The primary focus of this approach is using human perception of the environment and danger awareness of interferences. The implementation of our model has shown that the path planned by the agent shares many similarities with a human pedestrian in several aspects such as following common walking conventions and human behaviours._

If you use this implementation for your research, please cite our paper  
_Trinh T-T, Kimura M. The Impact of Obstacle’s Risk in Pedestrian Agent’s Local Path-Planning. Applied Sciences. 2021; 11(12):5442. https://doi.org/10.3390/app11125442_

## 2. Pedestrian interaction simulation with reinforcement learning and human cognitive prediction

**Abstract**  
_Perfectly simulating the pedestrian behaviour is difficult because the human thinking process does not always select the optimised choice for the task. To replicate the behaviour, the simulation model needs to consider the cognitive process of the human brain. In this paper, we proposed a model to simulate the interactions between a pedestrian and another obstacle by adopting the concept of the human predictive system in neuroscience. The proposed model was correspondingly designed consisting of two tasks: an interaction learning task and an obstacle’s movement prediction task. We designed a reinforcement learning environment for the learning task, incorporating an interpolating method for the prediction task. Different from the accurate prediction models proposed in many studies, our approach reflects the way a human being observes the movement of the obstacle and perceives its risk. The empirical result demonstrates a highly realistic human behaviour of pedestrian interactions, which resembles actual situations in real life._

If you use this implementation for your research, please cite our paper  
_Trinh, T. & Kimura, M. (2022). Cognitive prediction of obstacle's movement for reinforcement learning pedestrian interacting model. Journal of Intelligent Systems, 31(1), 127-147. https://doi.org/10.1515/jisys-2022-0002_

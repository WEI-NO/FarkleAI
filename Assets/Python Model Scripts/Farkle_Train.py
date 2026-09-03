from FarkleSimulation import Farkle
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks
from sb3_contrib.common.maskable.callbacks import MaskableEvalCallback
import gymnasium as gym
import torch

# ep_rew_mean           : average total reward per completed game
# 
# ep_len_mean           : average total of rounds per game (steps() called)
# 
# explained_variance    : how well the model predicted rewards
# 
# approx_kl &           : how much the model is updating the policy
# clip_fraction 
#
# entropy_loss          : how uncertain/random the current policy is
#
# value_loss            : error from training

# Check if the environment sees your NVIDIA graphics card
print("GPU Available:", torch.cuda.is_available())

if torch.cuda.is_available():
    # Print the name of your graphics card
    print("Device Name:", torch.cuda.get_device_name(0))
    
    # Explicitly assign your tensor operations to the GPU
    device = torch.device("cuda")
    x = torch.rand(3, 3).to(device)
    print("Tensor is running on:", x.device)

def train(timesteps:int, model_name:str="", new_model:bool=False):

    model : MaskablePPO = None

    train_env = Farkle()
    eval_env = Farkle()
    
    if new_model:
        # Start training a new model
        model = MaskablePPO(
            "MlpPolicy",
            train_env,

            learning_rate=0.0001,

            # n_steps=2048,               # How many steps before learning update
            # batch_size=128,             # How many samples it trains on per learning update
            # n_epochs=10,                # How much epochs to run before training
            
            # gamma=0.99,                 # How much agent focuses on future rewards (0 - 1)
            # gae_lambda=0.95,            # How much each action is creditted for a Win or Reward
            # clip_range=0.2,             # Lower = less influence on each batch/samples | Higher = changes more aggressively
            # clip_range_vf=None,         
            # normalize_advantage=True,
            # ent_coef=0,                 # Adds randomness to decisions
            # vf_coef=0.5,                
            # max_grad_norm=0.5,          # Clamps the gradient update to this value
            # target_kl=None,

            verbose=1,
            tensorboard_log="./farkle_logs/",
            # seed=42
        )
    else:
        # Train existing the model
        model = MaskablePPO.load(
            model_name,
            env=train_env
        )

    # eval_callback = MaskableEvalCallback(
    #     eval_env,
    #     best_model_save_path="./farkle_logs/best_model/",
    #     log_path="./farkle_logs/results/",
    #     eval_freq=5000,
    #     deterministic=True,
    #     render=False,
    # )

    model.learn(
        total_timesteps=timesteps,
        # callback=eval_callback,
    )

    print("Total Wins: ", train_env._total_wins)
    print("Total Loses: ", train_env._total_loses)

    model.save(model_name)
        
train(1_000_000, "farkle_pro_test", new_model=True)
# train(env, 500_000, "farkle_pro", False)


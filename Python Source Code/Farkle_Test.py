from FarkleSimulation import Farkle
from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.utils import get_action_masks

env = Farkle()

model = MaskablePPO.load(
    "farkle_pro",
    env=env
)

obs, info = env.reset()
while True:

    mask = get_action_masks(env)

    action, _ = model.predict(
        obs,
        action_masks=mask,
        deterministic=True
    )
    selection = env.binary_to_selection(action)
    action_string = "Bank" if action < 64 else "Reroll"
    print(f"Action: {action} : {action_string} -> {env.selection_to_dice(selection)}")

    obs, reward, terminated, truncated, info = env.step(action)
    # print("Observation:", obs)
    print("Info: ", info)
    print("\n\n")
    if terminated:
        break 
    


# obs, info = env.reset()

# print("Initial Observation:", obs)

# terminated = False
# truncated = False

# while not terminated and not truncated:

#     mask = get_action_masks(env)

#     action, _ = model.predict(
#         obs,
#         action_masks=mask,
#         deterministic=True
#     )
#     print(action)

#     obs, reward, terminated, truncated, info = env.step(action)
#     print("\n")
#     # print("Action:", action, " Binary: ", bi  n(action))
#     print("Game Info:", info)
#     print("Reward:", reward)

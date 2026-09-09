import numpy as np
import torch
import torch.nn as nn
from sb3_contrib import MaskablePPO

MODEL_PATH = "farkle_pro_v3.zip"
ONNX_PATH = "farkle_policy_v4.onnx"

class UnityMaskablePPOWrapper(nn.Module):
    def __init__(self, policy):
        super().__init__()
        self.policy = policy

    def forward(self, observation: torch.Tensor, action_mask: torch.Tensor):
        # Applies the policy's normal observation preprocessing and feature extration
        features = self.policy.extract_features(observation)

        # Only run the actor side of PPO
        latent_policy = self.policy.mlp_extractor.forward_actor(features)

        # Produce one logit for each of the 128 actions
        logits = self.policy.action_net(latent_policy)

        # Unity supplies masks as 0.0 or 1.0
        valid_actions = action_mask > 0.5

        # Invalid actions can not be selected
        masked_logits = torch.where(valid_actions, logits, torch.full_like(logits, -1e9))

        return masked_logits
        

# 1. Load trained sb3_contrib model
model = MaskablePPO.load(MODEL_PATH, device="cpu")
policy = model.policy
policy.eval()

# 2. Instantiate Unity-compatible wrapper
wrapper = UnityMaskablePPOWrapper(policy)
wrapper.eval()

# 3. Define Dimensions from spaces
observation_shape = model.observation_space.shape
action_count = model.action_space.n

print("Observation shape:", observation_shape)
print("Action count:", action_count)

dummy_observation = torch.zeros(1, *observation_shape, dtype=torch.float32)

dummy_action_mask = torch.ones(1, action_count, dtype=torch.float32)

# torch.onnx.export(
#     wrapper,
#     (dummy_observation, dummy_action_mask),
#     ONNX_PATH,
#     input_names=[
#         "observation",
#         "action_mask"
#     ],
#     output_names=[
#         "masked_logits"
#     ],
#     opset_version=15,
#     do_constant_folding=True
# )

torch.onnx.export(
    wrapper,
    (dummy_observation, dummy_action_mask),
    ONNX_PATH,
    input_names=[
        "observation",
        "action_mask"
    ],
    output_names=[
        "masked_logits"
    ],
    opset_version=15,
    do_constant_folding=True,

    # Critical for Unity Inference Engine 2.4.1
    external_data=False,

    # Keeps the legacy exporter and reliably honors opset 15
    dynamo=False
)

print(f"Exported Model to {ONNX_PATH}")
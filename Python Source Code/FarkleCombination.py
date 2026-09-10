import math
import matplotlib.pyplot as plt

dice_picked = [1, 2, 3, 4, 5, 6]

combinations = [math.comb(6, k) for k in dice_picked]
total_combinations = sum(combinations)

plt.figure(figsize=(8, 5))
bars = plt.bar(dice_picked, combinations, color='royalblue', edgecolor='black', alpha=0.8)

for bar in bars:
    yval = bar.get_height()
    plt.text(bar.get_x() + bar.get_width()/2, yval + 0.5, str(yval), ha='center', va='bottom', fontsize=10, weight='bold')

plt.ylim(0, 25)
plt.title(f'Possible Combinations (Total = {total_combinations})', fontsize=12, weight='bold')
plt.xlabel('Number of Dice Picked', fontsize=10)
plt.ylabel('Number of Selections', fontsize=10)
plt.xticks(dice_picked)
plt.grid(axis='y', linestyle='--', alpha=0.5)

plt.show()
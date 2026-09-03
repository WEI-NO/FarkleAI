import gymnasium as gym
import numpy as np
# import random as rand
from itertools import combinations, combinations_with_replacement


class Farkle(gym.Env):

    def __init__(self):
        self._score_to_win = 10000
        self._max_dice = 6
        self._opponent_consistent_score = 446
        # Game State
        self._banked_score = 0
        self._unbanked_score = 0
        self._dice_to_roll = self._max_dice
        self._current_dice:list = []

        self._opponent_score = 0
        self._opponent_current_dice = []
        self._opponent_unbanked_score = 0
        self._opponent_dice_to_roll = self._max_dice

        self._use_smart_opponent = True

        self._total_wins = 0
        self._total_loses = 0
        self.observation_space = gym.spaces.Box(low=-np.inf, high=np.inf, shape=(10,), dtype=np.float32)
        self.action_space = gym.spaces.Discrete(128)

    # If the value should affect the outcome of the action
    def _get_obs(self):

        dices = []
        for i in range(self._max_dice):
            if i < len(self._current_dice):
                dices.append(self._current_dice[i])
            else:
                dices.append(0)
        '''
        0. banked_score
        1. unbanked_score
        2. dice_to_roll
        3. opponent_score
        4. dice1, 5. dice2, 6. dice3
        7. dice4, 8. dice5, 9. dice6
        '''
        # Normalize
        return np.array([
            self._banked_score / self._score_to_win,  
            self._unbanked_score / self._score_to_win, 
            self._dice_to_roll / 6.0, 
            self._opponent_score / self._score_to_win,
            dices[0] / 6, 
            dices[1] / 6, 
            dices[2] / 6,
            dices[3] / 6, 
            dices[4] / 6, 
            dices[5] / 6
        ], dtype=np.float32)

    # Used for debugging, evaluation, statistics, and logging
    def _get_info(self):
        return {
            "banked_score": self._banked_score,
            "unbanked_score": self._unbanked_score,
            "opponent_score": self._opponent_score,

            "dice_to_roll": self._dice_to_roll,
            "current_dice": self._current_dice.copy(),

            "total_wins": self._total_wins,
            "total_loses": self._total_loses,
        }

    def reset(self, seed:int=None, options:dict = None):
        super().reset()

        # Reset Score
        self._banked_score = 0
        self._unbanked_score = 0
        # Reset dice count to 6
        self._dice_to_roll = self._max_dice

        # Opponents
        self._opponent_score = 0
        self._opponent_current_dice = []
        self._opponent_unbanked_score = 0
        self._opponent_dice_to_roll = self._max_dice


        # Roll 6 dice
        # Store roll in current_dice
        # self._current_dice = self.roll_dice(self._dice_to_roll)
        self._ensure_valid_roll_dice(self._dice_to_roll)
        # return new game state

        observation = self._get_obs()
        info = self._get_info()

        return observation, info

    def step(self, action):
        # action = 0 - 127
        # 0-63: index of selected subset - BANK - TRUE
        # 64-127: index of selected subset - REROLL - FALSE

        mask = self._get_action_mask(self._current_dice)

        # INVALID ACTION
        if not mask[action]:
            print(f"Chosen an invalid action: action {action} dice {self._current_dice}")
            raise RuntimeError(f"Selected invalid action {action}, dice={self._current_dice}")

        # Local States
        farkled = False
        banked_score_this_turn = 0
        lost_unbanked = 0

        
        # Translate the action into binary/int
        binary, bank_or_roll = self.translate_action(action)

        # print(f"Banked or Roll: {bank_or_roll}")

        # Retrieve the index of which dice it chose
        chosen_index = self.binary_to_selection(binary)
        # Chosen Dices
        chosen_dice = self.selection_to_dice(chosen_index, self._current_dice)
        valid, score = self.calculate_score(chosen_dice)

        # print(f"Chosen Die: {chosen_dice}")
        # print(f"Unbanked Score {self._unbanked_score} + score {score}")
        
        self._unbanked_score += score

        if bank_or_roll:
            # Bank
            banked_score_this_turn = self._unbanked_score
            self.end_player_turn(bank_current=True)
        else:
            # Reroll
            self._dice_to_roll -= len(chosen_dice)
            if self._dice_to_roll == 0:
                self._dice_to_roll = self._max_dice
            self.roll_dice(self._dice_to_roll)
            mask = self._get_action_mask(self._current_dice)
            # Check if farkled
            if not (True in mask):
                lost_unbanked = self._unbanked_score
                self.end_player_turn(bank_current=False) # Farkled
                farkled = True

        terminated = self._banked_score >= self._score_to_win or self._opponent_score >= self._score_to_win

        reward = self._get_reward(farkled, banked_score_this_turn, lost_unbanked)

        truncated = False

        observation = self._get_obs()
        info = self._get_info()

        return observation, reward, terminated, truncated, info

    def _get_reward(self, farkled:bool, banked_this_turn, lost_unbanked):
        reward = 0

        if farkled:
            reward -= (lost_unbanked*2) / self._score_to_win # Farkled

        if self._opponent_score >= self._score_to_win:
            reward -= 1
            self._total_loses += 1

        if self._banked_score >= self._score_to_win:
            reward += 1 # The bot wins
            self._total_wins += 1

        reward += (banked_this_turn*2) / self._score_to_win # Amount banked this turn

        return reward

    def opponent_should_bank(self, unbanked_score, dice_remaining):
        if unbanked_score >= 500:
            return True

        if dice_remaining <= 2 and unbanked_score >= 300:
            return True

        return False

    def end_player_turn(self, bank_current=False):
        # Reset unbanked_score
        # print(f"Ended Player Turn: Bank current {bank_current} banked: {self._unbanked_score}")
        if bank_current: # Optional bank_current
            self._banked_score += self._unbanked_score
            self._unbanked_score = 0
            if self._banked_score >= self._score_to_win:
                return

        self._unbanked_score = 0
        self._dice_to_roll = self._max_dice
        # Reset dice to roll
        self._ensure_valid_roll_dice(self._dice_to_roll)


        self._play_opponent_turn()
        # print(f"Opponent Scored")

    def _play_opponent_turn(self):
        if self._use_smart_opponent:
            while True:
                self.roll_opponent_dice(self._opponent_dice_to_roll) # Opponent Rolls Dice
                opponent_action_mask = self._get_action_mask(self._opponent_current_dice)
                opponent_action_mask = opponent_action_mask[:63] # Slice action mask to include only information about possible selections
                possible_choices = [x for x in range(len(opponent_action_mask)) if opponent_action_mask[x] == True] # Get the int of every True

                if len(possible_choices) == 0:
                    # Opponent Farkled
                    self._opponent_unbanked_score = 0
                    self._opponent_dice_to_roll = self._max_dice
                    break

                # Simple greedy takes highest combination
                largest_selection = possible_choices[len(possible_choices)-1]

                # Convert binary to dice selection
                opponent_index_selection = self.binary_to_selection(largest_selection)
                opponent_dice = self.selection_to_dice(opponent_index_selection, self._opponent_current_dice)
                valid, score = self.calculate_score(opponent_dice)

                self._opponent_unbanked_score += score
                self._opponent_dice_to_roll -= len(opponent_dice)

                if self.opponent_should_bank(self._opponent_unbanked_score, self._opponent_dice_to_roll):
                    # Opponent Banks
                    self._opponent_score += self._opponent_unbanked_score
                    self._opponent_unbanked_score = 0
                    self._opponent_dice_to_roll = self._max_dice
                    break

                # Opponent Rerolls

        else:
            self._opponent_score += self._opponent_consistent_score 

    def action_masks(self):
        return self._get_action_mask(self._current_dice)

    '''
        Possible Combinations: 6^6 = 46_656
        Possible Farkles: 1_080 (2.31%)
    '''        
    def roll_dice(self, num_rolls_):
        # roll_result = [rand.randint(1, 6) for x in range(num_rolls_)]

        # Sorted to reduce extra checks during training
        self._current_dice = sorted(self.np_random.integers(1, 7, size=num_rolls_).tolist())

    def roll_opponent_dice(self, num_rolls_):
        self._opponent_current_dice = sorted(self.np_random.integers(1, 7, size=num_rolls_).tolist())

    # Ensures no invalid rolls (For single player)
    def _ensure_valid_roll_dice(self, _num_roll):
        # Roll New Dice
        self.roll_dice(_num_roll)

        # Make sure it is a valid set
        mask = self._get_action_mask(self._current_dice)
        while not True in mask:
            self.roll_dice(_num_roll)
            mask = self._get_action_mask(self._current_dice)

    def translate_action(self, action):
        # Returns DICE_SET, BANK/ROLL
        subset_index = action
        bank_or_roll = True # True = Bank, False = Reroll
        if action >= 64:
            # Reroll
            action -= 64
            bank_or_roll = False

        return action, bank_or_roll

    def count_single_scores(self, counter):
        if 2 in counter.keys() or 3 in counter.keys() or 4 in counter.keys() or 6 in counter.keys():
            return False, 0
        score = 0
        if 1 in counter.keys():
            score += counter[1] * 100
        if 5 in counter.keys():
            score += counter[5] * 50
        return True, score

    def calculate_score(self, combination : list[int]):
        # 1. Evaluate if combination is valid
        # 2. All dice in the combination should be able to contribute to score
        # 3. Return the score
        dice_count = len(combination)

        if dice_count == 0:
            return False, 0
        
        counter = {}
        for dice in combination:
            if dice in counter:
                counter[dice] += 1
            else:
                counter[dice] = 1

        # Combinations of 6
        if dice_count == 6:
            counts = sorted(counter.values())
            # Straight
            if len(counter) == 6:
                return True, 1500
            # Three pairs
            if counts == [2, 2, 2]:
                return True, 1500
            # Two triples
            if counts == [3, 3]:
                return True, 2500
            # Four of a kind + pair
            if counts == [2, 4]:
                return True, 1500

        # Other combinations
        score = 0
        scoring_dice = 0
        for dice, count in counter.items():
            # Six of a kind / e.g. 5 5 5 5 5 5 { 5:6 }
            if count == 6:
                score += 3000
                scoring_dice += 6
            # Five of a kind / e.g. 5 5 5 5 5 2 { 5:5, 2:1 }
            elif count == 5:
                score += 2000
                scoring_dice += 5
            # Four of a kind / e.g. 5 5 5 5 2 3 { 5:4, 2:1, 3:1 }
            elif count == 4:
                score += 1000
                scoring_dice += 4
            # Three of a kind / e.g. 5 5 5 2 3 4 { 5:3, 2:1, 3:1, 4:1 }
            elif count == 3:
                if dice == 1:
                    score += 300
                else:
                    score += dice * 100
                scoring_dice += 3
            # Ones and Fives
            else:
                if dice == 1: # Ones
                    score += count * 100
                    scoring_dice += count
                elif dice == 5: # Fives
                    score += count * 50
                    scoring_dice += count
        if scoring_dice != dice_count:
            return False, 0

        return True, score

    def get_all_binary_selections(self, dices):
        ''' --- OLD IMPLEMENTATION ---
            binary_lists = [[int(bit) for bit in f"{i:06b}"] for i in range(64)]
            for i in binary_lists:
                # print(f"Value: {i}, Binary: {bin(i)}")
                entry = 0
                for j in i:
                    entry = (entry << 1) | j
                binary_list.append(entry)
        '''
        binary_list = [x for x in range(64)]
        return binary_list
    
    def binary_to_selection(self, binary):
        indexes = []
        # 10100
        for i in range(self._max_dice):
            if (binary & (1 << i)):
                indexes.append(i)
        # print(f"Integer: {binary}, Binary View: {bin(binary)}")
        # print(f"Chosen Index: {indexes}")
        return indexes

    def selection_to_dice(self, selection: list[int], current_dice : list[int]):
        dice_number_list = []
        for i in selection:
            if i < len(current_dice):
                dice_number_list.append(current_dice[i])
            else:
                break
        return dice_number_list

    def _get_action_mask(self, current_dice: list[int]):
            ''' --- OLD IMPLEMENTATION ---
                LENGTH = 128
                MIDPOINT = 64
                mask[dice_amt_mask:MIDPOINT] = False
                mask[dice_amt_mask + MIDPOINT:LENGTH] = False
            '''
            # Create an all False mask
            mask = np.zeros(128, dtype=bool)
            
            # Mask based on the number of dice
            dice_amt_mask = 1 << self._dice_to_roll # e.g. _dice_to_roll = 5 eliminates 1 << 5 which is anything above 32

            # Remove duplicate
            seen = set()

            # Eliminate whether the subset is valid (Score or No Score)
            for i in range(1, dice_amt_mask):

                # Binary number to dice index selection
                selection = self.binary_to_selection(i)

                # Get dice selected by index selection
                dice_number = self.selection_to_dice(selection, current_dice)

                # Calculate valid score and score
                valid, score = self.calculate_score(dice_number)

                # Current seen
                current_seen = tuple(sorted(dice_number))

                # Eliminate invalid and seen selection
                if valid and current_seen not in seen:
                    seen.add(current_seen)
                    mask[i] = True
                    mask[i + 64] = True

            return mask

# gym.register(
#     id="gymnasium_env/FarkleBot",
#     entry_point=Farkle,
#     max_episode_steps=100
# )



# env = gym.make("gymnasium_env/FarkleBot")
# observation, info = env.reset()

''' --- OLD IMPLEMENTATION ---
    print(observation) 
    print(info["action_mask"])

    key_count = len(counter.keys())
    # Check special conditions
    if dice_count == 6:
        if key_count == 6:
            return True, 1500 # Straight
        if key_count == 3:
            # Three Pairs
            pairs = [x for x in counter.values() if x == 2]
            if pairs == 3:
                return True, 1500
        if key_count == 2:
            # Double Triple
            triples = [x for x in counter.values() if x == 3]
            if len(triples) == 2:
                return True, 2500

            # Four of a kind + Pair
            if 4 in counter.values() and 2 in counter.values():
                return True, 1500
            
        return self.count_single_scores(counter)
    elif dice_count == 5:
        if key_count == 1:
            return True, 2000 # Five of a Kind
        return self.count_single_scores(counter)
    elif dice_count == 4:
        if key_count == 1:
            return True, 1000 # Four of a Kind
        return self.count_single_scores(counter)
    elif dice_count == 3:
        if key_count == 1:
            # Only one type of dice, meaning Three of a Kind
            dice_face = next(iter(counter.keys()))
            match dice_face:
                case 1:
                    return True, 300
                case _:
                    return True, 100 * dice_face
        return self.count_single_scores(counter)
    elif dice_count == 2:
        return self.count_single_scores(counter)
    elif dice_count == 1:
        if 1 in counter.keys():
            return True, 100
        if 5 in counter.keys():
            return True, 50

    return False, 0
'''
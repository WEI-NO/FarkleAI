from sb3_contrib import MaskablePPO
from FarkleSimulation import Farkle
from sb3_contrib.common.maskable.utils import get_action_masks


import random as rand
import numpy as np

from tkinter import *
import tkinter as ttk

import time

env = Farkle()
bot = MaskablePPO.load(
    "farkle_pro_test",
    env=env
)

def print_delay(message:str, delay:float=0):
    if delay != 0: time.sleep(delay)
    print(message)


def roll_dice(num_rolls_):
    # roll_result = [rand.randint(1, 6) for x in range(num_rolls_)]

    # Sorted to reduce extra checks during training
    return sorted([rand.randint(1, 6) for x in range(num_rolls_)])

def selection_to_dice(selection: list[int], dices: list[int]):
    dice_number_list = []
    for i in selection:
        dice_number_list.append(dices[i])
    return dice_number_list

def _get_action_mask(env, current_dice_, num_rolls_):
        ''' --- OLD IMPLEMENTATION ---
            LENGTH = 128
            MIDPOINT = 64
            mask[dice_amt_mask:MIDPOINT] = False
            mask[dice_amt_mask + MIDPOINT:LENGTH] = False
        '''
        # Create an all False mask
        mask = np.zeros(128, dtype=bool)
        
        # Mask based on the number of dice
        dice_amt_mask = 1 << num_rolls_ # e.g. _dice_to_roll = 5 eliminates 1 << 5 which is anything above 32

        # Remove duplicate
        seen = set()

        bank_scores = {}

        # Eliminate whether the subset is valid (Score or No Score)
        for i in range(1, dice_amt_mask):

            # Binary number to dice index selection
            selection = env.binary_to_selection(i)

            # Get dice selected by index selection
            dice_number = selection_to_dice(selection, current_dice_)

            # Calculate valid score and score
            valid, score = env.calculate_score(dice_number)

            # Current seen
            current_seen = tuple(sorted(dice_number))

            # Eliminate invalid and seen selection
            if valid and current_seen not in seen:
                seen.add(current_seen)
                # mask[i] = True
                mask[i + 64] = True
                bank_scores[i] = score

        if bank_scores:
            best_score = max(bank_scores.values())

            for i, score in bank_scores.items():
                mask[i] = score == best_score
                

        return mask

def _get_dice(dice:list[int], index:int, normalize_ = 1):
    dice_length = len(dice)
    if index < dice_length:
        return dice[index] / normalize_
    else:
        return 0

class FarkleGame():

    def __init__(self):
        self.MAX_DICE = 6
        self.SCORE_TO_WIN = 10000
        
        self.bot_unbanked_score = 0
        self.bot_score = 0
        self.bot_dice_to_roll = self.MAX_DICE
        self.bot_dice = []

        self.player_score = 0
        self.player_unbanked_score = 0
        self.player_dice_to_roll = self.MAX_DICE
        self.player_dice = []

    def ResetGame(self):
        self.MAX_DICE = 6
        self.bot_unbanked_score = 0
        self.bot_score = 0
        self.bot_dice_to_roll = self.MAX_DICE
        self.bot_dice = []

        self.player_score = 0
        self.player_unbanked_score = 0
        self.player_dice_to_roll = self.MAX_DICE
        self.player_dice = []

    def RunGame(self):
        game_ended = False
        player_turn = True
        round = 0
        end_turn = True
        while not game_ended:

            if self.bot_score >= self.SCORE_TO_WIN:
                print_delay(f"Bot won! With {self.bot_score} scores, Player has {self.player_score} score", 2)
                break
            elif self.player_score >+ self.SCORE_TO_WIN:
                print_delay(F"You Won! With {self.player_score} score, Bot has {self.bot_score} score", 2)
                break
            if end_turn:
                round += 1
                print_delay("=============================================", 2)
                print_delay(f"Round {round} - {"Player" if player_turn else "Bot"}'s Turn")
                print_delay(f"Current Score:\nPlayer - {self.player_score}\nBot - {self.bot_score}")
                print_delay("=============================================")
                end_turn = False

            if player_turn:
                # Player Turn
                self.player_dice = roll_dice(self.player_dice_to_roll)
                print_delay(f"Rolled Dice: {self.player_dice}",2)
                print_delay(f"              -  -  -  -  -  -")
                print_delay(f"             {[1,2,3,4,5,6]}\n")

                # Check Farkled
                action_mask = _get_action_mask(env, self.player_dice, self.player_dice_to_roll)

                if not True in action_mask:
                    print_delay(f"Player FARKLED!", 1.5)
                    self.player_unbanked_score = 0
                    self.player_dice_to_roll = self.MAX_DICE
                    player_turn = not player_turn
                    end_turn = True
                else:
                    # Make sure it is a valid scoring set of dice
                    while True:
                        # Choosing Dice
                        while True:
                            player_selection = input("Select Dice to Score -> ")
                            selection = player_selection.split(' ')
                            selection_index = []
                            for i in selection:
                                if i.isdigit():
                                    selection_index.append(int(i) - 1)
                                else:
                                    print_delay(f"{i} cannot be converted to int")
                            if len(selection_index) > 0:
                                break
                            else:
                                print_delay(f"Must select at least one dice")

                        # Choose Bank or Reroll
                        bank_or_reroll:bool
                        while True:
                            decision = input("Would you like to reroll or bank ('r' 'reroll' 'b' or 'bank') -> ")
                            if decision == 'r' or decision == 'reroll':
                                # Reroll
                                bank_or_reroll = False
                                break
                            elif decision == 'b' or decision == 'bank':
                                # Bank
                                bank_or_reroll = True
                                break
                            else:
                                print_delay(f"Invalid Input {decision}, please enter 'r' 'reroll' 'b' or 'bank' ")

                        dice_selected = selection_to_dice(selection_index, self.player_dice)

                        valid, score = env.calculate_score(dice_selected)
                        if valid:
                            self.player_unbanked_score += score
                            print_delay(f"\n| Player Unbanked Score | -> {self.player_unbanked_score}")
                            break
                        else:
                            print_delay(f"Selected invalid scoring set of dice {dice_selected}")

                    # Reroll or Bank
                    if bank_or_reroll:
                        # Bank
                        self.player_score += self.player_unbanked_score
                        end_turn = True
                    else:
                        # Reroll
                        self.player_dice_to_roll -= len(dice_selected)
                        if self.player_dice_to_roll <= 0:
                            self.player_dice_to_roll = self.MAX_DICE
                        end_turn = False

                    if end_turn:
                        player_turn = not player_turn

            else:
                self.bot_dice = roll_dice(self.bot_dice_to_roll)
                print_delay(f"Rolled Dice: {self.bot_dice}", 2)
                print_delay(f"              -  -  -  -  -  -")
                print_delay(f"Index        {[1,2,3,4,5,6]}")

                observation = np.array([
                    self.bot_score / self.SCORE_TO_WIN,
                    self.bot_unbanked_score / self.SCORE_TO_WIN,
                    self.bot_dice_to_roll / self.MAX_DICE,
                    self.player_score / self.SCORE_TO_WIN,
                    _get_dice(self.bot_dice, 0, self.MAX_DICE),
                    _get_dice(self.bot_dice, 1, self.MAX_DICE),
                    _get_dice(self.bot_dice, 2, self.MAX_DICE),
                    _get_dice(self.bot_dice, 3, self.MAX_DICE),
                    _get_dice(self.bot_dice, 4, self.MAX_DICE),
                    _get_dice(self.bot_dice, 5, self.MAX_DICE),
                ])

                mask = _get_action_mask(env, self.bot_dice, self.bot_dice_to_roll)

                if not True in mask:
                    print_delay(f"Bot FARKLED!", 1.5)
                    self.bot_unbanked_score = 0
                    self.bot_dice_to_roll = self.MAX_DICE
                    end_turn = True
                    continue

                action, _ = bot.predict(
                    observation,
                    action_masks=mask,
                    deterministic=True
                )

                action, bank_or_reroll = env.translate_action(action=action)

                selection = env.binary_to_selection(action)
                dice_selected = selection_to_dice(selection, self.bot_dice)
                print_delay(f"Bot selected - {dice_selected}", 2)
                valid, score = env.calculate_score(dice_selected)
                self.bot_unbanked_score += score

                if bank_or_reroll:
                    # Bank
                    print_delay(f"Bot chose to bank {self.bot_unbanked_score} score", 2)
                    self.bot_score += self.bot_unbanked_score
                    self.bot_unbanked_score = 0
                    self.bot_dice_to_roll = self.MAX_DICE
                    player_turn = not player_turn
                    end_turn = True
                else:
                    # Reroll
                    self.bot_dice_to_roll -= len(dice_selected)
                    print_delay(f"Bot chose to reroll with {self.bot_dice_to_roll} dice", 2)
                    if self.bot_dice_to_roll <= 0:
                        self.bot_dice_to_roll = self.MAX_DICE
                    end_turn = False
                    

game = FarkleGame()

game.ResetGame()

game.RunGame()
            
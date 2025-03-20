import json
from config import CHANNELS
from bot_instance import bot
from aiogram.enums import ChatMemberStatus
from aiogram.types import ReplyKeyboardRemove
from models import User
from services import user_service as users
from keyboards import subscribe_nigga
import re

MESSAGES_CACHE = None

def load_messages():
    global MESSAGES_CACHE
    if MESSAGES_CACHE is None:
        with open("C:\\Projects\\ndevsuz\\test-bot\\pybot\\src\\messages.json", "r", encoding="utf-8") as file:
            MESSAGES_CACHE = json.load(file)
    return MESSAGES_CACHE
    
async def check_member(user_id) -> bool:
    for channel in CHANNELS:
        member = await bot.get_chat_member(channel, user_id)
        if member.status not in [ChatMemberStatus.MEMBER, ChatMemberStatus.ADMINISTRATOR, ChatMemberStatus.CREATOR]:
            return False
        
    return True

async def check(message):
    user_id = message.from_user.id
    full_name = message.from_user.full_name
    username = message.from_user.username

    # Check if user exists
    messages = load_messages()
    user = await users.get_by_id(user_id)
    if not user:
        user = User(id=user_id, full_name=full_name, username=username)
        await users.create(user)
        await message.answer(messages["welcome"])

    # Check if user is subscribed
    is_subscribed = await check_member(user_id)
    if not is_subscribed:
        await message.answer(messages["subscribe_nigga"], reply_markup=subscribe_nigga)  # 🔥 Send warning
        return False  # 🚫 Stop execution
    
    return True  # ✅ Continue execution

def extract_answers(answers: str) -> dict:
    answer_dict = {}

    # Check if there are numbers in the answer
    is_keyed = any(char.isdigit() for char in answers)

    if is_keyed:
        key = 0
        for part in re.split(r"(?<=\D)(?=\d)|(?<=\d)(?=\D)", answers):
            if part.isdigit():
                key = int(part)
            else:
                answer_dict[key] = part
    else:
        for i, char in enumerate(answers):
            answer_dict[i + 1] = char

    return answer_dict



def calculate_correct_answer_percentage(user_answers: dict, correct_answers: dict):
    """Compare user answers with correct answers and return percentage, correct count, incorrect count, and incorrect question numbers."""
    incorrect_answers = []  # List to store only the question numbers

    correct_count = 0
    for key in user_answers:
        key_str = str(key)  # Ensure key is a string (fix key mismatch)
        if key_str in correct_answers:
            if user_answers[key] == correct_answers[key_str]:
                correct_count += 1
            else:
                incorrect_answers.append(int(key_str))  # Store only the question number

    incorrect_count = len(correct_answers) - correct_count
    percentage = (correct_count / len(correct_answers)) * 100 if correct_answers else 0

    return percentage, correct_count, incorrect_count, incorrect_answers

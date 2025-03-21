from aiogram.types import (
    KeyboardButton, ReplyKeyboardMarkup,
    InlineKeyboardButton, InlineKeyboardMarkup, ReplyKeyboardRemove)

subscribe_nigga = InlineKeyboardMarkup(
    inline_keyboard=[
        [InlineKeyboardButton(text="Obuna bo'lish", url="https://t.me/kelajakkabirqadam_1"), InlineKeyboardButton(text="Tekshrish",callback_data="check_subscription")]
    ]
)

go_back_inline = InlineKeyboardMarkup(
    inline_keyboard=[
        [InlineKeyboardButton(text="⬅️", callback_data="go_back_inline")]
    ]
)



main_menu = ReplyKeyboardMarkup(
    resize_keyboard=True,
    keyboard=[
        [KeyboardButton(text="➕Test yaratish"), KeyboardButton(text="✅Javobni tekshirish")],
        [KeyboardButton(text="ℹ️Qo'llanma"), KeyboardButton(text="📝Testlarni ko'rish")],
    ]
)

cancel_keyboard = ReplyKeyboardMarkup(
    resize_keyboard=True,
    keyboard=[
        [KeyboardButton(text="Bekor qilish❌")]
    ]
)